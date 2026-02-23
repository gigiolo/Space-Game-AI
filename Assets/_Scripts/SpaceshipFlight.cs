using UnityEngine;
using System.Collections;

public class SpaceshipFlight : MonoBehaviour
{
    [Header("Traiettoria (Timing)")]
    [Tooltip("Secondi di volo perpendicolare prima di iniziare a curvare. Tienilo basso (0.2 - 0.5) per evitare l'effetto 'ascensore'.")]
    public float verticalDuration = 0.5f; 

    [Tooltip("Quanto tempo impiega per completare la virata verso lo spazio profondo.")]
    public float curveDuration = 5.0f;
    
    [Tooltip("Definisce la morbidezza della virata. ASSE X = Tempo (0 a 1), ASSE Y = Angolo (0=Su, 1=Spazio). Assicurati che inizi e finisca dolce.")]
    public AnimationCurve turnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Controllo Velocità")]
    [Tooltip("Velocità al momento del decollo.")]
    public float startSpeed = 2.0f;

    [Tooltip("Velocità massima raggiunta alla fine.")]
    public float endSpeed = 25.0f;

    [Tooltip("Come accelera la nave nel tempo totale di vita. (0 = StartSpeed, 1 = EndSpeed).")]
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Visuals")]
    public float lifeTime = 20f;
    public float fadeDuration = 3f;
    public MeshRenderer[] shipRenderers;

    public void Launch(Vector3 startPos, Vector3 surfaceNormal)
    {
        transform.position = startPos;
        // Allinea la nave alla normale (punta in alto rispetto al terreno)
        transform.rotation = Quaternion.LookRotation(surfaceNormal, Vector3.up);
        
        StartCoroutine(FlightRoutine(surfaceNormal));
    }

    private IEnumerator FlightRoutine(Vector3 upDir)
    {
        float timer = 0f;
        
        // --- CALCOLO DIREZIONE SPAZIO PROFONDO ---
        // Generiamo una direzione completamente casuale a 360°
        Vector3 targetSpaceDir = Random.onUnitSphere;
        
        // 1. Evitiamo che la nave scavi nel terreno (deve puntare in su, nel cielo)
        // Se il "Dot Product" è minore di 0.3, significa che sta puntando parallelamente al suolo o verso il basso.
        if (Vector3.Dot(targetSpaceDir, upDir) < 0.3f)
        {
            // La spingiamo forzatamente verso l'alto
            targetSpaceDir = (targetSpaceDir + upDir * 2f).normalized;
        }

        // 2. ANTI-CLIPPING CAMERA (La magia che cercavamo)
        // Se la nave sta per caso puntando dritta in faccia al giocatore, la facciamo deviare.
        if (Camera.main != null)
        {
            Vector3 camForward = Camera.main.transform.forward;
            // Un dot product vicino a -1 significa che i vettori si scontrano frontalmente
            if (Vector3.Dot(targetSpaceDir, camForward) < -0.2f)
            {
                // Aggiungiamo il vettore "in avanti" della telecamera, spingendo la rotta verso lo sfondo
                targetSpaceDir = (targetSpaceDir + camForward).normalized;
            }
        }

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;
            
            // 1. GESTIONE VELOCITA' (Basata sul tempo totale di vita)
            float normalizedTime = Mathf.Clamp01(timer / lifeTime);
            float speedMultiplier = accelerationCurve.Evaluate(normalizedTime);
            float currentSpeed = Mathf.Lerp(startSpeed, endSpeed, speedMultiplier);

            // 2. GESTIONE TRAIETTORIA
            Vector3 currentDirection;

            // Fase A: Brevissimo decollo verticale (per staccarsi da terra in sicurezza)
            if (timer < verticalDuration)
            {
                currentDirection = upDir;
            }
            // Fase B: Virata Graduale verso lo spazio profondo
            else if (timer < verticalDuration + curveDuration)
            {
                float timeInCurve = timer - verticalDuration;
                float t = Mathf.Clamp01(timeInCurve / curveDuration);
                
                float curveT = turnCurve.Evaluate(t);

                // SLERP: Ruota gradualmente il vettore da SU verso la direzione spaziale
                currentDirection = Vector3.Slerp(upDir, targetSpaceDir, curveT);
            }
            // Fase C: Crociera (Spazio profondo raggiunto)
            else
            {
                currentDirection = targetSpaceDir;
            }

            // 3. APPLICAZIONE FISICA
            if (currentDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(currentDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            // Muovi in avanti alla velocità calcolata
            transform.position += transform.forward * currentSpeed * Time.deltaTime;

            // 4. DISSOLVENZA FINALE
            if (timer >= lifeTime - fadeDuration)
            {
                StartFadeOut((lifeTime - timer) / fadeDuration);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void StartFadeOut(float alpha)
    {
        // Riduce la scala come fallback
        transform.localScale = Vector3.one * alpha;
        
        // Riduce l'alpha per i materiali che lo supportano
        foreach (var rend in shipRenderers)
        {
            if (rend == null) continue;
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_BaseColor")) 
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
                else if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }
        }
    }
}