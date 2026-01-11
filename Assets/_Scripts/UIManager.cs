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
    
    [Tooltip("Collega qui il testo che prima mostrava la Capacità Massima")]
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
            
            if(prestigeButton) prestigeButton.onClick.AddListener(gm.PerformQuantumReset);

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

        // 5. RESET QUANTISTICO
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