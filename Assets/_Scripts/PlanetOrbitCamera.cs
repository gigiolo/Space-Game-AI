using UnityEngine;
using System.Collections;

public class PlanetOrbitCamera : MonoBehaviour
{
    [Header("CONFIGURAZIONE")]
    public Transform planetTarget;
    public float distance = 20.0f;
    public float heightOffset = 0.0f;
    public float sensitivity = 0.2f;
    public float damping = 5.0f;

    // --- VARIABILE STATICA GLOBALE ---
    // Questa è accessibile da qualsiasi script del gioco.
    // Se è vera, l'input utente viene ignorato.
    public static bool IsInputBlocked = false; 

    private float currentAngleH = 0.0f;
    private float targetAngleH = 0.0f;

    // Flag interno per sapere se stiamo animando via script (cinematica)
    private bool _isAnimating = false;

    void Start()
    {
        if (planetTarget != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentAngleH = angles.y;
            targetAngleH = angles.y;
        }
    }

    void LateUpdate()
    {
        if (!planetTarget) return;

        // Gestiamo l'input manuale solo se NON stiamo animando automaticamente
        if (!_isAnimating)
        {
            HandleInput();
        }

        // Movimento fluido (Lerp)
        // Se stiamo animando, targetAngleH viene pilotato dalla coroutine frame per frame
        currentAngleH = Mathf.Lerp(currentAngleH, targetAngleH, Time.deltaTime * damping);
        
        // Calcolo posizione
        Quaternion rotation = Quaternion.Euler(0, currentAngleH, 0);
        
        // La camera sta a 'distance' unità indietro rispetto alla rotazione
        Vector3 direction = new Vector3(0, 0, -distance);
        Vector3 position = planetTarget.position + (rotation * direction); 
        position.y += heightOffset;

        transform.position = position;
        transform.rotation = rotation;
        
        // Assicuriamoci che guardi sempre il centro del pianeta
        transform.LookAt(planetTarget);
    }
    
    void HandleInput()
    {
        // 1. Blocco Totale (da UI o Eventi)
        if (IsInputBlocked) return;

        // 2. Input Touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                targetAngleH += touch.deltaPosition.x * sensitivity;
            }
        }
        // 3. Input Mouse (Editor/PC)
        else if (Input.GetMouseButton(0))
        {
            targetAngleH += Input.GetAxis("Mouse X") * sensitivity * 5;
        }
    }

    // --- METODO PUBBLICO: Avvia la rotazione cinematica ---
    public void AnimateToLookAt(Vector3 worldPoint, float angleOffset, float duration, System.Action onComplete)
    {
        // Interrompiamo eventuali coroutine precedenti per evitare conflitti
        StopAllCoroutines();
        StartCoroutine(AnimateRotationRoutine(worldPoint, angleOffset, duration, onComplete));
    }

    private IEnumerator AnimateRotationRoutine(Vector3 worldPoint, float offset, float duration, System.Action onComplete)
    {
        _isAnimating = true;
        IsInputBlocked = true; // Blocchiamo l'input utente durante l'animazione

        // 1. Calcoliamo la direzione dal Centro del Pianeta al Sito di Lancio
        // Questo vettore punta "fuori" dalla superficie verso lo spazio
        Vector3 directionFromCenter = (worldPoint - planetTarget.position).normalized;

        // 2. Calcoliamo la rotazione necessaria.
        // La logica nel LateUpdate posiziona la camera "indietro" (Vector3.back * distance).
        // Quindi, per guardare il sito di lancio, la camera deve trovarsi lungo la linea 'directionFromCenter'.
        // Matematicamente: rotation * Vector3.back deve essere allineato con directionFromCenter.
        // Oppure: rotation * Vector3.forward deve essere allineato con -directionFromCenter.
        // Usiamo LookRotation per trovare l'angolo Y che guarda verso il centro del pianeta partendo dal sito.
        Quaternion targetLookRotation = Quaternion.LookRotation(-directionFromCenter);
        float targetBaseAngle = targetLookRotation.eulerAngles.y;

        // 3. Aggiungiamo l'offset desiderato (es. 45 gradi per vedere la navicella di profilo)
        float finalAngle = targetBaseAngle + offset;

        // 4. Calcolo del percorso più breve (Shortest Path)
        // Usiamo DeltaAngle per evitare giri di 360° inutili (es. da 350° a 10°)
        float startAngle = currentAngleH;
        float deltaAngle = Mathf.DeltaAngle(startAngle, finalAngle);
        
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // SmoothStep per un movimento morbido (accelera all'inizio, frena alla fine)
            t = t * t * (3f - 2f * t); 

            // Interpoliamo l'angolo target
            targetAngleH = startAngle + (deltaAngle * t);
            
            // Forziamo anche currentAngleH per evitare che il Lerp nel LateUpdate introduca ritardo/elasticità indesiderata
            // durante la cinematica precisa. Vogliamo controllo totale qui.
            currentAngleH = targetAngleH; 

            yield return null;
        }

        // 5. Fine sicura: assicuriamoci di essere arrivati esattamente al punto
        targetAngleH = startAngle + deltaAngle;
        currentAngleH = targetAngleH;
        
        _isAnimating = false;
        
        // Nota: Non sblocchiamo IsInputBlocked qui perché probabilmente sta iniziando il lancio 
        // e non vogliamo che il giocatore ruoti la camera mentre la nave parte.
        
        onComplete?.Invoke();
    }
}