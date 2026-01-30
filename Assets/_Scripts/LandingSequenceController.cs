using UnityEngine;
using UnityEngine.SceneManagement; // NECESSARIO per il Core System
using System.Collections;

public class LandingSequenceController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Il prefab della nave con lo script SpaceshipLanding.")]
    public SpaceshipLanding spaceshipPrefab; 
    
    // Questi riferimenti vengono cercati dinamicamente ad ogni cambio scena
    private PlanetPopulationVisuals planetVisuals;
    private Camera mainCamera;

    [Header("Settings")]
    [Tooltip("Distanza laterale dalla camera da cui parte la nave (Offset X rispetto alla Camera).")]
    public float spawnOffsetX = 15f;
    [Tooltip("Distanza in avanti dalla camera (Offset Z) per non clippare.")]
    public float spawnOffsetZ = 5f;
    [Tooltip("Distanza verticale (Offset Y) opzionale.")]
    public float spawnOffsetY = 2f;

    [Header("--- DEBUG TOOLS ---")]
    [Tooltip("SE VERO: L'animazione parte subito premendo Play (Solo per test in Editor).")]
    public bool debugTestOnStart = false;

    // --- GESTIONE CORE SYSTEM (Persistent) ---
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Questo metodo viene chiamato AUTOMATICAMENTE ogni volta che una scena finisce di caricare
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Aggiorna i riferimenti specifici della nuova scena
        mainCamera = Camera.main;
        planetVisuals = FindFirstObjectByType<PlanetPopulationVisuals>();

        // 2. Controlla se il PlanetManager ha programmato un atterraggio
        if (PlanetManager.Instance != null && PlanetManager.Instance.pendingLanding)
        {
            // Resetta il flag per non farlo scattare di nuovo per errore
            PlanetManager.Instance.pendingLanding = false;

            // Avvia la sequenza
            StartCoroutine(SequenceRoutine());
        }
    }

    private void Start()
    {
        // LOGICA SOLO PER TEST RAPIDO IN EDITOR
        // Se siamo in Core_Systems, questo Start parte una volta sola all'avvio del gioco.
        if (debugTestOnStart)
        {
            Debug.LogWarning("LANDING SEQUENCE: Modalità Debug Attiva! Avvio sequenza forzata.");
            
            // Cerchiamo i riferimenti manualmente per il test
            if (mainCamera == null) mainCamera = Camera.main;
            if (planetVisuals == null) planetVisuals = FindFirstObjectByType<PlanetPopulationVisuals>();

            StartCoroutine(SequenceRoutine());
        }
    }

    private void Update()
    {
        // Tasto rapido per riavviare l'animazione durante il Play Mode (solo se debug è attivo)
        if (debugTestOnStart && Input.GetKeyDown(KeyCode.L))
        {
            StopAllCoroutines();
            StartCoroutine(SequenceRoutine());
        }
    }

    private IEnumerator SequenceRoutine()
    {
        // Attendiamo un frame per assicurarci che la Camera e la Scena siano stabili
        yield return null;

        if (mainCamera == null) mainCamera = Camera.main;
        if (planetVisuals == null) planetVisuals = FindFirstObjectByType<PlanetPopulationVisuals>();

        // Se non troviamo la visuale del pianeta, aspettiamo ancora un attimo (caso limite caricamento lento)
        if (planetVisuals == null) yield return new WaitForEndOfFrame();

        // 1. Calcoliamo Punto di Partenza (Fuori Camera, di lato)
        // Usiamo il Transform della Camera per essere relativi alla vista attuale
        Vector3 camPos = mainCamera.transform.position;
        Vector3 camRight = mainCamera.transform.right;
        Vector3 camFwd = mainCamera.transform.forward;

        // Partiamo da Destra o Sinistra casualmente
        float side = (Random.value > 0.5f) ? 1f : -1f;
        
        // Posizione start: Camera + Lato + Avanti + Su
        Vector3 startPos = camPos + (camRight * side * spawnOffsetX) + (camFwd * spawnOffsetZ) + (Vector3.up * spawnOffsetY);

        // 2. Calcoliamo Punto di Arrivo (Superficie del Pianeta)
        // Deve essere visibile dalla camera. Usiamo un punto "davanti" alla camera sulla sfera.
        float planetRadius = planetVisuals != null ? planetVisuals.surfaceRadius : 1.6f;
        
        // Raycast dal centro schermo (0.5, 0.5) verso il pianeta per trovare un punto centrale perfetto
        Vector3 targetPos = Vector3.forward * planetRadius; // Default fallback
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // LayerMask generica o tutto
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Se colpisce il pianeta, usiamo quel punto
            targetPos = hit.point;
        }
        else
        {
            // Fallback: proiezione semplice davanti alla camera
            targetPos = (mainCamera.transform.position + mainCamera.transform.forward * 10f).normalized * planetRadius;
        }

        // 3. Spawna la Nave
        if (spaceshipPrefab != null)
        {
            SpaceshipLanding ship = Instantiate(spaceshipPrefab);
            
            // Inizia la discesa
            ship.BeginLanding(startPos, targetPos, OnShipLanded);
        }
        else
        {
            Debug.LogError("LandingSequence: Spaceship Prefab mancante!");
        }
    }

    // Callback chiamato quando la nave tocca terra e scompare
    private void OnShipLanded(Vector3 landingSpot)
    {
        // 1. VISUALE: Spawna la luce nel punto esatto
        if (planetVisuals != null)
        {
            planetVisuals.SpawnSpecificEmitter(landingSpot);
        }

        // 2. LOGICA: Attiva l'economia (passa da 0 a 1 emitter)
        // (Eseguiamo solo se NON siamo in modalità debug, per non sporcare il salvataggio mentre testi)
        if (!debugTestOnStart && GameManager.Instance != null)
        {
            // Aggiunge 1 emitter, attiva l'economia e aggiorna la UI
            GameManager.Instance.AddInstantEmitters(1);
            
            // Salviamo subito che abbiamo iniziato la colonizzazione
            GameManager.Instance.SaveGame();
        }
    }
}