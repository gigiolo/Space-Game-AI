using UnityEngine;

public class PlanetSunRotator : MonoBehaviour
{
    public static PlanetSunRotator Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Gradi al secondo di rotazione (Ciclo Giorno/Notte).")]
    public float rotationSpeed = 2.0f; 

    [Tooltip("Asse di rotazione del sole.")]
    public Vector3 axis = Vector3.up;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // 1. Ripristina l'ultima rotazione salvata
            float savedAngle = GameManager.Instance.StoredSunRotation;
            
            // 2. Calcola quanto avrebbe ruotato mentre eri offline
            // GameManager.PendingOfflineSeconds viene popolato al caricamento
            double secondsOffline = GameManager.Instance.PendingOfflineSeconds;
            
            float angleDelta = (float)(secondsOffline * rotationSpeed);
            
            // 3. Applica la rotazione totale (Salvata + Offline)
            float finalAngle = savedAngle + angleDelta;
            
            // Normalizziamo l'angolo tra 0 e 360 per pulizia
            finalAngle %= 360f;

            transform.rotation = Quaternion.Euler(0, finalAngle, 0);

            // Resettiamo il contatore offline per evitare di ri-applicarlo se ricarichiamo la scena
            // (Nota: lo facciamo nel GameManager idealmente, ma qui va bene per il consumo locale)
        }
    }

    private void Update()
    {
        // Rotazione normale in tempo reale
        transform.Rotate(axis, rotationSpeed * Time.deltaTime);
    }

    // Metodo helper per salvare
    public float GetCurrentYRotation()
    {
        return transform.rotation.eulerAngles.y;
    }
}