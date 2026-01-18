using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;

[RequireComponent(typeof(ParticleSystem))]
[ExecuteAlways]
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
    
    [Header(">>> TRASCINA QUI IL MATERIALE <<<")]
    [Tooltip("Trascina qui il file Mat_ConnectionLine dalla cartella Project")]
    public Material targetMaterial;

    [Tooltip("Distanza massima per creare una connessione.")]
    public float maxConnectionDistance = 0.5f; 
    
    [Tooltip("Spessore della linea.")]
    public float lineThickness = 0.00125f; // I tuoi valori ottimali

    [Tooltip("Altezza dell'arco.")]
    public float arcHeight = 0.0025f; // I tuoi valori ottimali

    [Tooltip("Variazione casuale altezza.")]
    public float randomHeightVariance = 0.01f;

    [Range(1, 5)]
    public int maxConnectionsPerNode = 2;

    public Color connectionColor = new Color(1f, 0.8f, 0f, 1f);

    [Header("Connection Fade Effect")]
    public AnimationCurve fadeCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 0), new Keyframe(1, 1));
    
    // --- VARIABILI INTERNE ---
    private Mesh _arcMesh; 
    
    private ParticleSystemRenderer _psRenderer;
    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particlesBuffer; 
    public int SpawnedCount { get; private set; } = 0;
    private List<Vector3> _occupiedPositions = new List<Vector3>();
    private static readonly int SunDirID = Shader.PropertyToID("_SunDirection");
    
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
                
                GenerateArcMesh();
                
                var connRenderer = connectionPS.GetComponent<ParticleSystemRenderer>();
                if (connRenderer != null && _arcMesh != null)
                {
                    connRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    connRenderer.mesh = _arcMesh;
                    connRenderer.alignment = ParticleSystemRenderSpace.Local;
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
        if (Application.isPlaying && sunLight != null && _psRenderer != null)
            _psRenderer.material.SetVector(SunDirID, -sunLight.forward);

        if (Application.isPlaying) AnimateParticlesColor();
    }

    // --- FIX UV: MAPPING ORIZZONTALE (U) ---
    private void GenerateArcMesh()
    {
        if (_arcMesh != null) return;

        _arcMesh = new Mesh();
        _arcMesh.name = "Arc_HorizontalUV";

        int segments = 16; 
        float width = 1f;  
        float length = 1f; 

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments; 
            
            // Z (Lunghezza) da 0 a 1 -> Pivot Start
            float zPos = t * length;
            float yPos = Mathf.Sin(t * Mathf.PI); 

            // Vertici
            vertices.Add(new Vector3(-width/2, yPos, zPos));
            vertices.Add(new Vector3(width/2, yPos, zPos));
            
            // --- FIX QUI: Usiamo 't' sulla coordinata X (U) ---
            // Mappiamo la lunghezza della linea sull'asse orizzontale della texture
            uvs.Add(new Vector2(t, 0)); 
            uvs.Add(new Vector2(t, 1));

            if (i > 0)
            {
                int currentBase = i * 2;
                int prevBase = (i - 1) * 2;

                // Top
                triangles.Add(prevBase + 0);
                triangles.Add(currentBase + 0);
                triangles.Add(prevBase + 1);

                triangles.Add(prevBase + 1);
                triangles.Add(currentBase + 0);
                triangles.Add(currentBase + 1);
                
                // Bottom
                triangles.Add(prevBase + 0);
                triangles.Add(prevBase + 1);
                triangles.Add(currentBase + 0);

                triangles.Add(prevBase + 1);
                triangles.Add(currentBase + 1);
                triangles.Add(currentBase + 0);
            }
        }

        _arcMesh.SetVertices(vertices);
        _arcMesh.SetTriangles(triangles, 0);
        _arcMesh.SetUVs(0, uvs);
        _arcMesh.RecalculateNormals();
    }

    // --- FIX TEXTURE: GENERAZIONE ORIZZONTALE ---
    [ContextMenu("Genera e Salva Texture Fade")]
    public void SaveTextureToFile()
    {
        // Creiamo una texture ORIZZONTALE (128 larghezza, 1 altezza)
        Texture2D tex = new Texture2D(128, 1, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp; 
        
        for (int x = 0; x < 128; x++)
        {
            float t = (float)x / 127f;
            float val = fadeCurve.Evaluate(t);
            // Scriviamo lungo l'asse X
            tex.SetPixel(x, 0, new Color(val, val, val, 1f));
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/ConnectionFade.png";
        File.WriteAllBytes(path, bytes);
        Debug.Log("Texture Fade Orizzontale salvata in: " + path);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh(); 
#endif
    }

    private void CreateVisualConnection(Vector3 posA, Vector3 posB, float distance)
    {
        ParticleSystem.EmitParams lineParams = new ParticleSystem.EmitParams();
        lineParams.position = posA; 
        lineParams.startColor = connectionColor; 
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

    // --- METODI STANDARD ---
    private void AnimateParticlesColor()
    {
        if(_particlesBuffer == null || _ps == null) return;
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

    public List<string> GetEncodedPositions()
    {
        List<string> list = new List<string>();
        foreach (Vector3 pos in _occupiedPositions) list.Add(Vector3ToString(pos));
        return list;
    }

    public void ResetVisuals() { ResetInternalData(); RefreshLights(); }

    private void ResetInternalData()
    {
        if(_ps) _ps.Clear();
        if (connectionPS != null) connectionPS.Clear();
        _occupiedPositions.Clear();
        SpawnedCount = 0;
    }

    public void RefreshLights()
    {
        if (GameManager.Instance == null) return;
        int target = Mathf.Min(GameManager.Instance.EmitterCount, maxLights);
        if (target > SpawnedCount) { SpawnParticles(target - SpawnedCount); SpawnedCount = target; }
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

    private void TryConnectToNeighbors(Vector3 newPos)
    {
        if (connectionPS == null || _occupiedPositions.Count == 0) return;
        int scanLimit = 50; 
        int startIndex = Mathf.Max(0, _occupiedPositions.Count - scanLimit);
        List<NeighborCandidate> candidates = new List<NeighborCandidate>();

        for (int i = _occupiedPositions.Count - 1; i >= startIndex; i--)
        {
            float dist = Vector3.Distance(newPos, _occupiedPositions[i]);
            if (dist < maxConnectionDistance) candidates.Add(new NeighborCandidate { position = _occupiedPositions[i], distance = dist });
        }

        if (candidates.Count > 0)
        {
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            int connectionsToMake = Mathf.Min(candidates.Count, maxConnectionsPerNode);
            for (int k = 0; k < connectionsToMake; k++)
            {
                CreateVisualConnection(newPos, candidates[k].position, candidates[k].distance);
            }
        }
    }

    private Vector3 GetSmartPosition()
    {
        if (_occupiedPositions.Count == 0 || Random.value < newHubChance) return Random.onUnitSphere * surfaceRadius;
        int randomIndex = Random.Range(0, _occupiedPositions.Count);
        Vector3 randomNeighbor = _occupiedPositions[randomIndex];
        Vector3 randomOffset = Random.insideUnitSphere * clusterSpread;
        Vector3 targetPos = randomNeighbor + randomOffset;
        return targetPos.normalized * surfaceRadius;
    }

    private string Vector3ToString(Vector3 v) { return $"{v.x.ToString(CultureInfo.InvariantCulture)}|{v.y.ToString(CultureInfo.InvariantCulture)}|{v.z.ToString(CultureInfo.InvariantCulture)}"; }

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