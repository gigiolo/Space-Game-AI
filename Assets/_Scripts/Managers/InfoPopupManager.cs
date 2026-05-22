// --- File: _Scripts\UI\InfoPopupManager.cs ---
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InfoPopupManager : MonoBehaviour
{
    public static InfoPopupManager Instance { get; private set; }

    [Header("Riferimenti UI")]
    [Tooltip("Il pannello principale del popup (deve avere UIPopupEffect)")]
    public GameObject popupPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    public Button closeButton;

    private Canvas _popupCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            
            // RIMOSSA LA REGISTRAZIONE AL UIMANAGER
            // Questo impedisce che il pannello interagisca con la chiusura degli altri menu.
            
            SetupCanvasSorting();
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    private void SetupCanvasSorting()
    {
        // 1. Aggiunge un Canvas indipendente al pannello se non esiste
        _popupCanvas = popupPanel.GetComponent<Canvas>();
        if (_popupCanvas == null)
        {
            _popupCanvas = popupPanel.AddComponent<Canvas>();
        }
        
        // 2. Aggiunge un GraphicRaycaster per permettere ai bottoni di funzionare nel nuovo Canvas
        if (popupPanel.GetComponent<GraphicRaycaster>() == null)
        {
            popupPanel.AddComponent<GraphicRaycaster>();
        }

        // 3. Sovrascrive l'ordine gerarchico e lo spinge in primissimo piano
        _popupCanvas.overrideSorting = true;
        _popupCanvas.sortingOrder = 100; 
    }

    public void ShowInfo(string title, string content)
    {
        if (titleText != null) titleText.text = title;
        if (contentText != null) contentText.text = content;
        
        // Sicurezza per l'ordine nella gerarchia base
        popupPanel.transform.SetAsLastSibling();
        
        popupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        if (popupPanel == null) return;

        UIPopupEffect effect = popupPanel.GetComponent<UIPopupEffect>();
        if (effect != null)
        {
            // Passiamo "false" per evitare che UIPopupEffect scateni eventi di chiusura globale
            effect.Close(false);
        }
        else
        {
            popupPanel.SetActive(false);
        }
    }
}