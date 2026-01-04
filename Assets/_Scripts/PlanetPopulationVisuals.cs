using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PlanetPopulationVisuals : MonoBehaviour
{
    [Header("CONFIGURAZIONE")]
    public float surfaceRadius = 1.60f; 
    public float baseLightSize = 0.05f;
    public int maxLights = 2000;

    [Header("Rendering Giorno/Notte")]
    [Tooltip("Trascina qui la Directional Light principale (il Sole)")]
    public Transform sunLight; 
    
    // Riferimento al renderer per cambiare le proprietà dello shader
    private ParticleSystemRenderer _psRenderer;
    private ParticleSystem _ps;
    private int _spawnedCount = 0;
    
    // ID della proprietà nello Shader Graph per ottimizzazione
    private static readonly int SunDirID = Shader.PropertyToID("_SunDirection");

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        _psRenderer = _ps.GetComponent<ParticleSystemRenderer>();

        // CONFIGURAZIONE AUTOMATICA DI SICUREZZA
        var emission = _ps.emission;
        emission.enabled = false;
        
        var main = _ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = maxLights;
        // Importante: Simulation Space deve essere LOCAL affinché lo shader funzioni con la logica Object Space
        main.simulationSpace = ParticleSystemSimulationSpace.Local; 
        
        if (!_ps.isPlaying) _ps.Play();

        // Trova il sole in automatico se non assegnato
        if (sunLight == null)
        {
            var light = FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
                sunLight = light.transform;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEconomyUpdated += RefreshLights;
            Invoke("RefreshLights", 0.1f);
        }
    }

    void Update()
    {
        // Aggiorna la direzione del sole nello shader ogni frame
        // Passiamo -forward perché la luce punta VERSO il pianeta, 
        // ma noi vogliamo il vettore che va DAL pianeta AL sole.
        if (sunLight != null && _psRenderer != null)
        {
            // Usiamo materialPropertyBlock per performance se necessario, 
            // ma qui l'accesso diretto a material va bene per un singolo sistema.
            _psRenderer.material.SetVector(SunDirID, -sunLight.forward);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated -= RefreshLights;
    }

    [ContextMenu("TEST: Add 1 Light")]
    public void TestAddOne() => SpawnParticles(1);

    public void RefreshLights()
    {
        if (GameManager.Instance == null) return;
        int target = Mathf.Min(GameManager.Instance.EmitterCount, maxLights);

        if (target > _spawnedCount)
        {
            SpawnParticles(target - _spawnedCount);
            _spawnedCount = target;
        }
    }

    private void SpawnParticles(int count)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        
        // Il colore base è bianco, sarà lo shader a tingerlo
        emitParams.startColor = Color.white; 
        emitParams.startLifetime = float.MaxValue;
        
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        if (parentScale == 0) parentScale = 1f;

        emitParams.startSize = baseLightSize / parentScale;

        for (int i = 0; i < count; i++)
        {
            emitParams.position = Random.onUnitSphere * surfaceRadius;
            _ps.Emit(emitParams, 1);
        }
    }
}