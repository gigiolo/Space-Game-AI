using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Linq; // NECESSARIO per ordinare le liste

[RequireComponent(typeof(ParticleSystem))]
public class PlanetPopulationVisuals : MonoBehaviour
{
    [Header("CONFIGURAZIONE")]
    public float surfaceRadius = 1.60f; 
    public float baseLightSize = 0.05f;
    public int maxLights = 2000;

    [Header("Animazione Spawn (Flash)")] 
    public Color spawnFlashColor = Color.red; 
    [Range(0.1f, 3.0f)] 
    public float flashDuration = 0.5f;

    [Header("Algoritmo Colonizzazione")]
    public float clusterSpread = 0.2f; 
    public float newHubChance = 0.05f;

    [Header("Rendering Giorno/Notte")]
    public Transform sunLight; 

    // --- SEZIONE CONNESSIONI ---
    [Header("Visual Connections (Lines)")]
    public ParticleSystem connectionPS; 
    
    [Tooltip("Distanza massima per creare una connessione.")]
    public float maxConnectionDistance = 0.5f; 
    
    [Tooltip("Spessore della linea.")]
    public float lineThickness = 0.0025f; // Il tuo valore funzionante

    [Tooltip("Quanto sollevare le linee dalla superficie.")]
    public float lineHeightOffset = 1f; // Il tuo valore funzionante

    [Tooltip("Quante connessioni massime può avere ogni nodo.")]
    [Range(1, 5)] // Slider da 1 a 5
    public int maxConnectionsPerNode = 2; // <--- NUOVA VARIABILE

    public Color connectionColor = new Color(1f, 0.8f, 0f, 1f); 
    // ----------------------------
    
    private ParticleSystemRenderer _psRenderer;
    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particlesBuffer; 

    public int SpawnedCount { get; private set; } = 0;
    
    private List<Vector3> _occupiedPositions = new List<Vector3>();
    private static readonly int SunDirID = Shader.PropertyToID("_SunDirection");

    // Piccola struct per gestire i candidati vicini
    private struct NeighborCandidate
    {
        public Vector3 position;
        public float distance;
    }

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        _psRenderer = _ps.GetComponent<ParticleSystemRenderer>();
        _particlesBuffer = new ParticleSystem.Particle[maxLights];

        var emission = _ps.emission;
        emission.enabled = false;
        
        var main = _ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = maxLights;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; 
        
        if (!_ps.isPlaying) _ps.Play();
        
        if (connectionPS != null)
        {
            var connMain = connectionPS.main;
            connMain.loop = false;
            connMain.playOnAwake = true;
            connMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            connectionPS.Stop();
            connectionPS.Clear();
            connectionPS.Play();
        }

        ResetInternalData();

        if (sunLight == null)
        {
            var light = FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
                sunLight = light.transform;
        }
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated += RefreshLights;
    }

    void Update()
    {
        if (sunLight != null && _psRenderer != null)
            _psRenderer.material.SetVector(SunDirID, -sunLight.forward);

        AnimateParticlesColor();
    }

    private void AnimateParticlesColor()
    {
        int count = _ps.GetParticles(_particlesBuffer);
        bool hasChanges = false;
        float step = (1f / Mathf.Max(flashDuration, 0.01f)) * Time.deltaTime;
        Vector4 targetColorV4 = (Vector4)Color.white;

        for (int i = 0; i < count; i++)
        {
            Vector4 currentColorV4 = (Vector4)(Color)_particlesBuffer[i].startColor;

            if (Vector4.Distance(currentColorV4, targetColorV4) > 0.01f)
            {
                Vector4 newColorV4 = Vector4.MoveTowards(currentColorV4, targetColorV4, step);
                _particlesBuffer[i].startColor = (Color)newColorV4;
                hasChanges = true;
            }
            else if (_particlesBuffer[i].startColor.r != 255 || _particlesBuffer[i].startColor.g != 255 || _particlesBuffer[i].startColor.b != 255)
            {
                _particlesBuffer[i].startColor = (Color32)Color.white;
                hasChanges = true;
            }
        }

        if (hasChanges) _ps.SetParticles(_particlesBuffer, count);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated -= RefreshLights;
    }

    public void LoadEncodedPositions(List<string> savedPositions)
    {
        if (savedPositions == null || savedPositions.Count == 0) return;

        ResetInternalData();

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.startColor = Color.white; 
        emitParams.startLifetime = float.MaxValue;
        
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        if (parentScale == 0) parentScale = 1f;
        emitParams.startSize = baseLightSize / parentScale;

        foreach (string posStr in savedPositions)
        {
            Vector3 pos = StringToVector3(posStr);
            TryConnectToNeighbors(pos);
            _occupiedPositions.Add(pos);
            emitParams.position = pos;
            _ps.Emit(emitParams, 1);
        }

        SpawnedCount = _occupiedPositions.Count;
    }

    public List<string> GetEncodedPositions()
    {
        List<string> list = new List<string>();
        foreach (Vector3 pos in _occupiedPositions)
            list.Add(Vector3ToString(pos));
        return list;
    }

    public void ResetVisuals()
    {
        ResetInternalData();
        RefreshLights();
    }

    private void ResetInternalData()
    {
        _ps.Clear();
        if (connectionPS != null) connectionPS.Clear();
        _occupiedPositions.Clear();
        SpawnedCount = 0;
    }

    public void RefreshLights()
    {
        if (GameManager.Instance == null) return;
        int target = Mathf.Min(GameManager.Instance.EmitterCount, maxLights);

        if (target > SpawnedCount)
        {
            SpawnParticles(target - SpawnedCount);
            SpawnedCount = target;
        }
    }

    private void SpawnParticles(int count)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.startColor = spawnFlashColor; 
        emitParams.startLifetime = float.MaxValue;
        
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        if (parentScale == 0) parentScale = 1f;
        emitParams.startSize = baseLightSize / parentScale;

        for (int i = 0; i < count; i++)
        {
            Vector3 newPos = GetSmartPosition();
            TryConnectToNeighbors(newPos);
            _occupiedPositions.Add(newPos);
            emitParams.position = newPos;
            _ps.Emit(emitParams, 1);
        }
    }

    // --- LOGICA DI CONNESSIONE MULTIPLA ---
    private void TryConnectToNeighbors(Vector3 newPos)
    {
        if (connectionPS == null || _occupiedPositions.Count == 0) return;

        // Scansioniamo gli ultimi 50 punti (performance)
        int scanLimit = 50; 
        int startIndex = Mathf.Max(0, _occupiedPositions.Count - scanLimit);

        // Lista temporanea per salvare tutti i candidati validi
        List<NeighborCandidate> candidates = new List<NeighborCandidate>();

        for (int i = _occupiedPositions.Count - 1; i >= startIndex; i--)
        {
            float dist = Vector3.Distance(newPos, _occupiedPositions[i]);
            
            // Se è dentro il raggio, è un candidato
            if (dist < maxConnectionDistance)
            {
                candidates.Add(new NeighborCandidate { position = _occupiedPositions[i], distance = dist });
            }
        }

        // Se abbiamo trovato candidati
        if (candidates.Count > 0)
        {
            // Li ordiniamo per distanza (dal più vicino al più lontano)
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Decidiamo quanti collegarne: il minimo tra quelli che abbiamo trovato e il limite imposto
            int connectionsToMake = Mathf.Min(candidates.Count, maxConnectionsPerNode);

            // Creiamo le linee per i primi N candidati
            for (int k = 0; k < connectionsToMake; k++)
            {
                CreateVisualConnection(newPos, candidates[k].position, candidates[k].distance);
            }
        }
    }

    private void CreateVisualConnection(Vector3 posA, Vector3 posB, float distance)
    {
        Vector3 midPoint = (posA + posB) / 2f;

        // CORREZIONE ALTEZZA (Galleggiamento)
        float currentRadius = surfaceRadius + lineHeightOffset;
        midPoint = midPoint.normalized * currentRadius;

        ParticleSystem.EmitParams lineParams = new ParticleSystem.EmitParams();
        lineParams.position = midPoint;
        lineParams.startColor = connectionColor;
        lineParams.startLifetime = float.MaxValue;

        // CORREZIONE LUNGHEZZA
        // Scala la lunghezza in base a quanto siamo distanti dal centro rispetto alla superficie
        float lengthScaleFactor = currentRadius / surfaceRadius; 
        float adjustedLength = distance * lengthScaleFactor;

        lineParams.startSize3D = new Vector3(lineThickness, adjustedLength, lineThickness);

        Vector3 direction = (posB - posA).normalized;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        lineParams.rotation3D = rotation.eulerAngles;

        connectionPS.Emit(lineParams, 1);
    }
    // ---------------------------------------------

    private Vector3 GetSmartPosition()
    {
        if (_occupiedPositions.Count == 0 || Random.value < newHubChance)
            return Random.onUnitSphere * surfaceRadius;

        int randomIndex = Random.Range(0, _occupiedPositions.Count);
        Vector3 randomNeighbor = _occupiedPositions[randomIndex];
        Vector3 randomOffset = Random.insideUnitSphere * clusterSpread;
        Vector3 targetPos = randomNeighbor + randomOffset;
        return targetPos.normalized * surfaceRadius;
    }

    private string Vector3ToString(Vector3 v)
    {
        return $"{v.x.ToString(CultureInfo.InvariantCulture)}|{v.y.ToString(CultureInfo.InvariantCulture)}|{v.z.ToString(CultureInfo.InvariantCulture)}";
    }

    private Vector3 StringToVector3(string s)
    {
        string[] parts = s.Split('|');
        if (parts.Length < 3) return Vector3.zero;

        float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[2], CultureInfo.InvariantCulture);
        return new Vector3(x, y, z);
    }
}