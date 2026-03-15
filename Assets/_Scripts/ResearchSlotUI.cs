using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BreakInfinity;

public class ResearchSlotUI : MonoBehaviour
{
    [Header("Collegamenti UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI levelText; 
    public Button buyButton;
    public Slider progressBar;
    public Image iconImage;
    
    [Header("Gestione Blocco")]
    public CanvasGroup mainCanvasGroup; 

    private ResearchItem _myData;
    private System.Action<ResearchItem> _buyAction;
    private Image _buttonBg; 

    private bool _isHolding = false;
    private float _nextBuyTime = 0f;
    private const float INITIAL_DELAY = 0.4f;
    private const float BUY_SPEED = 0.08f;

    public void Setup(ResearchItem item, System.Action<ResearchItem> onBuyClick)
    {
        _myData = item;
        _buyAction = onBuyClick;
        _buttonBg = buyButton.GetComponent<Image>();

        if (mainCanvasGroup == null) mainCanvasGroup = GetComponent<CanvasGroup>();

        if (titleText) titleText.text = item.title;
        if (descText) descText.text = item.description;
        if (item.icon != null && iconImage) iconImage.sprite = item.icon;

        buyButton.onClick.RemoveAllListeners();
        SetupCustomButtonEvents();

        RefreshUI();
    }

    private void SetupCustomButtonEvents()
    {
        EventTrigger trigger = buyButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = buyButton.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((data) => { OnPointerDown(); });
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entryUp.callback.AddListener((data) => { OnPointerUp(); });
        trigger.triggers.Add(entryUp);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => { OnPointerUp(); });
        trigger.triggers.Add(entryExit);
    }

    private void OnPointerDown()
    {
        if (!buyButton.interactable) return;
        _isHolding = true;
        TryBuy();
        _nextBuyTime = Time.time + INITIAL_DELAY;
    }

    private void OnPointerUp() => _isHolding = false;

    private void Update()
    {
        if (_isHolding && buyButton.interactable)
        {
            if (Time.time >= _nextBuyTime)
            {
                TryBuy();
                _nextBuyTime = Time.time + BUY_SPEED;
            }
        }
    }

    private void TryBuy()
    {
        if (_myData.IsMaxed() || ResearchManager.Instance == null) { _isHolding = false; return; }
        
        // Verifica di sicurezza (sebbene il bottone sia invisibile, meglio lasciarlo)
        if (!ResearchManager.Instance.IsTierUnlocked(_myData.tier)) { _isHolding = false; return; }

        if (GameManager.Instance.CurrentEnergy >= _myData.GetCost())
            _buyAction(_myData);
    }

    public void RefreshUI()
    {
        if (_myData == null || GameManager.Instance == null || ResearchManager.Instance == null) return;

        // --- 1. VERIFICA DELLO SBLOCCO TIER ---
        bool isTierUnlocked = ResearchManager.Instance.IsTierUnlocked(_myData.tier);

        // Se il tier è bloccato, spegniamo questo GameObject.
        // Questo dirà al Vertical Layout Group di collassare lo spazio, nascondendo la ricerca!
        if (gameObject.activeSelf != isTierUnlocked)
        {
            gameObject.SetActive(isTierUnlocked);
        }

        // Se è invisibile, non ha senso sprecare CPU per aggiornare testi e colori. Usciamo.
        if (!isTierUnlocked) return;

        // --- 2. AGGIORNAMENTO NORMALE (Se visibile) ---
        BigDouble cost = _myData.GetCost();
        bool isMaxed = _myData.IsMaxed();
        bool canAfford = GameManager.Instance.CurrentEnergy >= cost;

        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f; // Assicuriamoci che sia opaco

        if (costText != null) 
        {
            if (isMaxed) 
            {
                if (costText.text != "MAX") costText.text = "MAX"; 
                costText.color = Color.black;
            } 
            else 
            {
                costText.text = FormatNumber(cost) + " Energy";
                costText.color = canAfford ? Color.black : new Color(0.4f, 0.4f, 0.4f, 1f);
            }
        }
        
        if (buyButton != null)
        {
            buyButton.interactable = !isMaxed && canAfford;
            
            if (_buttonBg != null && GameManager.Instance.activeTheme != null)
            {
                Color targetCol = isMaxed ? Color.gray : 
                                 (canAfford ? GameManager.Instance.activeTheme.primaryAction : new Color(0.25f, 0.25f, 0.25f, 1f));
                if(_buttonBg.color != targetCol) _buttonBg.color = targetCol;
            }
        }

        if (progressBar != null)
            progressBar.value = _myData.maxLevel > 0 ? (float)_myData.currentLevel / _myData.maxLevel : 0;

        if (levelText != null)
            levelText.text = $"Level <color=white>{_myData.currentLevel}</color>/{_myData.maxLevel}";
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 10) return number.ToString("F2");
        if (number < 1000) return number.ToString("F0");
        
        long exponent = (long)BigDouble.Log10(number);
        if (exponent < 6) return (number / 1000).ToString("F1") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}