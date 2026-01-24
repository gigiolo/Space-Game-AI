using UnityEngine;
using System.Collections;

public class SpaceshipFlight : MonoBehaviour
{
    [Header("Traiettoria (Timing)")]
    [Tooltip("Secondi di volo perpendicolare prima di iniziare a curvare. Tienilo basso (0.2 - 0.5) per evitare l'effetto 'ascensore'.")]
    public float verticalDuration = 0.5f; 

    [Tooltip("Quanto tempo impiega per completare la virata verso l'orizzonte.")]
    public float curveDuration = 10.0f;
    
    [Tooltip("Definisce la morbidezza della virata. ASSE X = Tempo (0 a 1), ASSE Y = Angolo (0=Su, 1=Orizzonte). Assicurati che inizi e finisca dolce.")]
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
        
        // --- CALCOLO VETTORE TANGENTE (ORIZZONTE) ---
        // Calcoliamo la direzione "destra" relativa alla superficie e poi ricalcoliamo il "forward"
        // per ottenere una tangente perfetta all'equatore/orizzonte locale.
        Vector3 horizonDir = Vector3.Cross(upDir, Vector3.up).normalized;
        if (horizonDir == Vector3.zero) horizonDir = Vector3.forward; // Fallback se siamo ai poli
        horizonDir = Vector3.Cross(horizonDir, upDir).normalized;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;
            
            // 1. GESTIONE VELOCITA' (Basata sul tempo totale di vita)
            // Normalizziamo il tempo da 0 a 1 rispetto alla vita totale
            float normalizedTime = Mathf.Clamp01(timer / lifeTime);
            // Leggiamo la curva di accelerazione
            float speedMultiplier = accelerationCurve.Evaluate(normalizedTime);
            // Interpoliamo tra min e max
            float currentSpeed = Mathf.Lerp(startSpeed, endSpeed, speedMultiplier);

            // 2. GESTIONE TRAIETTORIA
            Vector3 currentDirection;

            // Fase A: Brevissimo decollo verticale (per staccarsi da terra)
            if (timer < verticalDuration)
            {
                currentDirection = upDir;
            }
            // Fase B: Virata Graduale verso l'orizzonte
            else if (timer < verticalDuration + curveDuration)
            {
                // Calcoliamo quanto siamo avanti nella virata (da 0 a 1)
                float timeInCurve = timer - verticalDuration;
                float t = Mathf.Clamp01(timeInCurve / curveDuration);
                
                // Valutiamo la curva (0 = Verticale, 1 = Orizzontale)
                float curveT = turnCurve.Evaluate(t);

                // SLERP: Ruota gradualmente il vettore da SU a ORIZZONTE
                currentDirection = Vector3.Slerp(upDir, horizonDir, curveT);
            }
            // Fase C: Crociera (Orbita raggiunta)
            else
            {
                currentDirection = horizonDir;
            }

            // 3. APPLICAZIONE FISICA
            // Ruota la nave verso la direzione calcolata
            if (currentDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(currentDirection);
                // Time.deltaTime * 5f rende la rotazione visiva fluida ma reattiva
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
                // Supporto per URP e Standard Shader
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