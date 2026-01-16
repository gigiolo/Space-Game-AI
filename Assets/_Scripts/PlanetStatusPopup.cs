using UnityEngine;
using TMPro;
using BreakInfinity; 
using UnityEngine.UI;

public class PlanetStatusPopup : MonoBehaviour
{
    [Header("--- UI References ---")]
    public GameObject contentPanel;
    public TextMeshProUGUI planetNameText;
    public TextMeshProUGUI planetValueText; 
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI descriptionText;
    public Image planetIcon;

    [Header("--- Buttons & Progress ---")]
    [Tooltip("Il tasto per avviare il caricamento (Start Preparation)")]
    public Button startPreparationButton;
    
    [Tooltip("Il tasto per partire (Start Travel) - Appare alla fine")]
    public Button startTravelButton;
    
    [Tooltip("La barra di caricamento (Slider)")]
    public Slider launchProgressBar;
    
    [Tooltip("Testo opzionale per la percentuale (es. '50%')")]
    public TextMeshProUGUI progressText;

    [Header("--- Settings ---")]
    public Animator popupAnimator;

    private bool isOpen = false;

    private void Start()
    {
        if(contentPanel != null) contentPanel.SetActive(false);

        // --- COLLEGAMENTO DEI BOTTONI ---
        if (startPreparationButton != null)
        {
            startPreparationButton.onClick.RemoveAllListeners();
            startPreparationButton.onClick.AddListener(() => 
            {
                if (PlanetManager.Instance != null) 
                    PlanetManager.Instance.StartLaunchPreparation();
            });
        }

        if (startTravelButton != null)
        {
            startTravelButton.onClick.RemoveAllListeners();
            startTravelButton.onClick.AddListener(() => 
            {
                if (PlanetManager.Instance != null) 
                    PlanetManager.Instance.StartInterplanetaryTravel();
            });
        }
    }

    private void Update()
    {
        if (isOpen || (PlanetManager.Instance != null && PlanetManager.Instance.isPreparingForLaunch))
        {
            UpdatePlanetValue();
            UpdateLaunchStatus();
        }
    }

    public void OpenPopup()
    {
        if (contentPanel != null) 
        {
            contentPanel.SetActive(true);
            isOpen = true;
            
            UpdateStaticInfo();
            UpdatePlanetValue();
            UpdateLaunchStatus(); 
            
            if (popupAnimator != null) popupAnimator.Play("PopupOpen");
        }
    }

    public void ClosePopup()
    {
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
            isOpen = false;
        }
    }

    private void UpdateStaticInfo()
    {
        if (PlanetManager.Instance == null) return;

        var planetData = PlanetManager.Instance.GetCurrentPlanetData();
        int currentIndex = PlanetManager.Instance.currentPlanetIndex;

        if (planetData != null)
        {
            if (planetNameText != null) 
                planetNameText.text = planetData.planetName;

            if (multiplierText != null) 
                multiplierText.text = $"Multi: x{FormatMultiplier(planetData.productionMultiplier)}";

            if (descriptionText != null)
                descriptionText.text = $"Planet #{currentIndex + 1}\nGravity: Stable";
        }
    }

    private void UpdatePlanetValue()
    {
        if (PlanetManager.Instance == null) return;

        var planetData = PlanetManager.Instance.GetCurrentPlanetData();
        if (planetData != null)
        {
            BigDouble currentVal = PlanetManager.Instance.CalculatePlanetValue();
            BigDouble requiredVal = planetData.requiredPlanetValue;

            if (planetValueText != null)
                planetValueText.text = $"Value: {FormatNumber(currentVal)} / {FormatNumber(requiredVal)}";
        }
    }

    // --- FIX LOGICA QUI SOTTO ---
    private void UpdateLaunchStatus()
    {
        if (PlanetManager.Instance == null) return;

        bool isPrep = PlanetManager.Instance.isPreparingForLaunch;
        bool isTravel = PlanetManager.Instance.isTraveling;
        
        BigDouble currentProgress = PlanetManager.Instance.launchPreparationProgress;
        BigDouble requiredEnergy = PlanetManager.Instance.GetLaunchEnergyRequirement();
        
        // FIX: Non confrontiamo più con 'requiredEnergy' per decidere se abbiamo finito.
        // Se non stiamo preparando (isPrep == false) e la barra non è vuota (> 10), 
        // significa che il Manager ha terminato il processo con successo.
        bool isFinished = !isPrep && !isTravel && currentProgress > 10;

        // 1. Gestione Barra di Progressione
        if (launchProgressBar != null)
        {
            // Mostriamo la barra durante la preparazione O quando è finita (piena)
            bool showBar = isPrep || isFinished;
            launchProgressBar.gameObject.SetActive(showBar);

            if (showBar && requiredEnergy > 0)
            {
                if (isFinished)
                {
                    // Se è finita, mostriamola piena al 100% anche se il costo è salito nel frattempo
                    launchProgressBar.value = 1.0f;
                    if (progressText != null) progressText.text = "READY";
                }
                else
                {
                    // Durante il caricamento, calcolo normale
                    float progress = (float)(currentProgress / requiredEnergy).ToDouble();
                    launchProgressBar.value = progress;
                    if (progressText != null) progressText.text = $"{progress * 100:F0}%";
                }
            }
        }

        // 2. Gestione Bottoni
        if (startPreparationButton != null)
        {
            // Il tasto Start si vede solo se NON stiamo facendo nulla e NON abbiamo finito
            bool showPrepBtn = !isPrep && !isTravel && !isFinished;
            startPreparationButton.gameObject.SetActive(showPrepBtn);

            if (showPrepBtn)
            {
                var pData = PlanetManager.Instance.GetCurrentPlanetData();
                bool canClick = pData != null && PlanetManager.Instance.CalculatePlanetValue() >= pData.requiredPlanetValue;
                startPreparationButton.interactable = canClick;
            }
        }

        if (startTravelButton != null)
        {
            // Il tasto Viaggio appare quando è FINITO
            startTravelButton.gameObject.SetActive(isFinished);
        }
    }

    private string FormatMultiplier(BigDouble number)
    {
        return number < 1000 ? number.ToString("F2") : number.ToString("F0");
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F2");
        long exponent = (long)BigDouble.Log10(number);
        if (exponent < 6) return (number / 1000).ToString("F2") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}