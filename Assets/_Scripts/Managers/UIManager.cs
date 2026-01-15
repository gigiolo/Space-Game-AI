using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;        
using BreakInfinity; 
using System; 

public class UIManager : MonoBehaviour
{
    [Header("Top HUD")]
    public TextMeshProUGUI scoreText;                
    public TextMeshProUGUI incomeText;               
    public TextMeshProUGUI logisticsStatusText; 
    
    [Tooltip("Collega qui il testo che mostra il moltiplicatore attivo (es. x3.0)")]
    public TextMeshProUGUI energyMultiplierText;

    [Tooltip("Collega qui il testo che prima mostrava la Capacità Massima")]
    public TextMeshProUGUI storageText; 

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

    [Header("OPTIONS MENU")]
    public Button optionsButton;                  
    public OptionsMenu optionsMenuController;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;        
    public Color warningColor = new Color(1f, 0.3f, 0.3f); 

    private GameManager gm;
    private PlanetManager pm;
    
    public static UIManager Instance { get; private set; }

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

    void Start()
    {
        gm = GameManager.Instance;
        pm = PlanetManager.Instance;
        if (gm != null)
        {
            gm.OnEconomyUpdated += RefreshUI;
            
            if(prestigeButton) prestigeButton.onClick.AddListener(gm.PerformQuantumReset);

            // Planet Travel Button Listeners
            if (pm != null)
            {
                if (startPreparationButton) startPreparationButton.onClick.AddListener(pm.StartLaunchPreparation);
                if (startTravelButton) startTravelButton.onClick.AddListener(pm.StartInterplanetaryTravel);
            }

            if (optionsButton != null && optionsMenuController != null)
            {
                optionsButton.onClick.AddListener(optionsMenuController.ToggleMenu);
            }
            
            SetupHoldButton();
            RefreshUI();
        }
    }
    
    void SetupHoldButton()
    {
        if (mainEnergyButtonObj == null) return;

        EventTrigger trigger = mainEnergyButtonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = mainEnergyButtonObj.AddComponent<EventTrigger>();
        
        // Quando premi (PointerDown), chiami OnEnergyButtonPress
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { gm.OnEnergyButtonPress(); }); 
        trigger.triggers.Add(entryDown);

        // Quando rilasci (PointerUp), chiami OnEnergyButtonRelease
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { gm.OnEnergyButtonRelease(); });
        trigger.triggers.Add(entryUp);
    }

    void OnDestroy()
    {
        if (gm != null) gm.OnEconomyUpdated -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (gm == null) return;

        // 1. ENERGIA (Infinita)
        if (scoreText) scoreText.text = $"{FormatNumber(gm.CurrentEnergy)} Energy";
        
        // 2. TEMPO OFFLINE (Sostituisce Max Cap)
        if (storageText) 
        {
            TimeSpan ts = TimeSpan.FromSeconds(gm.MaxOfflineSeconds);
            string formattedTime = string.Format("{0}h {1:D2}m", (int)ts.TotalHours, ts.Minutes);
            storageText.text = $"Offline: {formattedTime}";
        }

        // 3. INCOME
        if (incomeText) incomeText.text = $"+{FormatNumber(gm.EffectiveIncomePerSec)}/s";

        // 4. LOGISTICA E EMETTITORI
        if (logisticsStatusText)
        {
            string emitterString = $"Units: {gm.EmitterCount} / {gm.EmitterCap}";
            
            if (gm.EmitterCount >= gm.EmitterCap)
            {
                emitterString = $"<color=red>{emitterString} (MAX)</color>";
            }

            logisticsStatusText.text = $"{emitterString}\nProd: {FormatNumber(gm.RawProductionRate)} | Log Cap: {FormatNumber(gm.LogisticsCap)}";
        }

        // 5. MOLTIPLICATORE ENERGY BUTTON
        if (energyMultiplierText != null)
        {
            float currentMult = gm.CurrentEnergyMultiplier;

            // Se il moltiplicatore è significativamente maggiore di 1
            if (currentMult > 1.01f) 
            {
                if (!energyMultiplierText.gameObject.activeSelf) 
                    energyMultiplierText.gameObject.SetActive(true);

                energyMultiplierText.text = $"x {currentMult:F2}";
            }
            else
            {
                if (energyMultiplierText.gameObject.activeSelf) 
                    energyMultiplierText.gameObject.SetActive(false);
            }
        }

        // 6. RESET QUANTISTICO
        if (prestigeInfoText)
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
            if(planetTravelPanel) planetTravelPanel.SetActive(false);
            return;
        }

        BigDouble currentPlanetValue = pm.CalculatePlanetValue();
        bool canShowPanel = currentPlanetValue >= currentPlanet.requiredPlanetValue || pm.isPreparingForLaunch || pm.isTraveling;
        
        planetTravelPanel.SetActive(canShowPanel);
        if (!canShowPanel) return;

        if (planetValueText)
        {
            planetValueText.text = $"Planet Value: {FormatNumber(currentPlanetValue)} / {FormatNumber(currentPlanet.requiredPlanetValue)}";
        }
        
        if (pm.isTraveling)
        {
            // Travel is in progress
            startPreparationButton.gameObject.SetActive(false);
            startTravelButton.gameObject.SetActive(false);
            launchProgressBar.gameObject.SetActive(false);
            travelStatusText.gameObject.SetActive(true);

            TimeSpan timeRemaining = TimeSpan.FromSeconds(PlanetManager.TRAVEL_DURATION_SECONDS) - (DateTime.UtcNow - pm.travelStartTime);
            if (timeRemaining.TotalSeconds > 0)
            {
                travelStatusText.text = $"Time to arrival: {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
            }
            else
            {
                travelStatusText.text = "Arriving...";
            }
        }
        else if (pm.isPreparingForLaunch)
        {
            // Preparation is in progress
            startPreparationButton.gameObject.SetActive(false);
            startTravelButton.gameObject.SetActive(false);
            launchProgressBar.gameObject.SetActive(true);
            travelStatusText.gameObject.SetActive(false);

            BigDouble energyRequirement = pm.GetLaunchEnergyRequirement();
            if (energyRequirement > 0)
            {
                // CORREZIONE QUI: Aggiunto .ToDouble() prima del cast a float
                launchProgressBar.value = (float)(pm.launchPreparationProgress / energyRequirement).ToDouble();
            }
        }
        else
        {
            // Ready to start preparation or travel
            BigDouble energyRequirement = pm.GetLaunchEnergyRequirement();
            bool preparationComplete = pm.launchPreparationProgress >= energyRequirement && energyRequirement > 0;

            startPreparationButton.gameObject.SetActive(!preparationComplete);
            startTravelButton.gameObject.SetActive(preparationComplete);
            launchProgressBar.gameObject.SetActive(false);
            travelStatusText.gameObject.SetActive(false);

            startPreparationButton.interactable = currentPlanetValue >= currentPlanet.requiredPlanetValue;
        }
    }

    void CheckBottleneck()
    {
        bool isBottleneck = gm.RawProductionRate > gm.LogisticsCap;
        Color targetColor = isBottleneck ? warningColor : normalColor;

        if(incomeText) incomeText.color = targetColor;
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