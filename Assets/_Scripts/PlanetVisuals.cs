using UnityEngine;

// Questo script si occupa SOLO di estetica: particelle, texture, animazioni.
public class PlanetVisuals : MonoBehaviour
{
    [Header("Feedback Visivi")]
    [Tooltip("Il sistema particellare che parte quando tocchi il pianeta")]
    [SerializeField] private ParticleSystem tapFeedbackVFX;

    [Tooltip("Materiale del pianeta (per cambiare colore/texture in futuro)")]
    [SerializeField] private MeshRenderer planetMeshRenderer;

    // Metodo chiamato quando il giocatore tocca il pianeta
    public void OnPlanetTapped(Vector3 hitPoint)
    {
        // 1. Sposta l'effetto visivo nel punto esatto del tocco
        if (tapFeedbackVFX != null)
        {
            tapFeedbackVFX.transform.position = hitPoint;
            
            // "Emit" è più performante di "Play" per tocchi rapidi, 
            // perché non riavvia il sistema ma aggiunge particelle.
            // Rispetta la regola "Niente Instantiate" (Source 19).
            tapFeedbackVFX.Emit(5); 
        }

        // 2. Qui potremmo aggiungere un piccolo "punch" (ridimensionamento rapido) del pianeta
        // per dare una sensazione fisica e "succosa" al click.
    }

    // Metodo che il GameManager chiamerà quando sblocchi una nuova Era (Source 30)
    public void EvolvePlanetLook(Material newMaterial)
    {
        if (planetMeshRenderer != null)
        {
            planetMeshRenderer.material = newMaterial;
        }
    }
}