using UnityEngine;
using BreakInfinity; 
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static GameManager Instance { get; private set; }

    [Header("--- COLLEGAMENTI ---")]
    public ResearchManager targetResearchManager; 
    public UITheme activeTheme; 
    
    // Riferimento allo script che gestisce le luci sul pianeta
    public PlanetPopulationVisuals planetVisuals; 

    public GameObject[] emitters;

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
    
    // --- CAPACITA' & LIMITI ---
    public BigDouble BaseEmissionPerUnit { get; private set; } = 0.01; 
    
    // TEMPO OFFLINE (Sostituisce lo Storage Cap)
    // Base: 7200 secondi (2 Ore)
    public double MaxOfflineSeconds { get; private set; } = 7200; 

    public BigDouble LogisticsCap { get; private set; } = 3;

    // --- LIMITE EMETTITORI ---
    public int EmitterCap { get; private set; } = 1; 

    // --- MOLTIPLICATORI & BONUS ---
    public BigDouble ResearchMultiplier { get; set; } = 1;
    public BigDouble LogisticsResearchBonus { get; set; } = 0;
    
    // Questo bonus ora rappresenta LIVELLI di batteria
    public BigDouble StorageResearchBonus { get; set; } = 0; 
    
    // Bonus accumulato dalle ricerche per il Cap Emettitori
    public int EmitterCapResearchBonus { get; set; } = 0; 
    
    // --- GESTIONE VELOCITA' NANOBOT ---
    public double BaseAutoGrowthSpeed = 0.3; 
    
    // IMPORTANTE: Questo deve essere 'double' per funzionare con Time.deltaTime
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

    // Nuove variabili per gestire la discesa proporzionale
    private float _rampDownStartMultiplier = 1.0f;
    private float _currentRampDownDuration = 0.0f;


    // FORMULE DI PRODUZIONE
    public BigDouble RawProductionRate 
    {
        get 
        {
            BigDouble baseProd = EmitterCount * BaseEmissionPerUnit;

            // Get planet multiplier, default to 1 if not available
            BigDouble planetMultiplier = PlanetManager.Instance?.GetCurrentPlanetData()?.productionMultiplier ?? 1;

            BigDouble multipliers = ResearchMultiplier * EarningsBonus * planetMultiplier;
            
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

                if (_currentRampDownDuration > 0)
                {
                    // Usa le nuove variabili per un Lerp proporzionale e corretto
                    float normalizedTime = _energyButtonTimer / _currentRampDownDuration;
                    CurrentEnergyMultiplier = Mathf.Lerp(_rampDownStartMultiplier, 1.0f, normalizedTime);
                }
                else
                {
                    // Se la durata è 0, imposta direttamente il moltiplicatore a 1 per evitare errori
                    CurrentEnergyMultiplier = 1.0f;
                }

                if (_energyButtonTimer >= _currentRampDownDuration)
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

    public BigDouble TotalEnergyPerSecond => BigDouble.Min(RawProductionRate, LogisticsCap);

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
        // SHORTCUT HARD RESET
        if (Input.GetKeyDown(KeyCode.K) || Input.touchCount >= 4)
        {
            PerformFullHardReset();
            return;
        }

        // 1. GESTIONE LOGICA ENERGY BUTTON
        UpdateEnergyButtonState();

        // 2. GESTIONE INCOME (INFINITO)
        BigDouble income = TotalEnergyPerSecond;
        
        if (income > 0)
        {
            BigDouble amount = income * Time.deltaTime;

            // Energia Infinita: sale sempre
            CurrentEnergy += amount;
            LifetimeEarnings += amount;

            _uiRefreshTimer += Time.deltaTime;
            if (_uiRefreshTimer >= _uiRefreshRate)
            {
                OnEconomyUpdated?.Invoke(); 
                _uiRefreshTimer = 0f;
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

        // DEBUG
        if (Input.GetKeyDown(KeyCode.N))
        {
            AddInstantEmitters(5);
        }
    }

    public BigDouble CalculatePotentialNodes()
    {
        if (LifetimeEarnings < 1000) return 0;
        
        // Rendimenti decrescenti (Radice Quadrata)
        BigDouble baseVal = LifetimeEarnings / 1000;
        return BigDouble.Floor(BigDouble.Pow(baseVal, 0.5));    
    }

    public void RecalculateCaps()
    {
        LogisticsCap = 5000 + (LogisticsLevel * 50) + LogisticsResearchBonus; 
        
        // --- CALCOLO TEMPO OFFLINE ---
        // FIX: Usiamo .ToDouble() per convertire in sicurezza BigDouble -> double
        double bonusSeconds = StorageResearchBonus.ToDouble() * 1800;
        
        MaxOfflineSeconds = 7200 + bonusSeconds;

        EmitterCap = 5 + EmitterCapResearchBonus;
    }

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
        
        InitializeGameState(); 

        if (planetVisuals != null) planetVisuals.ResetVisuals();

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

    public void PerformPlanetChangeReset()
    {
        // This reset is for changing planets. It preserves Scientific Nodes.
        InitializeGameState();

        // Reset researches
        if (targetResearchManager != null)
        {
            foreach(var res in targetResearchManager.allResearches)
            {
                res.currentLevel = 0;
            }
            targetResearchManager.RecalculateAllResearches();
        }

        // We don't save here immediately, the PlanetManager will handle saving
        // after the new planet scene is loaded.
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
        EmitterAutoGrowthSpeed = BaseAutoGrowthSpeed;
        
        _emitterAccumulator = 0;

        RecalculateCaps();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.currentEnergy = CurrentEnergy.ToString();
        data.lifetimeEarnings = LifetimeEarnings.ToString();
        data.emitterCount = EmitterCount;
        data.logisticsLevel = LogisticsLevel;
        
        data.lastSaveTime = DateTime.UtcNow.ToBinary().ToString(); 
        
        data.scientificNodes = ScientificNodes.ToString();

        // Save planet progression
        if (PlanetManager.Instance != null)
        {
            data.currentPlanetIndex = PlanetManager.Instance.currentPlanetIndex;
            data.isPreparingForLaunch = PlanetManager.Instance.isPreparingForLaunch;
            data.launchPreparationProgress = PlanetManager.Instance.launchPreparationProgress.ToString();
            data.isTraveling = PlanetManager.Instance.isTraveling;
            data.travelStartTimeBinary = PlanetManager.Instance.travelStartTime.ToBinary().ToString();
        }

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

        // Load planet progression
        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.currentPlanetIndex = data.currentPlanetIndex;
            PlanetManager.Instance.isPreparingForLaunch = data.isPreparingForLaunch;
            if (!string.IsNullOrEmpty(data.launchPreparationProgress))
            {
                PlanetManager.Instance.launchPreparationProgress = BigDouble.Parse(data.launchPreparationProgress);
            }
            PlanetManager.Instance.isTraveling = data.isTraveling;
            if (!string.IsNullOrEmpty(data.travelStartTimeBinary))
            {
                long binaryTime = long.Parse(data.travelStartTimeBinary);
                PlanetManager.Instance.travelStartTime = DateTime.FromBinary(binaryTime);
            }
        }

        if (targetResearchManager != null)
        {
            targetResearchManager.LoadResearchLevels(data.researches);
        }

        if (planetVisuals != null && data.cityLightPositions != null)
        {
            planetVisuals.LoadEncodedPositions(data.cityLightPositions);
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
        // Correct Offline Travel Handling
        if (PlanetManager.Instance != null && PlanetManager.Instance.isTraveling)
        {
            TimeSpan timeSinceTravelStart = DateTime.UtcNow - PlanetManager.Instance.travelStartTime;
            if (timeSinceTravelStart.TotalSeconds >= PlanetManager.TRAVEL_DURATION_SECONDS)
            {
                PlanetManager.Instance.CompleteTravel();
                return; // Stop further offline processing as the planet has changed
            }
        }

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
            // LIMITA IL TEMPO, NON L'ENERGIA
            double actualSeconds = Math.Min(secondsAway, MaxOfflineSeconds);
            
            BigDouble actualEarnings = TotalEnergyPerSecond * actualSeconds * offlineProductionRatio;

            if (actualEarnings > 0) 
            {
                CurrentEnergy += actualEarnings;
                LifetimeEarnings += actualEarnings;
                LastOfflineEarnings = actualEarnings;
            }

            // CRESCITA EMETTITORI OFFLINE
            if (EmitterAutoGrowthSpeed > 0 && EmitterCount < EmitterCap)
            {
                // FIX: Assicurati che tutte le variabili qui siano 'double'
                double rawGrowth = EmitterAutoGrowthSpeed * actualSeconds * offlineProductionRatio;
                
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
        if (_energyButtonState == EnergyButtonState.RampingUp)
        {
            // Salva il moltiplicatore corrente
            _rampDownStartMultiplier = CurrentEnergyMultiplier;

            // Calcola la proporzione del moltiplicatore raggiunto rispetto al massimo
            float multiplierRatio = (_rampDownStartMultiplier - 1.0f) / (energyButton_MaxMultiplier - 1.0f);

            // Calcola la durata della discesa in modo proporzionale
            _currentRampDownDuration = energyButton_RampDownDuration * multiplierRatio;

            _energyButtonState = EnergyButtonState.RampingDown;
            _energyButtonTimer = 0.0f;
        }
        else if (_energyButtonState == EnergyButtonState.HoldingMax)
        {
            // Se siamo al massimo, usa i valori standard
            _rampDownStartMultiplier = energyButton_MaxMultiplier;
            _currentRampDownDuration = energyButton_RampDownDuration;

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

    public void AddInstantEmitters(int amount)
    {
        EmitterCount += amount;
        if (EmitterCount > EmitterCap) EmitterCount = EmitterCap; 
        RecalculateCaps();
        OnEconomyUpdated?.Invoke();
        if (planetVisuals != null) planetVisuals.RefreshLights();
    }

    public void AddEnergy(BigDouble amount)
    {
        CurrentEnergy += amount;
        LifetimeEarnings += amount; 
        OnEconomyUpdated?.Invoke();
    }

    public void PerformFullHardReset()
    {
        Debug.LogWarning("HARD RESET INIZIATO.");

        SaveManager.DeleteSaveFile();

        ScientificNodes = 0;
        LifetimeEarnings = 0;
        CurrentEnergy = 0;
        EmitterCount = 1;
        LogisticsLevel = 1;
        
        if (targetResearchManager != null)
        {
            foreach (var item in targetResearchManager.allResearches)
            {
                item.currentLevel = 0;
            }
            targetResearchManager.RecalculateAllResearches();
        }

        if (planetVisuals != null) planetVisuals.ResetVisuals();

        InitializeGameState();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}