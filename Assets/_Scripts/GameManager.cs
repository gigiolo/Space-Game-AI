using UnityEngine;
using BreakInfinity; 
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static GameManager Instance { get; private set; }

    [Header("--- COLLEGAMENTI ---")]
    public ResearchManager targetResearchManager; 
    public UITheme activeTheme; 

    [Header("--- BILANCIAMENTO ---")]
    public BigDouble emitterBaseCost = 10;
    public double emitterGrowth = 1.15d;
    public BigDouble logisticsBaseCost = 15;
    public double logisticsGrowth = 1.50d;
    public BigDouble holdButtonMultiplier = 2.0; 
    public double offlineProductionRatio = 0.5d;

    [Header("--- SALVATAGGIO ---")]
    public float autoSaveInterval = 30f; 

    // --- VARIABILI DI GIOCO (RESETTABILI) ---
    public BigDouble CurrentEnergy { get; private set; }
    public BigDouble LifetimeEarnings { get; private set; }
    public int EmitterCount { get; private set; }
    public int LogisticsLevel { get; private set; }
    
    // --- VARIABILI PERMANENTI (PERSISTONO AL RESET) ---
    public BigDouble ScientificNodes { get; private set; } = 0;
    
    // --- CAPACITA' ---
    public BigDouble BaseEmissionPerUnit { get; private set; } = 1;
    public BigDouble StorageCap { get; private set; } = 50;
    public BigDouble LogisticsCap { get; private set; } = 3;

    // --- MOLTIPLICATORI & BONUS ---
    public BigDouble ResearchMultiplier { get; set; } = 1;
    public BigDouble LogisticsResearchBonus { get; set; } = 0;
    public BigDouble StorageResearchBonus { get; set; } = 0;
    
    // Il bonus ora dipende dai Nodi salvati! (+10% per nodo)
    public BigDouble EarningsBonus => 1 + (ScientificNodes * 0.10); 

    // --- STATO ---
    private bool _isHoldingButton = false;
    public BigDouble LastOfflineEarnings { get; private set; } = 0;
    public TimeSpan LastOfflineTimeSpan { get; private set; }

    // EVENTI
    public event Action OnEconomyUpdated;
    public event Action OnOfflineProductionCalculated;

    // TIMER UI
    private float _uiRefreshTimer = 0f;
    private float _uiRefreshRate = 0.05f;

    // FORMULE DI PRODUZIONE
    public BigDouble RawProductionRate 
    {
        get 
        {
            BigDouble baseProd = EmitterCount * BaseEmissionPerUnit;
            // Applichiamo qui il bonus dei Nodi Scientifici
            BigDouble multipliers = ResearchMultiplier * EarningsBonus;
            
            if (_isHoldingButton) multipliers *= holdButtonMultiplier;

            return baseProd * multipliers;
        }
    }

    public BigDouble EffectiveIncomePerSec => BigDouble.Min(RawProductionRate, LogisticsCap);

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
        
        QualitySettings.vSyncCount = 0; 
        Application.targetFrameRate = 60; 
        
        // Inizializza a zero, poi il LoadGame sovrascriverà se c'è un save
        InitializeGameState();
    }

    private void Start()
    {
        if (activeTheme != null) ThemedUIElement.SetGlobalTheme(activeTheme);
        if (targetResearchManager == null) targetResearchManager = FindFirstObjectByType<ResearchManager>();

        LoadGame(); // Carica tutto (Run corrente + Nodi)
        StartCoroutine(AutoSaveRoutine());
    }

    private void Update()
    {
        BigDouble income = EffectiveIncomePerSec;
        
        if (income > 0)
        {
            if (CurrentEnergy < StorageCap)
            {
                BigDouble amount = income * Time.deltaTime;
                CurrentEnergy += amount;
                LifetimeEarnings += amount;

                if (CurrentEnergy > StorageCap) CurrentEnergy = StorageCap;
                
                _uiRefreshTimer += Time.deltaTime;
                if (_uiRefreshTimer >= _uiRefreshRate)
                {
                    OnEconomyUpdated?.Invoke(); 
                    _uiRefreshTimer = 0f;
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // --- GESTIONE RESET QUANTISTICO (PRESTIGIO) ---
    // ----------------------------------------------------------------------
    
    // HELPER PER LA UI: Calcola quanti nodi otterresti se resettassi ora
    public BigDouble CalculatePotentialNodes()
    {
        // Se hai guadagnato meno di 1000 in questa vita, niente nodi
        if (LifetimeEarnings < 10) return 0;

        // Formula: Radice quadrata dei guadagni diviso 1000 (modificabile)
        return BigDouble.Floor(LifetimeEarnings / 1);    
    }

    public void PerformQuantumReset()
    {
        // 1. Usiamo la stessa funzione helper per coerenza
        BigDouble nodesToGain = CalculatePotentialNodes();

        if (nodesToGain <= 0) 
        {
            Debug.Log("Non hai guadagnato abbastanza per resettare!");
            return;
        }

        // 2. Aggiungi ai nodi permanenti
        ScientificNodes += nodesToGain;
        Debug.Log($"<color=cyan>RESET ESEGUITO! Guadagnati {nodesToGain} nodi. Totale: {ScientificNodes}</color>");

        // 3. Resetta lo stato della partita (Energia, Edifici, Ricerche)
        InitializeGameState(); 

        // 4. Resetta le ricerche nel manager
        if (targetResearchManager != null)
        {
            foreach(var res in targetResearchManager.allResearches)
            {
                res.currentLevel = 0;
            }
            targetResearchManager.RecalculateAllResearches();
        }

        // 5. SALVA SUBITO
        // Questo sovrascrive il file: avrai Energia 0 ma Nodi Alti.
        SaveGame(); 
        
        // 6. Aggiorna la grafica
        OnEconomyUpdated?.Invoke();
    }

    // Resetta solo le variabili della "Run", non i Nodi
    private void InitializeGameState()
    {
        CurrentEnergy = 0;
        LifetimeEarnings = 0;
        EmitterCount = 1;
        LogisticsLevel = 1;
        
        ResearchMultiplier = 1;
        LogisticsResearchBonus = 0;
        StorageResearchBonus = 0;

        RecalculateCaps();
    }

    // ----------------------------------------------------------------------
    // --- SISTEMA DI SALVATAGGIO UNIFICATO ---
    // ----------------------------------------------------------------------
    
    public void SaveGame()
    {
        SaveData data = new SaveData();

        // 1. Dati Run Corrente
        data.currentEnergy = CurrentEnergy.ToString();
        data.lifetimeEarnings = LifetimeEarnings.ToString();
        data.emitterCount = EmitterCount;
        data.logisticsLevel = LogisticsLevel;
        data.lastSaveTime = DateTime.Now.ToString(); 

        // 2. Dati Permanenti (IMPORTANTE!)
        data.scientificNodes = ScientificNodes.ToString();

        // 3. Ricerche
        if (targetResearchManager != null)
        {
            foreach (var item in targetResearchManager.allResearches)
            {
                if (item.currentLevel > 0) 
                {
                    data.researches.Add(new ResearchSaveData 
                    { 
                        id = item.id, 
                        level = item.currentLevel 
                    });
                }
            }
        }

        SaveManager.Save(data);
    }

    public void LoadGame()
    {
        SaveData data = SaveManager.Load();

        if (data == null) 
        {
            // Se non c'è salvataggio, inizia da zero
            InitializeGameState();
            ScientificNodes = 0;
            return; 
        }

        // 1. Ripristino Valuta Run
        if (!string.IsNullOrEmpty(data.currentEnergy)) CurrentEnergy = BigDouble.Parse(data.currentEnergy);
        if (!string.IsNullOrEmpty(data.lifetimeEarnings)) LifetimeEarnings = BigDouble.Parse(data.lifetimeEarnings);

        // 2. Ripristino Edifici Run
        EmitterCount = data.emitterCount > 0 ? data.emitterCount : 1;
        LogisticsLevel = data.logisticsLevel > 0 ? data.logisticsLevel : 1;

        // 3. Ripristino Dati Permanenti (NODI)
        if (!string.IsNullOrEmpty(data.scientificNodes))
            ScientificNodes = BigDouble.Parse(data.scientificNodes);
        else
            ScientificNodes = 0;

        // 4. Ripristino Ricerche
        if (targetResearchManager != null)
        {
            // Reset preventivo livelli
            foreach(var r in targetResearchManager.allResearches) r.currentLevel = 0;

            if (data.researches != null)
            {
                foreach (var savedRes in data.researches)
                {
                    var item = targetResearchManager.allResearches.Find(r => r.id == savedRes.id);
                    if (item != null) item.currentLevel = savedRes.level;
                }
            }
            targetResearchManager.RecalculateAllResearches();
        }

        RecalculateCaps();

        // 5. Offline
        if (!string.IsNullOrEmpty(data.lastSaveTime))
        {
            HandleOfflineProgress(data.lastSaveTime);
        }

        OnEconomyUpdated?.Invoke();
    }
    
    private void HandleOfflineProgress(string lastSaveTimeStr)
    {
        if (string.IsNullOrEmpty(lastSaveTimeStr)) return;
        DateTime lastSaveTime;
        if (!DateTime.TryParse(lastSaveTimeStr, out lastSaveTime)) return;

        TimeSpan timeAway = DateTime.Now - lastSaveTime;
        double secondsAway = timeAway.TotalSeconds;

        if (secondsAway > 1) 
        {
            BigDouble potentialEarnings = EffectiveIncomePerSec * secondsAway * offlineProductionRatio;
            BigDouble spaceAvailable = StorageCap - CurrentEnergy;
            if (spaceAvailable < 0) spaceAvailable = 0;
            BigDouble actualEarnings = BigDouble.Min(potentialEarnings, spaceAvailable);

            if (potentialEarnings > 0) 
            {
                CurrentEnergy += actualEarnings;
                LifetimeEarnings += actualEarnings;
                LastOfflineEarnings = actualEarnings;
                LastOfflineTimeSpan = timeAway;
                OnOfflineProductionCalculated?.Invoke();
            }
        }
    }

    IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            SaveGame();
        }
    }
    
    private void OnApplicationQuit() => SaveGame();
    private void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGame(); }

    // --- AZIONI GIOCATORE ---
    public void SetHoldState(bool isHolding) { _isHoldingButton = isHolding; }

    public void BuyEmitter()
    {
        BigDouble cost = GetEmitterCost();
        if (TrySpend(cost)) { EmitterCount++; RecalculateCaps(); OnEconomyUpdated?.Invoke(); }
    }

    public void BuyLogistics()
    {
        BigDouble cost = GetLogisticsCost();
        if (TrySpend(cost)) { LogisticsLevel++; RecalculateCaps(); OnEconomyUpdated?.Invoke(); }
    }

    public BigDouble GetEmitterCost() => emitterBaseCost * BigDouble.Pow(emitterGrowth, EmitterCount);
    public BigDouble GetLogisticsCost() => logisticsBaseCost * BigDouble.Pow(logisticsGrowth, LogisticsLevel);

    public bool TrySpend(BigDouble amount)
    {
        if (CurrentEnergy >= amount) { CurrentEnergy -= amount; return true; }
        return false;
    }

    public void RecalculateCaps()
    {
        LogisticsCap = 2 + (LogisticsLevel * 1.5) + LogisticsResearchBonus; 
        StorageCap = 500 + (LogisticsLevel * 50) + StorageResearchBonus;
    }

    public void UpdateCapsFromResearch() { RecalculateCaps(); OnEconomyUpdated?.Invoke(); }
    public void ForceUIUpdate() { OnEconomyUpdated?.Invoke(); }
}