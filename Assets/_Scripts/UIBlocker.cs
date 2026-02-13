using UnityEngine;
using UnityEngine.EventSystems;

public class UIBlocker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // Quando tocchi l'interfaccia
    public void OnPointerDown(PointerEventData eventData)
    {
        PlanetOrbitCamera.IsInputBlocked = true;
    }

    // Quando rilasci il tocco
    public void OnPointerUp(PointerEventData eventData)
    {
        PlanetOrbitCamera.IsInputBlocked = false;
    }
    
    // SICUREZZA: Se il menu viene spento/chiuso mentre stai ancora toccando, 
    // l'evento OnPointerUp fallirebbe. OnDisable garantisce lo sblocco.
    private void OnDisable()
    {
        PlanetOrbitCamera.IsInputBlocked = false;
    }

    // SICUREZZA: Se l'oggetto UI viene distrutto
    private void OnDestroy()
    {
        PlanetOrbitCamera.IsInputBlocked = false;
    }
}