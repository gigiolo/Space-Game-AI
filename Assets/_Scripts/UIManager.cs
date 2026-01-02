using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;        
using BreakInfinity; 

public class UIManager : MonoBehaviour
{
    [Header("Top HUD")]
    public TextMeshProUGUI scoreText;            
    public TextMeshProUGUI incomeText;           
    public TextMeshProUGUI logisticsStatusText; 
    public TextMeshProUGUI storageText; 

    [Header("Bottom Deck")]
    public GameObject mainEnergyButtonObj; 
    public Button buyEmitterButton; 
    public Button buyLogisticsButton;            
    
    [Header("RESET QUANTISTICO")] // <--- NUOVA SEZIONE
    public Button prestigeButton;
    public TextMeshProUGUI prestigeInfoText; // Testo dentro o sopra il bottone (es: "Prestige for +5 Nodes")

    [Header("Visual Feedback")]
    public Image logisticsButtonImage;            
    public Color normalColor = Color.white;       
    public Color warningColor = new Color(1f, 0.3f, 0.3f); 
    public TextMeshProUGUI emitterCostText;
    public TextMeshProUGUI logisticsCostText;

    private GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnEconomyUpdated += RefreshUI;
            
            // Setup Bottoni Acquisto
            if(buyEmitterButton) buyEmitterButton.onClick.AddListener(gm.BuyEmitter);
            if(buyLogisticsButton) buyLogisticsButton.onClick.AddListener(gm.BuyLogistics);
            
            // Setup Bottone PRESTIGIO
            if(prestigeButton) prestigeButton.onClick.AddListener(gm.PerformQuantumReset);
            
            // Setup Hold Button
            SetupHoldButton();

            RefreshUI();
        }
    }
    
    void SetupHoldButton()
    {
        if (mainEnergyButtonObj == null) return;

        EventTrigger trigger = mainEnergyButtonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = mainEnergyButtonObj.AddComponent<EventTrigger>();

        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { gm.SetHoldState(true); });
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { gm.SetHoldState(false); });
        trigger.triggers.Add(entryUp);
    }

    void OnDestroy()
    {
        if (gm != null) gm.OnEconomyUpdated -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (gm == null) return;

        // 1. ENERGIA E INCOME
        if (scoreText) scoreText.text = $"{FormatNumber(gm.CurrentEnergy)} Energy";
        if (storageText) storageText.text = $"Max Cap: {FormatNumber(gm.StorageCap)}";
        if (incomeText) incomeText.text = $"+{FormatNumber(gm.EffectiveIncomePerSec)}/s";

        // 2. LOGISTICA
        if (logisticsStatusText)
            logisticsStatusText.text = $"Prod: {FormatNumber(gm.RawProductionRate)} | Log: {FormatNumber(gm.LogisticsCap)}";

        // 3. COSTI BOTTONI
        if (emitterCostText) emitterCostText.text = "Emitter: " + FormatNumber(gm.GetEmitterCost());
        if (logisticsCostText) logisticsCostText.text = "Logistics: " + FormatNumber(gm.GetLogisticsCost());
        
        // 4. RESET QUANTISTICO (NUOVO)
        if (prestigeInfoText)
        {
            BigDouble potentialNodes = gm.CalculatePotentialNodes();
            // Mostra anche quanti ne hai già accumulati
            prestigeInfoText.text = $"RESET (Current: {gm.ScientificNodes})\nGain: <color=#00FFFF>+{FormatNumber(potentialNodes)} Nodes</color>";
            
            // Opzionale: Disabilita il tasto se guadagni 0
            if (prestigeButton) prestigeButton.interactable = potentialNodes > 0;
        }

        CheckBottleneck();
    }

    void CheckBottleneck()
    {
        bool isBottleneck = gm.RawProductionRate > gm.LogisticsCap;
        Color targetColor = isBottleneck ? warningColor : normalColor;

        if(incomeText) incomeText.color = targetColor;
        if(logisticsStatusText) logisticsStatusText.color = targetColor;
        if(logisticsButtonImage) logisticsButtonImage.color = targetColor;
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