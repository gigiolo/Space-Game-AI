using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;            
using BreakInfinity; 
using System; 
using System.Collections; 
using System.Collections.Generic; // Necessario per le Liste

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Top HUD - Standard")]
    public TextMeshProUGUI scoreText;                 
    public TextMeshProUGUI incomeText;                
    public TextMeshProUGUI logisticsStatusText; 
    
    [Tooltip("Collega qui il testo che mostra il moltiplicatore attivo (es. x3.0)")]
    public TextMeshProUGUI energyMultiplierText;

    [Tooltip("Collega qui il testo che prima mostrava la Capacità Massima (Offline)")]
    public TextMeshProUGUI storageText; 

    [Header("Top HUD - Special Currencies")]
    [Tooltip("Testo per visualizzare l'Iridio Puro (Premium)")]
    public TextMeshProUGUI pureIridiumText;

    [Tooltip("Testo per visualizzare l'Iridio Grezzo (Accumulato)")]
    public TextMeshProUGUI rawIridiumText;

    [Header("--- NUOVO: Nanobot Growth ---")]
    [Tooltip("Collega qui il testo per visualizzare la velocità di crescita degli emitter")]
    public TextMeshProUGUI emitterGrowthText;

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
    
    [Tooltip("NUOVO: Testo per mostrare Velocità Nave e Stima Tempo PRIMA di partire")]
    public TextMeshProUGUI travelInfoText; 

    [Header("OPTIONS MENU")]
    public Button optionsButton;                    
    public OptionsMenu optionsMenuController;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;        
    public Color warningColor = new Color(1f, 0.3f, 0.3f); 
    
    private GameManager gm;
    private PlanetManager pm;
    
    private Coroutine _iridiumFeedbackRoutine;
    private bool _isShowingIridiumFeedback = false; 

    // --- NUOVO SISTEMA MENU ESCLUSIVI ---
    private List<GameObject> _registeredMenus = new List<GameObject>();

    private void Awake()
    {
        // --- MODIFICA SINGLETON ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }
        Instance = this;
        
        // RIMOSSO: DontDestroyOnLoad(transform.root.gameObject);
        // Ci pensa il GameManager (che è nello stesso prefab root) a mantenere vivo tutto.
    }

    void Start()
    {
        gm = GameManager.Instance;
        pm = PlanetManager.Instance;
        
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

            if (optionsButton != null && optionsMenuController != null)
            {
                // Registriamo il menu opzioni
                RegisterMenu(optionsMenuController.panelVisuals);

                optionsButton.onClick.RemoveAllListeners();
                optionsButton.onClick.AddListener(optionsMenuController.ToggleMenu);
            }
            
            SetupHoldButton();
            RefreshUI();
        }
    }

    // --- NUOVI METODI PER GESTIONE MENU ---
    
    // I Manager chiameranno questo metodo in Start() per farsi conoscere
    public void RegisterMenu(GameObject menuPanel)
    {
        if (menuPanel != null && !_registeredMenus.Contains(menuPanel))
        {
            _registeredMenus.Add(menuPanel);
        }
    }

    // I Manager chiameranno questo metodo PRIMA di aprirsi
    public void CloseAllMenusExcept(GameObject menuToKeepOpen)
    {
        // Rimuoviamo eventuali riferimenti nulli (in caso di cambi scena/distruzioni)
        _registeredMenus.RemoveAll(x => x == null);

        foreach (var menu in _registeredMenus)
        {
            // Saltiamo quello che vogliamo aprire
            if (menu == menuToKeepOpen) continue;

            // Se il menu è attivo, chiudiamolo
            if (menu.activeSelf)
            {
                // Proviamo a chiudere elegantemente con l'effetto
                UIPopupEffect effect = menu.GetComponent<UIPopupEffect>();
                if (effect != null)
                {
                    effect.Close();
                }
                else
                {
                    // Chiusura brutale se non c'è l'effetto
                    menu.SetActive(false);
                }
            }
        }
    }

    // --------------------------------------

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
        
        // Puliamo la lista menu quando cambiamo scena per evitare riferimenti a oggetti distrutti
        // (Nota: I manager persistenti si ri-registreranno da soli, quelli di scena verranno distrutti)
        _registeredMenus.RemoveAll(x => x == null);

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

        string currentTotal = FormatNumber(GameManager.Instance.PureIridium);
        pureIridiumText.text = $"Pure Iridium: {currentTotal} <color=#FF00FF>(+{gain})</color>";

        yield return new WaitForSeconds(2.0f);

        _isShowingIridiumFeedback = false;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (gm == null) return;

        if (scoreText != null) scoreText.text = $"{FormatNumber(gm.CurrentEnergy)} Energy";
        
        if (storageText != null) 
        {
            TimeSpan ts = TimeSpan.FromSeconds(gm.MaxOfflineSeconds);
            string formattedTime = string.Format("{0}h {1:D2}m", (int)ts.TotalHours, ts.Minutes);
            storageText.text = $"Offline: {formattedTime}";
        }

        if (incomeText != null) incomeText.text = $"+{FormatNumber(gm.EffectiveIncomePerSec)}/s";

        if (pureIridiumText != null)
        {
            if (!_isShowingIridiumFeedback)
            {
                pureIridiumText.text = $"Pure Iridium: {FormatNumber(gm.PureIridium)}"; 
            }
        }

        if (rawIridiumText != null)
        {
            rawIridiumText.text = $"Raw Iridium: {FormatNumber(gm.RawIridium)}";
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

        if (energyMultiplierText != null)
        {
            float currentMult = gm.CurrentEnergyMultiplier;
            if (currentMult > 1.01f) 
            {
                if (!energyMultiplierText.gameObject.activeSelf) energyMultiplierText.gameObject.SetActive(true);
                energyMultiplierText.text = $"x {currentMult:F2}";
            }
            else
            {
                if (energyMultiplierText.gameObject.activeSelf) energyMultiplierText.gameObject.SetActive(false);
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

            if(startPreparationButton) 
                startPreparationButton.interactable = currentPlanetValue >= currentPlanet.requiredPlanetValue;
            
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
        if (ts.TotalHours >= 1)
            return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
        else
            return string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
    }

    void CheckBottleneck()
    {
        if (incomeText == null) return;
        bool isBottleneck = gm.RawProductionRate > gm.LogisticsCap;
        Color targetColor = isBottleneck ? warningColor : normalColor;
        incomeText.color = targetColor;
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F2");
        long exponent = (long)BigDouble.Log10(number);
        if (exponent < 6) return (number / 1000).ToString("F2") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        return number.ToString("e2");
    }
}