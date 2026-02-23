// --- File: _Scripts\UIManager.cs ---
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;                                        
using BreakInfinity; 
using System; 
using System.Collections; 
using System.Collections.Generic; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("--- INTRO & VISIBILITY ---")]
    public CanvasGroup mainHUDGroup; 

    [Header("Top HUD - Standard")]
    public TextMeshProUGUI scoreText;                                       
    public TextMeshProUGUI incomeText;
    [Tooltip("Trascina qui l'icona dell'Income (Produzione)")]
    public Image incomeIcon; 
    public TextMeshProUGUI logisticsStatusText; 
    
    [Header("Multiplier UI")]
    [Tooltip("Trascina qui l'OGGETTO PADRE 'MultiplierContainer' (NON quello della produzione)")]
    public GameObject multiplierContainer; 
    [Tooltip("Trascina qui il TESTO dentro al container del moltiplicatore")]
    public TextMeshProUGUI energyMultiplierText;
    
    [Tooltip("Scegli qui il colore del testo del moltiplicatore")]
    public Color multiplierTextColor = new Color(0f, 1f, 1f, 1f); // Default Ciano

    [Tooltip("Collega qui il testo isolato per il conteggio Emitter")]
    public TextMeshProUGUI emitterCountText; 
    [Tooltip("Collega qui l'IMMAGINE (Icona) accanto al conteggio Emitter")]
    public Image emitterIcon; 

    public TextMeshProUGUI storageText; 

    [Header("Top HUD - Special Currencies")]
    public TextMeshProUGUI pureIridiumText;
    [Tooltip("Collega qui l'IMMAGINE (Icona) del Pure Iridium")]
    public Image pureIridiumIcon; 
    [Tooltip("Scegli il colore dedicato per il Pure Iridium")]
    public Color pureIridiumColor = Color.magenta; 
    
    public TextMeshProUGUI rawIridiumText;

    [Header("--- Nanobot Growth ---")]
    public TextMeshProUGUI emitterGrowthText;
    
    [Tooltip("L'immagine radiale che si riempie man mano che nasce un emitter.")]
    public Image emitterProgressPie; 

    [Header("Bottom Deck")]
    public GameObject mainEnergyButtonObj; 
    
    [Header("RESET QUANTISTICO")]
    public Button prestigeButton;
    public TextMeshProUGUI prestigeInfoText;

    [Header("PLANET TRAVEL")]
    public GameObject planetTravelPanel;
    public Button startPreparationButton;
    public Button startTravelButton;
    public TextMeshProUGUI planetValueText;
    public Slider launchProgressBar;
    public TextMeshProUGUI travelStatusText; 
    public TextMeshProUGUI travelInfoText; 

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;        
    public Color warningColor = new Color(1f, 0.3f, 0.3f); 
    
    private GameManager gm;
    private PlanetManager pm;
    
    private Coroutine _iridiumFeedbackRoutine;
    private bool _isShowingIridiumFeedback = false; 

    // Variabili per Animazione Multiplier
    private CanvasGroup _multCanvasGroup;
    private bool _isMultVisible = false;
    private Coroutine _multAnimRoutine;

    // Variabili per Animazione Pie Chart (Torta)
    private CanvasGroup _pieCanvasGroup;
    private bool _isPieVisible = false;
    private Coroutine _pieAnimRoutine;

    private List<GameObject> _registeredMenus = new List<GameObject>();

    // --- Riferimenti agli Animatori dei Numeri ---
    private NumberDigitAnimator _scoreAnimator;
    private NumberDigitAnimator _incomeAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }
        Instance = this;

        // --- SETUP INIZIALE MOLTIPLICATORE ---
        if (multiplierContainer != null)
        {
            _multCanvasGroup = multiplierContainer.GetComponent<CanvasGroup>();
            if (_multCanvasGroup == null) _multCanvasGroup = multiplierContainer.AddComponent<CanvasGroup>();

            _multCanvasGroup.alpha = 0f;
            _isMultVisible = false;
            multiplierContainer.SetActive(false);
        }

        // --- SETUP INIZIALE PIE CHART ---
        if (emitterProgressPie != null)
        {
            _pieCanvasGroup = emitterProgressPie.GetComponent<CanvasGroup>();
            if (_pieCanvasGroup == null) _pieCanvasGroup = emitterProgressPie.gameObject.AddComponent<CanvasGroup>();

            _pieCanvasGroup.alpha = 0f;
            _isPieVisible = false;
            emitterProgressPie.gameObject.SetActive(true); 
        }
    }

    void Start()
    {
        gm = GameManager.Instance;
        pm = PlanetManager.Instance;
        
        if (scoreText != null) _scoreAnimator = scoreText.GetComponent<NumberDigitAnimator>();
        if (incomeText != null) _incomeAnimator = incomeText.GetComponent<NumberDigitAnimator>();

        if (gm != null)
        {
            gm.OnEconomyUpdated -= RefreshUI; 
            gm.OnEconomyUpdated += RefreshUI;
            
            if(prestigeButton) 
            {
                prestigeButton.onClick.RemoveAllListeners();
                prestigeButton.onClick.AddListener(gm.PerformQuantumReset);
            }

            SetupPlanetButtons(); 
            SetupHoldButton();
            RefreshUI();
        }
    }

    public void SetHUDVisibility(bool visible, float duration = 0.5f)
    {
        if (mainHUDGroup == null) return;
        StartCoroutine(FadeCanvasGroup(mainHUDGroup, visible ? 1f : 0f, duration));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float timer = 0f;
        if (targetAlpha == 0f) cg.blocksRaycasts = false;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        if (targetAlpha == 1f) cg.blocksRaycasts = true;
    }

    public void RegisterMenu(GameObject menuPanel)
    {
        if (menuPanel != null && !_registeredMenus.Contains(menuPanel))
        {
            _registeredMenus.Add(menuPanel);
        }
    }

    public void CloseAllMenusExcept(GameObject menuToKeepOpen)
    {
        _registeredMenus.RemoveAll(x => x == null);
        foreach (var menu in _registeredMenus)
        {
            if (menu == menuToKeepOpen) continue;
            if (menu.activeSelf)
            {
                UIPopupEffect effect = menu.GetComponent<UIPopupEffect>();
                if (effect != null) effect.Close();
                else menu.SetActive(false);
            }
        }
    }

    private void SetupPlanetButtons()
    {
        if (pm == null) return;
        if (startPreparationButton) 
        {
            startPreparationButton.onClick.RemoveAllListeners();
            startPreparationButton.onClick.AddListener(pm.StartLaunchPreparation);
        }
        if (startTravelButton) 
        {
            startTravelButton.onClick.RemoveAllListeners();
            startTravelButton.onClick.AddListener(pm.StartInterplanetaryTravel);
        }
    }
    
    void SetupHoldButton()
    {
        if (mainEnergyButtonObj == null) return;
        EventTrigger trigger = mainEnergyButtonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = mainEnergyButtonObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { if(gm) gm.OnEnergyButtonPress(); }); 
        trigger.triggers.Add(entryDown);
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { if(gm) gm.OnEnergyButtonRelease(); });
        trigger.triggers.Add(entryUp);
    }

    void OnDestroy()
    {
        if (gm != null) gm.OnEconomyUpdated -= RefreshUI;
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        gm = GameManager.Instance;
        pm = PlanetManager.Instance;
        _registeredMenus.RemoveAll(x => x == null);
        
        if (multiplierContainer != null) 
        {
            multiplierContainer.SetActive(false);
            _isMultVisible = false;
        }
        
        if (scoreText != null) _scoreAnimator = scoreText.GetComponent<NumberDigitAnimator>();
        if (incomeText != null) _incomeAnimator = incomeText.GetComponent<NumberDigitAnimator>();

        RefreshUI();
    }

    public void ShowPureIridiumFeedback(int gainAmount)
    {
        if (pureIridiumText == null) return;
        if (_iridiumFeedbackRoutine != null) StopCoroutine(_iridiumFeedbackRoutine);
        _iridiumFeedbackRoutine = StartCoroutine(IridiumFeedbackRoutine(gainAmount));
    }

    IEnumerator IridiumFeedbackRoutine(int gain)
    {
        _isShowingIridiumFeedback = true;
        string currentTotal = FormatNumber(GameManager.Instance.PureIridium, 0);
        pureIridiumText.text = $"{currentTotal} <color=#FF00FF>(+{gain})</color>";
        yield return new WaitForSeconds(2.0f);
        _isShowingIridiumFeedback = false;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (gm == null) return;

        if (scoreText != null) 
        {
            string val = FormatNumber(gm.CurrentEnergy);
            if (_scoreAnimator != null) _scoreAnimator.SetText(val);
            else scoreText.text = val;
        }
        
        if (storageText != null) 
        {
            TimeSpan ts = TimeSpan.FromSeconds(gm.MaxOfflineSeconds);
            string formattedTime = string.Format("{0}h {1:D2}m", (int)ts.TotalHours, ts.Minutes);
            storageText.text = $"Offline: {formattedTime}";
        }

        if (incomeText != null) 
        {
            string val = $"{FormatNumber(gm.EffectiveIncomePerSec)}/sec";
            if (_incomeAnimator != null) _incomeAnimator.SetText(val);
            else incomeText.text = val;
        }

        if (pureIridiumText != null)
        {
            if (!_isShowingIridiumFeedback) 
            {
                pureIridiumText.text = FormatNumber(gm.PureIridium, 0); 
            }

            if (pureIridiumText.color != pureIridiumColor) 
                pureIridiumText.color = pureIridiumColor;
            
            if (pureIridiumIcon != null && pureIridiumIcon.color != pureIridiumColor)
            {
                pureIridiumIcon.color = pureIridiumColor;
            }
        }

        if (rawIridiumText != null) rawIridiumText.text = $"Raw Iridium: {FormatNumber(gm.RawIridium)}";

        if (emitterCountText != null)
        {
            string currentStr = (gm.EmitterCount < 1000) 
                ? gm.EmitterCount.ToString("F0") 
                : FormatNumber(gm.EmitterCount);

            string capStr = (gm.EmitterCap < 1000) 
                ? gm.EmitterCap.ToString("F0") 
                : FormatNumber(gm.EmitterCap);
            
            emitterCountText.text = $"{currentStr} / {capStr}";

            bool isMaxed = gm.EmitterCount >= gm.EmitterCap;
            
            Color baseColor = normalColor;
            if (gm.activeTheme != null) baseColor = gm.activeTheme.textHighlight; 
            
            Color finalColor = isMaxed ? warningColor : baseColor;

            if (emitterCountText.color != finalColor) emitterCountText.color = finalColor;
            if (emitterIcon != null && emitterIcon.color != finalColor) emitterIcon.color = finalColor;
        }

        if (multiplierContainer != null && energyMultiplierText != null)
        {
            float currentMult = gm.CurrentEnergyMultiplier;
            bool shouldBeVisible = currentMult > 1.01f; 

            energyMultiplierText.text = $"< {currentMult:F2} x";
            
            if (energyMultiplierText.color != multiplierTextColor)
                energyMultiplierText.color = multiplierTextColor;

            if (shouldBeVisible != _isMultVisible)
            {
                _isMultVisible = shouldBeVisible;
                if (_multAnimRoutine != null) StopCoroutine(_multAnimRoutine);
                _multAnimRoutine = StartCoroutine(ToggleMultiplierRoutine(shouldBeVisible));
            }
        }

        if (logisticsStatusText != null)
        {
            string emitterString = $"Units: {gm.EmitterCount} / {gm.EmitterCap}";
            if (gm.EmitterCount >= gm.EmitterCap) emitterString = $"<color=red>{emitterString} (MAX)</color>";
            logisticsStatusText.text = $"{emitterString}\nProd: {FormatNumber(gm.RawProductionRate)} | Log Cap: {FormatNumber(gm.LogisticsCap)}";
        }

        if (emitterGrowthText != null)
        {
            if (gm.EmitterCount >= gm.EmitterCap)
                emitterGrowthText.text = "Growth: <color=red>PAUSED (Max Cap)</color>";
            else
            {
                double speed = gm.EmitterAutoGrowthSpeed;
                emitterGrowthText.text = $"Growth: <color=#00FF00>+{speed:F2}/s</color>";
            }
        }

        if (emitterProgressPie != null && _pieCanvasGroup != null)
        {
            bool shouldShowPie = gm.EmitterCount < gm.EmitterCap;

            if (shouldShowPie != _isPieVisible)
            {
                _isPieVisible = shouldShowPie;
                if (_pieAnimRoutine != null) StopCoroutine(_pieAnimRoutine);
                _pieAnimRoutine = StartCoroutine(TogglePieRoutine(shouldShowPie));
            }

            if (_isPieVisible || _pieCanvasGroup.alpha > 0.01f)
            {
                emitterProgressPie.fillAmount = gm.GetEmitterGrowthProgress();
            }
        }

        if (prestigeInfoText != null)
        {
            BigDouble potentialNodes = gm.CalculatePotentialNodes();
            prestigeInfoText.text = $"RESET (Current: {gm.ScientificNodes})\nGain: <color=#00FFFF>+{FormatNumber(potentialNodes)} Nodes</color>";
            if (prestigeButton) prestigeButton.interactable = potentialNodes > 0;
        }

        CheckBottleneck();
        RefreshPlanetUI();
    }

    private IEnumerator ToggleMultiplierRoutine(bool show)
    {
        float timer = 0f;
        float duration = 0.4f;

        if (show)
        {
            multiplierContainer.SetActive(true);
            _multCanvasGroup.alpha = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                _multCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            _multCanvasGroup.alpha = 1f;
        }
        else
        {
            float startAlpha = _multCanvasGroup.alpha;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                _multCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            _multCanvasGroup.alpha = 0f;
            multiplierContainer.SetActive(false); 
        }
    }

    private IEnumerator TogglePieRoutine(bool show)
    {
        float timer = 0f;
        float duration = 0.5f;

        float startAlpha = _pieCanvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            _pieCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        _pieCanvasGroup.alpha = targetAlpha;
    }

    void RefreshPlanetUI()
    {
        if (pm == null) return;
        PlanetData currentPlanet = pm.GetCurrentPlanetData();
        
        if (currentPlanet == null || planetTravelPanel == null)
        {
            if(planetTravelPanel != null) planetTravelPanel.SetActive(false);
            return;
        }

        BigDouble currentPlanetValue = pm.CalculatePlanetValue();
        bool canShowPanel = currentPlanetValue >= currentPlanet.requiredPlanetValue || pm.isPreparingForLaunch || pm.isTraveling;
        planetTravelPanel.SetActive(canShowPanel);
        if (!canShowPanel) return;

        if (planetValueText != null)
            planetValueText.text = $"Planet Value: {FormatNumber(currentPlanetValue)} / {FormatNumber(currentPlanet.requiredPlanetValue)}";
        
        bool isTraveling = pm.isTraveling;
        bool isPreparing = pm.isPreparingForLaunch;
        double totalDuration = pm.GetTotalTravelDuration();
        BigDouble shipSpeed = (SpaceshipManager.Instance != null) ? SpaceshipManager.Instance.GetTotalSpaceshipSpeed() : 0;
        if (shipSpeed <= 0) shipSpeed = 10; 

        if (isTraveling)
        {
            if(startPreparationButton) startPreparationButton.gameObject.SetActive(false);
            if(startTravelButton) startTravelButton.gameObject.SetActive(false);
            if(launchProgressBar) launchProgressBar.gameObject.SetActive(false);
            if(travelStatusText)
            {
                travelStatusText.gameObject.SetActive(true);
                TimeSpan timeRemaining = TimeSpan.FromSeconds(totalDuration) - (DateTime.UtcNow - pm.travelStartTime);
                if (timeRemaining.TotalSeconds > 0)
                    travelStatusText.text = $"Arriving in: <color=yellow>{FormatTimeSpan(timeRemaining)}</color>";
                else
                    travelStatusText.text = "Docking...";
            }
            if(travelInfoText) travelInfoText.gameObject.SetActive(false);
        }
        else if (isPreparing)
        {
            if(startPreparationButton) startPreparationButton.gameObject.SetActive(false);
            if(startTravelButton) startTravelButton.gameObject.SetActive(false);
            if(travelStatusText) travelStatusText.gameObject.SetActive(false);
            if(launchProgressBar)
            {
                launchProgressBar.gameObject.SetActive(true);
                BigDouble energyRequirement = pm.GetLaunchEnergyRequirement();
                if (energyRequirement > 0)
                    launchProgressBar.value = (float)(pm.launchPreparationProgress / energyRequirement).ToDouble();
            }
            UpdateTravelInfoText(shipSpeed, totalDuration);
        }
        else
        {
            BigDouble energyRequirement = pm.GetLaunchEnergyRequirement();
            bool preparationComplete = pm.launchPreparationProgress >= energyRequirement && energyRequirement > 0;
            if(startPreparationButton) startPreparationButton.gameObject.SetActive(!preparationComplete);
            if(startTravelButton) startTravelButton.gameObject.SetActive(preparationComplete);
            if(launchProgressBar) launchProgressBar.gameObject.SetActive(false);
            if(travelStatusText) travelStatusText.gameObject.SetActive(false);
            if(startPreparationButton) startPreparationButton.interactable = currentPlanetValue >= currentPlanet.requiredPlanetValue;
            UpdateTravelInfoText(shipSpeed, totalDuration);
        }
    }

    private void UpdateTravelInfoText(BigDouble speed, double duration)
    {
        if (travelInfoText != null)
        {
            travelInfoText.gameObject.SetActive(true);
            string timeStr = FormatTimeSpan(TimeSpan.FromSeconds(duration));
            travelInfoText.text = $"Fleet Speed: <color=#00FFFF>{FormatNumber(speed)} km/s</color>\nEst. Duration: <color=yellow>{timeStr}</color>";
        }
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
        else return string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
    }

    void CheckBottleneck()
    {
        if (incomeText == null) return;
        bool isBottleneck = gm.RawProductionRate > gm.LogisticsCap;
        Color targetNormal = normalColor;
        if (gm.activeTheme != null) targetNormal = gm.activeTheme.textHighlight; 
        Color finalColor = isBottleneck ? warningColor : targetNormal;
        
        if (incomeText.color != finalColor) incomeText.color = finalColor;
        
        if (incomeIcon != null && incomeIcon.color != finalColor) 
        {
            incomeIcon.color = finalColor;
        }
    }

    private string FormatNumber(BigDouble number, int decimals = 2)
    {
        if (number < 1000) return number.ToString("F" + decimals);
        long exponent = (long)BigDouble.Log10(number);
        if (exponent < 6) return (number / 1000).ToString("F" + decimals) + "k";
        if (exponent < 9) return (number / 1e6).ToString("F" + decimals) + "M";
        if (exponent < 12) return (number / 1e9).ToString("F" + decimals) + "B";
        if (exponent < 15) return (number / 1e12).ToString("F" + decimals) + "T";
        
        return $"{number.Mantissa.ToString("F" + decimals)}e{number.Exponent}";
    }
}