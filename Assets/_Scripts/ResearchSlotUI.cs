using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Necessario per rilevare la pressione
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

    // --- VARIABILI PER IL SISTEMA HOLD-TO-BUY ---
    private bool _isHolding = false;
    private float _nextBuyTime = 0f;

    // --- CONFIGURAZIONE VELOCITA' ---
    private const float INITIAL_DELAY = 0.4f; // Pausa prima che parta l'autofire
    private const float BUY_SPEED = 0.08f;    // Velocità costante (es. 0.08s = 12.5 acquisti al secondo)

    public void Setup(ResearchItem item, System.Action<ResearchItem> onBuyClick)
    {
        _myData = item;
        _buyAction = onBuyClick;

        if (titleText) titleText.text = item.title;
        if (descText) descText.text = item.description;
        if (item.icon != null && iconImage) iconImage.sprite = item.icon;

        // --- SETUP HOLD TO BUY ---
        // Rimuoviamo i vecchi listener standard
        buyButton.onClick.RemoveAllListeners();

        // Aggiungiamo trigger personalizzati per rilevare Pressione e Rilascio
        SetupCustomButtonEvents();

        RefreshUI();
    }

    private void SetupCustomButtonEvents()
    {
        EventTrigger trigger = buyButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = buyButton.gameObject.AddComponent<EventTrigger>();
        
        trigger.triggers.Clear();

        // Evento: Pressione (Down)
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { OnPointerDown(); });
        trigger.triggers.Add(entryDown);

        // Evento: Rilascio (Up)
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { OnPointerUp(); });
        trigger.triggers.Add(entryUp);
        
        // Evento: Uscita (Exit) - Se trascini il dito fuori dal bottone
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnPointerUp(); });
        trigger.triggers.Add(entryExit);
    }

    private void OnPointerDown()
    {
        if (!buyButton.interactable) return;

        _isHolding = true;

        // 1. ACQUISTO IMMEDIATO (Snappy feel)
        TryBuy();

        // 2. Imposta il tempo per il prossimo acquisto (Ritardo iniziale)
        _nextBuyTime = Time.time + INITIAL_DELAY;
    }

    private void OnPointerUp()
    {
        _isHolding = false;
    }

    private void Update()
    {
        // Se stiamo tenendo premuto e il bottone è ancora cliccabile (abbiamo soldi e non è maxato)
        if (_isHolding && buyButton.interactable)
        {
            if (Time.time >= _nextBuyTime)
            {
                TryBuy();
                // Imposta il prossimo acquisto usando la velocità costante
                _nextBuyTime = Time.time + BUY_SPEED;
            }
        }
    }

    private void TryBuy()
    {
        if (_myData.IsMaxed()) 
        {
            _isHolding = false; 
            return;
        }

        BigDouble cost = _myData.GetCost();
        if (GameManager.Instance.CurrentEnergy >= cost)
        {
            _buyAction(_myData);
        }
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
                costText.text = FormatNumber(cost) + " Energy";
                if (canAfford) costText.color = Color.black;
                else costText.color = new Color(0.4f, 0.4f, 0.4f, 1f);
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
                if (isMaxed) btnBg.color = Color.gray; 
                else if (canAfford) btnBg.color = GameManager.Instance.activeTheme.primaryAction;
                else btnBg.color = new Color(0.25f, 0.25f, 0.25f, 1f); 
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

    private string FormatNumber(BigDouble number)
    {
        if (number < 10) return number.ToString("F2");
        if (number < 1000) return number.ToString("F0");

        long exponent = (long)BigDouble.Log10(number);
        if (exponent < 6) return (number / 1000).ToString("F2") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        
        return number.ToString("e2");
    }
}