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
    
    // --- MODIFICA MATEMATICA 1: CRESCITA LOGISTICA RILASSATA ---
    // Prima era 1.50 (troppo ripido). Ora 1.25 permette di comprare più livelli.
    public double logisticsGrowth = 1.25d; 

    public BigDouble holdButtonMultiplier = 2.0; 
    public double offlineProductionRatio = 0.5d;

    [Header("--- SALVATAGGIO ---")]
    public float autoSaveInterval = 30f; 

    // --- VARIABILI DI GIOCO ---
    public BigDouble CurrentEnergy { get; private set; }
    public BigDouble LifetimeEarnings { get; private set; }
    public int EmitterCount { get; private set; }
    public int LogisticsLevel { get; private set; }
    
    // --- VARIABILI PERMANENTI ---
    public BigDouble ScientificNodes { get; private set; } = 0;
    
    // --- CAPACITA' ---
    public BigDouble BaseEmissionPerUnit { get; private set; } = 1;
    public BigDouble StorageCap { get; private set; } = 50;
    public BigDouble LogisticsCap { get; private set; } = 3;

    // --- MOLTIPLICATORI & BONUS ---
    public BigDouble ResearchMultiplier { get; set; } = 1;
    public BigDouble LogisticsResearchBonus { get; set; } = 0;
    public BigDouble StorageResearchBonus { get; set; } = 0;
    
    public double EmitterAutoGrowthSpeed { get; set; } = 0; 
    private double _emitterAccumulator = 0; 

    // --- MODIFICA MATEMATICA 2: BONUS NODI PIÙ POTENTE ---
    // Dato che la formula della radice quadrata ci darà meno nodi, 
    // aumentiamo la potenza del singolo nodo da 10% (0.10) a 50% (0.50).
    public BigDouble EarningsBonus => 1 + (ScientificNodes * 0.50); 

    // --- STATO & TIMER ---
    private bool _isHoldingButton = false;
    public BigDouble LastOfflineEarnings { get; private set; } = 0;
    public TimeSpan LastOfflineTimeSpan { get; private set; }
    public event Action OnEconomyUpdated;
    public event Action OnOfflineProductionCalculated;
    private float _uiRefreshTimer = 0f;
    private float _uiRefreshRate = 0.05f;

    // FORMULE DI PRODUZIONE
    public BigDouble RawProductionRate 
    {
        get 
        {
            BigDouble baseProd = EmitterCount * BaseEmissionPerUnit;
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
        
        InitializeGameState();
    }

    private void Start()
    {
        if (activeTheme != null) ThemedUIElement.SetGlobalTheme(activeTheme);
        if (targetResearchManager == null) targetResearchManager = FindFirstObjectByType<ResearchManager>();

        LoadGame(); 
        StartCoroutine(AutoSaveRoutine());
    }

    private void Update()
    {
        // 1. GESTIONE INCOME
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

        // 2. GESTIONE AUTO-CRESCITA
        if (EmitterAutoGrowthSpeed > 0)
        {
            _emitterAccumulator += EmitterAutoGrowthSpeed * Time.deltaTime;
            if (_emitterAccumulator >= 1.0)
            {
                int toAdd = (int)_emitterAccumulator; 
                _emitterAccumulator -= toAdd;         
                
                EmitterCount += toAdd;
                RecalculateCaps();
                OnEconomyUpdated?.Invoke();
            }
        }
    }

    // ----------------------------------------------------------------------
    // --- NUOVO SISTEMA MATEMATICO (PRESTIGIO & CAPS) ---
    // ----------------------------------------------------------------------
    
    // --- MODIFICA MATEMATICA 3: FORMULA RADICE QUADRATA ---
    public BigDouble CalculatePotentialNodes()
    {
        // Soglia minima per resettare aumentata leggermente per evitare reset inutili
        if (LifetimeEarnings < 1000) return 0;

        // FORMULA: Radice Quadrata di (Earnings / 1000)
        // Esempio: 
        // 1000 Energy -> Sqrt(1) = 1 Nodo
        // 1.000.000 Energy -> Sqrt(1000) = 31 Nodi
        // Questo rallenta l'inflazione dei nodi nel late-game.
        BigDouble baseVal = LifetimeEarnings / 1000;
        
        // BreakInfinity usa BigDouble.Pow(val, 0.5) per la radice quadrata
        return BigDouble.Floor(BigDouble.Pow(baseVal, 0.5));    
    }

    public void RecalculateCaps()
    {
        // --- MODIFICA MATEMATICA 4: CAPACITÀ LOGISTICA AUMENTATA ---
        // Formula vecchia: 2 + (LogisticsLevel * 1.5) -> Troppo lenta
        // Formula nuova: 5 + (LogisticsLevel * 5) -> Molto più respiro
        LogisticsCap = 5 + (LogisticsLevel * 5) + LogisticsResearchBonus; 
        
        // Storage: formula leggermente potenziata
        StorageCap = 500 + (LogisticsLevel * 100) + StorageResearchBonus;
    }

    // ----------------------------------------------------------------------
    
    public void PerformQuantumReset()
    {
        BigDouble nodesToGain = CalculatePotentialNodes();

        if (nodesToGain <= 0) 
        {
            Debug.Log("Non hai guadagnato abbastanza per resettare!");
            return;
        }

        ScientificNodes += nodesToGain;
        Debug.Log($"<color=cyan>RESET ESEGUITO! Guadagnati {nodesToGain} nodi. Totale: {ScientificNodes}</color>");

        InitializeGameState(); 

        if (targetResearchManager != null)
        {
            foreach(var res in targetResearchManager.allResearches)
            {
                res.currentLevel = 0;
            }
            targetResearchManager.RecalculateAllResearches();
        }

        SaveGame(); 
        OnEconomyUpdated?.Invoke();
    }

    private void InitializeGameState()
    {
        CurrentEnergy = 0;
        LifetimeEarnings = 0;
        EmitterCount = 1;
        LogisticsLevel = 1;
        
        ResearchMultiplier = 1;
        LogisticsResearchBonus = 0;
        StorageResearchBonus = 0;
        
        EmitterAutoGrowthSpeed = 0;
        _emitterAccumulator = 0;

        RecalculateCaps();
    }

    // --- SALVATAGGIO (Invariato) ---
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.currentEnergy = CurrentEnergy.ToString();
        data.lifetimeEarnings = LifetimeEarnings.ToString();
        data.emitterCount = EmitterCount;
        data.logisticsLevel = LogisticsLevel;
        data.lastSaveTime = DateTime.Now.ToString(); 
        data.scientificNodes = ScientificNodes.ToString();

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
            InitializeGameState();
            ScientificNodes = 0;
            return; 
        }

        if (!string.IsNullOrEmpty(data.currentEnergy)) CurrentEnergy = BigDouble.Parse(data.currentEnergy);
        if (!string.IsNullOrEmpty(data.lifetimeEarnings)) LifetimeEarnings = BigDouble.Parse(data.lifetimeEarnings);

        EmitterCount = data.emitterCount > 0 ? data.emitterCount : 1;
        LogisticsLevel = data.logisticsLevel > 0 ? data.logisticsLevel : 1;

        if (!string.IsNullOrEmpty(data.scientificNodes))
            ScientificNodes = BigDouble.Parse(data.scientificNodes);
        else
            ScientificNodes = 0;

        if (targetResearchManager != null)
        {
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
            }

            if (EmitterAutoGrowthSpeed > 0)
            {
                double rawGrowth = EmitterAutoGrowthSpeed * secondsAway * offlineProductionRatio;
                int emittersGained = (int)rawGrowth;
                double decimalRemainder = rawGrowth - emittersGained;

                if (emittersGained > 0)
                {
                    EmitterCount += emittersGained;
                    _emitterAccumulator += decimalRemainder;
                    RecalculateCaps(); 
                    Debug.Log($"Offline Growth: gained {emittersGained} emitters");
                }
            }

            LastOfflineTimeSpan = timeAway;
            OnOfflineProductionCalculated?.Invoke();
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

    public void UpdateCapsFromResearch() { RecalculateCaps(); OnEconomyUpdated?.Invoke(); }
    public void ForceUIUpdate() { OnEconomyUpdated?.Invoke(); }
}