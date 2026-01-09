using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BreakInfinity;

public class ResearchSlotUI : MonoBehaviour
{
    [Header("Collegamenti UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    
    [Tooltip("Collega qui il testo verde 'Level'")]
    public TextMeshProUGUI levelText; 
    
    public Button buyButton;
    public Slider progressBar;
    public Image iconImage;

    private ResearchItem _myData;
    private System.Action<ResearchItem> _buyAction;

    public void Setup(ResearchItem item, System.Action<ResearchItem> onBuyClick)
    {
        _myData = item;
        _buyAction = onBuyClick;

        if (titleText) titleText.text = item.title;
        if (descText) descText.text = item.description;
        if (item.icon != null && iconImage) iconImage.sprite = item.icon;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => _buyAction(_myData));

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_myData == null) return;
        if (GameManager.Instance == null) return;

        BigDouble cost = _myData.GetCost();
        bool isMaxed = _myData.IsMaxed();
        bool canAfford = GameManager.Instance.CurrentEnergy >= cost;

        // 1. GESTIONE TESTO COSTO
        if (costText != null) 
        {
            if (isMaxed)
            {
                costText.text = "MAX";
                costText.color = Color.white;
            }
            else
            {
                // --- MODIFICA QUI: Usiamo il metodo FormatNumber invece di ToString("F0") ---
                costText.text = FormatNumber(cost) + " Energy";

                if (canAfford)
                {
                    costText.color = Color.black;
                }
                else
                {
                    costText.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                }
            }
        }
        
        // 2. GESTIONE SFONDO BOTTONE
        if (buyButton != null)
        {
            bool isInteractable = !isMaxed && canAfford;
            buyButton.interactable = isInteractable;

            Image btnBg = buyButton.GetComponent<Image>();
            
            if (btnBg != null && GameManager.Instance.activeTheme != null)
            {
                if (isMaxed)
                {
                    btnBg.color = Color.gray; 
                }
                else if (canAfford)
                {
                    btnBg.color = GameManager.Instance.activeTheme.primaryAction;
                }
                else
                {
                    btnBg.color = new Color(0.25f, 0.25f, 0.25f, 1f); 
                }
            }
        }

        // 3. Barra Progresso
        if (progressBar != null)
        {
            if (_myData.maxLevel > 0)
                progressBar.value = (float)_myData.currentLevel / _myData.maxLevel;
            else
                progressBar.value = 0; 
        }

        // 4. Testo Livello
        if (levelText != null)
        {
            levelText.text = $"Level <color=white>{_myData.currentLevel}</color>/{_myData.maxLevel}";
        }
    }

    // --- NUOVO METODO PER FORMATTAZIONE INTELLIGENTE ---
    private string FormatNumber(BigDouble number)
    {
        // Se il numero è molto piccolo (es. 0.49, 3.25, 9.99)
        // Mostriamo 2 cifre decimali
        if (number < 10) 
            return number.ToString("F2");

        // Se il numero è medio (es. 10, 500, 999)
        // Mostriamo solo l'intero (niente decimali inutili)
        if (number < 1000) 
            return number.ToString("F0");

        // Se il numero è grande (es. 1200 -> 1.20k)
        long exponent = (long)BigDouble.Log10(number);
        if (exponent < 6) return (number / 1000).ToString("F2") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        
        return number.ToString("e2");
    }
}