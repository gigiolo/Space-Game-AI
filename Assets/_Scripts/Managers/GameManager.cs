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
    
    public PlanetPopulationVisuals planetVisuals; 
    public GameObject[] emitters;

    // --- MODIFICA: Riferimento al DailyGiftManager ---
    [Tooltip("Trascina qui l'oggetto che ha lo script DailyGiftManager")]
    public DailyGiftManager dailyGiftManager; 
    // -----------------------------------------------

    [Header("--- BILANCIAMENTO ---")]
    public double offlineProductionRatio = 0.5d;

    [Header("--- ENERGY BUTTON ---")]
    [SerializeField] private float energyButton_RampUpDuration = 7.0f;
    [SerializeField] private float energyButton_MaxMultiplier = 3.0f;
    [SerializeField] private float energyButton_MaxHoldDuration = 12.0f;
    [SerializeField] private float energyButton_RampDownDuration = 7.0f;
    [SerializeField] private float energyButton_CooldownMultiplier = 1.0f;

    [Header("--- SALVATAGGIO ---")]
    public float autoSaveInterval = 30f; 

    // --- VARIABILI DI GIOCO ---
    public BigDouble CurrentEnergy { get; private set; }
    public BigDouble LifetimeEarnings { get; private set; }
    public int EmitterCount { get; private set; } 
    public int LogisticsLevel { get; private set; }
    public BigDouble ScientificNodes { get; private set; } = 0;
    
    // --- CAPACITA' & LIMITI ---
    public BigDouble BaseEmissionPerUnit { get; private set; } = 0.01; 
    public double MaxOfflineSeconds { get; private set; } = 7200; 
    public BigDouble LogisticsCap { get; private set; } = 3;
    public int EmitterCap { get; private set; } = 1; 

    // --- MOLTIPLICATORI & BONUS ---
    public BigDouble ResearchMultiplier { get; set; } = 1;
    public BigDouble LogisticsResearchBonus { get; set; } = 0;
    public BigDouble StorageResearchBonus { get; set; } = 0; 
    public int EmitterCapResearchBonus { get; set; } = 0; 
    
    public double BaseAutoGrowthSpeed = 0.3; 
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
    public float CurrentEnergyMultiplier { get; private set; } = 1.0f;
    private float _energyButtonTimer = 0.0f;
    private float _timeSpentAtMax = 0.0f;
    private float _cooldownTimer = 0.0f;
    private float _rampDownStartMultiplier = 1.0f;
    private float _currentRampDownDuration = 0.0f;

    // FORMULE
    public BigDouble RawProductionRate 
    {
        get 
        {
            BigDouble baseProd = EmitterCount * BaseEmissionPerUnit;
            BigDouble planetMultiplier = PlanetManager.Instance?.GetCurrentPlanetData()?.productionMultiplier ?? 1;
            BigDouble multipliers = ResearchMultiplier * EarningsBonus * planetMultiplier;
            multipliers *= CurrentEnergyMultiplier;
            return baseProd * multipliers;
        }
    }

    public BigDouble RawStableProductionRate 
    {
        get 
        {
            BigDouble baseProd = EmitterCount * BaseEmissionPerUnit;
            BigDouble planetMultiplier = PlanetManager.Instance?.GetCurrentPlanetData()?.productionMultiplier ?? 1;
            BigDouble multipliers = ResearchMultiplier * EarningsBonus * planetMultiplier;
            return baseProd * multipliers;
        }
    }

    public BigDouble EffectiveIncomePerSec => BigDouble.Min(RawProductionRate, LogisticsCap);
    public BigDouble EffectiveStableIncomePerSec => BigDouble.Min(RawStableProductionRate, LogisticsCap);

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
        
        QualitySettings.vSyncCount = 0; 
        Application.targetFrameRate = 60; 
        
        InitializeGameState();
    }

    // --- MODIFICA: Registrazione evento SceneLoaded ---
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Questa funzione viene chiamata automaticamente ogni volta che cambia la scena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cerca il nuovo ResearchManager nella nuova scena
        targetResearchManager = FindFirstObjectByType<ResearchManager>();
        
        // Cerca i nuovi visuals del pianeta
        planetVisuals = FindFirstObjectByType<PlanetPopulationVisuals>();
        
        // Forza un refresh della UI
        OnEconomyUpdated?.Invoke();
    }
    // ------------------------------------------------

    private void Start()
    {
        if (activeTheme != null) ThemedUIElement.SetGlobalTheme(activeTheme);
        
        // Questo trova il manager della PRIMA scena
        if (targetResearchManager == null) targetResearchManager = FindFirstObjectByType<ResearchManager>();

        // --- MODIFICA: Inizializzazione DailyGiftManager ---
        if (dailyGiftManager == null) dailyGiftManager = FindFirstObjectByType<DailyGiftManager>();
        // --------------------------------------------------

        LoadGame(); 
        StartCoroutine(AutoSaveRoutine());
    }

    private void Update()
    {
        // HARD RESET
        if (Input.GetKeyDown(KeyCode.K) || Input.touchCount >= 4)
        {
            PerformFullHardReset();
            return;
        }

        UpdateEnergyButtonState();

        BigDouble income = EffectiveIncomePerSec;
        if (income > 0)
        {
            BigDouble amount = income * Time.deltaTime;
            CurrentEnergy += amount;
            LifetimeEarnings += amount;

            _uiRefreshTimer += Time.deltaTime;
            if (_uiRefreshTimer >= _uiRefreshRate)
            {
                OnEconomyUpdated?.Invoke(); 
                _uiRefreshTimer = 0f;
            }
        }

        // NANOBOT
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

        if (Input.GetKeyDown(KeyCode.N)) AddInstantEmitters(5);
    }

    private void UpdateEnergyButtonState()
    {
        float deltaTime = Time.deltaTime;
        switch (_energyButtonState)
        {
            case EnergyButtonState.RampingUp:
                _energyButtonTimer += deltaTime;
                CurrentEnergyMultiplier = Mathf.Lerp(1.0f, energyButton_MaxMultiplier, _energyButtonTimer / energyButton_RampUpDuration);
                if (_energyButtonTimer >= energyButton_RampUpDuration) {
                    CurrentEnergyMultiplier = energyButton_MaxMultiplier;
                    _energyButtonState = EnergyButtonState.HoldingMax;
                    _energyButtonTimer = 0.0f; 
                }
                break;
            case EnergyButtonState.HoldingMax:
                _energyButtonTimer += deltaTime;
                _timeSpentAtMax += deltaTime;
                if (_energyButtonTimer >= energyButton_MaxHoldDuration) {
                    _energyButtonState = EnergyButtonState.RampingDown;
                    _energyButtonTimer = 0.0f; 
                }
                break;
            case EnergyButtonState.RampingDown:
                _energyButtonTimer += deltaTime;
                if (_currentRampDownDuration > 0) {
                    float normalizedTime = _energyButtonTimer / _currentRampDownDuration;
                    CurrentEnergyMultiplier = Mathf.Lerp(_rampDownStartMultiplier, 1.0f, normalizedTime);
                } else { CurrentEnergyMultiplier = 1.0f; }
                if (_energyButtonTimer >= _currentRampDownDuration) {
                    CurrentEnergyMultiplier = 1.0f;
                    _energyButtonState = EnergyButtonState.Cooldown;
                    _cooldownTimer = _timeSpentAtMax * energyButton_CooldownMultiplier;
                    _timeSpentAtMax = 0; _energyButtonTimer = 0;
                }
                break;
            case EnergyButtonState.Cooldown:
                _cooldownTimer -= deltaTime;
                if (_cooldownTimer <= 0) _energyButtonState = EnergyButtonState.Idle;
                break;
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
        LogisticsCap = 5000 + (LogisticsLevel * 50) + LogisticsResearchBonus; 
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

        // NOTA: Qui usiamo targetResearchManager. 
        // Grazie al fix OnSceneLoaded, questo riferimento è sempre valido per la scena corrente.
        if (targetResearchManager != null)
        {
            foreach(var res in targetResearchManager.allResearches)
                res.currentLevel = 0;
            targetResearchManager.RecalculateAllResearches();
        }

        SaveGame(); 
        OnEconomyUpdated?.Invoke();
    }
    
    public void PerformPlanetChangeReset()
    {
        InitializeGameState();

        // Aggiorna il riferimento per sicurezza (se la scena è appena caricata)
        if (targetResearchManager == null) 
            targetResearchManager = FindFirstObjectByType<ResearchManager>();

        if (targetResearchManager != null)
        {
            foreach(var res in targetResearchManager.allResearches)
                res.currentLevel = 0;
            targetResearchManager.RecalculateAllResearches();
        }

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
        
        if (PlanetManager.Instance != null)
        {
            data.currentPlanetIndex = PlanetManager.Instance.currentPlanetIndex;
            data.isPreparingForLaunch = PlanetManager.Instance.isPreparingForLaunch;
            data.launchPreparationProgress = PlanetManager.Instance.launchPreparationProgress.ToString();
            data.lockedLaunchRequirement = PlanetManager.Instance.lockedLaunchRequirement.ToString();
            data.isTraveling = PlanetManager.Instance.isTraveling;
            data.travelStartTimeBinary = PlanetManager.Instance.travelStartTime.ToBinary().ToString();
        }

        if (planetVisuals != null)
            data.cityLightPositions = planetVisuals.GetEncodedPositions();
        
        // --- MODIFICA: Salvataggio Daily Gift ---
        if (dailyGiftManager != null)
        {
            dailyGiftManager.Save(data);
        }
        // ----------------------------------------

        if (targetResearchManager != null)
        {
            foreach (var item in targetResearchManager.allResearches)
            {
                if (item.currentLevel > 0) 
                    data.researches.Add(new ResearchSaveData { id = item.id, level = item.currentLevel });
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

            // --- MODIFICA: Inizializza Daily Gift per nuovo gioco ---
            if (dailyGiftManager != null) dailyGiftManager.Initialize(null);
            // --------------------------------------------------------

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
            
        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.currentPlanetIndex = data.currentPlanetIndex;
            PlanetManager.Instance.isPreparingForLaunch = data.isPreparingForLaunch;
            if (!string.IsNullOrEmpty(data.launchPreparationProgress))
                PlanetManager.Instance.launchPreparationProgress = BigDouble.Parse(data.launchPreparationProgress);
            
            if (!string.IsNullOrEmpty(data.lockedLaunchRequirement))
                PlanetManager.Instance.lockedLaunchRequirement = BigDouble.Parse(data.lockedLaunchRequirement);

            PlanetManager.Instance.isTraveling = data.isTraveling;
            if (!string.IsNullOrEmpty(data.travelStartTimeBinary))
            {
                long binaryTime = long.Parse(data.travelStartTimeBinary);
                PlanetManager.Instance.travelStartTime = DateTime.FromBinary(binaryTime);
            }
        }

        if (targetResearchManager != null)
            targetResearchManager.LoadResearchLevels(data.researches);

        if (planetVisuals != null && data.cityLightPositions != null)
            planetVisuals.LoadEncodedPositions(data.cityLightPositions);

        RecalculateCaps();
        
        // --- MODIFICA: Caricamento Daily Gift ---
        if (dailyGiftManager != null)
        {
            dailyGiftManager.Initialize(data);
        }
        // ----------------------------------------

        if (!string.IsNullOrEmpty(data.lastSaveTime))
            HandleOfflineProgress(data.lastSaveTime);

        OnEconomyUpdated?.Invoke();
    }
    
    private void HandleOfflineProgress(string lastSaveTimeStr)
    {
        if (PlanetManager.Instance != null && PlanetManager.Instance.isTraveling)
        {
            TimeSpan timeSinceTravelStart = DateTime.UtcNow - PlanetManager.Instance.travelStartTime;
            if (timeSinceTravelStart.TotalSeconds >= PlanetManager.TRAVEL_DURATION_SECONDS)
            {
                PlanetManager.Instance.CompleteTravel();
                return; 
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
            double actualSeconds = Math.Min(secondsAway, MaxOfflineSeconds);
            BigDouble actualEarnings = EffectiveIncomePerSec * actualSeconds * offlineProductionRatio;

            if (actualEarnings > 0) 
            {
                CurrentEnergy += actualEarnings;
                LifetimeEarnings += actualEarnings;
                LastOfflineEarnings = actualEarnings;
            }

            if (EmitterAutoGrowthSpeed > 0 && EmitterCount < EmitterCap)
            {
                double rawGrowth = EmitterAutoGrowthSpeed * actualSeconds * offlineProductionRatio;
                _emitterAccumulator += rawGrowth;
                int potentialGained = (int)_emitterAccumulator;

                if (potentialGained > 0)
                {
                    int spaceLeft = EmitterCap - EmitterCount;
                    int actualGained = Mathf.Min(potentialGained, spaceLeft);

                    if (actualGained > 0)
                    {
                        EmitterCount += actualGained;
                        RecalculateCaps(); 
                    }
                    if (EmitterCount >= EmitterCap) _emitterAccumulator = 0;
                    else _emitterAccumulator -= actualGained;
                }
            }
            LastOfflineTimeSpan = timeAway;
            OnOfflineProductionCalculated?.Invoke();
        }
    }

    IEnumerator AutoSaveRoutine()
    {
        while (true) { yield return new WaitForSeconds(autoSaveInterval); SaveGame(); }
    }
    
    private void OnApplicationQuit() => SaveGame();
    private void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGame(); }

    public void OnEnergyButtonPress()
    {
        if (_energyButtonState == EnergyButtonState.Idle) {
            _energyButtonState = EnergyButtonState.RampingUp;
            _energyButtonTimer = 0.0f;
        }
    }

    public void OnEnergyButtonRelease()
    {
        if (_energyButtonState == EnergyButtonState.RampingUp) {
            _rampDownStartMultiplier = CurrentEnergyMultiplier;
            float multiplierRatio = (_rampDownStartMultiplier - 1.0f) / (energyButton_MaxMultiplier - 1.0f);
            _currentRampDownDuration = energyButton_RampDownDuration * multiplierRatio;
            _energyButtonState = EnergyButtonState.RampingDown;
            _energyButtonTimer = 0.0f;
        }
        else if (_energyButtonState == EnergyButtonState.HoldingMax) {
            _rampDownStartMultiplier = energyButton_MaxMultiplier;
            _currentRampDownDuration = energyButton_RampDownDuration;
            _energyButtonState = EnergyButtonState.RampingDown;
            _energyButtonTimer = 0.0f;
        }
    }

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
        ScientificNodes = 0; LifetimeEarnings = 0; CurrentEnergy = 0;
        EmitterCount = 1; LogisticsLevel = 1; 
        if (targetResearchManager != null) {
            foreach (var item in targetResearchManager.allResearches) item.currentLevel = 0;
            targetResearchManager.RecalculateAllResearches();
        }
        if (planetVisuals != null) planetVisuals.ResetVisuals();
        InitializeGameState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}