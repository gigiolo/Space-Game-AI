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
    public double offlineProductionRatio = 0.5d;

    [Header("--- ENERGY BUTTON (RAMP-UP) ---")]
    [Tooltip("Tempo in secondi per raggiungere il moltiplicatore massimo.")]
    [SerializeField] private float energyButton_RampUpDuration = 7.0f;
    [Tooltip("Il moltiplicatore massimo applicato alla produzione.")]
    [SerializeField] private float energyButton_MaxMultiplier = 3.0f;
    [Tooltip("Durata massima in secondi in cui il moltiplicatore resta al suo picco.")]
    [SerializeField] private float energyButton_MaxHoldDuration = 12.0f;
    [Tooltip("Tempo in secondi per far tornare il moltiplicatore a 1x.")]
    [SerializeField] private float energyButton_RampDownDuration = 7.0f;
    [Tooltip("Modifica il costo del cooldown. Es: 1.0 = cooldown uguale al tempo di utilizzo.")]
    [SerializeField] private float energyButton_CooldownMultiplier = 1.0f;


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
    public BigDouble BaseEmissionPerUnit { get; private set; } = 0.01;
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
    
    // --- GESTIONE VELOCITA' NANOBOT ---
    // La velocità di base (senza ricerche). Modifica qui il valore (es. 0.3)
    public double BaseAutoGrowthSpeed = 0.3; 

    // La velocità effettiva attuale (Base + Bonus Ricerche)
    public double EmitterAutoGrowthSpeed { get; set; } 
    
    private double _emitterAccumulator = 0; 

    public BigDouble EarningsBonus => 1 + (ScientificNodes * 0.50); 

    // --- STATO & TIMER ---
    public BigDouble LastOfflineEarnings { get; private set; } = 0;
    public TimeSpan LastOfflineTimeSpan { get; private set; }
    public event Action OnEconomyUpdated;
    public event Action OnOfflineProductionCalculated;
    private float _uiRefreshTimer = 0f;
    private float _uiRefreshRate = 0.05f;

    // --- ENERGY BUTTON STATE ---
    private enum EnergyButtonState { Idle, RampingUp, HoldingMax, RampingDown, Cooldown }
    private EnergyButtonState _energyButtonState = EnergyButtonState.Idle;

    // Il valore attuale del moltiplicatore, pubblico per la UI
    public float CurrentEnergyMultiplier { get; private set; } = 1.0f;

    // Timers interni per la logica
    private float _energyButtonTimer = 0.0f;
    private float _timeSpentAtMax = 0.0f;
    private float _cooldownTimer = 0.0f;


    // FORMULE DI PRODUZIONE
    public BigDouble RawProductionRate 
    {
        get 
        {
            BigDouble baseProd = EmitterCount * BaseEmissionPerUnit;
            BigDouble multipliers = ResearchMultiplier * EarningsBonus;
            
            // Applica il nuovo moltiplicatore dinamico
            multipliers *= CurrentEnergyMultiplier;

            return baseProd * multipliers;
        }
    }

    private void UpdateEnergyButtonState()
    {
        float deltaTime = Time.deltaTime;

        switch (_energyButtonState)
        {
            case EnergyButtonState.RampingUp:
                _energyButtonTimer += deltaTime;
                CurrentEnergyMultiplier = Mathf.Lerp(1.0f, energyButton_MaxMultiplier, _energyButtonTimer / energyButton_RampUpDuration);

                if (_energyButtonTimer >= energyButton_RampUpDuration)
                {
                    CurrentEnergyMultiplier = energyButton_MaxMultiplier;
                    _energyButtonState = EnergyButtonState.HoldingMax;
                    _energyButtonTimer = 0.0f; // Reset timer for hold phase
                }
                break;

            case EnergyButtonState.HoldingMax:
                _energyButtonTimer += deltaTime;
                _timeSpentAtMax += deltaTime;

                if (_energyButtonTimer >= energyButton_MaxHoldDuration)
                {
                    _energyButtonState = EnergyButtonState.RampingDown;
                    _energyButtonTimer = 0.0f; // Reset timer for ramp down
                }
                break;

            case EnergyButtonState.RampingDown:
                _energyButtonTimer += deltaTime;
                CurrentEnergyMultiplier = Mathf.Lerp(energyButton_MaxMultiplier, 1.0f, _energyButtonTimer / energyButton_RampDownDuration);

                if (_energyButtonTimer >= energyButton_RampDownDuration)
                {
                    CurrentEnergyMultiplier = 1.0f;
                    _energyButtonState = EnergyButtonState.Cooldown;
                    _cooldownTimer = _timeSpentAtMax * energyButton_CooldownMultiplier;
                    _timeSpentAtMax = 0; // Reset for next cycle
                    _energyButtonTimer = 0;
                }
                break;

            case EnergyButtonState.Cooldown:
                _cooldownTimer -= deltaTime;
                if (_cooldownTimer <= 0)
                {
                    _energyButtonState = EnergyButtonState.Idle;
                }
                break;
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
        // 1. GESTIONE LOGICA ENERGY BUTTON
        UpdateEnergyButtonState();

        // 2. GESTIONE INCOME
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
        // Funziona solo se la velocità è > 0 e c'è spazio
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
        LogisticsCap = 5000 + (LogisticsLevel * 1) + LogisticsResearchBonus; 
        StorageCap = 249 + (LogisticsLevel * 1) + StorageResearchBonus;

        // --- CALCOLO CAP EMETTITORI ---
        // Qui hai impostato 5 come base nel codice che mi hai inviato.
        // Se vuoi tornare a 250, cambia il 5 in 250.
        EmitterCap = 5 + EmitterCapResearchBonus;
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
        
        // --- MODIFICA FONDAMENTALE ---
        // Inizializziamo usando la variabile Base, così non è hardcodata
        EmitterAutoGrowthSpeed = BaseAutoGrowthSpeed;
        
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

    // --- AZIONI GIOCATORE (Energy Button)---
    public void OnEnergyButtonPress()
    {
        if (_energyButtonState == EnergyButtonState.Idle)
        {
            _energyButtonState = EnergyButtonState.RampingUp;
            _energyButtonTimer = 0.0f;
        }
    }

    public void OnEnergyButtonRelease()
    {
        if (_energyButtonState == EnergyButtonState.RampingUp || _energyButtonState == EnergyButtonState.HoldingMax)
        {
            _energyButtonState = EnergyButtonState.RampingDown;
            _energyButtonTimer = 0.0f;
        }
    }


    // --- AZIONI GIOCATORE ---
    public bool TrySpend(BigDouble amount)
    {
        if (CurrentEnergy >= amount) { CurrentEnergy -= amount; return true; }
        return false;
    }

    public void ForceUIUpdate() { OnEconomyUpdated?.Invoke(); }
}