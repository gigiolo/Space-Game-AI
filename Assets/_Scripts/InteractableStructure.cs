using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class InteractableStructure : MonoBehaviour
{
    [Header("Azione al Click")]
    [Tooltip("Cosa deve succedere quando si clicca questa struttura?")]
    public UnityEvent onStructureClicked;

    void Update()
    {
        // Rileva il tocco su schermo (mobile) o il click del mouse (PC)
        if (Input.GetMouseButtonDown(0))
        {
            // 1. SICUREZZA: Se stiamo toccando la UI (es. un bottone), ignoriamo il click 3D
            if (IsPointerOverUIObject()) return;

            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                
                // 2. Controlla se il raggio colpisce qualcosa fisicamente
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // 3. Se l'oggetto colpito è questo (o un suo figlio), scateniamo l'evento
                    if (hit.collider != null && hit.collider.transform.IsChildOf(this.transform))
                    {
                        onStructureClicked?.Invoke();
                    }
                }
            }
        }
    }

    // Metodo helper per capire se il dito/mouse è sopra un elemento della UI
    private bool IsPointerOverUIObject()
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return EventSystem.current.IsPointerOverGameObject();
    }
}