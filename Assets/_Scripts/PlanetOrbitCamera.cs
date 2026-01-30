using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems; // NECESSARIO PER RILEVARE I CLICK SUI BOTTONI

public class PlanetOrbitCamera : MonoBehaviour
{
    [Header("CONFIGURAZIONE")]
    public Transform planetTarget;
    public float distance = 20.0f;
    public float heightOffset = 0.0f;
    public float sensitivity = 0.2f;
    public float damping = 5.0f;

    [Header("Posizione Iniziale")]
    [Tooltip("Regola questo valore (0-360) per ruotare la camera attorno al pianeta all'avvio.")]
    [Range(0f, 360f)]
    public float startAngle = 0.0f;

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
            // Usiamo l'angolo definito nell'Inspector
            currentAngleH = startAngle;
            targetAngleH = startAngle;

            // Aggiorniamo subito la posizione al primo frame per evitare scatti
            UpdateCameraPosition();
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
        
        UpdateCameraPosition();
    }

    // Metodo separato per pulizia, calcola posizione e rotazione
    void UpdateCameraPosition()
    {
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
        // 1. Blocco Totale (da UI Blockers o Eventi)
        if (IsInputBlocked) return;

        // 2. NUOVO: Controllo se stiamo toccando un bottone o un pannello UI
        // Se il puntatore è sopra la UI, IGNORIAMO la rotazione della camera
        if (IsPointerOverUIObject()) return;

        // 3. Input Touch (Mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                targetAngleH += touch.deltaPosition.x * sensitivity;
            }
        }
        // 4. Input Mouse (Editor/PC)
        else if (Input.GetMouseButton(0))
        {
            targetAngleH += Input.GetAxis("Mouse X") * sensitivity * 5;
        }
    }

    // --- METODO DI CONTROLLO UI ---
    // Ritorna TRUE se stiamo cliccando/toccando un elemento della Canvas
    private bool IsPointerOverUIObject()
    {
        // A. Controllo Touch (per Mobile)
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        
        // B. Controllo Mouse (per Editor/PC)
        return EventSystem.current.IsPointerOverGameObject();
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
        Vector3 directionFromCenter = (worldPoint - planetTarget.position).normalized;

        // 2. Calcoliamo la rotazione necessaria.
        Quaternion targetLookRotation = Quaternion.LookRotation(-directionFromCenter);
        float targetBaseAngle = targetLookRotation.eulerAngles.y;

        // 3. Aggiungiamo l'offset desiderato
        float finalAngle = targetBaseAngle + offset;

        // 4. Calcolo del percorso più breve (Shortest Path)
        float startRotation = currentAngleH;
        float deltaAngle = Mathf.DeltaAngle(startRotation, finalAngle);
        
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // SmoothStep per un movimento morbido
            t = t * t * (3f - 2f * t); 

            // Interpoliamo l'angolo target
            targetAngleH = startRotation + (deltaAngle * t);
            
            // Forziamo anche currentAngleH per controllo totale durante la cinematica
            currentAngleH = targetAngleH; 

            yield return null;
        }

        // 5. Fine sicura
        targetAngleH = startRotation + deltaAngle;
        currentAngleH = targetAngleH;
        
        _isAnimating = false;
        
        onComplete?.Invoke();
    }
}