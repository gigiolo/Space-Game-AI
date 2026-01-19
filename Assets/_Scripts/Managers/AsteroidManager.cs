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
    public float trajectoryAngle = 45f; // Default diagonale
    
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
        if (asteroidPrefab == null || mainCamera == null) return;

        // 1. CALCOLO DELL'ANGOLO DI MOVIMENTO
        // Aggiungiamo la varianza casuale all'angolo scelto nell'Inspector
        float currentAngle = trajectoryAngle + Random.Range(-angleVariance, angleVariance);
        
        // Convertiamo l'angolo in una direzione Vector2 (Spazio Schermo Normalizzato)
        // Usiamo Math geometrica di base: Angolo -> Direzione (X,Y)
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)); 
        // Nota: Sin/Cos invertiti o segni cambiati in base a dove vuoi che sia lo 0.
        // Qui: 0 gradi = direzione (0, 1) cioè va VERSO l'alto? No, noi vogliamo provenienza.
        // Facciamo che l'angolo indica la DIREZIONE DI MOVIMENTO.
        
        // 2. CALCOLO PUNTI VIEWPORT (Fuori schermo)
        // Centro schermo è (0.5, 0.5). Ci spostiamo dal centro in direzione opposta per trovare Start
        // e in direzione dell'angolo per trovare End.
        Vector2 center = new Vector2(0.5f, 0.5f);
        float spawnRadius = 1.0f; // Abbastanza grande da uscire dallo schermo (Viewport va da 0 a 1)
        
        // Start: parte "dietro" rispetto alla direzione
        Vector2 startViewport = center - (direction * spawnRadius);
        // End: arriva "avanti"
        Vector2 endViewport = center + (direction * spawnRadius);

        // 3. PROFONDITA' 3D
        float startDepth = baseDistance + Random.Range(-depthVariance, depthVariance);
        float endDepth = baseDistance + Random.Range(-depthVariance, depthVariance);

        Vector3 startView3D = new Vector3(startViewport.x, startViewport.y, startDepth);
        Vector3 endView3D = new Vector3(endViewport.x, endViewport.y, endDepth);

        // 4. CONVERSIONE IN WORLD SPACE
        Vector3 worldStart = mainCamera.ViewportToWorldPoint(startView3D);
        Vector3 worldEnd = mainCamera.ViewportToWorldPoint(endView3D);

        // 5. CALCOLO PUNTO DI CONTROLLO (Per la curva ellittica)
        // Troviamo il punto medio
        Vector3 midPoint = (worldStart + worldEnd) * 0.5f;
        
        // Calcoliamo una direzione perpendicolare alla traiettoria per spostare il punto di controllo.
        // Cross product con "Forward" della camera ci dà una destra/sinistra relativa.
        Vector3 pathDir = (worldEnd - worldStart).normalized;
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 perpendicular = Vector3.Cross(pathDir, cameraForward).normalized;
        
        // Aggiungiamo anche un po' di offset verso l'alto/basso locale per renderlo 3D
        float randomSide = Random.value > 0.5f ? 1f : -1f; // Curva a dx o sx
        
        // Il punto di controllo è: Mezzo + (Perpendicolare * ForzaCurva)
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
    
    // Disegna la linea di spawn nell'editor per capire dove andranno gli asteroidi
    private void OnDrawGizmos()
    {
        if (mainCamera == null) return;
        
        // Visualizzazione rapida della direzione nell'editor
        float rad = trajectoryAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector3 worldDir = mainCamera.transform.TransformDirection(new Vector3(direction.x, direction.y, 0));
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(mainCamera.transform.position + mainCamera.transform.forward * 2, 
                        mainCamera.transform.position + mainCamera.transform.forward * 2 + worldDir);
    }
}