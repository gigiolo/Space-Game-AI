// --- File: _Scripts\TheoryDropZoneUI.cs ---
using UnityEngine;
using UnityEngine.EventSystems;

public class TheoryDropZoneUI : MonoBehaviour, IDropHandler
{
    [Tooltip("L'indice di questo slot (0, 1 o 2)")]
    public int slotIndex; 
    public ArtifactsMenuUI menuManager;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableTheoryUI draggedTheory = eventData.pointerDrag.GetComponent<DraggableTheoryUI>();
            if (draggedTheory != null && draggedTheory.theory != null)
            {
                menuManager.TryEquipTheory(draggedTheory.theory, slotIndex);
            }
        }
    }
}