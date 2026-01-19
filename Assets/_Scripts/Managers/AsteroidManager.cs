using UnityEngine;
using System.Collections.Generic;

public class AsteroidManager : MonoBehaviour
{
    [Header("Tempi di Spawn")]
    [Tooltip("Minimo secondi tra un asteroide e l'altro")]
    [SerializeField] private float minSpawnInterval = 15f;
    [Tooltip("Massimo secondi tra un asteroide e l'altro")]
    [SerializeField] private float maxSpawnInterval = 40f;
    
    [Header("Area di Spawn 3D")]
    [Tooltip("Distanza media dalla camera. (Camera=20, Pianeta=0. Metti 10 per stare nel mezzo)")]
    [SerializeField] private float baseDistance = 10f; 
    
    [Tooltip("Variazione di profondità. Se 3, nasce random tra distanze 7 e 13.")]
    [SerializeField] private float depthVariance = 3f;

    [Header("Riferimenti")]
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
        // 1. Gestione Timer
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0)
        {
            SpawnAsteroid();
            ResetTimer();
        }

        // 2. Gestione Input (Click / Touch)
        HandleInput();
    }

    private void ResetTimer()
    {
        _spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void SpawnAsteroid()
    {
        if (asteroidPrefab == null || mainCamera == null) return;

        // A. Scegliamo un lato dello schermo da cui partire (0:Alto, 1:Basso, 2:Sx, 3:Dx)
        int side = Random.Range(0, 4);
        
        Vector3 startView = Vector3.zero;
        Vector3 endView = Vector3.zero;

        // B. Calcoliamo profondità diverse per inizio e fine (traiettoria obliqua 3D)
        float startDepth = baseDistance + Random.Range(-depthVariance, depthVariance);
        float endDepth = baseDistance + Random.Range(-depthVariance, depthVariance);

        // C. Definiamo i punti nel Viewport (0-1). Usciamo dai bordi (-0.2 / 1.2) per nascondere lo spawn.
        switch (side)
        {
            case 0: // Dall'alto verso il basso
                startView = new Vector3(Random.Range(0f, 1f), 1.2f, startDepth);
                endView = new Vector3(Random.Range(0f, 1f), -0.2f, endDepth);
                break;
            case 1: // Dal basso verso l'alto
                startView = new Vector3(Random.Range(0f, 1f), -0.2f, startDepth);
                endView = new Vector3(Random.Range(0f, 1f), 1.2f, endDepth);
                break;
            case 2: // Da sinistra a destra
                startView = new Vector3(-0.2f, Random.Range(0f, 1f), startDepth);
                endView = new Vector3(1.2f, Random.Range(0f, 1f), endDepth);
                break;
            case 3: // Da destra a sinistra
                startView = new Vector3(1.2f, Random.Range(0f, 1f), startDepth);
                endView = new Vector3(-0.2f, Random.Range(0f, 1f), endDepth);
                break;
        }

        // D. CONVERSIONE CRUCIALE: Viewport -> World Point
        // Fissiamo le coordinate nel mondo ORA. Se la camera si muove dopo, questi punti restano fissi nel mondo.
        Vector3 worldStart = mainCamera.ViewportToWorldPoint(startView);
        Vector3 worldEnd = mainCamera.ViewportToWorldPoint(endView);

        // E. Creazione Oggetto (Nota: non usare mainCamera.transform come parent!)
        AsteroidEvent newAsteroid = Instantiate(asteroidPrefab, transform); 
        
        newAsteroid.Setup(worldStart, worldEnd, OnAsteroidDespawn);
        _spawnedAsteroids.Add(newAsteroid);
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) // Funziona anche su mobile come primo tocco
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Opzionale: Aggiungi una LayerMask se vuoi cliccare SOLO asteroidi
            if (Physics.Raycast(ray, out hit))
            {
                AsteroidEvent asteroid = hit.collider.GetComponent<AsteroidEvent>();
                if (asteroid != null)
                {
                    asteroid.OnHit();
                }
            }
        }
    }

    private void OnAsteroidDespawn(AsteroidEvent asteroid)
    {
        if (_spawnedAsteroids.Contains(asteroid))
        {
            _spawnedAsteroids.Remove(asteroid);
        }
        if(asteroid != null) Destroy(asteroid.gameObject);
    }
}