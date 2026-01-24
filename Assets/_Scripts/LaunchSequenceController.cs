using UnityEngine;
using System.Globalization;
using System.Collections;

public class LaunchSequenceController : MonoBehaviour
{
    [Header("References")]
    public PlanetOrbitCamera orbitCamera;
    public PlanetStatusPopup statusPopup; // Per chiudere la UI
    
    [Header("Spaceship Assets")]
    public SpaceshipFlight spaceshipPrefab; // Trascina qui il prefab della nave
    public ParticleSystem launchVFX; // Fumo/Fuoco al suolo (opzionale - lascia vuoto per pulizia)

    [Header("Camera Animation Settings")]
    [Tooltip("Angolo offset per inquadrare la nave (45° = vista 3/4)")]
    public float viewAngleOffset = 45f;
    [Tooltip("Tempo impiegato dalla camera per ruotare")]
    public float cameraRotationDuration = 2.0f;

    [Header("Debug")]
    public bool testLaunch = false; // Checkalo in playmode per testare il lancio senza requisiti

    private void Start()
    {
        if (PlanetManager.Instance != null)
        {
            // Ci iscriviamo all'evento: quando il manager dice "Si parte", noi facciamo lo show
            PlanetManager.Instance.OnTravelStarted += StartSequence;
        }
        
        // Trova i riferimenti se non assegnati manualmente
        if (orbitCamera == null) orbitCamera = FindFirstObjectByType<PlanetOrbitCamera>();
        if (statusPopup == null) statusPopup = FindFirstObjectByType<PlanetStatusPopup>();
    }

    private void OnDestroy()
    {
        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.OnTravelStarted -= StartSequence;
        }
    }

    private void Update()
    {
        // Logica per il test rapido da Inspector
        if (testLaunch)
        {
            testLaunch = false;
            StartSequence();
        }
    }

    public void StartSequence()
    {
        // 1. Chiudi UI
        if (statusPopup != null) statusPopup.ClosePopup();

        // 2. Trova posizione LaunchSite
        Vector3 launchPos = Vector3.zero;
        
        // TENTATIVO A: Chiediamo direttamente al Visual Script (Più preciso, include rotazione attuale)
        var visualScript = FindFirstObjectByType<LaunchSiteVisuals>();
        if (visualScript != null)
        {
            launchPos = visualScript.GetCurrentWorldPosition();
        }

        // TENTATIVO B: Fallback GameManager (Posizione salvata)
        if (launchPos == Vector3.zero && GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.StoredLaunchSitePosition))
        {
            Vector3 localPos = StringToVector3(GameManager.Instance.StoredLaunchSitePosition);
            
            // Convertiamo da locale a world usando il pianeta target della camera
            if (orbitCamera != null && orbitCamera.planetTarget != null)
            {
                launchPos = orbitCamera.planetTarget.TransformPoint(localPos);
            }
            else
            {
                launchPos = localPos; // Fallback estremo
            }
        }

        // TENTATIVO C: Default assoluto (davanti alla camera, se tutto fallisce)
        if (launchPos == Vector3.zero)
        {
            Debug.LogWarning("LaunchSequence: Posizione LaunchSite non trovata, uso default.");
            if (orbitCamera != null && orbitCamera.planetTarget != null)
                launchPos = orbitCamera.planetTarget.position + (Vector3.back * 1.6f);
            else
                launchPos = new Vector3(0, 0, -1.6f);
        }

        // 3. Ruota la camera e lancia
        if (orbitCamera != null)
        {
            orbitCamera.AnimateToLookAt(launchPos, viewAngleOffset, cameraRotationDuration, () => 
            {
                // --- CALLBACK DI FINE ROTAZIONE ---
                
                // A) Lancia la nave
                SpawnSpaceship(launchPos);

                // B) SBLOCCA L'INPUT DELLA CAMERA IMMEDIATAMENTE
                // Permette al giocatore di ruotare/ammirare mentre la nave parte
                PlanetOrbitCamera.IsInputBlocked = false;
            });
        }
        else
        {
            // Se non c'è lo script camera, lancia subito e sblocca
            SpawnSpaceship(launchPos);
            PlanetOrbitCamera.IsInputBlocked = false;
        }
    }

    private void SpawnSpaceship(Vector3 pos)
    {
        if (spaceshipPrefab != null)
        {
            // Istanzia la nave
            SpaceshipFlight ship = Instantiate(spaceshipPrefab);
            
            // Calcolo preciso della normale alla superficie (Vettore "Su")
            // Necessario per la nuova logica di traiettoria curva
            Vector3 planetCenter = Vector3.zero;
            if (orbitCamera != null && orbitCamera.planetTarget != null)
            {
                planetCenter = orbitCamera.planetTarget.position;
            }

            // La normale va dal centro del pianeta verso il punto di lancio
            Vector3 surfaceNormal = (pos - planetCenter).normalized;
            
            // Avvia la logica di volo passando posizione e direzione verticale
            ship.Launch(pos, surfaceNormal);
        }

        // (Opzionale) Effetto a terra, se presente
        if (launchVFX != null)
        {
            launchVFX.transform.position = pos;
            launchVFX.transform.rotation = Quaternion.LookRotation(pos.normalized);
            launchVFX.Play();
        }
    }

    // Helper per leggere la stringa "x|y|z" salvata dal GameManager
    private Vector3 StringToVector3(string s) 
    {
        if (string.IsNullOrEmpty(s)) return Vector3.zero;
        string[] parts = s.Split('|'); 
        if (parts.Length < 3) return Vector3.zero;
        
        float x = float.Parse(parts[0], CultureInfo.InvariantCulture); 
        float y = float.Parse(parts[1], CultureInfo.InvariantCulture); 
        float z = float.Parse(parts[2], CultureInfo.InvariantCulture);
        return new Vector3(x, y, z);
    }
}