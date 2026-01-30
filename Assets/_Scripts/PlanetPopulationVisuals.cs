using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

[RequireComponent(typeof(ParticleSystem))]
[ExecuteAlways]
public class PlanetPopulationVisuals : MonoBehaviour
{
    public IReadOnlyList<Vector3> GetOccupiedPositions() => _occupiedPositions;

    [Header("CONFIGURAZIONE STANDARD")]
    public float surfaceRadius = 1.60f; 
    public float baseLightSize = 0.05f;
    public int maxLights = 2000;

    [Header("--- URBANIZZAZIONE (City Base) ---")]
    [Tooltip("Sistema particellare per la texture del terreno urbanizzato")]
    public ParticleSystem cityBasePS;
    [Tooltip("Dimensione base della chiazza di città")]
    public float cityBaseSize = 0.15f; 
    [Tooltip("Colore della base cittadina (Grigio/Bluastro)")]
    public Color cityBaseColor = new Color(0.3f, 0.35f, 0.4f, 0.8f);
    [Tooltip("Probabilità che compaia una base cittadina (0.3 = 30%). Riduce il carico GPU.")]
    [Range(0f, 1f)]
    public float citySpawnChance = 0.3f; 
    [Tooltip("Quanto ingrandire le chiazze per compensare il numero ridotto (1.5 = +50%).")]
    public float citySizeMultiplier = 1.7f;

    [Header("ENERGY FEEDBACK")]
    [ColorUsage(true, true)] public Color idleColor = new Color(1f, 0.6f, 0.2f, 1f); 
    public float idleIntensity = 1.0f;
    [ColorUsage(true, true)] public Color maxPowerColor = new Color(0.4f, 0.8f, 1f, 1f); 
    public float maxPowerIntensity = 2.5f;

    [Header("Vincoli di Generazione")]
    [Range(0f, 90f)] public float firstNodeMaxLatitude = 50f;
    [Range(0f, 90f)] public float generalMaxLatitude = 70f;
    public float minDistance = 0.05f;
    public int maxSpawnAttempts = 20;

    [Header("Animazione")] 
    [Range(0.1f, 5.0f)] public float fadeDuration = 1.5f; 

    [Header("Algoritmo Colonizzazione")]
    public float clusterSpread = 0.2f; 
    [Tooltip("Probabilità di generare un nuovo punto isolato invece di espandere un cluster esistente.")]
    public float newHubChance = 0.05f;
    [Tooltip("Numero di emitter iniziali che DEVONO essere vicini al primo (cluster forzato) prima di permettere nuovi Hub.")]
    public int safeStartCount = 6; // <--- NUOVA VARIABILE AGGIUNTA

    [Header("Rendering")]
    public Transform sunLight; 

    [Header("Visual Connections")]
    public ParticleSystem connectionPS; 
    [ColorUsage(true, true)] public Color connectionNightColor = new Color(1f, 0.8f, 0f, 1f);
    public Color connectionDayColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    public float maxConnectionDistance = 0.5f; 
    public float lineThickness = 0.00125f;
    public float arcHeight = 0.0025f;
    public float randomHeightVariance = 0.01f;
    [Range(1, 5)] public int maxConnectionsPerNode = 2;

    // INTERNE
    private Mesh _arcMesh; 
    private ParticleSystemRenderer _psRenderer;
    private ParticleSystemRenderer _connectionRenderer;
    private ParticleSystemRenderer _cityBaseRenderer;
    private ParticleSystem _ps; 
    
    private ParticleSystem.Particle[] _particlesBuffer; 
    private ParticleSystem.Particle[] _connectionsBuffer; 
    private ParticleSystem.Particle[] _cityBuffer; 

    public int SpawnedCount { get; private set; } = 0;
    private List<Vector3> _occupiedPositions = new List<Vector3>();
    
    private static readonly int SunDirID = Shader.PropertyToID("_SunDirection");
    private static readonly int DayColorID = Shader.PropertyToID("_DayColor");
    private static readonly int NightColorID = Shader.PropertyToID("_NightColor");
    private static readonly int PlanetPosID = Shader.PropertyToID("_PlanetPosition");
    
    private struct NeighborCandidate { public Vector3 position; public float distance; }

    void Awake()
    {
        if (Application.isPlaying)
        {
            // 1. LUCI (EMITTERS)
            _ps = GetComponent<ParticleSystem>();
            _psRenderer = _ps.GetComponent<ParticleSystemRenderer>();
            
            if (_psRenderer != null) _psRenderer.sortingOrder = 10;

            _particlesBuffer = new ParticleSystem.Particle[maxLights];
            var emission = _ps.emission; emission.enabled = false;
            var main = _ps.main; main.loop = false; main.playOnAwake = true; main.maxParticles = maxLights; main.simulationSpace = ParticleSystemSimulationSpace.Local; 
            if (!_ps.isPlaying) _ps.Play();
            
            // 2. CONNESSIONI (LINEE)
            if (connectionPS != null)
            {
                var connMain = connectionPS.main;
                connMain.loop = false; connMain.playOnAwake = true; connMain.simulationSpace = ParticleSystemSimulationSpace.Local;
                connMain.maxParticles = maxLights * maxConnectionsPerNode; 
                _connectionRenderer = connectionPS.GetComponent<ParticleSystemRenderer>();
                
                if (_connectionRenderer != null) _connectionRenderer.sortingOrder = 5;

                _connectionsBuffer = new ParticleSystem.Particle[connMain.maxParticles];
                GenerateArcMesh();
                if (_connectionRenderer != null && _arcMesh != null)
                {
                    _connectionRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    _connectionRenderer.mesh = _arcMesh;
                    _connectionRenderer.alignment = ParticleSystemRenderSpace.Local;
                }
                connectionPS.Stop(); connectionPS.Clear(); connectionPS.Play();
            }

            // 3. CITY BASE (URBANIZZAZIONE)
            if (cityBasePS != null)
            {
                var cityMain = cityBasePS.main;
                cityMain.loop = false; cityMain.playOnAwake = true; cityMain.simulationSpace = ParticleSystemSimulationSpace.Local;
                cityMain.maxParticles = maxLights;
                cityMain.startRotation3D = true; 

                _cityBaseRenderer = cityBasePS.GetComponent<ParticleSystemRenderer>();
                
                if (_cityBaseRenderer != null) _cityBaseRenderer.sortingOrder = 0;

                _cityBaseRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                _cityBaseRenderer.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx"); 
                if(_cityBaseRenderer.mesh == null) _cityBaseRenderer.renderMode = ParticleSystemRenderMode.Billboard; 

                _cityBuffer = new ParticleSystem.Particle[maxLights];
                cityBasePS.Stop(); cityBasePS.Clear(); cityBasePS.Play();
            }
            
            ResetInternalData();
            if (sunLight == null) {
                var light = FindFirstObjectByType<Light>();
                if (light != null && light.type == LightType.Directional) sunLight = light.transform;
            }
        }
    }

    void Start()
    {
        if (Application.isPlaying && GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated += RefreshLights;
    }

    void Update()
    {
        if (this == null || gameObject == null) return;

        // Update Shaders
        if (Application.isPlaying && sunLight != null)
        {
            Vector3 sunDir = -sunLight.forward;
            if (_psRenderer != null) _psRenderer.material.SetVector(SunDirID, sunDir);
            if (_connectionRenderer != null) {
                Material mat = _connectionRenderer.material;
                mat.SetVector(SunDirID, sunDir);
                mat.SetColor(DayColorID, connectionDayColor);
                mat.SetColor(NightColorID, connectionNightColor);
                mat.SetVector(PlanetPosID, transform.position);
            }
        }

        // Feedback Colore Tasto
        Color currentTargetColor = idleColor * idleIntensity;
        if (Application.isPlaying && GameManager.Instance != null) {
            float t = 0f;
            if (GameManager.Instance.EffectiveMaxMultiplier > 1.0f)
                t = Mathf.Clamp01((GameManager.Instance.CurrentEnergyMultiplier - 1.0f) / (GameManager.Instance.EffectiveMaxMultiplier - 1.0f));
            currentTargetColor = Color.Lerp(idleColor, maxPowerColor, t) * Mathf.Lerp(idleIntensity, maxPowerIntensity, t);
        }

        // Animazione Fade e Colore
        if (Application.isPlaying) 
        {
            AnimateSystem(_ps, _particlesBuffer, true, currentTargetColor);
            AnimateSystem(connectionPS, _connectionsBuffer, false, Color.white);
            AnimateSystem(cityBasePS, _cityBuffer, false, cityBaseColor);
        }
    }

    private void AnimateSystem(ParticleSystem sys, ParticleSystem.Particle[] buffer, bool isLightNode, Color targetRGB)
    {
        if(sys == null || buffer == null) return;
        int count = sys.GetParticles(buffer);
        bool hasChanges = false;
        float fadeStep = (1f / Mathf.Max(fadeDuration, 0.01f)) * Time.deltaTime;
        
        for (int i = 0; i < count; i++)
        {
            float currentAlpha = buffer[i].startColor.a;
            if (currentAlpha < 1f) {
                currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, fadeStep);
                hasChanges = true;
            }
            if (isLightNode) {
                Color newColor = targetRGB; newColor.a = currentAlpha;
                if (buffer[i].startColor != newColor) { buffer[i].startColor = newColor; hasChanges = true; }
            } else {
                Color c = targetRGB; c.a = currentAlpha * targetRGB.a;
                if (buffer[i].startColor != c) { buffer[i].startColor = c; hasChanges = true; }
            }
        }
        if (hasChanges) sys.SetParticles(buffer, count);
    }

    // --- GENERAZIONE & SPAWN ---

    // --- NUOVO: METODO PER SPAWN FORZATO (ATTERRAGGIO) ---
    public void SpawnSpecificEmitter(Vector3 exactPosition)
    {
        _occupiedPositions.Add(exactPosition);
        SpawnedCount++; // Incrementiamo così non ne spawna un altro per errore

        float parentScale = GetParentScale();
        
        // 1. Luce
        ParticleSystem.EmitParams lightParams = new ParticleSystem.EmitParams();
        lightParams.startColor = new Color(1, 1, 1, 0f); // Parte invisibile e fa fade in
        lightParams.startLifetime = float.MaxValue;
        lightParams.startSize = baseLightSize / parentScale;
        lightParams.position = exactPosition * 1.002f; // Micro-sollevamento
        _ps.Emit(lightParams, 1);

        // 2. Connessioni
        TryConnectToNeighbors(exactPosition);

        // 3. City Base (Sempre presente per il primo atterraggio)
        if (cityBasePS != null)
        {
            ParticleSystem.EmitParams cityParams = new ParticleSystem.EmitParams();
            cityParams.startColor = new Color(cityBaseColor.r, cityBaseColor.g, cityBaseColor.b, 0f);
            cityParams.startLifetime = float.MaxValue;
            cityParams.startSize = (cityBaseSize / parentScale) * citySizeMultiplier;
            cityParams.position = exactPosition;

            Quaternion lookRot = Quaternion.LookRotation(exactPosition.normalized);
            Vector3 euler = lookRot.eulerAngles;
            euler.z = Random.Range(0, 360f);
            cityParams.rotation3D = euler;

            cityBasePS.Emit(cityParams, 1);
        }
    }

    private void SpawnParticles(int count)
    {
        float parentScale = GetParentScale();
        
        ParticleSystem.EmitParams lightParams = new ParticleSystem.EmitParams();
        lightParams.startColor = new Color(1, 1, 1, 0f); 
        lightParams.startLifetime = float.MaxValue;
        lightParams.startSize = baseLightSize / parentScale;

        ParticleSystem.EmitParams cityParams = new ParticleSystem.EmitParams();
        cityParams.startColor = new Color(cityBaseColor.r, cityBaseColor.g, cityBaseColor.b, 0f);
        cityParams.startLifetime = float.MaxValue;
        cityParams.startSize = (cityBaseSize / parentScale) * citySizeMultiplier;

        for (int i = 0; i < count; i++)
        {
            Vector3 newPos = GetSmartPosition();
            if (newPos == Vector3.zero) continue;
            
            TryConnectToNeighbors(newPos);
            _occupiedPositions.Add(newPos);
            
            lightParams.position = newPos * 1.002f; 
            _ps.Emit(lightParams, 1);

            if (cityBasePS != null && Random.value < citySpawnChance)
            {
                cityParams.position = newPos;
                Quaternion lookRot = Quaternion.LookRotation(newPos.normalized);
                Vector3 euler = lookRot.eulerAngles;
                euler.z = Random.Range(0, 360f); 
                
                cityParams.rotation3D = euler;
                cityBasePS.Emit(cityParams, 1);
            }
        }
    }

    public void LoadEncodedPositions(List<string> savedPositions)
    {
        if (savedPositions == null || savedPositions.Count == 0) return;
        ResetInternalData();
        float parentScale = GetParentScale();
        
        ParticleSystem.EmitParams lightParams = new ParticleSystem.EmitParams();
        lightParams.startColor = Color.white; lightParams.startLifetime = float.MaxValue;
        lightParams.startSize = baseLightSize / parentScale;

        ParticleSystem.EmitParams cityParams = new ParticleSystem.EmitParams();
        cityParams.startColor = cityBaseColor; cityParams.startLifetime = float.MaxValue;
        cityParams.startSize = (cityBaseSize / parentScale) * citySizeMultiplier;

        foreach (string posStr in savedPositions)
        {
            Vector3 pos = StringToVector3(posStr);
            TryConnectToNeighbors(pos);
            _occupiedPositions.Add(pos);
            
            lightParams.position = pos * 1.002f;
            _ps.Emit(lightParams, 1);

            if (cityBasePS != null && Random.value < citySpawnChance)
            {
                cityParams.position = pos;
                Quaternion lookRot = Quaternion.LookRotation(pos.normalized);
                Vector3 euler = lookRot.eulerAngles;
                euler.z = Random.Range(0, 360f);
                cityParams.rotation3D = euler;
                cityBasePS.Emit(cityParams, 1);
            }
        }
        SpawnedCount = _occupiedPositions.Count;
    }

    public void RefreshLights() 
    { 
        if (this == null || gameObject == null) return;
        if (GameManager.Instance == null) return; 
        
        int target = Mathf.Min(GameManager.Instance.EmitterCount, maxLights); 
        if (target > SpawnedCount) { SpawnParticles(target - SpawnedCount); SpawnedCount = target; } 
    }

    private float GetParentScale() 
    { 
        if (this == null || transform == null) return 1f;
        float s = transform.parent != null ? transform.parent.localScale.x : 1f; 
        return (s == 0) ? 1f : s; 
    }

    private void GenerateArcMesh() {
        if (_arcMesh != null) return;
        _arcMesh = new Mesh(); _arcMesh.name = "Arc_Procedural";
        int segments = 16; float width = 1f; float length = 1f; 
        List<Vector3> vertices = new List<Vector3>(); List<int> triangles = new List<int>(); List<Vector2> uvs = new List<Vector2>();
        for (int i = 0; i <= segments; i++) {
            float t = (float)i / segments; float zPos = t * length; float yPos = Mathf.Sin(t * Mathf.PI); 
            vertices.Add(new Vector3(-width/2, yPos, zPos)); vertices.Add(new Vector3(width/2, yPos, zPos));
            uvs.Add(new Vector2(t, 0)); uvs.Add(new Vector2(t, 1));
            if (i > 0) {
                int cb = i * 2; int pb = (i - 1) * 2;
                triangles.Add(pb + 0); triangles.Add(cb + 0); triangles.Add(pb + 1);
                triangles.Add(pb + 1); triangles.Add(cb + 0); triangles.Add(cb + 1);
                triangles.Add(pb + 0); triangles.Add(pb + 1); triangles.Add(cb + 0);
                triangles.Add(pb + 1); triangles.Add(cb + 1); triangles.Add(cb + 0);
            }
        }
        _arcMesh.SetVertices(vertices); _arcMesh.SetTriangles(triangles, 0); _arcMesh.SetUVs(0, uvs); _arcMesh.RecalculateNormals();
    }
    private void CreateVisualConnection(Vector3 posA, Vector3 posB, float distance) {
        ParticleSystem.EmitParams lineParams = new ParticleSystem.EmitParams();
        lineParams.position = posA; lineParams.startColor = new Color(1, 1, 1, 0); lineParams.startLifetime = float.MaxValue;
        Vector3 direction = (posB - posA).normalized; Vector3 chordCenter = (posA + posB).normalized; 
        Quaternion rotation = Quaternion.LookRotation(direction, chordCenter);
        lineParams.rotation3D = rotation.eulerAngles;
        float preciseDistance = Vector3.Distance(posA, posB); Vector3 midPoint = (posA + posB) * 0.5f;
        float sag = surfaceRadius - midPoint.magnitude;
        float totalHeight = sag + arcHeight + Random.Range(0f, randomHeightVariance);
        lineParams.startSize3D = new Vector3(lineThickness, totalHeight, preciseDistance);
        connectionPS.Emit(lineParams, 1);
    }
    public List<string> GetEncodedPositions() { List<string> list = new List<string>(); foreach (Vector3 pos in _occupiedPositions) list.Add(Vector3ToString(pos)); return list; }
    public void ResetVisuals() { ResetInternalData(); RefreshLights(); }
    private void ResetInternalData() { if(_ps) _ps.Clear(); if (connectionPS != null) connectionPS.Clear(); if (cityBasePS != null) cityBasePS.Clear(); _occupiedPositions.Clear(); SpawnedCount = 0; }
    
    private void TryConnectToNeighbors(Vector3 newPos) {
        if (connectionPS == null || _occupiedPositions.Count == 0) return;
        int scanLimit = 50; int startIndex = Mathf.Max(0, _occupiedPositions.Count - scanLimit);
        List<NeighborCandidate> candidates = new List<NeighborCandidate>();
        for (int i = _occupiedPositions.Count - 1; i >= startIndex; i--) {
            float dist = Vector3.Distance(newPos, _occupiedPositions[i]);
            if (dist < maxConnectionDistance) candidates.Add(new NeighborCandidate { position = _occupiedPositions[i], distance = dist });
        }
        if (candidates.Count > 0) {
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            int connectionsToMake = Mathf.Min(candidates.Count, maxConnectionsPerNode);
            for (int k = 0; k < connectionsToMake; k++) CreateVisualConnection(newPos, candidates[k].position, candidates[k].distance);
        }
    }

    private Vector3 GetSmartPosition() {
        int attempts = 0; 
        float limitDegrees = (_occupiedPositions.Count == 0) ? firstNodeMaxLatitude : generalMaxLatitude; 
        float maxY = Mathf.Sin(limitDegrees * Mathf.Deg2Rad);
        
        while (attempts < maxSpawnAttempts) {
            Vector3 candidatePos = Vector3.zero;
            
            // --- FIX MODIFICA: Logica Safe Start ---
            // Se abbiamo meno di 'safeStartCount' luci, forziamo il cluster (chance = 0).
            // Altrimenti usiamo la chance definita nell'Inspector.
            // Nota: _occupiedPositions.Count == 0 è gestito nell'if sotto per il primo punto assoluto.
            float currentHubChance = (_occupiedPositions.Count > 0 && _occupiedPositions.Count < safeStartCount) ? 0f : newHubChance;

            if (_occupiedPositions.Count == 0 || Random.value < currentHubChance) 
                candidatePos = Random.onUnitSphere * surfaceRadius;
            else 
            { 
                int randomIndex = Random.Range(0, _occupiedPositions.Count); 
                Vector3 randomNeighbor = _occupiedPositions[randomIndex]; 
                Vector3 randomOffset = Random.insideUnitSphere * clusterSpread; 
                candidatePos = (randomNeighbor + randomOffset).normalized * surfaceRadius; 
            }
            // ----------------------------------------

            float normalizedY = candidatePos.y / surfaceRadius;
            if (Mathf.Abs(normalizedY) > maxY) { attempts++; continue; }
            bool isTooClose = false;
            foreach (var pos in _occupiedPositions) if (Vector3.SqrMagnitude(candidatePos - pos) < minDistance * minDistance) { isTooClose = true; break; }
            if (isTooClose) { attempts++; continue; }
            return candidatePos;
        }
        return Vector3.zero;
    }
    private string Vector3ToString(Vector3 v) { return $"{v.x.ToString(CultureInfo.InvariantCulture)}|{v.y.ToString(CultureInfo.InvariantCulture)}|{v.z.ToString(CultureInfo.InvariantCulture)}"; }
    private Vector3 StringToVector3(string s) {
        string[] parts = s.Split('|'); if (parts.Length < 3) return Vector3.zero;
        float x = float.Parse(parts[0], CultureInfo.InvariantCulture); float y = float.Parse(parts[1], CultureInfo.InvariantCulture); float z = float.Parse(parts[2], CultureInfo.InvariantCulture);
        return new Vector3(x, y, z);
    }
}