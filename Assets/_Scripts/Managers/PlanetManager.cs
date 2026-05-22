// --- File: _Scripts\Managers\PlanetManager.cs ---
using UnityEngine;
using System; 
using System.Collections.Generic;
using BreakInfinity;
using UnityEngine.SceneManagement; 

public class PlanetManager : MonoBehaviour
{
    public static PlanetManager Instance { get; private set; }

    public event Action OnLaunchPrepStarted;
    public event Action OnTravelStarted;

    [Header("Planet Configuration")]
    public List<PlanetData> planets;

    [HideInInspector] public int currentPlanetIndex = 0;

    [HideInInspector] public bool isPreparingForLaunch = false;
    [HideInInspector] public BigDouble launchPreparationProgress = 0;
    [HideInInspector] public BigDouble lockedLaunchRequirement = 0; 

    [HideInInspector] public bool isTraveling = false;
    [HideInInspector] public DateTime travelStartTime;

    [HideInInspector] public double currentLockedDuration = 0f;
    [HideInInspector] public bool pendingLanding = false; 

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        UpdatePlanetMusic();
    }

    private void UpdatePlanetMusic()
    {
        var currentData = GetCurrentPlanetData();
        if (currentData != null && AudioManager.Instance != null && currentData.planetThemeMusic != null)
        {
            AudioManager.Instance.PlayMusic(currentData.planetThemeMusic);
        }
    }

    private void Update()
    {
        if (isPreparingForLaunch) UpdateLaunchPreparation();
        if (isTraveling) UpdateTravel();
    }

    public PlanetData GetCurrentPlanetData()
    {
        if (planets != null && planets.Count > currentPlanetIndex) return planets[currentPlanetIndex];
        return null;
    }

    public PlanetData GetNextPlanetData()
    {
        if (planets != null && planets.Count > currentPlanetIndex + 1) return planets[currentPlanetIndex + 1];
        return null;
    }

    private double CalculateTheoreticalDuration()
    {
        PlanetData destination = GetNextPlanetData();
        if (destination == null || destination.travelDistance <= 0) return 3.0f; 

        BigDouble speed = 0;
        if (SpaceshipManager.Instance != null) speed = SpaceshipManager.Instance.GetTotalSpaceshipSpeed();
        if (speed <= 0) speed = 10; 

        BigDouble durationBig = destination.travelDistance / speed;
        return durationBig.ToDouble(); 
    }

    public double GetTotalTravelDuration()
    {
        if (isTraveling && currentLockedDuration > 0) return currentLockedDuration;
        return CalculateTheoreticalDuration();
    }

    public BigDouble CalculatePlanetValue()
    {
        if (GameManager.Instance == null) return 0;
        BigDouble currentEnergyProduction = GameManager.Instance.EffectiveStableIncomePerSec;
        BigDouble maxEmitters = GameManager.Instance.EmitterCap;
        if (maxEmitters <= 0) maxEmitters = 1;
        BigDouble balanceFactor = GetCurrentPlanetData()?.balanceFactor ?? 1;
        if (balanceFactor <= 0) balanceFactor = 1;

        return currentEnergyProduction * maxEmitters * balanceFactor;
    }

    public BigDouble GetLaunchEnergyRequirement()
    {
        if (GameManager.Instance == null) return 0;

        if (isPreparingForLaunch && lockedLaunchRequirement > 0)
        {
            return lockedLaunchRequirement;
        }

        BigDouble baseReq = GameManager.Instance.EffectiveStableIncomePerSec * 60;
        
        // --- APPLICAZIONE BONUS TEORIA: Sconto Veicoli e Viaggi ---
        double discount = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.SpacecraftAndTravelDiscount) : 0;
        discount = System.Math.Min(discount, 0.99);

        return baseReq * (1.0 - discount);
    }

    private void UpdateLaunchPreparation()
    {
        BigDouble energyRequirement = GetLaunchEnergyRequirement();
        
        if (energyRequirement <= 0) 
        {
            isPreparingForLaunch = false;
            return;
        }

        BigDouble energyToConsume = GameManager.Instance.EffectiveIncomePerSec * Time.deltaTime;
        BigDouble remainingEnergy = energyRequirement - launchPreparationProgress;
        
        if (energyToConsume > remainingEnergy) energyToConsume = remainingEnergy;
        energyToConsume = BigDouble.Min(energyToConsume, GameManager.Instance.CurrentEnergy);

        if (GameManager.Instance.TrySpend(energyToConsume))
        {
            launchPreparationProgress += energyToConsume;
        }

        if (launchPreparationProgress >= energyRequirement * 0.9999f) 
        {
            launchPreparationProgress = energyRequirement;
            isPreparingForLaunch = false;
            lockedLaunchRequirement = 0; 
        }
    }

    private void UpdateTravel()
    {
        TimeSpan travelTime = DateTime.UtcNow - travelStartTime;
        if (travelTime.TotalSeconds >= GetTotalTravelDuration()) CompleteTravel();
    }

    public void StartLaunchPreparation()
    {
        if (isPreparingForLaunch || isTraveling) return;

        PlanetData currentPlanet = GetCurrentPlanetData();
        if (currentPlanet == null || CalculatePlanetValue() < currentPlanet.requiredPlanetValue) return;

        isPreparingForLaunch = true;
        launchPreparationProgress = 0;
        
        lockedLaunchRequirement = GetLaunchEnergyRequirement();
        if (lockedLaunchRequirement <= 0) lockedLaunchRequirement = 100;
        
        GameManager.Instance.SaveGame();
        OnLaunchPrepStarted?.Invoke();
    }

    public void StartInterplanetaryTravel()
    {
        if (isTraveling || isPreparingForLaunch) return;

        isTraveling = true;
        currentLockedDuration = CalculateTheoreticalDuration();
        travelStartTime = DateTime.UtcNow;
        GameManager.Instance.SaveGame();
        OnTravelStarted?.Invoke();
    }

    public void CompleteTravel()
    {
        isTraveling = false;
        currentLockedDuration = 0; 
        isPreparingForLaunch = false;
        launchPreparationProgress = 0;
        lockedLaunchRequirement = 0;
        currentPlanetIndex++;
        UpdatePlanetMusic();
        pendingLanding = true;

        if (planets == null || currentPlanetIndex >= planets.Count)
        {
            currentPlanetIndex = (planets != null) ? planets.Count - 1 : 0;
            return;
        }

        PlanetData nextPlanet = GetCurrentPlanetData();
        string nextSceneName = nextPlanet.sceneName;

        GameManager.Instance.PerformPlanetChangeReset();
        GameManager.Instance.OverrideEmitterCount(0);

        if (GameManager.Instance.planetVisuals != null) GameManager.Instance.planetVisuals.ResetVisuals();
        
        GameManager.Instance.SaveGame();

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.SetLoadingInfo(nextPlanet.planetName, nextPlanet.planetIcon);
            SceneFader.Instance.FadeAndLoadScene(nextSceneName, null);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}