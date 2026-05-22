// --- File: _Scripts\DraggableTheoryUI.cs ---
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ArtifactSlotUI))]
public class DraggableTheoryUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
{
    public PhysicalTheorySO theory;
    
    private ArtifactsMenuUI _menuManager;
    private GameObject _ghostIcon;
    private Canvas _mainCanvas;

    // --- NUOVE VARIABILI PER RISOLVERE IL CONFLITTO ---
    private ScrollRect _scrollRect;
    private bool _isScrolling;
    private float _pointerDownTime;
    private const float HOLD_TO_DRAG_TIME = 0.2f; // Tempo in secondi da attendere per "staccare" l'artefatto

    public void Setup(PhysicalTheorySO t, ArtifactsMenuUI manager, Canvas canvas)
    {
        theory = t;
        _menuManager = manager;
        _mainCanvas = canvas;
        
        // Cerchiamo automaticamente la Scroll View in cui ci troviamo
        _scrollRect = GetComponentInParent<ScrollRect>();
    }

    // Registra il momento in cui il dito tocca lo schermo
    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDownTime = Time.unscaledTime;
    }

    // GESTIONE TAP: Apre il popup dei dettagli
    public void OnPointerClick(PointerEventData eventData)
    {
        // Ignoriamo il click se stavamo scrollando la lista
        if (!eventData.dragging && theory != null && !_isScrolling)
        {
            _menuManager.OpenDetailsPopup(theory);
        }
    }

    // GESTIONE TRASCINAMENTO
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_scrollRect == null) _scrollRect = GetComponentInParent<ScrollRect>();

        // 1. CONTROLLO INTENZIONE DELL'UTENTE
        // Se si inizia a trascinare quasi subito (sotto gli 0.2s), passiamo l'evento alla Scroll View
        if (Time.unscaledTime - _pointerDownTime < HOLD_TO_DRAG_TIME)
        {
            _isScrolling = true;
            if (_scrollRect != null) _scrollRect.OnBeginDrag(eventData);
            return;
        }

        _isScrolling = false;

        // 2. LOGICA ORIGINALE DI DRAG & DROP
        if (DroneManager.Instance == null || !DroneManager.Instance.theoryDatabase.ContainsKey(theory.id)) return;

        var state = DroneManager.Instance.theoryDatabase[theory.id];
        
        // Impedisci il trascinamento se la teoria non è ancora sbloccata
        if (state.level == 0) return; 

        // Crea un'icona "Fantasma" che vola sopra tutta la UI
        _ghostIcon = new GameObject("GhostIcon");
        _ghostIcon.transform.SetParent(_mainCanvas.transform, false);
        _ghostIcon.transform.SetAsLastSibling();

        Image img = _ghostIcon.AddComponent<Image>();
        img.sprite = theory.icon;
        img.raycastTarget = false; // FONDAMENTALE: Se è true blocca i drop!

        RectTransform rect = _ghostIcon.GetComponent<RectTransform>();
        rect.sizeDelta = GetComponent<RectTransform>().sizeDelta * 1.2f; // Leggermente più grande

        UpdateGhostPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Se stiamo scrollando, aggiorniamo la Scroll View
        if (_isScrolling)
        {
            if (_scrollRect != null) _scrollRect.OnDrag(eventData);
            return;
        }

        // Altrimenti muoviamo l'icona fantasma
        if (_ghostIcon != null) UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Se stavamo scrollando, chiudiamo l'evento della Scroll View (Serve per l'inerzia)
        if (_isScrolling)
        {
            if (_scrollRect != null) _scrollRect.OnEndDrag(eventData);
            _isScrolling = false;
            return;
        }

        // Altrimenti distruggiamo il fantasma (il drop viene intercettato da TheoryDropZoneUI)
        if (_ghostIcon != null) Destroy(_ghostIcon);
    }

    private void OnDestroy()
    {
        if (_ghostIcon != null) Destroy(_ghostIcon);
    }

    private void UpdateGhostPosition(PointerEventData eventData)
    {
        if (_mainCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _mainCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
            
        _ghostIcon.transform.localPosition = localPoint;
    }
}