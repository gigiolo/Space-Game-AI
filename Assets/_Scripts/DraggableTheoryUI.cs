// --- File: _Scripts\DraggableTheoryUI.cs ---
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ArtifactSlotUI))]
public class DraggableTheoryUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public PhysicalTheorySO theory;
    
    private ArtifactsMenuUI _menuManager;
    private GameObject _ghostIcon;
    private Canvas _mainCanvas;

    public void Setup(PhysicalTheorySO t, ArtifactsMenuUI manager, Canvas canvas)
    {
        theory = t;
        _menuManager = manager;
        _mainCanvas = canvas;
    }

    // GESTIONE TAP: Apre il popup dei dettagli
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!eventData.dragging && theory != null)
        {
            _menuManager.OpenDetailsPopup(theory);
        }
    }

    // GESTIONE TRASCINAMENTO
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Se DroneManager o il database non sono pronti, usciamo
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
        if (_ghostIcon != null) UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ghostIcon != null) Destroy(_ghostIcon);
    }

    // --- NUOVO: RETE DI SICUREZZA ---
    // Se l'oggetto viene distrutto dal Refresh della UI mentre stiamo trascinando,
    // assicuriamoci di pulire il fantasma rimasto in sospeso!
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