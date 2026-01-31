using UnityEngine;
using System; 
using System.Collections.Generic;
using BreakInfinity;
using UnityEngine.SceneManagement; // Necessario per il caricamento diretto (fallback)

public class PlanetManager : MonoBehaviour
{
    public static PlanetManager Instance { get; private set; }

    // --- EVENTI ---
    public event Action OnLaunchPrepStarted;
    public event Action OnTravelStarted;

    [Header("Planet Configuration")]
    [Tooltip("The list of all planets available in the game, in order of progression.")]
    public List<PlanetData> planets;

    [HideInInspector]
    public int currentPlanetIndex = 0;

    // --- TRAVEL STATE ---
    [HideInInspector] public bool isPreparingForLaunch = false;
    [HideInInspector] public BigDouble launchPreparationProgress = 0;
    
    // Costo fisso bloccato all'inizio del lancio
    [HideInInspector] public BigDouble lockedLaunchRequirement = 0; 

    [HideInInspector] public bool isTraveling = false;
    [HideInInspector] public DateTime travelStartTime;

    // Durata fissata al momento della partenza
    [HideInInspector] public double currentLockedDuration = 0f;

    // --- NUOVO: Flag per indicare alla prossima scena di suonare l'animazione di atterraggio ---
    [HideInInspector] public bool pendingLanding = false; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // RIMOSSO: DontDestroyOnLoad(transform.root.gameObject);
            // Essendo parte del prefab Core_Systems, non serve.
        }
    }

    private void Update()
    {
        if (isPreparingForLaunch) UpdateLaunchPreparation();
        if (isTraveling) UpdateTravel();
    }

    public PlanetData GetCurrentPlanetData()
    {
        if (planets != null && planets.Count > currentPlanetIndex)
        {
            return planets[currentPlanetIndex];
        }
        return null;
    }

    public PlanetData GetNextPlanetData()
    {
        if (planets != null && planets.Count > currentPlanetIndex + 1)
        {
            return planets[currentPlanetIndex + 1];
        }
        return null;
    }

    private double CalculateTheoreticalDuration()
    {
        PlanetData destination = GetNextPlanetData();
        if (destination == null || destination.travelDistance <= 0) return 3.0f; 

        BigDouble speed = 0;
        if (SpaceshipManager.Instance != null)
        {
            speed = SpaceshipManager.Instance.GetTotalSpaceshipSpeed();
        }
        
        if (speed <= 0) speed = 10; 

        BigDouble durationBig = destination.travelDistance / speed;
        return durationBig.ToDouble(); 
    }

    public double GetTotalTravelDuration()
    {
        if (isTraveling && currentLockedDuration > 0)
        {
            return currentLockedDuration;
        }
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

        return GameManager.Instance.EffectiveStableIncomePerSec * 60;
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
        
        if (travelTime.TotalSeconds >= GetTotalTravelDuration())
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
            return;
        }

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
        // 1. Reset stato Viaggio
        isTraveling = false;
        currentLockedDuration = 0; 
        
        // 2. Reset stato Preparazione per il prossimo pianeta
        isPreparingForLaunch = false;
        launchPreparationProgress = 0;
        lockedLaunchRequirement = 0;

        // 3. Incremento Indice Pianeta
        currentPlanetIndex++;

        // Segnaliamo che dobbiamo atterrare
        pendingLanding = true;

        if (planets == null || currentPlanetIndex >= planets.Count)
        {
            currentPlanetIndex = (planets != null) ? planets.Count - 1 : 0;
            return;
        }

        // Recuperiamo i dati del nuovo pianeta
        PlanetData nextPlanet = GetCurrentPlanetData();
        string nextSceneName = nextPlanet.sceneName;

        // --- CORREZIONE: Reset dei dati PRIMA di caricare la scena ---
        
        // A. Reset standard (mette EmitterCount = 1 di default)
        GameManager.Instance.PerformPlanetChangeReset();
        
        // B. Sovrascriviamo: Il pianeta deve essere deserto finché la nave non atterra (0 emitter)
        GameManager.Instance.OverrideEmitterCount(0);

        // C. FIX CRITICO: Resettiamo anche i dati visivi correnti in memoria
        if (GameManager.Instance.planetVisuals != null)
        {
            GameManager.Instance.planetVisuals.ResetVisuals();
        }
        
        // D. Salviamo questo stato "pulito" IMMEDIATAMENTE
        GameManager.Instance.SaveGame();

        // E. MODIFICA VISIVA: Usiamo SceneFader se disponibile
        if (SceneFader.Instance != null)
        {
            // Impostiamo Icona e Nome del nuovo pianeta
            SceneFader.Instance.SetLoadingInfo(nextPlanet.planetName, nextPlanet.planetIcon);
            SceneFader.Instance.FadeAndLoadScene(nextSceneName, null);
        }
        else
        {
            // Fallback: caricamento diretto
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}