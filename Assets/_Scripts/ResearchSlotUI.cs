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
    private System.Action<ResearchItem> _buyAction;

    // Questa funzione riempie la riga con i dati
    public void Setup(ResearchItem item, System.Action<ResearchItem> onBuyClick)
    {
        _myData = item;
        _buyAction = onBuyClick;

        titleText.text = item.title;
        descText.text = item.description;
        if(item.icon != null) iconImage.sprite = item.icon;

        // Configura il click del bottone
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => _buyAction(_myData));

        RefreshUI();
    }

    public void RefreshUI()
{
    // 1. Sicurezza Dati: Se non ci sono dati, fermati.
    if (_myData == null) return;

    // 2. Sicurezza Prezzo: Scrivi SOLO se costText è collegato
    if (costText != null) 
    {
        costText.text = _myData.GetCost().ToString("F0") + " Energy";
    }
    else 
    {
        // Questo messaggio apparirà in console se ti sei dimenticato il collegamento!
        Debug.LogWarning("Attenzione: Manca il collegamento a 'Cost Text' nel Prefab!");
    }
    
    // 3. Sicurezza Livello/Barra
    if (progressBar != null)
    {
        if (_myData.maxLevel > 0)
            progressBar.value = (float)_myData.currentLevel / _myData.maxLevel;
        else
            progressBar.value = 0; 
    }
}
}