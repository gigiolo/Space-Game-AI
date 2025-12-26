using UnityEngine;
using UnityEngine.EventSystems; // Necessario per rilevare il tocco UI

// Questo script dice alla camera: "Sto venendo toccato, fermati!"
public class UIBlocker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // Quando metti il dito (o clicchi) su questo oggetto UI
    public void OnPointerDown(PointerEventData eventData)
    {
        PlanetOrbitCamera.IsInputBlocked = true;
    }

    // Quando alzi il dito (o rilasci il click)
    public void OnPointerUp(PointerEventData eventData)
    {
        PlanetOrbitCamera.IsInputBlocked = false;
    }
    
    // Sicurezza: se disattivi il menù mentre tieni premuto, sblocca la camera
    void OnDisable()
    {
        PlanetOrbitCamera.IsInputBlocked = false;
    }
}