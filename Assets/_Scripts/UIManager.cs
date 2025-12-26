using UnityEngine;
using UnityEngine.UI;
using TMPro;       
using BreakInfinity; 

public class UIManager : MonoBehaviour
{
    [Header("--- 1. COLLEGA QUI I TESTI (TOP HUD) ---")]
    public TextMeshProUGUI scoreText;           
    public TextMeshProUGUI incomeText;          
    public TextMeshProUGUI logisticsStatusText; 

    [Header("--- 2. COLLEGA QUI I BOTTONI (BOTTOM DECK) ---")]
    public Button clickButton;                  
    public Button buyHabitatButton;             
    public Button buyLogisticsButton;           

    [Header("--- 4. VISUAL FEEDBACK (BOTTLENECK) ---")]
    public Image logisticsButtonImage;          
    public Color normalColor = Color.white;     
    public Color warningColor = new Color(1f, 0.3f, 0.3f); 

    private GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;

        if (gm != null)
        {
            gm.OnEconomyUpdated += RefreshUI;
            
            if(clickButton) clickButton.onClick.AddListener(() => gm.HandleManualTap());

            // Collegamento manuale dei bottoni upgrade
            if(buyHabitatButton) buyHabitatButton.onClick.AddListener(() => gm.BuyHabitat());
            if(buyLogisticsButton) buyLogisticsButton.onClick.AddListener(() => gm.BuyLogistics());

            RefreshUI();
        }
    }

    void OnDestroy()
    {
        if (gm != null) gm.OnEconomyUpdated -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (gm == null) return;

        // 1. AGGIORNA TOP HUD
        if (scoreText != null)
            scoreText.text = $"{FormatNumber(gm.CurrentEnergy)} Energy";

        if (incomeText != null)
            incomeText.text = $"+{FormatNumber(gm.IncomePerSec)}/sec";

        if (logisticsStatusText != null)
            logisticsStatusText.text = $"Prod: {FormatNumber(gm.GenerationRate)} | Cap: {FormatNumber(gm.LogisticsCap)}";

        CheckBottleneck();
    }

    void CheckBottleneck()
    {
        bool isBottleneck = gm.GenerationRate > gm.LogisticsCap;
        Color targetColor = isBottleneck ? warningColor : normalColor;

        if(incomeText) incomeText.color = targetColor;
        if(logisticsStatusText) logisticsStatusText.color = targetColor;
        if(logisticsButtonImage) logisticsButtonImage.color = targetColor;
    }

    // --- FORMATTAZIONE CORRETTA E UNIVERSALE ---
    private string FormatNumber(BigDouble number)
    {
        // Gestione numeri piccoli e zero
        if (number < 1000) return number.ToString("F0");

        // SOLUZIONE MATEMATICA:
        // Log10 ci dice la potenza di 10 (es. Log10(1000) = 3)
        // Usiamo (long) per trasformarlo in un numero intero.
        long exponent = (long)BigDouble.Log10(number);
        
        if (exponent < 6) return (number / 1000).ToString("F2") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        
        // Per numeri ancora più grandi, usiamo la notazione scientifica
        return number.ToString("e2");
    }
}