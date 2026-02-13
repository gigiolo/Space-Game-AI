// --- File completo aggiornato: _Scripts\LaunchSequenceController.cs ---
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

    [Header("Audio")]
    public AudioClip launchSFX;

    [Header("Camera Animation Settings")]
    public float viewAngleOffset = 45f;
    public float cameraRotationDuration = 2.0f;

    [Header("Debug")]
    public bool testLaunch = false; 

    private void Start()
    {
        if (orbitCamera == null) orbitCamera = FindFirstObjectByType<PlanetOrbitCamera>();
        if (statusPopup == null) statusPopup = FindFirstObjectByType<PlanetStatusPopup>();

        if (PlanetManager.Instance != null)
            PlanetManager.Instance.OnTravelStarted += StartSequence;
    }

    private void OnDestroy()
    {
        if (PlanetManager.Instance != null)
            PlanetManager.Instance.OnTravelStarted -= StartSequence;
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

        // Forza lo stato di blocco prima di iniziare qualunque logica
        PlanetOrbitCamera.IsInputBlocked = true;

        Vector3 launchPos = GetLaunchPosition();

        if (orbitCamera != null)
        {
            orbitCamera.AnimateToLookAt(launchPos, viewAngleOffset, cameraRotationDuration, () => 
            {
                SpawnSpaceship(launchPos);
                // Sblocca solo alla fine dell'animazione
                PlanetOrbitCamera.IsInputBlocked = false;
            });
        }
        else
        {
            // Se non c'è camera, eseguiamo subito e sblocchiamo
            SpawnSpaceship(launchPos);
            PlanetOrbitCamera.IsInputBlocked = false;
        }
    }

    private Vector3 GetLaunchPosition()
    {
        var visualScript = FindFirstObjectByType<LaunchSiteVisuals>();
        if (visualScript != null) return visualScript.GetCurrentWorldPosition();

        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.StoredLaunchSitePosition))
        {
            Vector3 localPos = StringToVector3(GameManager.Instance.StoredLaunchSitePosition);
            if (orbitCamera != null && orbitCamera.planetTarget != null)
                return orbitCamera.planetTarget.TransformPoint(localPos);
            return localPos;
        }

        return new Vector3(0, 0, -1.6f); // Fallback
    }

    private void SpawnSpaceship(Vector3 pos)
    {
        if (spaceshipPrefab != null)
        {
            SpaceshipFlight ship = Instantiate(spaceshipPrefab);
            Vector3 planetCenter = (orbitCamera != null && orbitCamera.planetTarget != null) ? orbitCamera.planetTarget.position : Vector3.zero;
            Vector3 surfaceNormal = (pos - planetCenter).normalized;
            ship.Launch(pos, surfaceNormal);
        }

        if (launchSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(launchSFX, 1.0f, 0.1f);

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