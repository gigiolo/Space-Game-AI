using UnityEngine;
using System.Collections;

public class SpaceshipLanding : MonoBehaviour
{
    [Header("Landing Settings")]
    [Tooltip("Durata del volo dall'ingresso in camera all'impatto.")]
    public float duration = 4.0f;
    
    [Tooltip("Curva di movimento (Decelerazione). Consigliato: EaseOut.")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Model Settings")]
    [Tooltip("SPUNTA QUESTO se la nave vola 'di schiena'. Inverte la rotazione.")]
    public bool fixInvertedModel = false; 

    [Header("Visuals")]
    public GameObject trailVisuals;
    public ParticleSystem landingVFX;
    public GameObject shipMesh;

    private System.Action<Vector3> _onLandedCallback;

    public void BeginLanding(Vector3 startPos, Vector3 targetPos, System.Action<Vector3> onLanded)
    {
        transform.position = startPos;
        _onLandedCallback = onLanded;

        StartCoroutine(LandingRoutine(startPos, targetPos));
    }

    private IEnumerator LandingRoutine(Vector3 start, Vector3 end)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float curveT = movementCurve.Evaluate(t);

            // Interpolazione posizione
            Vector3 currentPos = Vector3.Lerp(start, end, curveT);
            transform.position = currentPos;

            // --- GESTIONE ROTAZIONE ---
            Vector3 lookTarget = end; // Di base guardiamo dove andiamo (il pianeta)

            // Se il modello è invertito, guardiamo da dove veniamo (lo spazio)
            if (fixInvertedModel) 
            {
                lookTarget = start; 
            }

            // Applichiamo la rotazione
            // Usiamo una rotazione morbida (Slerp) per evitare scatti all'avvio
            Vector3 direction = (lookTarget - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            yield return null;
        }

        HandleImpact(end);
    }

    private void HandleImpact(Vector3 pos)
    {
        if (shipMesh) shipMesh.SetActive(false);
        if (trailVisuals) trailVisuals.SetActive(false);

        if (landingVFX)
        {
            landingVFX.transform.position = pos;
            landingVFX.transform.rotation = Quaternion.LookRotation(pos.normalized);
            landingVFX.Play();
        }

        StartCoroutine(CallbackDelay(pos));
    }

    private IEnumerator CallbackDelay(Vector3 pos)
    {
        yield return new WaitForSeconds(0.2f);
        _onLandedCallback?.Invoke(pos);
        yield return new WaitForSeconds(2.0f);
        Destroy(gameObject);
    }
}