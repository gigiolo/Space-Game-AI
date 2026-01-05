using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

[RequireComponent(typeof(ParticleSystem))]
public class PlanetPopulationVisuals : MonoBehaviour
{
    [Header("CONFIGURAZIONE")]
    public float surfaceRadius = 1.60f; 
    public float baseLightSize = 0.05f;
    public int maxLights = 2000;

    [Header("Algoritmo Colonizzazione")]
    public float clusterSpread = 0.2f; 
    public float newHubChance = 0.05f;

    [Header("Rendering Giorno/Notte")]
    public Transform sunLight; 
    
    private ParticleSystemRenderer _psRenderer;
    private ParticleSystem _ps;
    
    public int SpawnedCount { get; private set; } = 0;
    
    private List<Vector3> _occupiedPositions = new List<Vector3>();
    private static readonly int SunDirID = Shader.PropertyToID("_SunDirection");

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        _psRenderer = _ps.GetComponent<ParticleSystemRenderer>();

        var emission = _ps.emission;
        emission.enabled = false;
        
        var main = _ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = maxLights;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; 
        
        if (!_ps.isPlaying) _ps.Play();
        
        // Pulizia iniziale
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
        {
            GameManager.Instance.OnEconomyUpdated += RefreshLights;
        }
    }

    void Update()
    {
        if (sunLight != null && _psRenderer != null)
        {
            _psRenderer.material.SetVector(SunDirID, -sunLight.forward);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated -= RefreshLights;
    }

    public void LoadEncodedPositions(List<string> savedPositions)
    {
        if (savedPositions == null || savedPositions.Count == 0) return;

        // FIX: Pulizia prima del caricamento per evitare duplicati
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
        {
            list.Add(Vector3ToString(pos));
        }
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
        emitParams.startColor = Color.white; 
        emitParams.startLifetime = float.MaxValue;
        
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        if (parentScale == 0) parentScale = 1f;
        emitParams.startSize = baseLightSize / parentScale;

        for (int i = 0; i < count; i++)
        {
            Vector3 newPos = GetSmartPosition();
            _occupiedPositions.Add(newPos);

            emitParams.position = newPos;
            _ps.Emit(emitParams, 1);
        }
    }

    private Vector3 GetSmartPosition()
    {
        if (_occupiedPositions.Count == 0 || Random.value < newHubChance)
        {
            return Random.onUnitSphere * surfaceRadius;
        }

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