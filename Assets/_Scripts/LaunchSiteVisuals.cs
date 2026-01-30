using UnityEngine;
using System.Collections;
using System.Globalization; 

[RequireComponent(typeof(ParticleSystem))]
public class LaunchSiteVisuals : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("Necessario per calcolare le distanze dagli emitter esistenti")]
    public PlanetPopulationVisuals populationVisuals;

    [Header("Configurazione Particella")]
    public float particleSize = 0.2f;
    public Color particleColor = Color.cyan;
    [Tooltip("Velocità del lampeggiamento in Hz (3Hz = 3 lampeggi al secondo)")]
    public float blinkSpeed = 3.0f;

    [Header("Posizionamento")]
    [Tooltip("Distanza dal centro. Modifica questo valore in Play Mode per alzare/abbassare la particella.")]
    public float surfaceRadius = 1.60f;
    [Range(0f, 90f)]
    public float maxLatitude = 60f;
    
    [Tooltip("Distanza MINIMA da un emitter esistente (Zona di esclusione).")]
    public float minDistanceFromEmitters = 0.15f; 
    
    [Tooltip("Distanza MASSIMA da un emitter esistente (Zona di inclusione). Il sito apparirà entro questo raggio da una colonia.")]
    public float maxDistanceFromEmitters = 0.6f;
    
    public int placementAttempts = 50;

    [Header("Post-Launch")]
    [Tooltip("Tempo totale in secondi prima che la particella venga distrutta")]
    public float postLaunchDuration = 5.0f;
    
    [Tooltip("Quanti secondi dura la dissolvenza finale (deve essere minore di Post Launch Duration)")]
    public float fadeOutDuration = 2.0f;

    // Riferimenti interni
    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _buffer;
    private bool _isActive = false;
    private Vector3 _currentDirection;
    
    private float _fadeMultiplier = 1.0f;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        
        var main = _ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = 1; 

        var emission = _ps.emission;
        emission.enabled = false; 

        _buffer = new ParticleSystem.Particle[1];
    }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.OnLaunchPrepStarted += OnPrepStarted;
            PlanetManager.Instance.OnTravelStarted += OnTravelStarted;
        }

        if (populationVisuals == null && transform.parent != null)
            populationVisuals = transform.parent.GetComponentInChildren<PlanetPopulationVisuals>();

        // Controllo Persistenza
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.StoredLaunchSitePosition))
        {
            Vector3 savedPos = StringToVector3(GameManager.Instance.StoredLaunchSitePosition);
            
            if (savedPos != Vector3.zero)
            {
                SpawnParticleAt(savedPos);
                
                if (PlanetManager.Instance != null && PlanetManager.Instance.isTraveling)
                {
                      StartCoroutine(WaitAndKillRoutine());
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.OnLaunchPrepStarted -= OnPrepStarted;
            PlanetManager.Instance.OnTravelStarted -= OnTravelStarted;
        }
    }

    private void Update()
    {
        if (_isActive)
        {
            UpdateParticleState();
        }
    }

    private void OnPrepStarted()
    {
        if (_isActive) return;

        Vector3 spawnPos = FindValidPosition();
        if (spawnPos != Vector3.zero)
        {
            SpawnParticleAt(spawnPos);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StoredLaunchSitePosition = Vector3ToString(spawnPos);
                GameManager.Instance.SaveGame(); 
            }
        }
    }

    private void SpawnParticleAt(Vector3 pos)
    {
        _ps.Clear(); 
        _currentDirection = pos.normalized;
        
        _fadeMultiplier = 1.0f;

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = _currentDirection * surfaceRadius;
        emitParams.startSize = particleSize;
        emitParams.startColor = particleColor;
        emitParams.startLifetime = float.MaxValue; 
            
        _ps.Emit(emitParams, 1);
        _isActive = true;
    }

    private void OnTravelStarted()
    {
        if (_isActive)
        {
            StartCoroutine(WaitAndKillRoutine());
        }
    }

    private IEnumerator WaitAndKillRoutine()
    {
        float waitTime = Mathf.Max(0f, postLaunchDuration - fadeOutDuration);
        
        if (waitTime > 0)
            yield return new WaitForSeconds(waitTime);

        float timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            _fadeMultiplier = Mathf.Lerp(1.0f, 0.0f, timer / fadeOutDuration);
            yield return null; 
        }

        _fadeMultiplier = 0f;
        _isActive = false;
        _ps.Clear();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StoredLaunchSitePosition = "";
            GameManager.Instance.SaveGame();
        }
    }

    private void UpdateParticleState()
    {
        int count = _ps.GetParticles(_buffer);
        if (count > 0)
        {
            float wave = (Mathf.Sin(Time.time * blinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            float brightness = Mathf.Lerp(0.2f, 1f, wave);

            _buffer[0].startColor = particleColor * brightness * _fadeMultiplier;
            _buffer[0].position = _currentDirection * surfaceRadius;
            
            _ps.SetParticles(_buffer, count);
        }
    }

    // --- METODO MODIFICATO PER LA LOGICA "VICINO MA NON TROPPO" ---
    private Vector3 FindValidPosition()
    {
        var existingPositions = populationVisuals != null ? populationVisuals.GetOccupiedPositions() : null;
        float maxY = Mathf.Sin(maxLatitude * Mathf.Deg2Rad);
        
        bool hasColonies = existingPositions != null && existingPositions.Count > 0;

        for (int i = 0; i < placementAttempts; i++)
        {
            Vector3 candidate = Vector3.zero;

            if (hasColonies)
            {
                // LOGICA SMART: Scegliamo un vicino esistente come ancora
                Vector3 randomNeighbor = existingPositions[Random.Range(0, existingPositions.Count)];
                
                // Generiamo una direzione casuale (offset)
                Vector3 randomOffsetDir = Random.onUnitSphere;
                
                // Calcoliamo una distanza casuale che sia:
                // > Min Distance (per non sovrapporsi)
                // < Max Distance (per stare "vicino")
                float randomDist = Random.Range(minDistanceFromEmitters, maxDistanceFromEmitters);
                
                // Calcoliamo la posizione grezza
                Vector3 roughPos = randomNeighbor + (randomOffsetDir * randomDist);
                
                // Proiettiamo sulla superficie sferica corretta
                candidate = roughPos.normalized * surfaceRadius;
            }
            else
            {
                // Se non ci sono colonie (es. inizio gioco ma impossibile in questa fase), fallback casuale
                candidate = Random.onUnitSphere * surfaceRadius;
            }

            // 1. Controllo Latitudine (Non troppo ai poli)
            float normalizedY = candidate.y / surfaceRadius;
            if (Mathf.Abs(normalizedY) > maxY) continue;

            // 2. Controllo Collisioni
            // Anche se abbiamo generato "vicino" a un vicino, potremmo essere finiti 
            // troppo vicini a un ALTRO vicino diverso. Controlliamo.
            if (hasColonies)
            {
                bool tooClose = false;
                foreach (Vector3 existing in existingPositions)
                {
                    float dist = Vector3.Distance(candidate, existing);
                    if (dist < minDistanceFromEmitters)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;
            }
            
            // Se siamo arrivati qui, la posizione è valida
            return candidate;
        }
        
        // Se falliscono tutti i tentativi "smart" (es. pianeta pienissimo), fallback casuale puro
        return Random.onUnitSphere * surfaceRadius;
    }

    private string Vector3ToString(Vector3 v) 
    { 
        return $"{v.x.ToString(CultureInfo.InvariantCulture)}|{v.y.ToString(CultureInfo.InvariantCulture)}|{v.z.ToString(CultureInfo.InvariantCulture)}"; 
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

    public Vector3 GetCurrentWorldPosition()
    {
        if (!_isActive) return Vector3.zero;
        Vector3 localPos = _currentDirection * surfaceRadius;
        return transform.TransformPoint(localPos);
    }
}