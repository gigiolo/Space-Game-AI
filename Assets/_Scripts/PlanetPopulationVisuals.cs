using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO; // Necessario per salvare il file

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
    public float lineThickness = 0.0025f;

    [Tooltip("Quanto sollevare le linee dalla superficie.")]
    public float lineHeightOffset = 1f;

    [Range(1, 5)]
    public int maxConnectionsPerNode = 2;

    public Color connectionColor = new Color(1f, 0.8f, 0f, 1f);

    [Header("Connection Fade Effect")]
    [Tooltip("Disegna la luminosità lungo la linea. Alto = Bianco (Visibile), Basso = Nero (Invisibile in Additive).")]
    public AnimationCurve fadeCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 0), new Keyframe(1, 1));
    
    private Texture2D _fadeTexture;
    
    // Variabili interne
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

        // Genera la texture per l'anteprima
        UpdateTexture();
    }

    void Start()
    {
        if (Application.isPlaying && GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated += RefreshLights;
    }

    void OnValidate()
    {
        UpdateTexture();
    }

    void Update()
    {
        if (Application.isPlaying && sunLight != null && _psRenderer != null)
            _psRenderer.material.SetVector(SunDirID, -sunLight.forward);

        if (Application.isPlaying) AnimateParticlesColor();
    }

    // --- ANTEPRIMA IN MEMORIA (RGB FIX) ---
    private void UpdateTexture()
    {
        if (targetMaterial == null) return;

        if (_fadeTexture == null)
        {
            _fadeTexture = new Texture2D(1, 128, TextureFormat.ARGB32, false);
            _fadeTexture.wrapMode = TextureWrapMode.Clamp;
            _fadeTexture.filterMode = FilterMode.Bilinear;
        }

        for (int y = 0; y < 128; y++)
        {
            float t = (float)y / 127f;
            float val = fadeCurve.Evaluate(t);
            // Additive usa il colore RGB, non l'Alpha.
            // Dipingiamo da Nero (invisibile) a Bianco (visibile).
            _fadeTexture.SetPixel(0, y, new Color(val, val, val, 1f));
        }
        _fadeTexture.Apply();

        targetMaterial.mainTexture = _fadeTexture;
    }

    // --- GENERATORE FILE SU DISCO (RGB FIX) ---
    [ContextMenu("Genera e Salva Texture Fade")]
    public void SaveTextureToFile()
    {
        Texture2D tex = new Texture2D(1, 128, TextureFormat.ARGB32, false);
        // Impostiamo Clamp qui, ma va impostato anche nell'inspector della texture dopo!
        tex.wrapMode = TextureWrapMode.Clamp; 
        
        for (int y = 0; y < 128; y++)
        {
            float t = (float)y / 127f;
            float val = fadeCurve.Evaluate(t);
            // RGB FIX: Scala di grigi invece di Alpha
            tex.SetPixel(0, y, new Color(val, val, val, 1f));
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/ConnectionFade.png";
        File.WriteAllBytes(path, bytes);
        
        Debug.Log("Texture Fade salvata in: " + path);
        
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh(); 
#endif
    }

    // --- LOGICA STANDARD ---
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
            
        if (_fadeTexture != null) DestroyImmediate(_fadeTexture);
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

    private void CreateVisualConnection(Vector3 posA, Vector3 posB, float distance)
    {
        Vector3 midPoint = (posA + posB) / 2f;
        float currentRadius = surfaceRadius + lineHeightOffset;
        midPoint = midPoint.normalized * currentRadius;

        ParticleSystem.EmitParams lineParams = new ParticleSystem.EmitParams();
        lineParams.position = midPoint;
        lineParams.startColor = connectionColor; 
        lineParams.startLifetime = float.MaxValue;

        float lengthScaleFactor = currentRadius / surfaceRadius; 
        float adjustedLength = distance * lengthScaleFactor;

        lineParams.startSize3D = new Vector3(lineThickness, adjustedLength, lineThickness);

        Vector3 direction = (posB - posA).normalized;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        lineParams.rotation3D = rotation.eulerAngles;

        connectionPS.Emit(lineParams, 1);
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