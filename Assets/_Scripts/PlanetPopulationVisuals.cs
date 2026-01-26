using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

[RequireComponent(typeof(ParticleSystem))]
[ExecuteAlways]
public class PlanetPopulationVisuals : MonoBehaviour
{
    // ... [METODI PUBBLICI PER IL SALVATAGGIO RIMANGONO INVARIATI] ...
    public IReadOnlyList<Vector3> GetOccupiedPositions()
    {
        return _occupiedPositions;
    }

    [Header("CONFIGURAZIONE STANDARD")]
    public float surfaceRadius = 1.60f; 
    public float baseLightSize = 0.05f;
    public int maxLights = 2000;

    [Header("ENERGY FEEDBACK (Power Button)")]
    [Tooltip("Colore quando il tasto NON è premuto (es. Arancione/Giallo caldo).")]
    [ColorUsage(true, true)] // Abilita HDR nell'inspector
    public Color idleColor = new Color(1f, 0.6f, 0.2f, 1f); 
    public float idleIntensity = 1.0f;

    [Tooltip("Colore quando il moltiplicatore è al MASSIMO (es. Azzurro/Bianco freddo).")]
    [ColorUsage(true, true)]
    public Color maxPowerColor = new Color(0.4f, 0.8f, 1f, 1f); 
    public float maxPowerIntensity = 2.5f;

    [Header("Vincoli di Generazione")]
    [Range(0f, 90f)] 
    public float firstNodeMaxLatitude = 50f;
    [Range(0f, 90f)] 
    public float generalMaxLatitude = 70f;
    public float minDistance = 0.05f;
    public int maxSpawnAttempts = 20;

    [Header("Animazione Spawn")] 
    [Tooltip("Durata in secondi dell'animazione di comparsa.")]
    [Range(0.1f, 5.0f)] 
    public float fadeDuration = 1.5f; 

    [Header("Algoritmo Colonizzazione")]
    public float clusterSpread = 0.2f; 
    public float newHubChance = 0.05f;

    [Header("Rendering Giorno/Notte")]
    public Transform sunLight; 

    [Header("Visual Connections (Lines)")]
    public ParticleSystem connectionPS; 
    [ColorUsage(true, true)] 
    public Color connectionNightColor = new Color(1f, 0.8f, 0f, 1f);
    public Color connectionDayColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    public float maxConnectionDistance = 0.5f; 
    public float lineThickness = 0.00125f;
    public float arcHeight = 0.0025f;
    public float randomHeightVariance = 0.01f;
    [Range(1, 5)]
    public int maxConnectionsPerNode = 2;

    // --- VARIABILI INTERNE ---
    private Mesh _arcMesh; 
    private ParticleSystemRenderer _psRenderer;
    private ParticleSystemRenderer _connectionRenderer;
    private ParticleSystem _ps;
    
    private ParticleSystem.Particle[] _particlesBuffer; 
    private ParticleSystem.Particle[] _connectionsBuffer; 

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
                connMain.maxParticles = maxLights * maxConnectionsPerNode; 
                
                _connectionRenderer = connectionPS.GetComponent<ParticleSystemRenderer>();
                _connectionsBuffer = new ParticleSystem.Particle[connMain.maxParticles];

                GenerateArcMesh();
                
                if (_connectionRenderer != null && _arcMesh != null)
                {
                    _connectionRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    _connectionRenderer.mesh = _arcMesh;
                    _connectionRenderer.alignment = ParticleSystemRenderSpace.Local;
                }

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
    }

    void Start()
    {
        if (Application.isPlaying && GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated += RefreshLights;
    }

    void Update()
    {
        // 1. Aggiornamento Shader Giorno/Notte
        if (Application.isPlaying && sunLight != null)
        {
            Vector3 sunDir = -sunLight.forward;
            if (_psRenderer != null) _psRenderer.material.SetVector(SunDirID, sunDir);

            if (_connectionRenderer != null)
            {
                Material mat = _connectionRenderer.material;
                mat.SetVector(SunDirID, sunDir);
                mat.SetColor(DayColorID, connectionDayColor);
                mat.SetColor(NightColorID, connectionNightColor);
                mat.SetVector(PlanetPosID, transform.position);
            }
        }

        // 2. Calcolo Colore Energetico (Feedback Tasto)
        Color currentTargetColor = idleColor * idleIntensity;
        
        if (Application.isPlaying && GameManager.Instance != null)
        {
            // Ottieni lo stato del moltiplicatore
            float currentMult = GameManager.Instance.CurrentEnergyMultiplier;
            float maxMult = GameManager.Instance.EffectiveMaxMultiplier;

            // Normalizza da 0 a 1 (0 = Idle, 1 = Max Power)
            float t = 0f;
            if (maxMult > 1.0f)
            {
                t = Mathf.Clamp01((currentMult - 1.0f) / (maxMult - 1.0f));
            }

            // Interpola colore e intensità
            Color c = Color.Lerp(idleColor, maxPowerColor, t);
            float i = Mathf.Lerp(idleIntensity, maxPowerIntensity, t);
            
            // Colore HDR Finale
            currentTargetColor = c * i;
        }

        // 3. Animazione Particelle
        if (Application.isPlaying) 
        {
            // Passiamo il colore calcolato dinamicamente
            AnimateSystem(_ps, _particlesBuffer, true, currentTargetColor);
            
            // Le connessioni usano una logica diversa (alpha fissa o gestita dallo shader), 
            // ma passiamo null o colore base per ora se vogliamo mantenerle standard
            AnimateSystem(connectionPS, _connectionsBuffer, false, Color.white);
        }
    }

    private void AnimateSystem(ParticleSystem sys, ParticleSystem.Particle[] buffer, bool isLightNode, Color targetRGB)
    {
        if(sys == null || buffer == null) return;
        
        int count = sys.GetParticles(buffer);
        bool hasChanges = false;
        
        // Velocità di fade per lo spawn
        float fadeStep = (1f / Mathf.Max(fadeDuration, 0.01f)) * Time.deltaTime;
        
        for (int i = 0; i < count; i++)
        {
            // Gestione Alpha (Spawn)
            float currentAlpha = buffer[i].startColor.a;
            
            // Se non è ancora completamente visibile, aumenta l'alpha
            if (currentAlpha < 1f)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, fadeStep);
                hasChanges = true;
            }

            // Gestione Colore RGB (Energy Feedback)
            if (isLightNode)
            {
                // Applica il colore RGB calcolato dal GameManager, ma conserva l'alpha dello spawn
                Color newColor = targetRGB;
                newColor.a = currentAlpha;
                
                // Assegna solo se diverso per ottimizzare (anche se SetParticles fa comunque tutto)
                if (buffer[i].startColor != newColor)
                {
                    buffer[i].startColor = newColor;
                    hasChanges = true;
                }
            }
            else
            {
                // Per le connessioni o altro, gestiamo solo l'alpha di spawn
                Color c = buffer[i].startColor;
                if (c.a != currentAlpha)
                {
                    c.a = currentAlpha;
                    buffer[i].startColor = c;
                    hasChanges = true;
                }
            }
        }
        
        if (hasChanges) sys.SetParticles(buffer, count);
    }

    // [IL RESTO DELLO SCRIPT (GenerateArcMesh, CreateVisualConnection, LoadEncodedPositions, ecc.) 
    // RIMANE IDENTICO A PRIMA. COPIALO DAL FILE PRECEDENTE O LASCIA CHE RIMANGA INTATTO SE MODIFICHI SOLO UPDATE]
    
    // ... INCOLLA QUI I METODI RIMANENTI (GenerateArcMesh, CreateVisualConnection, ecc.) ...
    // Per comodità, ecco i metodi essenziali per non rompere il copia-incolla:

    private void GenerateArcMesh()
    {
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

    private void CreateVisualConnection(Vector3 posA, Vector3 posB, float distance)
    {
        ParticleSystem.EmitParams lineParams = new ParticleSystem.EmitParams();
        lineParams.position = posA; 
        lineParams.startColor = new Color(1, 1, 1, 0); // Start Alpha 0
        lineParams.startLifetime = float.MaxValue;
        Vector3 direction = (posB - posA).normalized;
        Vector3 chordCenter = (posA + posB).normalized; 
        Quaternion rotation = Quaternion.LookRotation(direction, chordCenter);
        lineParams.rotation3D = rotation.eulerAngles;
        float preciseDistance = Vector3.Distance(posA, posB);
        Vector3 midPoint = (posA + posB) * 0.5f;
        float sag = surfaceRadius - midPoint.magnitude;
        float totalHeight = sag + arcHeight + Random.Range(0f, randomHeightVariance);
        lineParams.startSize3D = new Vector3(lineThickness, totalHeight, preciseDistance);
        connectionPS.Emit(lineParams, 1);
    }

    void OnDestroy()
    {
        if (Application.isPlaying && GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated -= RefreshLights;
        if (_arcMesh != null) DestroyImmediate(_arcMesh);
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

    public List<string> GetEncodedPositions() { List<string> list = new List<string>(); foreach (Vector3 pos in _occupiedPositions) list.Add(Vector3ToString(pos)); return list; }
    public void ResetVisuals() { ResetInternalData(); RefreshLights(); }
    private void ResetInternalData() { if(_ps) _ps.Clear(); if (connectionPS != null) connectionPS.Clear(); _occupiedPositions.Clear(); SpawnedCount = 0; }
    public void RefreshLights() { if (GameManager.Instance == null) return; int target = Mathf.Min(GameManager.Instance.EmitterCount, maxLights); if (target > SpawnedCount) { SpawnParticles(target - SpawnedCount); SpawnedCount = target; } }

    private void SpawnParticles(int count)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        // Start Alpha 0. Il colore RGB qui non conta molto perché viene sovrascritto in Update,
        // ma impostiamolo bianco per sicurezza.
        emitParams.startColor = new Color(1, 1, 1, 0f); 
        
        emitParams.startLifetime = float.MaxValue;
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        if (parentScale == 0) parentScale = 1f;
        emitParams.startSize = baseLightSize / parentScale;
        for (int i = 0; i < count; i++)
        {
            Vector3 newPos = GetSmartPosition();
            if (newPos == Vector3.zero) continue;
            TryConnectToNeighbors(newPos);
            _occupiedPositions.Add(newPos);
            emitParams.position = newPos;
            _ps.Emit(emitParams, 1);
        }
    }

    private void TryConnectToNeighbors(Vector3 newPos)
    {
        if (connectionPS == null || _occupiedPositions.Count == 0) return;
        int scanLimit = 50; 
        int startIndex = Mathf.Max(0, _occupiedPositions.Count - scanLimit);
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

    private Vector3 GetSmartPosition()
    {
        int attempts = 0;
        float limitDegrees = (_occupiedPositions.Count == 0) ? firstNodeMaxLatitude : generalMaxLatitude;
        float maxY = Mathf.Sin(limitDegrees * Mathf.Deg2Rad);
        while (attempts < maxSpawnAttempts) {
            Vector3 candidatePos = Vector3.zero;
            if (_occupiedPositions.Count == 0 || Random.value < newHubChance) candidatePos = Random.onUnitSphere * surfaceRadius;
            else {
                int randomIndex = Random.Range(0, _occupiedPositions.Count);
                Vector3 randomNeighbor = _occupiedPositions[randomIndex];
                Vector3 randomOffset = Random.insideUnitSphere * clusterSpread;
                candidatePos = (randomNeighbor + randomOffset).normalized * surfaceRadius;
            }
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