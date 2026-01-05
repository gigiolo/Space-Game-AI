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
    
    [Header("RESET QUANTISTICO")]
    public Button prestigeButton;
    public TextMeshProUGUI prestigeInfoText; 

    [Header("OPTIONS MENU")]
    public Button optionsButton;               
    public OptionsMenu optionsMenuController;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;        
    public Color warningColor = new Color(1f, 0.3f, 0.3f); 

    private GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnEconomyUpdated += RefreshUI;
            
            // Setup Bottone PRESTIGIO
            if(prestigeButton) prestigeButton.onClick.AddListener(gm.PerformQuantumReset);

            // Setup Bottone OPZIONI
            if (optionsButton != null && optionsMenuController != null)
            {
                optionsButton.onClick.AddListener(optionsMenuController.ToggleMenu);
            }
            
            // Setup Hold Button
            SetupHoldButton();

            // Aggiornamento iniziale
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

        // 2. LOGISTICA E EMETTITORI (Nuovo Display)
        if (logisticsStatusText)
        {
            // Mostra: Units: 1 / 1 (MAX)
            string emitterString = $"Units: {gm.EmitterCount} / {gm.EmitterCap}";
            
            // Se siamo pieni, colora di rosso
            if (gm.EmitterCount >= gm.EmitterCap)
            {
                emitterString = $"<color=red>{emitterString} (MAX)</color>";
            }

            logisticsStatusText.text = $"{emitterString}\nProd: {FormatNumber(gm.RawProductionRate)} | Log Cap: {FormatNumber(gm.LogisticsCap)}";
        }

        // 3. RESET QUANTISTICO
        if (prestigeInfoText)
        {
            BigDouble potentialNodes = gm.CalculatePotentialNodes();
            prestigeInfoText.text = $"RESET (Current: {gm.ScientificNodes})\nGain: <color=#00FFFF>+{FormatNumber(potentialNodes)} Nodes</color>";
            
            if (prestigeButton) prestigeButton.interactable = potentialNodes > 0;
        }

        CheckBottleneck();
    }

    void CheckBottleneck()
    {
        // Se produciamo più di quanto la logistica può trasportare
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