// --- File: _Scripts\Managers\GameManager.cs ---
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

    // --- EVENTI ---
    public event Action OnEconomyUpdated;
    public event Action OnOfflineProductionCalculated;
    public event Action OnFirstInput; 

    [Header("--- COLLEGAMENTI ---")]
    public ResearchManager targetResearchManager; 
    public SpaceshipManager spaceshipManager; 
    public UITheme activeTheme; 
    
    public PlanetPopulationVisuals planetVisuals; 
    public GameObject[] emitters;

    public DailyGiftManager dailyGiftManager; 

    [Header("--- BILANCIAMENTO ---")]
    public double offlineProductionRatio = 0.5d;

    [Header("--- PRESTIGE SETTINGS ---")]
    public double prestigeDivisor = 1000000; 
    public double prestigePower = 0.15; 
    public double nodesBonusPerUnit = 0.50; 

    [Header("--- ENERGY BUTTON ---")]
    [SerializeField] private float energyButton_RampUpDuration = 7.0f;
    [SerializeField] private float energyButton_MaxMultiplier = 3.0f; 
    [SerializeField] private float energyButton_MaxHoldDuration = 12.0f;
    [SerializeField] private float energyButton_RampDownDuration = 7.0f;
    [SerializeField] private float energyButton_CooldownMultiplier = 1.0f;

    [Header("--- AUDIO ---")]
    [SerializeField] private AudioClip energyClickSound;

    [Header("--- SALVATAGGIO ---")]
    public float autoSaveInterval = 30f; 

    // --- VARIABILI DI GIOCO (RESETTABILI) ---
    public BigDouble CurrentEnergy { get; private set; }
    public BigDouble LifetimeEarnings { get; private set; }
    public int EmitterCount { get; private set; } 
    public int LogisticsLevel { get; private set; }
    
    // --- VARIABILI PERMANENTI (RESET QUANTISTICO) ---
    public BigDouble ScientificNodes { get; private set; } = 0;
    public BigDouble QuantumManipulators { get; private set; } = 0; 

    // --- VALUTE IRIDIO (PERSISTENTI) ---
    public BigDouble RawIridium { get; private set; } = 0;
    public BigDouble PureIridium { get; private set; } = 0;
    
    [Header("--- BILANCIAMENTO BASE ---")]
    public BigDouble baseEmissionPerUnit = 0.01; 
    public BigDouble BaseEmissionPerUnit => baseEmissionPerUnit;

    public BigDouble initialLogisticsCap = 10; 
    public double baseMaxOfflineSeconds = 7200;

    public BigDouble LogisticsCap { get; private set; }
    public double MaxOfflineSeconds { get; private set; }
    public int EmitterCap { get; private set; } = 1; 

    public BigDouble ResearchMultiplier { get; set; } = 1;
    public BigDouble LogisticsResearchBonus { get; set; } = 0;
    public BigDouble LogisticsMultiplier { get; set; } = 1; 
    public BigDouble StorageResearchBonus { get; set; } = 0; 
    public int EmitterCapResearchBonus { get; set; } = 0; 
    
    public float ClickPowerResearchBonus { get; set; } = 0.0f; 

    public double BaseAutoGrowthSpeed = 0.3; 
    public double EmitterSpeedResearchBonus { get; set; } = 0; 
    
    public double EmitterAutoGrowthSpeed 
    {
        get 
        {
            double theoryGrowthBonus = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.EmitterGrowthSpeed) : 0;
            return (BaseAutoGrowthSpeed + EmitterSpeedResearchBonus) * (1.0 + theoryGrowthBonus);
        }
    }
    
    private double _emitterAccumulator = 0; 

    public BigDouble EarningsBonus => 1 + (ScientificNodes * nodesBonusPerUnit); 

    public BigDouble LastOfflineEarnings { get; private set; } = 0;
    public int LastOfflineEmittersGained { get; private set; } = 0; 
    public TimeSpan LastOfflineTimeSpan { get; private set; }

    private float _uiRefreshTimer = 0f;
    private float _uiRefreshRate = 0.05f;

    public enum EnergyButtonState { Idle, RampingUp, HoldingMax, RampingDown, Cooldown }
    private EnergyButtonState _energyButtonState = EnergyButtonState.Idle;
    public EnergyButtonState CurrentEnergyButtonState => _energyButtonState;
    
    public float CurrentEnergyMultiplier { get; private set; } = 1.0f;
    
    public float EnergyButtonFillAmount { get; private set; } = 1f;
    private float _energyButtonTimer = 0.0f;
    private float _timeSpentAtMax = 0.0f;
    private float _cooldownTimer = 0.0f;
    private float _rampDownStartMultiplier = 1.0f;
    private float _currentRampDownDuration = 0.0f;
    private float _fillAtRelease = 1f;
    private float _totalRecoveryTime = 1f;

    public string StoredLaunchSitePosition { get; set; } = "";
    public float StoredSunRotation { get; set; } = 0f;
    public double PendingOfflineSeconds { get; set; } = 0f;
    public bool IsFirstSession { get; set; } = true; 

    public BigDouble RawPassiveProduction
    {
        get
        {
            BigDouble baseProd = EmitterCount * BaseEmissionPerUnit;
            BigDouble planetMultiplier = PlanetManager.Instance?.GetCurrentPlanetData()?.productionMultiplier ?? 1;
            double theoryIncomeBonus = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.GlobalIncome) : 0;
            BigDouble finalMultiplier = ResearchMultiplier * EarningsBonus * planetMultiplier * (1.0 + theoryIncomeBonus);
            return baseProd * finalMultiplier;
        }
    }

    public BigDouble EffectiveIncomePerSec
    {
        get
        {
            BigDouble cappedPassive = BigDouble.Min(RawPassiveProduction, LogisticsCap);
            return cappedPassive * CurrentEnergyMultiplier;
        }
    }

    public BigDouble EffectiveStableIncomePerSec
    {
        get
        {
            return BigDouble.Min(RawPassiveProduction, LogisticsCap);
        }
    }

    public BigDouble PotentialProductionWithButton
    {
        get
        {
            return RawPassiveProduction * CurrentEnergyMultiplier;
        }
    }

    public BigDouble RawProductionRate => PotentialProductionWithButton;
    public float EffectiveMaxMultiplier => energyButton_MaxMultiplier + ClickPowerResearchBonus;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(transform.root.gameObject);
            InitializeGameState();
        }
        else 
        { 
            Destroy(transform.root.gameObject); 
            return; 
        }
        
        QualitySettings.vSyncCount = 0; 
        Application.targetFrameRate = 60; 
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (targetResearchManager == null) targetResearchManager = FindFirstObjectByType<ResearchManager>();
        if (spaceshipManager == null) spaceshipManager = FindFirstObjectByType<SpaceshipManager>();
        
        planetVisuals = FindFirstObjectByType<PlanetPopulationVisuals>();
        
        OnEconomyUpdated?.Invoke();
    }

    private void Start()
    {
        if (activeTheme != null) ThemedUIElement.SetGlobalTheme(activeTheme);
        
        if (targetResearchManager == null) targetResearchManager = ResearchManager.Instance;
        if (spaceshipManager == null) spaceshipManager = SpaceshipManager.Instance;
        if (dailyGiftManager == null) dailyGiftManager = FindFirstObjectByType<DailyGiftManager>();

        LoadGame(); 
        StartCoroutine(AutoSaveRoutine());
    }

    private void Update()
    {
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

        double currentGrowthSpeed = EmitterAutoGrowthSpeed;

        if (currentGrowthSpeed > 0 && EmitterCount > 0 && EmitterCount < EmitterCap)
        {
            _emitterAccumulator += currentGrowthSpeed * Time.deltaTime;
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

    private void UpdateEnergyButtonState()
    {
        float deltaTime = Time.deltaTime;
        float targetMax = EffectiveMaxMultiplier;
        float maxHoldDuration = energyButton_RampUpDuration + energyButton_MaxHoldDuration;

        switch (_energyButtonState)
        {
            case EnergyButtonState.RampingUp:
                _energyButtonTimer += deltaTime;
                
                float rampUpSpeed = (targetMax - 1.0f) / energyButton_RampUpDuration;
                CurrentEnergyMultiplier = Mathf.MoveTowards(CurrentEnergyMultiplier, targetMax, rampUpSpeed * deltaTime);
                
                EnergyButtonFillAmount = 1f - (_energyButtonTimer / maxHoldDuration);
                
                if (CurrentEnergyMultiplier >= targetMax && _energyButtonTimer >= energyButton_RampUpDuration) 
                {
                    _energyButtonState = EnergyButtonState.HoldingMax;
                    _energyButtonTimer = _energyButtonTimer - energyButton_RampUpDuration; 
                }

                if (_energyButtonTimer >= maxHoldDuration)
                {
                    OnEnergyButtonRelease();
                }
                break;

            case EnergyButtonState.HoldingMax:
                _energyButtonTimer += deltaTime;
                _timeSpentAtMax += deltaTime;
                CurrentEnergyMultiplier = targetMax;
                
                EnergyButtonFillAmount = 1f - ((energyButton_RampUpDuration + _energyButtonTimer) / maxHoldDuration);

                if (_energyButtonTimer >= energyButton_MaxHoldDuration) {
                    OnEnergyButtonRelease(); 
                }
                break;

            case EnergyButtonState.RampingDown:
                _energyButtonTimer += deltaTime;
                if (_currentRampDownDuration > 0) {
                    float normalizedTime = _energyButtonTimer / _currentRampDownDuration;
                    CurrentEnergyMultiplier = Mathf.Lerp(_rampDownStartMultiplier, 1.0f, normalizedTime);
                    
                    float recoveryProgress = _energyButtonTimer / Mathf.Max(0.01f, _totalRecoveryTime);
                    EnergyButtonFillAmount = Mathf.Lerp(_fillAtRelease, 1f, recoveryProgress);
                } else { 
                    CurrentEnergyMultiplier = 1.0f; 
                }

                if (_energyButtonTimer >= _currentRampDownDuration) {
                    CurrentEnergyMultiplier = 1.0f;
                    _energyButtonState = EnergyButtonState.Cooldown;
                    _cooldownTimer = _timeSpentAtMax * energyButton_CooldownMultiplier;
                    _timeSpentAtMax = 0; 
                    _energyButtonTimer = 0;
                }
                break;

            case EnergyButtonState.Cooldown:
                _cooldownTimer -= deltaTime;
                
                float expectedCooldown = _totalRecoveryTime - _currentRampDownDuration;
                float currentRecovery = _currentRampDownDuration + (expectedCooldown - _cooldownTimer);
                float progress = currentRecovery / Mathf.Max(0.01f, _totalRecoveryTime);
                EnergyButtonFillAmount = Mathf.Lerp(_fillAtRelease, 1f, progress);

                if (_cooldownTimer <= 0) {
                    _energyButtonState = EnergyButtonState.Idle;
                    EnergyButtonFillAmount = 1f; 
                }
                break;
        }
    }

    public BigDouble CalculatePotentialNodes()
    {
        if (LifetimeEarnings < prestigeDivisor) return 0;
        BigDouble normalizedEarnings = LifetimeEarnings / prestigeDivisor;
        BigDouble nodes = BigDouble.Pow(normalizedEarnings, prestigePower);
        
        // --- APPLICAZIONE BONUS TEORIA ---
        double theoryNodeBonus = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.ScientificNodesGain) : 0;
        nodes *= (1.0 + theoryNodeBonus);

        return BigDouble.Floor(nodes);        
    }

    public void RecalculateCaps()
    {
        double theoryStorageBonus = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.StorageCapacity) : 0;
        
        BigDouble planetMultiplier = PlanetManager.Instance?.GetCurrentPlanetData()?.productionMultiplier ?? 1;
        BigDouble baseLogistics = initialLogisticsCap + (LogisticsLevel * 5) + LogisticsResearchBonus;
        
        double theoryLogisticsBonus = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.LogisticsCap) : 0;
        LogisticsCap = baseLogistics * LogisticsMultiplier * planetMultiplier * (1.0 + theoryLogisticsBonus);

        double researchSeconds = StorageResearchBonus.ToDouble() * 1800;
        MaxOfflineSeconds = (baseMaxOfflineSeconds + researchSeconds) * (1.0 + theoryStorageBonus); 
        
        EmitterCap = 1 + EmitterCapResearchBonus;
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
        Debug.Log($"[GameManager] Quantum Reset. Nodes gained: {nodesToGain}. New Total: {ScientificNodes}");

        if (ShipTerminalController.Instance != null)
        {
            ShipTerminalController.Instance.ShowSystemMessage("ANOMALIA CRONALE RILEVATA. INIZIO SEQUENZA DI RIAVVOLGIMENTO.");
        }

        var rewindEffect = FindFirstObjectByType<QuantumEffectManager>();
        
        if (rewindEffect != null)
        {
            rewindEffect.PlayRewindEffect(
                onTriggerFade: () => { StartFadeSequence(); },
                onAnimationComplete: () => { ExecuteResetAndLoad(); }
            );
        }
        else
        {
            StartFadeSequence();
            StartCoroutine(DelayedResetFallback(1.0f));
        }
    }

    private void StartFadeSequence()
    {
        string targetSceneName = SceneManager.GetActiveScene().name; 
        string loadingText = "QUANTUM REBIRTH";
        Sprite loadingIcon = null;

        if (PlanetManager.Instance != null && PlanetManager.Instance.planets.Count > 0)
        {
            PlanetData firstPlanet = PlanetManager.Instance.planets[0];
            targetSceneName = firstPlanet.sceneName;
            loadingText = firstPlanet.planetName; 
            loadingIcon = firstPlanet.planetIcon; 
        }

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.SetLoadingInfo(loadingText, loadingIcon);
            SceneFader.Instance.FadeAndLoadScene(targetSceneName, null); 
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private void ExecuteResetAndLoad()
    {
        InitializeGameState(); 

        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.currentPlanetIndex = 0;
            PlanetManager.Instance.isPreparingForLaunch = false;
            PlanetManager.Instance.isTraveling = false;
            PlanetManager.Instance.launchPreparationProgress = 0;
            PlanetManager.Instance.currentLockedDuration = 0;
            PlanetManager.Instance.pendingLanding = false; 
        }

        if (planetVisuals != null) planetVisuals.ResetVisuals();

        if (targetResearchManager != null)
        {
            foreach(var res in targetResearchManager.allResearches)
                res.currentLevel = 0;
            targetResearchManager.RecalculateAllResearches();
        }

        if (spaceshipManager != null)
        {
            foreach(var ship in spaceshipManager.fleet)
                ship.currentLevel = 0;
        }

        SaveGame(); 
        OnEconomyUpdated?.Invoke();
    }

    private IEnumerator DelayedResetFallback(float delay)
    {
        yield return new WaitForSeconds(delay);
        ExecuteResetAndLoad();
    }

    public void PerformPlanetChangeReset()
    {
        InitializeGameState();

        if (targetResearchManager == null) targetResearchManager = ResearchManager.Instance;
        if (spaceshipManager == null) spaceshipManager = SpaceshipManager.Instance;

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
        EmitterCount = 0; 
        LogisticsLevel = 0; 
        ResearchMultiplier = 1;
        LogisticsResearchBonus = 0;
        LogisticsMultiplier = 1; 
        StorageResearchBonus = 0;
        EmitterCapResearchBonus = 0;
        ClickPowerResearchBonus = 0;
        EmitterSpeedResearchBonus = 0; 
        _emitterAccumulator = 0;
        
        LastOfflineEmittersGained = 0; 
        StoredLaunchSitePosition = ""; 
        StoredSunRotation = 0f; 

        if (baseEmissionPerUnit <= 0) baseEmissionPerUnit = 0.01;
        
        RecalculateCaps();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.isFirstSession = IsFirstSession;
        data.currentEnergy = CurrentEnergy.ToString();
        data.lifetimeEarnings = LifetimeEarnings.ToString();
        data.emitterCount = EmitterCount;
        data.logisticsLevel = LogisticsLevel;
        data.lastSaveTime = DateTime.UtcNow.ToBinary().ToString(); 
        
        data.scientificNodes = ScientificNodes.ToString();
        data.rawIridium = RawIridium.ToString();
        data.pureIridium = PureIridium.ToString();
        data.quantumManipulators = QuantumManipulators.ToString();

        if (PlanetSunRotator.Instance != null)
        {
            StoredSunRotation = PlanetSunRotator.Instance.GetCurrentYRotation();
        }
        data.sunRotationY = StoredSunRotation;
        
        if (PlanetManager.Instance != null)
        {
            data.currentPlanetIndex = PlanetManager.Instance.currentPlanetIndex;
            data.isPreparingForLaunch = PlanetManager.Instance.isPreparingForLaunch;
            data.launchPreparationProgress = PlanetManager.Instance.launchPreparationProgress.ToString();
            data.lockedLaunchRequirement = PlanetManager.Instance.lockedLaunchRequirement.ToString();
            data.isTraveling = PlanetManager.Instance.isTraveling;
            data.travelStartTimeBinary = PlanetManager.Instance.travelStartTime.ToBinary().ToString();
            data.lockedTravelDuration = PlanetManager.Instance.currentLockedDuration;
        }

        if (planetVisuals != null)
            data.cityLightPositions = planetVisuals.GetEncodedPositions();
        
        if (dailyGiftManager != null) dailyGiftManager.Save(data);

        if (targetResearchManager != null)
        {
            foreach (var item in targetResearchManager.allResearches)
            {
                if (item.currentLevel > 0) 
                    data.researches.Add(new ResearchSaveData { id = item.id, level = item.currentLevel });
            }
        }

        if (spaceshipManager != null)
        {
            foreach (var item in spaceshipManager.fleet)
            {
                if (item.currentLevel > 0)
                    data.spaceships.Add(new ResearchSaveData { id = item.info.id, level = item.currentLevel });
            }
        }

        data.launchSitePosition = StoredLaunchSitePosition;

        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.SaveData(data);
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
            QuantumManipulators = 0; 
            RawIridium = 0; 
            PureIridium = 0; 
            StoredSunRotation = 0f;
            if (dailyGiftManager != null) dailyGiftManager.Initialize(null);
            StoredLaunchSitePosition = ""; 
            IsFirstSession = true; 
            return; 
        }

        IsFirstSession = data.isFirstSession; 

        if (!string.IsNullOrEmpty(data.currentEnergy)) CurrentEnergy = BigDouble.Parse(data.currentEnergy);
        if (!string.IsNullOrEmpty(data.lifetimeEarnings)) LifetimeEarnings = BigDouble.Parse(data.lifetimeEarnings);

        EmitterCount = data.emitterCount; 
        
        if (IsFirstSession)
        {
            if (EmitterCount > 0 || LifetimeEarnings > 0)
            {
                IsFirstSession = false;
            }
        }
        
        LogisticsLevel = data.logisticsLevel;

        ScientificNodes = !string.IsNullOrEmpty(data.scientificNodes) ? BigDouble.Parse(data.scientificNodes) : 0;
        RawIridium = !string.IsNullOrEmpty(data.rawIridium) ? BigDouble.Parse(data.rawIridium) : 0;
        PureIridium = !string.IsNullOrEmpty(data.pureIridium) ? BigDouble.Parse(data.pureIridium) : 0;
        QuantumManipulators = !string.IsNullOrEmpty(data.quantumManipulators) ? BigDouble.Parse(data.quantumManipulators) : 0;
        
        StoredSunRotation = data.sunRotationY;

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

            PlanetManager.Instance.currentLockedDuration = data.lockedTravelDuration;

            PlanetData savedPlanet = PlanetManager.Instance.GetCurrentPlanetData();
            if (savedPlanet != null)
            {
                string currentSceneName = SceneManager.GetActiveScene().name;
                if (!PlanetManager.Instance.isTraveling && currentSceneName != savedPlanet.sceneName)
                {
                    SceneManager.LoadScene(savedPlanet.sceneName);
                }
            }
        }

        if (targetResearchManager != null)
            targetResearchManager.LoadResearchLevels(data.researches);

        if (spaceshipManager != null)
            spaceshipManager.LoadFleetLevels(data.spaceships);

        if (planetVisuals != null && data.cityLightPositions != null)
            planetVisuals.LoadEncodedPositions(data.cityLightPositions);

        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.LoadData(data);
        }

        RecalculateCaps();
        
        if (dailyGiftManager != null) dailyGiftManager.Initialize(data);

        if (!string.IsNullOrEmpty(data.launchSitePosition))
            StoredLaunchSitePosition = data.launchSitePosition;
        else
            StoredLaunchSitePosition = "";

        if (!IsFirstSession && !string.IsNullOrEmpty(data.lastSaveTime))
            HandleOfflineProgress(data.lastSaveTime);

        OnEconomyUpdated?.Invoke();
    }
    
    private void HandleOfflineProgress(string lastSaveTimeStr)
    {
        LastOfflineEmittersGained = 0;

        if (PlanetManager.Instance != null && PlanetManager.Instance.isTraveling)
        {
            TimeSpan timeSinceTravelStart = DateTime.UtcNow - PlanetManager.Instance.travelStartTime;
            
            if (timeSinceTravelStart.TotalSeconds >= PlanetManager.Instance.GetTotalTravelDuration())
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
        PendingOfflineSeconds = secondsAway;

        if (secondsAway > 1) 
        {
            double actualSeconds = Math.Min(secondsAway, MaxOfflineSeconds);

            // --- APPLICAZIONE BONUS TEORIA OFFLINE ---
            double theoryOfflineBonus = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.OfflineProduction) : 0;
            
            BigDouble actualEarnings = EffectiveStableIncomePerSec * actualSeconds * offlineProductionRatio * (1.0 + theoryOfflineBonus);

            if (actualEarnings > 0) 
            {
                CurrentEnergy += actualEarnings;
                LifetimeEarnings += actualEarnings;
                LastOfflineEarnings = actualEarnings;
            }

            double offlineGrowthSpeed = EmitterAutoGrowthSpeed;
            
            if (offlineGrowthSpeed > 0 && EmitterCount > 0 && EmitterCount < EmitterCap)
            {
                double rawGrowth = offlineGrowthSpeed * actualSeconds * offlineProductionRatio * (1.0 + theoryOfflineBonus);
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

                        LastOfflineEmittersGained = actualGained;

                        if (planetVisuals != null) planetVisuals.RefreshLights();
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
        if (_energyButtonState == EnergyButtonState.Idle || 
            _energyButtonState == EnergyButtonState.RampingDown || 
            _energyButtonState == EnergyButtonState.Cooldown) 
        {
            float maxHoldDuration = energyButton_RampUpDuration + energyButton_MaxHoldDuration;
            
            float virtualTimeSpent = (1.0f - EnergyButtonFillAmount) * maxHoldDuration;

            _energyButtonState = EnergyButtonState.RampingUp;
            _energyButtonTimer = virtualTimeSpent;
        }

        if (AudioManager.Instance != null && energyClickSound != null)
        {
            AudioManager.Instance.PlaySFX(energyClickSound, 1.0f, 0.05f);
        }

        if (IsFirstSession)
        {
            OnFirstInput?.Invoke();
        }

        if (EmitterCount == 0 && PlanetManager.Instance != null && !PlanetManager.Instance.pendingLanding)
        {
             AddInstantEmitters(1);
             SaveGame();
        }
    }

    public void OnEnergyButtonRelease()
    {
        if (_energyButtonState == EnergyButtonState.Idle || 
            _energyButtonState == EnergyButtonState.RampingDown || 
            _energyButtonState == EnergyButtonState.Cooldown) return;

        float maxMult = EffectiveMaxMultiplier;

        if (_energyButtonState == EnergyButtonState.RampingUp) {
            _rampDownStartMultiplier = CurrentEnergyMultiplier;
            float multiplierRatio = (_rampDownStartMultiplier - 1.0f) / (maxMult - 1.0f);
            _currentRampDownDuration = energyButton_RampDownDuration * multiplierRatio;
        }
        else if (_energyButtonState == EnergyButtonState.HoldingMax) {
            _rampDownStartMultiplier = maxMult;
            _currentRampDownDuration = energyButton_RampDownDuration;
        }

        _fillAtRelease = EnergyButtonFillAmount;
        _totalRecoveryTime = _currentRampDownDuration + (_timeSpentAtMax * energyButton_CooldownMultiplier);

        _energyButtonState = EnergyButtonState.RampingDown;
        _energyButtonTimer = 0.0f;
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

    public void AddRawIridium(BigDouble amount)
    {
        RawIridium += amount;
        OnEconomyUpdated?.Invoke();
    }

    public void AddPureIridium(BigDouble amount)
    {
        PureIridium += amount;
        SaveGame(); 
        OnEconomyUpdated?.Invoke();
    }
    
    public void AddQuantumManipulator(BigDouble amount)
    {
        QuantumManipulators += amount;
        SaveGame();
        OnEconomyUpdated?.Invoke();
    }

    public bool TrySpendPureIridium(BigDouble amount)
    {
        if (PureIridium >= amount)
        {
            PureIridium -= amount;
            SaveGame(); 
            OnEconomyUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public void ConvertRawToPure(BigDouble rawAmount, double conversionRate)
    {
        if (RawIridium >= rawAmount)
        {
            RawIridium -= rawAmount;
            BigDouble pureGained = BigDouble.Floor(rawAmount * conversionRate);
            PureIridium += pureGained;
            SaveGame();
            OnEconomyUpdated?.Invoke();
        }
    }

    public void PerformFullHardReset()
    {
        SaveManager.DeleteSaveFile();
        
        ScientificNodes = 0; 
        QuantumManipulators = 0; 
        LifetimeEarnings = 0; 
        CurrentEnergy = 0; 
        RawIridium = 0; 
        PureIridium = 0; 
        EmitterCount = 0; 
        LogisticsLevel = 0; 
        StoredLaunchSitePosition = "";
        StoredSunRotation = 0f;
        IsFirstSession = true; 
        
        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.currentPlanetIndex = 0;
            PlanetManager.Instance.isPreparingForLaunch = false;
            PlanetManager.Instance.isTraveling = false;
            PlanetManager.Instance.currentLockedDuration = 0; 
        }

        if (targetResearchManager != null) {
            foreach (var item in targetResearchManager.allResearches) item.currentLevel = 0;
            targetResearchManager.RecalculateAllResearches();
        }

        if (spaceshipManager != null) {
            foreach (var item in spaceshipManager.fleet) item.currentLevel = 0;
        }

        if (planetVisuals != null) planetVisuals.ResetVisuals();
        
        InitializeGameState();
        
        if (PlanetManager.Instance != null && PlanetManager.Instance.planets.Count > 0)
             SceneManager.LoadScene(PlanetManager.Instance.planets[0].sceneName);
        else
             SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OverrideEmitterCount(int value)
    {
        EmitterCount = value;
        RecalculateCaps();
    }

    public float GetEmitterGrowthProgress()
    {
        if (EmitterCount >= EmitterCap) return 0f;
        return (float)_emitterAccumulator; 
    }
}