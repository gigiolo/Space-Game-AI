using UnityEngine;
using System.Collections.Generic;

public class LogisticDrone : MonoBehaviour
{
    [Header("Riferimenti Visuali")]
    [Tooltip("Trascina qui il prefab del Line Renderer creato prima")]
    public LineRenderer beamPrefab;
    [Tooltip("Il punto esatto del modello 3D da cui deve partire il raggio (es. la sfera centrale)")]
    public Transform collectionCore;

    [Header("Impostazioni Raggio")]
    [Tooltip("Distanza massima per agganciare un emitter")]
    public float collectionRange = 3.0f;
    [Tooltip("Quanti raggi contemporanei può lanciare questo drone?")]
    public int maxBeams = 3;
    [Tooltip("Velocità con cui l'energia scorre verso l'alto (Effetto ottico)")]
    public float textureScrollSpeed = -2.0f;

    // Variabili interne
    private List<LineRenderer> _activeBeams = new List<LineRenderer>();
    private List<Transform> _currentTargets = new List<Transform>();
    private List<Vector3> _targetPositions = new List<Vector3>();
    
    private PlanetPopulationVisuals _planetVisuals;
    private float _targetUpdateTimer = 0f;
    private float _targetUpdateInterval = 0.2f; // Cerca nuovi bersagli ogni 0.2 secondi (Ottimizzazione!)

    void Start()
    {
        // Trova il manager delle luci sul pianeta
        _planetVisuals = FindFirstObjectByType<PlanetPopulationVisuals>();
        
        if (collectionCore == null) collectionCore = this.transform;

        // Pre-istanzia i raggi e tienili spenti nel "magazzino" (Object Pooling base)
        for (int i = 0; i < maxBeams; i++)
        {
            LineRenderer lr = Instantiate(beamPrefab, transform);
            lr.enabled = false;
            _activeBeams.Add(lr);
            _targetPositions.Add(Vector3.zero);
        }
    }

    void Update()
    {
        if (_planetVisuals == null) return;

        // 1. LOGICA (Ogni 0.2 secondi): Cerca gli emitter vicini
        _targetUpdateTimer -= Time.deltaTime;
        if (_targetUpdateTimer <= 0)
        {
            FindNearbyTargets();
            _targetUpdateTimer = _targetUpdateInterval;
        }

        // 2. GRAFICA (Ogni frame): Disegna e anima i raggi
        DrawAndAnimateBeams();
    }

    private void FindNearbyTargets()
    {
        // Resetta la lista dei bersagli attuali
        int foundTargets = 0;
        var allEmittersLocal = _planetVisuals.GetOccupiedPositions();

        for (int i = 0; i < allEmittersLocal.Count; i++)
        {
            if (foundTargets >= maxBeams) break; // Se abbiamo raggiunto il limite di raggi, smettiamo di cercare

            // Convertiamo la posizione dell'emitter dal pianeta allo spazio del mondo
            Vector3 worldEmitterPos = _planetVisuals.transform.TransformPoint(allEmittersLocal[i]);
            
            // Calcoliamo la distanza tra il drone e la luce
            float distance = Vector3.Distance(transform.position, worldEmitterPos);

            if (distance <= collectionRange)
            {
                // Bersaglio valido! Lo salviamo
                _targetPositions[foundTargets] = worldEmitterPos;
                foundTargets++;
            }
        }

        // Spegni i raggi che non hanno trovato un bersaglio in questo ciclo
        for (int i = 0; i < maxBeams; i++)
        {
            _activeBeams[i].enabled = (i < foundTargets);
        }
    }

    private void DrawAndAnimateBeams()
    {
        for (int i = 0; i < _activeBeams.Count; i++)
        {
            if (_activeBeams[i].enabled)
            {
                // Disegna la linea dal centro del drone alla città
                _activeBeams[i].SetPosition(0, collectionCore.position);
                _activeBeams[i].SetPosition(1, _targetPositions[i]);

                // Magia dell'animazione: fa scorrere la texture modificando l'offset UV
                float offset = Time.time * textureScrollSpeed;
                _activeBeams[i].material.mainTextureOffset = new Vector2(offset, 0);
            }
        }
    }
}