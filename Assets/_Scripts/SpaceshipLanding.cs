// --- File: _Scripts\SpaceshipLanding.cs ---
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
        // Nascondiamo solo la mesh della nave
        if (shipMesh) shipMesh.SetActive(false);
        
        // --- MODIFICA FLUIDITA' DELLA SCIA ---
        if (trailVisuals) 
        {
            // 1. Sganciamo la scia dalla nave, così non viene distrutta assieme ad essa
            trailVisuals.transform.SetParent(null);

            // 2. Fermiamo l'emissione di nuove particelle (sia per ParticleSystem che per eventuali figli)
            ParticleSystem[] pSystems = trailVisuals.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in pSystems)
            {
                // Questo comando dice al sistema: "Smetti di creare particelle, ma fai vivere e svanire quelle già create"
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // 3. Autodistruzione ritardata dell'oggetto "orfano" (5 secondi sono sufficienti per far sfumare qualsiasi scia)
            Destroy(trailVisuals, 5.0f);
        }
        // -------------------------------------

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