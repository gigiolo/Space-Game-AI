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

    [Header("Spaceship Settings")]
    [Tooltip("Velocità di viaggio della navicella (Km/s o unità arbitrarie). Tempo = Distanza / Velocità.")]
    public BigDouble baseSpaceshipSpeed = 100;

    [HideInInspector]
    public int currentPlanetIndex = 0;

    // --- TRAVEL STATE ---
    [HideInInspector] public bool isPreparingForLaunch = false;
    [HideInInspector] public BigDouble launchPreparationProgress = 0;
    
    // Costo fisso bloccato all'inizio del lancio
    [HideInInspector] public BigDouble lockedLaunchRequirement = 0; 

    [HideInInspector] public bool isTraveling = false;
    [HideInInspector] public DateTime travelStartTime;

    // Rimosso il valore costante, ora usiamo GetTotalTravelDuration()
    // public const float TRAVEL_DURATION_SECONDS = 3; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

    // --- NUOVO CALCOLO DEL TEMPO DI VIAGGIO ---
    public double GetTotalTravelDuration()
    {
        // Otteniamo il pianeta di destinazione (il prossimo nella lista)
        PlanetData destination = GetNextPlanetData();

        // Se non c'è una destinazione (siamo all'ultimo) o la distanza è 0, mettiamo un tempo minimo di debug (3s)
        if (destination == null || destination.travelDistance <= 0)
        {
            return 3.0f; 
        }

        // Evitiamo divisioni per zero
        if (baseSpaceshipSpeed <= 0) baseSpaceshipSpeed = 1;

        // Tempo = Distanza / Velocità
        BigDouble durationBig = destination.travelDistance / baseSpaceshipSpeed;
        
        // Convertiamo in double (secondi) per usarlo con DateTime e Time.deltaTime
        // Nota: Se la distanza è immensa e la velocità bassa, questo numero potrebbe essere alto (ore reali).
        return durationBig.ToDouble(); 
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
        
        // Confrontiamo il tempo trascorso con la durata calcolata dinamicamente
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
    }

    public void StartInterplanetaryTravel()
    {
        if (isTraveling || isPreparingForLaunch) return;

        isTraveling = true;
        travelStartTime = DateTime.UtcNow;
        GameManager.Instance.SaveGame();
    }

    public void CompleteTravel()
    {
        isTraveling = false;
        currentPlanetIndex++;

        if (planets == null || currentPlanetIndex >= planets.Count)
        {
            currentPlanetIndex = (planets != null) ? planets.Count - 1 : 0;
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(GetCurrentPlanetData().sceneName);
        GameManager.Instance.PerformPlanetChangeReset();
        GameManager.Instance.SaveGame();
    }
}