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
                costText.text = cost.ToString("F0") + " Energy";

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
}