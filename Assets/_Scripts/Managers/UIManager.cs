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
            return;
        }
        Instance = this;
    }

    void Start()
    {
        gm = GameManager.Instance;
        pm = PlanetManager.Instance;
        
        if (gm != null)
        {
            // Ci iscriviamo agli aggiornamenti
            gm.OnEconomyUpdated += RefreshUI;
            
            if(prestigeButton) 
            {
                prestigeButton.onClick.RemoveAllListeners();
                prestigeButton.onClick.AddListener(gm.PerformQuantumReset);
            }

            // Planet Travel Button Listeners
            if (pm != null)
            {
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

            if (optionsButton != null && optionsMenuController != null)
            {
                optionsButton.onClick.RemoveAllListeners();
                optionsButton.onClick.AddListener(optionsMenuController.ToggleMenu);
            }
            
            SetupHoldButton();
            
            // Forziamo un aggiornamento immediato appena parte la scena
            RefreshUI();
        }
    }
    
    void SetupHoldButton()
    {
        if (mainEnergyButtonObj == null) return;

        EventTrigger trigger = mainEnergyButtonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = mainEnergyButtonObj.AddComponent<EventTrigger>();
        
        // Pulisce trigger precedenti per evitare duplicati nei reload
        trigger.triggers.Clear();

        // Quando premi (PointerDown), chiami OnEnergyButtonPress
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { if(gm) gm.OnEnergyButtonPress(); }); 
        trigger.triggers.Add(entryDown);

        // Quando rilasci (PointerUp), chiami OnEnergyButtonRelease
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { if(gm) gm.OnEnergyButtonRelease(); });
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
        if (scoreText != null) scoreText.text = $"{FormatNumber(gm.CurrentEnergy)} Energy";
        
        // 2. TEMPO OFFLINE (Sostituisce Max Cap)
        if (storageText != null) 
        {
            TimeSpan ts = TimeSpan.FromSeconds(gm.MaxOfflineSeconds);
            string formattedTime = string.Format("{0}h {1:D2}m", (int)ts.TotalHours, ts.Minutes);
            storageText.text = $"Offline: {formattedTime}";
        }

        // 3. INCOME
        if (incomeText != null) incomeText.text = $"+{FormatNumber(gm.EffectiveIncomePerSec)}/s";

        // 4. LOGISTICA E EMETTITORI
        if (logisticsStatusText != null)
        {
            string emitterString = $"Units: {gm.EmitterCount} / {gm.EmitterCap}";
            
            if (gm.EmitterCount >= gm.EmitterCap)
            {
                emitterString = $"<color=red>{emitterString} (MAX)</color>";
            }

            logisticsStatusText.text = $"{emitterString}\nProd: {FormatNumber(gm.RawProductionRate)} | Log Cap: {FormatNumber(gm.LogisticsCap)}";
        }

        // --- NUOVO: Visualizzazione Velocità Crescita Nanobot ---
        if (emitterGrowthText != null)
        {
            // Se siamo pieni, scriviamo MAX
            if (gm.EmitterCount >= gm.EmitterCap)
            {
                emitterGrowthText.text = "Growth: <color=red>PAUSED (Max Cap)</color>";
            }
            else
            {
                // Mostriamo la velocità: es "+0.30/s"
                double speed = gm.EmitterAutoGrowthSpeed;
                emitterGrowthText.text = $"Growth: <color=#00FF00>+{speed:F2}/s</color>";
            }
        }
        // -------------------------------------------------------

        // 5. MOLTIPLICATORE ENERGY BUTTON
        if (energyMultiplierText != null)
        {
            float currentMult = gm.CurrentEnergyMultiplier;

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
        {
            planetValueText.text = $"Planet Value: {FormatNumber(currentPlanetValue)} / {FormatNumber(currentPlanet.requiredPlanetValue)}";
        }
        
        bool isTraveling = pm.isTraveling;
        bool isPreparing = pm.isPreparingForLaunch;

        if (isTraveling)
        {
            if(startPreparationButton) startPreparationButton.gameObject.SetActive(false);
            if(startTravelButton) startTravelButton.gameObject.SetActive(false);
            if(launchProgressBar) launchProgressBar.gameObject.SetActive(false);
            
            if(travelStatusText)
            {
                travelStatusText.gameObject.SetActive(true);
                TimeSpan timeRemaining = TimeSpan.FromSeconds(PlanetManager.TRAVEL_DURATION_SECONDS) - (DateTime.UtcNow - pm.travelStartTime);
                if (timeRemaining.TotalSeconds > 0)
                    travelStatusText.text = $"Time to arrival: {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
                else
                    travelStatusText.text = "Arriving...";
            }
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
        }
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