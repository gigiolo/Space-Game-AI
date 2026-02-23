// --- File: _Scripts\PlanetStatusPopup.cs ---
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
    public Button startPreparationButton;
    public Button startTravelButton;
    public Slider launchProgressBar;
    public TextMeshProUGUI progressText;

    [Header("--- Settings ---")]
    public Animator popupAnimator; 

    private bool _isOpenedByClick = false;

    private void Start()
    {
        if(contentPanel != null) 
        {
            // Fix Start() Sabotaggio
            if (!_isOpenedByClick) contentPanel.SetActive(false);
            
            if (UIManager.Instance != null)
                UIManager.Instance.RegisterMenu(contentPanel);
        }

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
                {
                    PlanetManager.Instance.StartInterplanetaryTravel();
                    ClosePopup();
                }
            });
        }
    }

    private void Update()
    {
        if ((contentPanel != null && contentPanel.activeSelf) || 
            (PlanetManager.Instance != null && PlanetManager.Instance.isPreparingForLaunch))
        {
            UpdatePlanetValue();
            UpdateLaunchStatus();
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
                
                UpdateStaticInfo();
                UpdatePlanetValue();
                UpdateLaunchStatus(); 
                
                if (popupAnimator != null && contentPanel.GetComponent<UIPopupEffect>() == null) 
                    popupAnimator.Play("PopupOpen");
            }
        }
    }

    public void OpenPopup()
    {
        if (contentPanel != null && !contentPanel.activeSelf) ToggleMenu();
    }

    public void ClosePopup()
    {
        if (contentPanel != null && contentPanel.activeSelf) ToggleMenu();
    }

    private void UpdateStaticInfo()
    {
        if (PlanetManager.Instance == null) return;
        var planetData = PlanetManager.Instance.GetCurrentPlanetData();
        int currentIndex = PlanetManager.Instance.currentPlanetIndex;
        if (planetData != null)
        {
            if (planetNameText != null) planetNameText.text = planetData.planetName;
            if (multiplierText != null) multiplierText.text = $"Multi: x{FormatMultiplier(planetData.productionMultiplier)}";
            if (descriptionText != null) descriptionText.text = $"Planet #{currentIndex + 1}\nGravity: Stable";
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

    private void UpdateLaunchStatus()
    {
        if (PlanetManager.Instance == null) return;

        bool isPrep = PlanetManager.Instance.isPreparingForLaunch;
        bool isTravel = PlanetManager.Instance.isTraveling;
        BigDouble currentProgress = PlanetManager.Instance.launchPreparationProgress;
        BigDouble requiredEnergy = PlanetManager.Instance.GetLaunchEnergyRequirement();
        bool isFinished = !isPrep && !isTravel && currentProgress > 10;

        if (launchProgressBar != null)
        {
            bool showBar = isPrep || isFinished;
            launchProgressBar.gameObject.SetActive(showBar);
            if (showBar && requiredEnergy > 0)
            {
                if (isFinished)
                {
                    launchProgressBar.value = 1.0f;
                    if (progressText != null) progressText.text = "READY";
                }
                else
                {
                    float progress = (float)(currentProgress / requiredEnergy).ToDouble();
                    launchProgressBar.value = progress;
                    if (progressText != null) progressText.text = $"{progress * 100:F0}%";
                }
            }
        }

        if (startPreparationButton != null)
        {
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
            startTravelButton.gameObject.SetActive(isFinished);
        }
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