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
    public Button buyButton;
    public Slider progressBar;
    public Image iconImage;

    private ResearchItem _myData;
    // Qui definiamo che la Setup vuole una "Action" (un metodo), non il Manager intero
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

    // Questo metodo DEVE essere public
    public void RefreshUI()
    {
        if (_myData == null) return;

        if (costText != null) 
        {
            // Se è maxato, mostra "MAX", altrimenti il prezzo
            if (_myData.IsMaxed())
                costText.text = "MAX";
            else
                costText.text = _myData.GetCost().ToString("F0") + " Energy";
        }
        
        if (progressBar != null)
        {
            if (_myData.maxLevel > 0)
                progressBar.value = (float)_myData.currentLevel / _myData.maxLevel;
            else
                progressBar.value = 0; 
        }
    }
}