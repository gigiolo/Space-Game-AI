using UnityEngine;
using System.Collections.Generic;

public class AsteroidManager : MonoBehaviour
{
    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnInterval = 10f;
    [SerializeField] private float maxSpawnInterval = 30f;
    
    [Header("Velocità e Movimento")]
    [Tooltip("Velocità minima dell'asteroide")]
    [SerializeField] private float minSpeed = 3f;
    [Tooltip("Velocità massima dell'asteroide")]
    [SerializeField] private float maxSpeed = 8f;

    [Header("Traiettoria (Angolo)")]
    [Tooltip("Angolo di arrivo in gradi (0 = Dall'alto verso il basso, 90 = Da destra a sx, ecc.)")]
    [Range(0f, 360f)] 
    public float trajectoryAngle = 45f; 
    
    [Tooltip("Quanto può variare l'angolo casualmente (+/- gradi)")]
    [Range(0f, 180f)]
    public float angleVariance = 30f;

    [Header("Curvatura (Effetto Ellittico)")]
    [Tooltip("Quanto è ampia la curva? 0 = linea retta, Alto = curva molto ampia")]
    [SerializeField] private float curveAmount = 5f;

    [Header("Posizionamento 3D")]
    [SerializeField] private float baseDistance = 10f; 
    [SerializeField] private float depthVariance = 3f;

    [Header("References")]
    [SerializeField] private AsteroidEvent asteroidPrefab;
    [SerializeField] private Camera mainCamera;

    private List<AsteroidEvent> _spawnedAsteroids = new List<AsteroidEvent>();
    private float _spawnTimer;

    private void Start()
    {
        // Se non assegnata, cerca la camera (ma potrebbe cambiare se cambi scena)
        if (mainCamera == null) mainCamera = Camera.main;
        ResetTimer();
    }

    private void Update()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0)
        {
            SpawnAsteroid();
            ResetTimer();
        }
        HandleInput();
    }

    private void ResetTimer()
    {
        _spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void SpawnAsteroid()
    {
        if (asteroidPrefab == null) return;

        // --- FIX 1: Aggiornamento Camera Persistente ---
        // Se la camera è nulla (o è stata distrutta nel cambio scena), cerchiamo quella nuova
        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
        }

        // --- FIX 2: Controllo Validità Camera ---
        // Se non c'è camera, o se la camera non ha ancora dimensioni (loading screen), usciamo
        if (mainCamera == null || mainCamera.pixelRect.width <= 0 || mainCamera.pixelRect.height <= 0) 
        {
            return;
        }

        // 1. CALCOLO DELL'ANGOLO DI MOVIMENTO
        float currentAngle = trajectoryAngle + Random.Range(-angleVariance, angleVariance);
        
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)); 
        
        // 2. CALCOLO PUNTI VIEWPORT (Fuori schermo)
        Vector2 center = new Vector2(0.5f, 0.5f);
        float spawnRadius = 1.0f; 
        
        Vector2 startViewport = center - (direction * spawnRadius);
        Vector2 endViewport = center + (direction * spawnRadius);

        // 3. PROFONDITA' 3D
        float startDepth = baseDistance + Random.Range(-depthVariance, depthVariance);
        float endDepth = baseDistance + Random.Range(-depthVariance, depthVariance);

        Vector3 startView3D = new Vector3(startViewport.x, startViewport.y, startDepth);
        Vector3 endView3D = new Vector3(endViewport.x, endViewport.y, endDepth);

        // 4. CONVERSIONE IN WORLD SPACE (Qui avveniva l'errore)
        Vector3 worldStart = mainCamera.ViewportToWorldPoint(startView3D);
        Vector3 worldEnd = mainCamera.ViewportToWorldPoint(endView3D);

        // 5. CALCOLO PUNTO DI CONTROLLO
        Vector3 midPoint = (worldStart + worldEnd) * 0.5f;
        
        Vector3 pathDir = (worldEnd - worldStart).normalized;
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 perpendicular = Vector3.Cross(pathDir, cameraForward).normalized;
        
        float randomSide = Random.value > 0.5f ? 1f : -1f; 
        Vector3 controlPoint = midPoint + (perpendicular * curveAmount * randomSide);

        // 6. VELOCITA'
        float chosenSpeed = Random.Range(minSpeed, maxSpeed);

        // 7. SPAWN
        AsteroidEvent newAsteroid = Instantiate(asteroidPrefab, transform);
        newAsteroid.Setup(worldStart, worldEnd, controlPoint, chosenSpeed, OnAsteroidDespawn);
        _spawnedAsteroids.Add(newAsteroid);
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                AsteroidEvent asteroid = hit.collider.GetComponent<AsteroidEvent>();
                if (asteroid != null) asteroid.OnHit();
            }
        }
    }

    private void OnAsteroidDespawn(AsteroidEvent asteroid)
    {
        if (_spawnedAsteroids.Contains(asteroid)) _spawnedAsteroids.Remove(asteroid);
        if(asteroid != null) Destroy(asteroid.gameObject);
    }
    
    private void OnDrawGizmos()
    {
        if (mainCamera == null) return;
        
        float rad = trajectoryAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector3 worldDir = mainCamera.transform.TransformDirection(new Vector3(direction.x, direction.y, 0));
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(mainCamera.transform.position + mainCamera.transform.forward * 2, 
                        mainCamera.transform.position + mainCamera.transform.forward * 2 + worldDir);
    }
}