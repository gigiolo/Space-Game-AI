using UnityEngine;

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
    // Se è vera, la camera è congelata.
    public static bool IsInputBlocked = false; 

    private float currentAngleH = 0.0f;
    private float targetAngleH = 0.0f;

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

        HandleInput();

        // Movimento fluido
        currentAngleH = Mathf.Lerp(currentAngleH, targetAngleH, Time.deltaTime * damping);
        
        // Calcolo posizione
        Quaternion rotation = Quaternion.Euler(0, currentAngleH, 0);
        Vector3 position = planetTarget.position + (rotation * direction); // direction definita sotto
        position.y += heightOffset;

        transform.position = position;
        transform.rotation = rotation;
        transform.LookAt(planetTarget);
    }
    
    // Variabile helper per la direzione
    private Vector3 direction => new Vector3(0, 0, -distance);

    void HandleInput()
    {
        // 1. IL CONTROLLO ASSOLUTO
        // Se qualcuno (il menu) ha attivato il blocco, usciamo subito.
        if (IsInputBlocked) return;

        // 2. INPUT TOUCH
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                targetAngleH += touch.deltaPosition.x * sensitivity;
            }
        }
        // 3. INPUT MOUSE
        else if (Input.GetMouseButton(0))
        {
            targetAngleH += Input.GetAxis("Mouse X") * sensitivity * 5;
        }
    }
}