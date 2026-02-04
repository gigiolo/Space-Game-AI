using UnityEngine;
using System.Globalization;
using System.Collections;

public class LaunchSequenceController : MonoBehaviour
{
    [Header("References (Auto-Found if null)")]
    public PlanetOrbitCamera orbitCamera;
    public PlanetStatusPopup statusPopup; 
    
    [Header("Spaceship Assets")]
    public SpaceshipFlight spaceshipPrefab; 
    public ParticleSystem launchVFX; 

    [Header("Audio")] // <--- NUOVO
    [Tooltip("Effetto sonoro del decollo")]
    public AudioClip launchSFX;

    [Header("Camera Animation Settings")]
    [Tooltip("Angolo offset per inquadrare la nave (45° = vista 3/4)")]
    public float viewAngleOffset = 45f;
    [Tooltip("Tempo impiegato dalla camera per ruotare")]
    public float cameraRotationDuration = 2.0f;

    [Header("Debug")]
    public bool testLaunch = false; 

    private void Start()
    {
        if (orbitCamera == null) 
            orbitCamera = FindFirstObjectByType<PlanetOrbitCamera>();

        if (statusPopup == null) 
            statusPopup = FindFirstObjectByType<PlanetStatusPopup>();

        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.OnTravelStarted += StartSequence;
        }
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
        if (testLaunch)
        {
            testLaunch = false;
            StartSequence();
        }
    }

    public void StartSequence()
    {
        if (statusPopup != null) statusPopup.ClosePopup();

        Vector3 launchPos = Vector3.zero;
        
        var visualScript = FindFirstObjectByType<LaunchSiteVisuals>();
        if (visualScript != null)
        {
            launchPos = visualScript.GetCurrentWorldPosition();
        }

        if (launchPos == Vector3.zero && GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.StoredLaunchSitePosition))
        {
            Vector3 localPos = StringToVector3(GameManager.Instance.StoredLaunchSitePosition);
            
            if (orbitCamera != null && orbitCamera.planetTarget != null)
            {
                launchPos = orbitCamera.planetTarget.TransformPoint(localPos);
            }
            else
            {
                launchPos = localPos; 
            }
        }

        if (launchPos == Vector3.zero)
        {
            Debug.LogWarning("LaunchSequence: Posizione LaunchSite non trovata, uso default.");
            if (orbitCamera != null && orbitCamera.planetTarget != null)
                launchPos = orbitCamera.planetTarget.position + (Vector3.back * 1.6f);
            else
                launchPos = new Vector3(0, 0, -1.6f);
        }

        if (orbitCamera != null)
        {
            orbitCamera.AnimateToLookAt(launchPos, viewAngleOffset, cameraRotationDuration, () => 
            {
                SpawnSpaceship(launchPos);
                PlanetOrbitCamera.IsInputBlocked = false;
            });
        }
        else
        {
            SpawnSpaceship(launchPos);
            PlanetOrbitCamera.IsInputBlocked = false;
        }
    }

    private void SpawnSpaceship(Vector3 pos)
    {
        if (spaceshipPrefab != null)
        {
            SpaceshipFlight ship = Instantiate(spaceshipPrefab);
            
            Vector3 planetCenter = Vector3.zero;
            if (orbitCamera != null && orbitCamera.planetTarget != null)
            {
                planetCenter = orbitCamera.planetTarget.position;
            }

            Vector3 surfaceNormal = (pos - planetCenter).normalized;
            ship.Launch(pos, surfaceNormal);
        }

        // --- NUOVO: Play Audio ---
        if (launchSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(launchSFX, 1.0f, 0.1f);
        }
        // -------------------------

        if (launchVFX != null)
        {
            launchVFX.transform.position = pos;
            launchVFX.transform.rotation = Quaternion.LookRotation(pos.normalized);
            launchVFX.Play();
        }
    }

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