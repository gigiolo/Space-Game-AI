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
    
    // Riferimento allo script che gestisce le luci sul pianeta
    public PlanetPopulationVisuals planetVisuals; 

    [Header("--- BILANCIAMENTO ---")]
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

    // --- LIMITE EMETTITORI ---
    public int EmitterCap { get; private set; } = 1; 

    // --- MOLTIPLICATORI & BONUS (Setters resi pubblici per ResearchManager) ---
    public BigDouble ResearchMultiplier { get; set; } = 1;
    public BigDouble LogisticsResearchBonus { get; set; } = 0;
    public BigDouble StorageResearchBonus { get; set; } = 0;
    
    // Bonus accumulato dalle ricerche per il Cap Emettitori
    public int EmitterCapResearchBonus { get; set; } = 0; 
    
    public double EmitterAutoGrowthSpeed { get; set; } = 0; 
    private double _emitterAccumulator = 0; 

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

        // 2. GESTIONE AUTO-CRESCITA (Nanobot)
        if (EmitterAutoGrowthSpeed > 0 && EmitterCount < EmitterCap)
        {
            _emitterAccumulator += EmitterAutoGrowthSpeed * Time.deltaTime;
            
            if (_emitterAccumulator >= 1.0)
            {
                int toAdd = (int)_emitterAccumulator; 
                
                int spaceLeft = EmitterCap - EmitterCount;
                int actualAdd = Mathf.Min(toAdd, spaceLeft);

                _emitterAccumulator -= actualAdd;            
                
                if (actualAdd < toAdd) _emitterAccumulator = 0;

                if (actualAdd > 0)
                {
                    EmitterCount += actualAdd;
                    RecalculateCaps();
                    OnEconomyUpdated?.Invoke();
                }
            }
        }
    }

    public BigDouble CalculatePotentialNodes()
    {
        if (LifetimeEarnings < 1000) return 0;
        BigDouble baseVal = LifetimeEarnings / 1000;
        return BigDouble.Floor(BigDouble.Pow(baseVal, 0.5));    
    }

    public void RecalculateCaps()
    {
        LogisticsCap = 5 + (LogisticsLevel * 5) + LogisticsResearchBonus; 
        StorageCap = 500 + (LogisticsLevel * 100) + StorageResearchBonus;

        // --- CALCOLO CAP EMETTITORI ---
        EmitterCap = 1 + EmitterCapResearchBonus;
    }

    // --- NUOVO METODO FONDAMENTALE PER LE RICERCHE ---
    public void UpdateCapsFromResearch() 
    { 
        RecalculateCaps(); 
        OnEconomyUpdated?.Invoke(); 
    }

    public void PerformQuantumReset()
    {
        BigDouble nodesToGain = CalculatePotentialNodes();

        if (nodesToGain <= 0) return;

        ScientificNodes += nodesToGain;
        
        // Questo resetta i dati matematici (Energy, EmitterCount=1, ecc.)
        InitializeGameState(); 

        // --- MODIFICA VISUALS ---
        if (planetVisuals != null)
        {
            planetVisuals.ResetVisuals();
        }

        // Reset Ricerche
        if (targetResearchManager != null)
        {
            // Reset livelli a 0
            foreach(var res in targetResearchManager.allResearches)
            {
                res.currentLevel = 0;
            }
            // Ricalcola per azzerare i bonus
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
        
        EmitterCapResearchBonus = 0;
        EmitterAutoGrowthSpeed = 0;
        _emitterAccumulator = 0;

        RecalculateCaps();
    }

    // --- SALVATAGGIO ---
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.currentEnergy = CurrentEnergy.ToString();
        data.lifetimeEarnings = LifetimeEarnings.ToString();
        data.emitterCount = EmitterCount;
        data.logisticsLevel = LogisticsLevel;
        
        data.lastSaveTime = DateTime.UtcNow.ToBinary().ToString(); 
        
        data.scientificNodes = ScientificNodes.ToString();

        // Salvataggio Posizioni Luci
        if (planetVisuals != null)
        {
            data.cityLightPositions = planetVisuals.GetEncodedPositions();
        }

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
        // 1. Carichiamo i dati dal disco
        SaveData data = SaveManager.Load();

        // 2. Se non esistono dati, resettiamo tutto e usciamo
        if (data == null) 
        {
            InitializeGameState();
            ScientificNodes = 0;
            return; 
        }

        // 3. Applichiamo i dati salvati
        if (!string.IsNullOrEmpty(data.currentEnergy)) CurrentEnergy = BigDouble.Parse(data.currentEnergy);
        if (!string.IsNullOrEmpty(data.lifetimeEarnings)) LifetimeEarnings = BigDouble.Parse(data.lifetimeEarnings);

        EmitterCount = data.emitterCount > 0 ? data.emitterCount : 1;
        LogisticsLevel = data.logisticsLevel > 0 ? data.logisticsLevel : 1;

        if (!string.IsNullOrEmpty(data.scientificNodes))
            ScientificNodes = BigDouble.Parse(data.scientificNodes);
        else
            ScientificNodes = 0;

        // --- CARICAMENTO NUOVO SISTEMA RICERCHE ---
        // Qui usiamo la variabile 'data' che abbiamo definito all'inizio
        if (targetResearchManager != null)
        {
            targetResearchManager.LoadResearchLevels(data.researches);
        }

        // Caricamento Posizioni Luci
        if (planetVisuals != null && data.cityLightPositions != null)
        {
            planetVisuals.LoadEncodedPositions(data.cityLightPositions);
        }

        RecalculateCaps();

        // Gestione tempo offline
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
        
        try 
        {
            long binaryDate = long.Parse(lastSaveTimeStr);
            lastSaveTime = DateTime.FromBinary(binaryDate);
        }
        catch 
        {
            if (!DateTime.TryParse(lastSaveTimeStr, out lastSaveTime)) return;
        }

        TimeSpan timeAway = DateTime.UtcNow - lastSaveTime;
        if (timeAway.TotalSeconds < 0) timeAway = DateTime.Now - lastSaveTime; 

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

            // --- OFFLINE GROWTH CON CAP ---
            if (EmitterAutoGrowthSpeed > 0 && EmitterCount < EmitterCap)
            {
                double rawGrowth = EmitterAutoGrowthSpeed * secondsAway * offlineProductionRatio;
                int potentialGained = (int)rawGrowth;
                double decimalRemainder = rawGrowth - potentialGained;

                int spaceLeft = EmitterCap - EmitterCount;
                int actualGained = Mathf.Min(potentialGained, spaceLeft);

                if (actualGained > 0)
                {
                    EmitterCount += actualGained;
                    if (EmitterCount < EmitterCap)
                    {
                        _emitterAccumulator += decimalRemainder;
                    }
                    RecalculateCaps(); 
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

    public bool TrySpend(BigDouble amount)
    {
        if (CurrentEnergy >= amount) { CurrentEnergy -= amount; return true; }
        return false;
    }

    public void ForceUIUpdate() { OnEconomyUpdated?.Invoke(); }
}