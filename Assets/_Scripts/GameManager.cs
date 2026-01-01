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
    [Tooltip("Trascina qui l'oggetto che contiene lo script ResearchManager.")]
    public ResearchManager targetResearchManager; 

    [Header("--- UI THEME ---")]
    public UITheme activeTheme; 

    [Header("--- BILANCIAMENTO ---")]
    public BigDouble emitterBaseCost = 10;
    public double emitterGrowth = 1.15d;
    
    public BigDouble logisticsBaseCost = 15;
    public double logisticsGrowth = 1.50d;

    public BigDouble holdButtonMultiplier = 2.0; 
    
    [Header("--- OFFLINE ---")]
    [Tooltip("Percentuale di guadagno offline (0.5 = 50%)")]
    public double offlineProductionRatio = 0.5d;

    // --- VARIABILI DI GIOCO ---
    public BigDouble CurrentEnergy { get; private set; }
    public BigDouble LifetimeEarnings { get; private set; }
    public int EmitterCount { get; private set; }
    public int LogisticsLevel { get; private set; }
    public BigDouble BaseEmissionPerUnit { get; private set; } = 1;
    public BigDouble StorageCap { get; private set; } = 500;
    public BigDouble LogisticsCap { get; private set; } = 50;

    // --- MOLTIPLICATORI & PRESTIGIO ---
    public BigDouble ResearchMultiplier { get; set; } = 1;
    public BigDouble ScientificNodes { get; private set; } = 0;
    public BigDouble QuantumModifiers { get; private set; } = 0;

    public BigDouble EarningsBonus => 1 + (ScientificNodes * 0.10); 

    public BigDouble LogisticsResearchBonus { get; set; } = 0;
    public BigDouble StorageResearchBonus { get; set; } = 0;

    // --- STATO ---
    private bool _isHoldingButton = false;

    // DATI PER LA UI OFFLINE
    public BigDouble LastOfflineEarnings { get; private set; } = 0;
    public TimeSpan LastOfflineTimeSpan { get; private set; }

    // EVENTI
    public event Action OnEconomyUpdated;
    public event Action OnOfflineProductionCalculated;

    // --- OTTIMIZZAZIONE PERFORMANCE ---
    private float _uiRefreshTimer = 0f;
    private float _uiRefreshRate = 0.05f; // Aggiorna la UI ~20 volte al secondo invece di 60/120

    // FORMULE
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
        
        // --- FIX FRAME RATE ---
        QualitySettings.vSyncCount = 0; // Disabilita VSync per permettere FPS alti
        Application.targetFrameRate = 120; // Targetta 120 FPS su schermi supportati
        
        InitializeGame();
    }

    private void InitializeGame()
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

    private void Update()
    {
        // Core Loop: Produzione
        // Calcoliamo la matematica ad ogni frame per massima precisione
        BigDouble income = EffectiveIncomePerSec;
        
        if (income > 0)
        {
            if (CurrentEnergy < StorageCap)
            {
                BigDouble amount = income * Time.deltaTime;
                CurrentEnergy += amount;
                LifetimeEarnings += amount;

                if (CurrentEnergy > StorageCap) CurrentEnergy = StorageCap;
                
                // --- FIX UI LAG ---
                // Aggiorniamo i testi della UI solo ogni tot millisecondi
                // Questo riduce drasticamente il lavoro del Garbage Collector
                _uiRefreshTimer += Time.deltaTime;
                if (_uiRefreshTimer >= _uiRefreshRate)
                {
                    OnEconomyUpdated?.Invoke(); 
                    _uiRefreshTimer = 0f;
                }
            }
        }
    }

    [Header("--- SALVATAGGIO ---")]
    public float autoSaveInterval = 30f; 

    private void Start()
    {
        if (activeTheme != null)
        {
            ThemedUIElement.SetGlobalTheme(activeTheme);
        }
        
        if (targetResearchManager == null)
        {
            targetResearchManager = FindFirstObjectByType<ResearchManager>();
        }

        LoadGame(); 
        StartCoroutine(AutoSaveRoutine());
    }

    private void OnApplicationQuit() => SaveGame();

    // --- SISTEMA DI SALVATAGGIO ---
    public void SaveGame()
    {
        SaveData data = new SaveData();

        data.currentEnergy = CurrentEnergy.ToString();
        data.lifetimeEarnings = LifetimeEarnings.ToString();
        data.emitterCount = EmitterCount;
        data.logisticsLevel = LogisticsLevel;
        data.lastSaveTime = DateTime.Now.ToString(); 

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

        if (data == null) return; 

        // 1. Ripristino Valuta
        if (!string.IsNullOrEmpty(data.currentEnergy))
            CurrentEnergy = BigDouble.Parse(data.currentEnergy);
            
        if (!string.IsNullOrEmpty(data.lifetimeEarnings))
            LifetimeEarnings = BigDouble.Parse(data.lifetimeEarnings);

        // 2. Ripristino Edifici
        EmitterCount = data.emitterCount;
        LogisticsLevel = data.logisticsLevel;

        // 3. Ripristino Ricerche
        if (targetResearchManager != null && data.researches != null)
        {
            foreach (var savedRes in data.researches)
            {
                var item = targetResearchManager.allResearches.Find(r => r.id == savedRes.id);
                if (item != null)
                {
                    item.currentLevel = savedRes.level;
                }
            }
            targetResearchManager.RecalculateAllResearches();
        }

        RecalculateCaps();

        // 4. CALCOLO GUADAGNI OFFLINE
        if (!string.IsNullOrEmpty(data.lastSaveTime))
        {
            HandleOfflineProgress(data.lastSaveTime);
        }

        OnEconomyUpdated?.Invoke();
        Debug.Log("Salvataggio caricato.");
    }

    private void HandleOfflineProgress(string lastSaveTimeStr)
    {
        if (string.IsNullOrEmpty(lastSaveTimeStr)) return;

        DateTime lastSaveTime;
        if (!DateTime.TryParse(lastSaveTimeStr, out lastSaveTime))
        {
            return; 
        }

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

    // --- AZIONI GIOCATORE ---
    public void SetHoldState(bool isHolding)
    {
        if (_isHoldingButton != isHolding)
        {
            _isHoldingButton = isHolding;
            // Qui lasciamo che l'Update gestisca il refresh grafico per non sovraccaricare
            // Ma se vuoi feedback istantaneo puoi decommentare la riga sotto:
            // OnEconomyUpdated?.Invoke(); 
        }
    }

    public void BuyEmitter()
    {
        BigDouble cost = GetEmitterCost();
        if (TrySpend(cost))
        {
            EmitterCount++;
            RecalculateCaps(); 
            OnEconomyUpdated?.Invoke(); // Qui forziamo l'update perché il click è un evento raro
        }
    }

    public void BuyLogistics()
    {
        BigDouble cost = GetLogisticsCost();
        if (TrySpend(cost))
        {
            LogisticsLevel++;
            RecalculateCaps();
            OnEconomyUpdated?.Invoke(); // Qui forziamo l'update
        }
    }

    // --- HELPER ---
    public BigDouble GetEmitterCost() => emitterBaseCost * BigDouble.Pow(emitterGrowth, EmitterCount);
    public BigDouble GetLogisticsCost() => logisticsBaseCost * BigDouble.Pow(logisticsGrowth, LogisticsLevel);

    public bool TrySpend(BigDouble amount)
    {
        if (CurrentEnergy >= amount)
        {
            CurrentEnergy -= amount;
            return true;
        }
        return false;
    }

    public void RecalculateCaps()
    {
        LogisticsCap = 50 + (LogisticsLevel * 10) + LogisticsResearchBonus; 
        StorageCap = 500 + (LogisticsLevel * 50) + StorageResearchBonus;
    }

    public void UpdateCapsFromResearch()
    {
        RecalculateCaps();
        OnEconomyUpdated?.Invoke();
    }
    
    public void ForceUIUpdate()
    {
        OnEconomyUpdated?.Invoke();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
        else
        {
            LoadGame();
        }
    }
}