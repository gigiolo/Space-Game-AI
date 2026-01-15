using UnityEngine;
using System; 
using System.Collections.Generic;
using BreakInfinity;

public class PlanetManager : MonoBehaviour
{
    public static PlanetManager Instance { get; private set; }

    [Header("Planet Configuration")]
    [Tooltip("The list of all planets available in the game, in order of progression.")]
    public List<PlanetData> planets;

    [HideInInspector]
    public int currentPlanetIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Important for scene changes
        }
    }

    private void Start()
    {
        // Logic to load the current planet will be added here later
        // For now, we assume we always start on the first planet (index 0)
    }

    public PlanetData GetCurrentPlanetData()
    {
        if (planets != null && planets.Count > currentPlanetIndex)
        {
            return planets[currentPlanetIndex];
        }
        Debug.LogError("Planet data for the current index is not available.");
        return null;
    }

    public BigDouble CalculatePlanetValue()
    {
        if (GameManager.Instance == null) return 0;

        // CORREZIONE: Usiamo EffectiveIncomePerSec invece di TotalEnergyPerSecond
        BigDouble currentEnergyProduction = GameManager.Instance.EffectiveIncomePerSec;
        
        // Assumiamo che emitters sia un array nel GameManager, altrimenti usiamo EmitterCount
        BigDouble maxEmitters = 0;
        if (GameManager.Instance.emitters != null)
             maxEmitters = GameManager.Instance.emitters.Length;
        else
             maxEmitters = GameManager.Instance.EmitterCap; // Fallback se l'array non è assegnato

        BigDouble balanceFactor = GetCurrentPlanetData()?.balanceFactor ?? 1;

        if (maxEmitters == 0) return 0;

        return currentEnergyProduction * maxEmitters * balanceFactor;
    }

    // --- TRAVEL STATE ---
    [HideInInspector] public bool isPreparingForLaunch = false;
    [HideInInspector] public BigDouble launchPreparationProgress = 0;
    [HideInInspector] public bool isTraveling = false;
    [HideInInspector] public DateTime travelStartTime;

    // --- BALANCE ---
    public const float TRAVEL_DURATION_SECONDS = 3600; // 1 hour

    private void Update()
    {
        if (isPreparingForLaunch)
        {
            UpdateLaunchPreparation();
        }

        if (isTraveling)
        {
            UpdateTravel();
        }
    }

    public BigDouble GetLaunchEnergyRequirement()
    {
        if (GameManager.Instance == null) return 0;
        // Requires 60 seconds of max production
        // CORREZIONE: Usiamo EffectiveIncomePerSec
        return GameManager.Instance.EffectiveIncomePerSec * 60;
    }

    private void UpdateLaunchPreparation()
    {
        BigDouble energyRequirement = GetLaunchEnergyRequirement();
        if (energyRequirement <= 0) return;

        // Consume energy up to the max needed for preparation
        // CORREZIONE: Usiamo EffectiveIncomePerSec
        BigDouble energyToConsume = GameManager.Instance.EffectiveIncomePerSec * Time.deltaTime;
        BigDouble remainingEnergy = energyRequirement - launchPreparationProgress;
        
        energyToConsume = BigDouble.Min(energyToConsume, remainingEnergy);
        energyToConsume = BigDouble.Min(energyToConsume, GameManager.Instance.CurrentEnergy);

        if (GameManager.Instance.TrySpend(energyToConsume))
        {
            launchPreparationProgress += energyToConsume;
        }

        if (launchPreparationProgress >= energyRequirement)
        {
            isPreparingForLaunch = false;
            // Preparation is complete, player can now start the travel.
        }
    }

    private void UpdateTravel()
    {
        TimeSpan travelTime = DateTime.UtcNow - travelStartTime;
        if (travelTime.TotalSeconds >= TRAVEL_DURATION_SECONDS)
        {
            CompleteTravel();
        }
    }

    public void StartLaunchPreparation()
    {
        if (isPreparingForLaunch || isTraveling) return;

        PlanetData currentPlanet = GetCurrentPlanetData();
        if (currentPlanet == null || CalculatePlanetValue() < currentPlanet.requiredPlanetValue)
        {
            Debug.LogWarning("Cannot start launch preparation: Planet Value not met.");
            return;
        }

        isPreparingForLaunch = true;
        launchPreparationProgress = 0;
    }

    public void StartInterplanetaryTravel()
    {
        if (isTraveling || isPreparingForLaunch) return;

        isTraveling = true;
        travelStartTime = DateTime.UtcNow;
        GameManager.Instance.SaveGame(); // Save progress immediately
    }

    public void CompleteTravel()
    {
        isTraveling = false;
        currentPlanetIndex++;

        if (currentPlanetIndex >= planets.Count)
        {
            Debug.LogError("Travel completed, but no more planets are available!");
            currentPlanetIndex = planets.Count - 1;
            return;
        }

        // Load the new planet's scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(GetCurrentPlanetData().sceneName);

        // Perform the reset
        GameManager.Instance.PerformPlanetChangeReset();

        // Save the new state
        GameManager.Instance.SaveGame();
    }
}