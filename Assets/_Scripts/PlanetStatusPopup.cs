// --- File: _Scripts\PlanetStatusPopup.cs ---
using UnityEngine;
using TMPro;
using BreakInfinity;
using UnityEngine.UI;
using System;

public class PlanetStatusPopup : MonoBehaviour
{
    [Header("--- UI References (Base) ---")]
    public GameObject contentPanel;
    public Animator popupAnimator;

    [Header("--- 1. Carousel Zone ---")]
    public Button prevButton;
    public Button nextButton;
    public Image planetIcon;
    public TextMeshProUGUI planetNameText;
    public TextMeshProUGUI descriptionText;

    [Header("--- 2. Intel Zone ---")]
    public TextMeshProUGUI planetValueText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI distanceText;

    [Header("--- 3. Fleet Zone ---")]
    public Image bestShipIcon;
    public TextMeshProUGUI fleetSpeedText;
    public TextMeshProUGUI fleetEtaText;

    [Header("--- 4. Action Zone ---")]
    public Button mainActionButton; 
    public TextMeshProUGUI mainActionText; 
    public Slider launchProgressBar;
    public TextMeshProUGUI progressText;

    // STATO INTERNO
    private bool _isOpenedByClick = false;
    private int _viewedPlanetIndex = 0;

    private void Start()
    {
        if(contentPanel != null) 
        {
            if (!_isOpenedByClick) contentPanel.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.RegisterMenu(contentPanel);
        }

        if (prevButton) prevButton.onClick.AddListener(() => ChangeViewedPlanet(-1));
        if (nextButton) nextButton.onClick.AddListener(() => ChangeViewedPlanet(1));

        if (mainActionButton != null)
        {
            mainActionButton.onClick.RemoveAllListeners();
            mainActionButton.onClick.AddListener(OnMainActionClicked);
        }
    }

    private void OnMainActionClicked()
    {
        if (PlanetManager.Instance == null) return;

        bool isPrep = PlanetManager.Instance.isPreparingForLaunch;
        bool isTravel = PlanetManager.Instance.isTraveling;
        BigDouble currentProgress = PlanetManager.Instance.launchPreparationProgress;
        BigDouble requiredEnergy = PlanetManager.Instance.GetLaunchEnergyRequirement();
        
        bool isFinished = !isPrep && !isTravel && currentProgress >= requiredEnergy && requiredEnergy > 0;

        if (isFinished)
        {
            PlanetManager.Instance.StartInterplanetaryTravel();
            ClosePopup();
        }
        else if (!isPrep && !isTravel)
        {
            PlanetManager.Instance.StartLaunchPreparation();
        }
    }

    private void Update()
    {
        if (contentPanel != null && contentPanel.activeSelf)
        {
            RefreshAllUI();
        }
    }

    public void ToggleMenu()
    {
        if (contentPanel)
        {
            _isOpenedByClick = true; 
            bool opening = !contentPanel.activeSelf;

            if (!opening)
            {
                UIPopupEffect effect = contentPanel.GetComponent<UIPopupEffect>();
                if (effect != null) effect.Close();
                else contentPanel.SetActive(false);
            }
            else
            {
                if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(contentPanel);
                contentPanel.SetActive(true);

                if (PlanetManager.Instance != null)
                {
                    _viewedPlanetIndex = Mathf.Min(PlanetManager.Instance.currentPlanetIndex + 1, PlanetManager.Instance.planets.Count - 1);
                }
                
                RefreshAllUI(); 
                
                if (popupAnimator != null && contentPanel.GetComponent<UIPopupEffect>() == null) 
                    popupAnimator.Play("PopupOpen");
            }
        }
    }

    public void OpenPopup() { if (contentPanel != null && !contentPanel.activeSelf) ToggleMenu(); }
    public void ClosePopup() { if (contentPanel != null && contentPanel.activeSelf) ToggleMenu(); }

    private void ChangeViewedPlanet(int direction)
    {
        if (PlanetManager.Instance == null) return;
        
        _viewedPlanetIndex += direction;
        _viewedPlanetIndex = Mathf.Clamp(_viewedPlanetIndex, 0, PlanetManager.Instance.planets.Count - 1);
        
        RefreshAllUI();
    }

    private void RefreshAllUI()
    {
        if (PlanetManager.Instance == null || PlanetManager.Instance.planets.Count == 0) return;

        PlanetData viewedData = PlanetManager.Instance.planets[_viewedPlanetIndex];
        int actualCurrentPlanet = PlanetManager.Instance.currentPlanetIndex;

        UpdateCarousel(viewedData);
        UpdateIntel(viewedData, actualCurrentPlanet);
        UpdateFleet(viewedData);
        UpdateActionZone(viewedData, actualCurrentPlanet);
    }

    private void UpdateCarousel(PlanetData data)
    {
        if (planetNameText) planetNameText.text = $"{data.planetName} ({_viewedPlanetIndex + 1}/{PlanetManager.Instance.planets.Count})";
        if (planetIcon && data.planetIcon) planetIcon.sprite = data.planetIcon;

        if (prevButton) prevButton.interactable = _viewedPlanetIndex > 0;
        if (nextButton) nextButton.interactable = _viewedPlanetIndex < PlanetManager.Instance.planets.Count - 1;
    }

    private void UpdateIntel(PlanetData data, int actualCurrentPlanet)
    {
        if (descriptionText)
        {
            if (_viewedPlanetIndex < actualCurrentPlanet) descriptionText.text = "<color=#00FF00>STATUS: COLONIZZATO</color>";
            else if (_viewedPlanetIndex == actualCurrentPlanet) descriptionText.text = "<color=#00FFFF>STATUS: ATTUALE</color>";
            else descriptionText.text = "<color=orange>STATUS: INESPLORATO</color>";
        }

        if (multiplierText) multiplierText.text = $"Bonus Economico: x{FormatMultiplier(data.productionMultiplier)}";

        if (distanceText) distanceText.text = $"Distanza: {FormatNumber(data.travelDistance)} km";

        if (planetValueText)
        {
            // LOGICA CORRETTA: Il valore per raggiungere il pianeta _viewedPlanetIndex 
            // è definito nel pianeta PRECEDENTE.
            if (_viewedPlanetIndex == 0)
            {
                planetValueText.text = "Valore Richiesto: Nessuno (Pianeta Madre)";
            }
            else
            {
                // Prendiamo il requisito dal pianeta precedente
                BigDouble costToReach = PlanetManager.Instance.planets[_viewedPlanetIndex - 1].requiredPlanetValue;

                if (_viewedPlanetIndex <= actualCurrentPlanet)
                {
                    // Lo abbiamo già raggiunto
                    planetValueText.text = $"Valore Richiesto: Sbloccato ({FormatNumber(costToReach)})";
                }
                else if (_viewedPlanetIndex == actualCurrentPlanet + 1)
                {
                    // È il prossimo pianeta, mostriamo la barra di avanzamento
                    BigDouble currentVal = PlanetManager.Instance.CalculatePlanetValue();
                    planetValueText.text = $"Valore Richiesto: {FormatNumber(currentVal)} / {FormatNumber(costToReach)}";
                }
                else
                {
                    // Pianeta futuro
                    planetValueText.text = $"Valore Richiesto: {FormatNumber(costToReach)}";
                }
            }
        }
    }

    private void UpdateFleet(PlanetData targetPlanetData)
    {
        if (SpaceshipManager.Instance == null || SpaceshipManager.Instance.fleet.Count == 0) return;

        SpaceshipItem bestShip = null;
        BigDouble highestSpeed = 0;

        foreach (var ship in SpaceshipManager.Instance.fleet)
        {
            if (ship.currentLevel > 0 && ship.GetCurrentSpeed() > highestSpeed)
            {
                highestSpeed = ship.GetCurrentSpeed();
                bestShip = ship;
            }
        }

        if (bestShip != null)
        {
            if (bestShipIcon && bestShip.info.icon) bestShipIcon.sprite = bestShip.info.icon;
            if (fleetSpeedText) fleetSpeedText.text = $"Velocità Flotta: {FormatNumber(highestSpeed)} Km/s";

            if (fleetEtaText)
            {
                BigDouble dist = targetPlanetData.travelDistance;
                double seconds = (dist / highestSpeed).ToDouble();
                TimeSpan time = TimeSpan.FromSeconds(seconds);
                fleetEtaText.text = $"ETA: {FormatTimeSpan(time)}";
            }
        }
        else
        {
            if (fleetSpeedText) fleetSpeedText.text = "Velocità Flotta: N/A";
            if (fleetEtaText) fleetEtaText.text = "ETA: N/A";
        }
    }

    private void UpdateActionZone(PlanetData data, int actualCurrentPlanet)
    {
        bool isNextPlanet = (_viewedPlanetIndex == actualCurrentPlanet + 1);
        
        bool isPrep = PlanetManager.Instance.isPreparingForLaunch;
        bool isTravel = PlanetManager.Instance.isTraveling;
        BigDouble currentProgress = PlanetManager.Instance.launchPreparationProgress;
        BigDouble requiredEnergy = PlanetManager.Instance.GetLaunchEnergyRequirement();
        
        bool isFinished = !isPrep && !isTravel && currentProgress >= requiredEnergy && requiredEnergy > 0;

        if (!isNextPlanet)
        {
            if (mainActionButton) mainActionButton.gameObject.SetActive(false);
            if (launchProgressBar) launchProgressBar.gameObject.SetActive(false);
            return;
        }

        if (mainActionButton) mainActionButton.gameObject.SetActive(true);

        if (launchProgressBar != null)
        {
            bool showBar = isPrep || isFinished;
            launchProgressBar.gameObject.SetActive(showBar);
            
            if (showBar && requiredEnergy > 0)
            {
                float progress = (float)(currentProgress / requiredEnergy).ToDouble();
                launchProgressBar.value = Mathf.Clamp01(progress);
                
                if (progressText != null) 
                {
                    if (isFinished) progressText.text = "100%";
                    else progressText.text = $"{progress * 100:F0}%";
                }
            }
        }

        if (mainActionButton && mainActionText)
        {
            if (isTravel)
            {
                mainActionButton.interactable = false;
                mainActionText.text = "IN VIAGGIO...";
            }
            else if (isFinished)
            {
                mainActionButton.interactable = true;
                mainActionText.text = "LANCIA FLOTTA";
                mainActionText.color = Color.white;
            }
            else if (isPrep)
            {
                mainActionButton.interactable = false; 
                mainActionText.text = "CARICAMENTO...";
                mainActionText.color = Color.yellow;
            }
            else
            {
                // LOGICA CORRETTA: Usiamo il costo per uscire dal pianeta ATTUALE
                BigDouble costToLaunch = PlanetManager.Instance.planets[actualCurrentPlanet].requiredPlanetValue;
                bool canAfford = PlanetManager.Instance.CalculatePlanetValue() >= costToLaunch;
                
                mainActionButton.interactable = canAfford;
                
                if (canAfford)
                {
                    mainActionText.text = "PIANIFICA MISSIONE";
                    mainActionText.color = Color.white;
                }
                else
                {
                    mainActionText.text = "VALORE INSUFFICIENTE";
                    mainActionText.color = Color.gray;
                }
            }
        }
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return string.Format("{0:D2}h {1:D2}m", (int)ts.TotalHours, ts.Minutes);
        return string.Format("{0:D2}m {1:D2}s", ts.Minutes, ts.Seconds);
    }

    private string FormatMultiplier(BigDouble number) => number < 1000 ? number.ToString("F2") : number.ToString("F0");

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