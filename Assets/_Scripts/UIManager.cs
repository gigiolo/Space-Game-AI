using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Fondamentale per il Hold
using TMPro;        
using BreakInfinity; 

public class UIManager : MonoBehaviour
{
    [Header("Top HUD")]
    public TextMeshProUGUI scoreText;            
    public TextMeshProUGUI incomeText;           
    public TextMeshProUGUI logisticsStatusText; 
    public TextMeshProUGUI storageText; // NUOVO: Serve indicatore Storage

    [Header("Bottom Deck")]
    // Sostituiamo il semplice Button con un EventTrigger o gestore custom
    public GameObject mainEnergyButtonObj; 
    public Button buyEmitterButton; // Ex Habitat
    public Button buyLogisticsButton;            
    
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
            
            // Setup Hold Button (Event Trigger manuale)
            SetupHoldButton();

            RefreshUI();
        }
    }
    
    // Configura il sistema "Tieni premuto"
    void SetupHoldButton()
    {
        if (mainEnergyButtonObj == null) return;

        EventTrigger trigger = mainEnergyButtonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = mainEnergyButtonObj.AddComponent<EventTrigger>();

        // Evento Down
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { gm.SetHoldState(true); });
        trigger.triggers.Add(entryDown);

        // Evento Up
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
        // Sicurezza: se il GameManager non è ancora pronto, esci
        if (gm == null) return;

        // 1. ENERGIA ATTUALE (Score Text)
        // Mostra solo l'energia che possiedi in questo momento
        if (scoreText)
            scoreText.text = $"{FormatNumber(gm.CurrentEnergy)} Energy";

        // 2. CAPACITÀ MASSIMA (Il nuovo testo "StorageCapText")
        // Mostra quanto puoi accumulare al massimo
        if (storageText)
            storageText.text = $"Max Cap: {FormatNumber(gm.StorageCap)}";

        // 3. INCOME (Guadagno al secondo)
        // Grazie alla modifica precedente, ora vedrai i decimali (es. +1.10/s)
        if (incomeText)
            incomeText.text = $"+{FormatNumber(gm.EffectiveIncomePerSec)}/s";

        // 4. STATO LOGISTICA
        // Mostra Produzione Potenziale vs Capacità di Trasporto
        if (logisticsStatusText)
            logisticsStatusText.text = $"Prod: {FormatNumber(gm.RawProductionRate)} | Log: {FormatNumber(gm.LogisticsCap)}";

        // 5. AGGIORNAMENTO COSTI DEI PULSANTI
        // Aggiorna il prezzo scritto sopra i bottoni di acquisto
        if (emitterCostText) 
            emitterCostText.text = "Emitter: " + FormatNumber(gm.GetEmitterCost());

        if (logisticsCostText) 
            logisticsCostText.text = "Logistics: " + FormatNumber(gm.GetLogisticsCost());

        // 6. CONTROLLO VISIVO COLLO DI BOTTIGLIA
        // Cambia il colore se la produzione supera la logistica
        CheckBottleneck();
    }

    void CheckBottleneck()
    {
        // Il bottleneck c'è se la produzione POTENZIALE supera la Logistica
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