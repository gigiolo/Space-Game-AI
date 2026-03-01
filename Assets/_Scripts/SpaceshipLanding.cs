using UnityEngine;
using System.Collections;

public class SpaceshipLanding : MonoBehaviour
{
    [Header("Landing Settings")]
    [Tooltip("Durata del volo dall'ingresso in camera all'impatto.")]
    public float duration = 4.0f;
    
    [Tooltip("Curva di accelerazione. Consigliato: EaseOut (veloce all'inizio, frena alla fine).")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Trajectory Randomization")]
    [Tooltip("Se VERO, ignora la posizione iniziale e calcola uno spawn nello spazio profondo a 360°.")]
    public bool randomizeStartVector = true;
    
    [Tooltip("Distanza da cui parte la nave (Nello spazio profondo).")]
    public float spawnDistance = 35f;
    
    [Tooltip("Altezza dell'apice della curva sopra il punto di atterraggio. Crea l'effetto 'picchiata'.")]
    public float curveHeight = 5f;

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
        _onLandedCallback = onLanded;

        Vector3 finalStartPos = startPos;

        // 1. GENERAZIONE SPAWN CASUALE E INTELLIGENTE
        if (randomizeStartVector)
        {
            Vector3 upDir = targetPos.normalized; // Direzione "Cielo" rispetto al punto di atterraggio
            Vector3 randomDir = Random.onUnitSphere;
            
            // Assicuriamoci che provenga dall'emisfero visibile (non deve attraversare il nucleo del pianeta)
            if (Vector3.Dot(randomDir, upDir) < 0.2f)
            {
                randomDir = (randomDir + upDir * 1.5f).normalized;
            }
            
            // ANTI-CLIPPING CAMERA: Evitiamo che spawni esattamente dietro o sopra la telecamera
            if (Camera.main != null)
            {
                Vector3 dirToCam = Camera.main.transform.position.normalized;
                // Se la direzione casuale è troppo vicina a dove si trova la telecamera, la deviamo
                if (Vector3.Dot(randomDir, dirToCam) > 0.6f) 
                {
                    randomDir = (randomDir - dirToCam).normalized;
                }
            }
            
            finalStartPos = randomDir * spawnDistance;
        }

        transform.position = finalStartPos;

        // 2. CREAZIONE DEL PUNTO DI CONTROLLO PER LA CURVA
        // Questo punto attira la traiettoria verso l'alto prima di scendere, creando l'arco di rientro.
        Vector3 controlPoint = targetPos + targetPos.normalized * curveHeight;

        StartCoroutine(LandingRoutine(finalStartPos, controlPoint, targetPos));
    }

    private IEnumerator LandingRoutine(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            
            // Valutiamo quanto siamo avanti basandoci sulla curva di accelerazione (es. frena alla fine)
            float curveT = movementCurve.Evaluate(t);

            // Calcolo Posizione usando una Curva di Bézier Quadratica
            Vector3 currentPos = CalculateBezierPoint(curveT, p0, p1, p2);
            transform.position = currentPos;

            // --- GESTIONE ROTAZIONE FLUIDA SULLA CURVA ---
            // Guardiamo un pochino più avanti sulla curva per capire in che direzione stiamo andando (Vettore Tangente)
            float lookAheadT = Mathf.Clamp01(curveT + 0.05f);
            Vector3 nextPos = CalculateBezierPoint(lookAheadT, p0, p1, p2);
            Vector3 tangentDir = (nextPos - currentPos).normalized;

            // Fallback per l'ultimo frame
            if (tangentDir == Vector3.zero) tangentDir = (p2 - p1).normalized;

            if (tangentDir != Vector3.zero)
            {
                Vector3 lookDir = fixInvertedModel ? -tangentDir : tangentDir;
                
                // Usiamo currentPos.normalized come "Alto" locale. 
                // Questo simula la nave che si raddrizza aerodinamicamente rispetto alla gravità del pianeta!
                Vector3 upDir = currentPos.normalized; 
                
                Quaternion targetRot = Quaternion.LookRotation(lookDir, upDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
            }

            yield return null;
        }

        HandleImpact(p2);
    }

    // Formula matematica della Curva di Bézier Quadratica
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector3 p = uu * p0; // Termine Start
        p += 2 * u * t * p1; // Termine Controllo
        p += tt * p2;        // Termine Target
        
        return p;
    }

    private void HandleImpact(Vector3 pos)
    {
        // Nascondiamo solo il modello 3D della nave
        if (shipMesh) shipMesh.SetActive(false);
        
        // --- Dissolvenza morbida della scia ---
        if (trailVisuals)
        {
            // 1. Ferma in modo morbido eventuali Particle System (es. fumo/fuoco del motore)
            ParticleSystem[] pSystems = trailVisuals.GetComponentsInChildren<ParticleSystem>();
            foreach(var ps in pSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // 2. Ferma in modo morbido eventuali Trail Renderer (es. strisce di luce)
            TrailRenderer[] trails = trailVisuals.GetComponentsInChildren<TrailRenderer>();
            foreach(var tr in trails)
            {
                tr.emitting = false;
            }
        }

        // Lanciamo le particelle dell'impatto/esplosione a terra
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
        // Un piccolo delay prima di dire al gioco che è atterrata, per sincronizzare visivamente l'impatto
        yield return new WaitForSeconds(0.1f);
        _onLandedCallback?.Invoke(pos);
        
        // Aspettiamo che le particelle e la scia finiscano di dissolversi prima di distruggere l'oggetto
        yield return new WaitForSeconds(3.0f);
        Destroy(gameObject);
    }
}