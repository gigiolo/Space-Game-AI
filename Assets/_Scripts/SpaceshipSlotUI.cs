using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BreakInfinity;

public class SpaceshipSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI speedText; 
    public Button buyButton;
    public Slider progressBar;
    public Image iconImage;

    [Header("Currency Icons")]
    public Sprite energyIcon;
    public Sprite iridiumIcon;
    public Image costIconImage;

    private SpaceshipItem _data;
    private System.Action<SpaceshipItem> _onBuy;

    public void Setup(SpaceshipItem item, System.Action<SpaceshipItem> onBuyCallback)
    {
        _data = item;
        _onBuy = onBuyCallback;

        if (titleText) titleText.text = item.info.title;
        if (descText) descText.text = item.info.description;
        if (iconImage && item.info.icon) iconImage.sprite = item.info.icon;
        
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => _onBuy?.Invoke(_data));

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_data == null || GameManager.Instance == null) return;

        BigDouble cost = _data.GetCost();
        bool isMaxed = _data.IsMaxed();
        bool canAfford = false;

        if (_data.info.currencyType == SpaceshipCurrency.Energy)
            canAfford = GameManager.Instance.CurrentEnergy >= cost;
        else
            canAfford = GameManager.Instance.PureIridium >= cost;

        if (costIconImage)
        {
            costIconImage.sprite = (_data.info.currencyType == SpaceshipCurrency.Energy) ? energyIcon : iridiumIcon;
        }

        if (costText)
        {
            if (isMaxed) costText.text = "MAX";
            else costText.text = FormatNumber(cost);
            
            costText.color = canAfford ? Color.white : Color.red;
        }

        buyButton.interactable = !isMaxed && canAfford;

        if (speedText)
        {
            BigDouble currentSpd = _data.GetCurrentSpeed();
            speedText.text = $"Speed: {FormatNumber(currentSpd)} Km/s";
        }

        if (progressBar)
        {
            if (_data.info.maxLevel > 0)
                progressBar.value = (float)_data.currentLevel / _data.info.maxLevel;
            else
                progressBar.value = 0;
        }

        if (levelText) levelText.text = $"Lvl {_data.currentLevel}";
    }

    // --- METODO CORRETTO ---
    private string FormatNumber(BigDouble number)
    {
        if (number < 10) return number.ToString("F2");
        if (number < 1000) return number.ToString("F0");
        long exponent = (long)BigDouble.Log10(number);
        
        if (exponent < 6) return (number / 1000).ToString("F2") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        
        // CORREZIONE QUI
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}