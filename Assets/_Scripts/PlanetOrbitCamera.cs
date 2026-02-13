using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement; // Aggiunto per gestire i cambi scena

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
    public static bool IsInputBlocked = false; 

    private float currentAngleH = 0.0f;
    private float targetAngleH = 0.0f;

    // Flag interno per sapere se stiamo animando via script (cinematica)
    private bool _isAnimating = false;

    // --- LOGICA DI SICUREZZA CAMBIO SCENA ---
    private void OnEnable()
    {
        // Ogni volta che la camera si attiva, resettiamo lo stato di blocco
        IsInputBlocked = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Forza lo sblocco dell'input ad ogni cambio scena per evitare "soft-lock"
        IsInputBlocked = false;
        _isAnimating = false;
    }
    // ----------------------------------------

    void Start()
    {
        if (planetTarget != null)
        {
            currentAngleH = startAngle;
            targetAngleH = startAngle;
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
        currentAngleH = Mathf.Lerp(currentAngleH, targetAngleH, Time.deltaTime * damping);
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(0, currentAngleH, 0);
        Vector3 direction = new Vector3(0, 0, -distance);
        Vector3 position = planetTarget.position + (rotation * direction); 
        position.y += heightOffset;

        transform.position = position;
        transform.rotation = rotation;
        transform.LookAt(planetTarget);
    }
    
    void HandleInput()
    {
        if (IsInputBlocked) return;

        if (IsPointerOverUIObject()) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                targetAngleH += touch.deltaPosition.x * sensitivity;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            targetAngleH += Input.GetAxis("Mouse X") * sensitivity * 5;
        }
    }

    private bool IsPointerOverUIObject()
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void AnimateToLookAt(Vector3 worldPoint, float angleOffset, float duration, System.Action onComplete)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateRotationRoutine(worldPoint, angleOffset, duration, onComplete));
    }

    private IEnumerator AnimateRotationRoutine(Vector3 worldPoint, float offset, float duration, System.Action onComplete)
    {
        _isAnimating = true;
        IsInputBlocked = true; 

        Vector3 directionFromCenter = (worldPoint - planetTarget.position).normalized;
        Quaternion targetLookRotation = Quaternion.LookRotation(-directionFromCenter);
        float targetBaseAngle = targetLookRotation.eulerAngles.y;
        float finalAngle = targetBaseAngle + offset;

        float startRotation = currentAngleH;
        float deltaAngle = Mathf.DeltaAngle(startRotation, finalAngle);
        
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            t = t * t * (3f - 2f * t); // Smoothstep

            targetAngleH = startRotation + (deltaAngle * t);
            currentAngleH = targetAngleH; 

            yield return null;
        }

        targetAngleH = startRotation + deltaAngle;
        currentAngleH = targetAngleH;
        
        _isAnimating = false;
        onComplete?.Invoke();
    }
}