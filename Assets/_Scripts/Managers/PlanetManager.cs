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

    // --- TRAVEL STATE ---
    [HideInInspector] public bool isPreparingForLaunch = false;
    [HideInInspector] public BigDouble launchPreparationProgress = 0;
    
    // NUOVO: Memorizza il costo fisso all'inizio del lancio
    [HideInInspector] public BigDouble lockedLaunchRequirement = 0; 

    [HideInInspector] public bool isTraveling = false;
    [HideInInspector] public DateTime travelStartTime;

    public const float TRAVEL_DURATION_SECONDS = 3; // 1 hour

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

    public BigDouble CalculatePlanetValue()
    {
        if (GameManager.Instance == null) return 0;

        // Usa la produzione stabile per evitare oscillazioni
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

        // --- FIX CRITICO: SE STIAMO GIA' PREPARANDO, USA IL VALORE BLOCCATO ---
        // Questo evita che il traguardo si sposti mentre giochi.
        if (isPreparingForLaunch && lockedLaunchRequirement > 0)
        {
            return lockedLaunchRequirement;
        }

        // Altrimenti calcola quello attuale (Produzione Stabile * 60 secondi)
        return GameManager.Instance.EffectiveStableIncomePerSec * 60;
    }

    private void UpdateLaunchPreparation()
    {
        // Ora recupera sempre il valore fisso (grazie alla modifica sopra)
        BigDouble energyRequirement = GetLaunchEnergyRequirement();
        
        if (energyRequirement <= 0) 
        {
            // Protezione: se per assurdo è 0, finiamo subito
            isPreparingForLaunch = false;
            return;
        }

        // Calcola quanto aggiungere questo frame
        BigDouble energyToConsume = GameManager.Instance.EffectiveIncomePerSec * Time.deltaTime;
        
        // Calcola quanto manca
        BigDouble remainingEnergy = energyRequirement - launchPreparationProgress;
        
        // Non consumare più del necessario
        if (energyToConsume > remainingEnergy) energyToConsume = remainingEnergy;
        
        // Non consumare più di quello che hai
        energyToConsume = BigDouble.Min(energyToConsume, GameManager.Instance.CurrentEnergy);

        if (GameManager.Instance.TrySpend(energyToConsume))
        {
            launchPreparationProgress += energyToConsume;
        }

        // Controllo di fine: tolleranza minima per errori di virgola mobile
        if (launchPreparationProgress >= energyRequirement * 0.9999f) 
        {
            // Arrotonda per pulizia e chiudi
            launchPreparationProgress = energyRequirement;
            isPreparingForLaunch = false;
            lockedLaunchRequirement = 0; // Reset per il prossimo pianeta
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
            return;
        }

        isPreparingForLaunch = true;
        launchPreparationProgress = 0;
        
        // --- FIX CRITICO: BLOCCHIAMO IL PREZZO ORA ---
        lockedLaunchRequirement = GetLaunchEnergyRequirement();
        
        // Sicurezza: se per caso è 0, mettiamo un valore minimo
        if (lockedLaunchRequirement <= 0) lockedLaunchRequirement = 100;
        
        // Salviamo subito per evitare problemi se il gioco crasha
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