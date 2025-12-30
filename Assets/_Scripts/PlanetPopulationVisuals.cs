using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PlanetPopulationVisuals : MonoBehaviour
{
    [Header("CONFIGURAZIONE")]
    // Raggio in "Spazio Locale". Poiché la sfera di Unity ha raggio 0.5,
    // 0.51 è appena sopra la superficie, INDIPENDENTEMENTE dalla scala del pianeta.
    public float surfaceRadius = 1.60f; 
    
    // Quanto devono essere grandi i punti? (Valore di base piccolo)
    public float baseLightSize = 0.05f;
    
    // Limite massimo di luci per performance
    public int maxLights = 2000;

    private ParticleSystem _ps;
    private int _spawnedCount = 0;

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();

        // CONFIGURAZIONE AUTOMATICA DI SICUREZZA
        // Spegniamo l'emissione automatica così non partono particelle a caso
        var emission = _ps.emission;
        emission.enabled = false;
        
        // Configuriamo il sistema per non cancellare le particelle vecchie
        var main = _ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = maxLights;
        
        // Assicuriamoci che il sistema sia avviato
        if (!_ps.isPlaying) _ps.Play();

        // Iscrizione agli eventi
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEconomyUpdated += RefreshLights;
            // Ritardo per sicurezza caricamento
            Invoke("RefreshLights", 0.1f);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnEconomyUpdated -= RefreshLights;
    }

    // TASTO DESTRO -> "TEST: Add 1 Light" per provare senza comprare
    [ContextMenu("TEST: Add 1 Light")]
    public void TestAddOne()
    {
        SpawnParticles(1);
    }

    public void RefreshLights()
    {
        if (GameManager.Instance == null) return;

        // Quanti ne dobbiamo avere?
        int target = Mathf.Min(GameManager.Instance.EmitterCount, maxLights);

        if (target > _spawnedCount)
        {
            int amount = target - _spawnedCount;
            SpawnParticles(amount);
            _spawnedCount = target;
        }
    }

    private void SpawnParticles(int count)
    {
        // Prepariamo i parametri per sparare
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        
        // COLORE: Bianco (sarà colorato dal Materiale)
        emitParams.startColor = Color.white;
        
        // VITA: Infinita
        emitParams.startLifetime = float.MaxValue;
        
        // DIMENSIONE: 
        // Qui sta il trucco. Se il pianeta è scalato (3,3,3), le particelle vengono ingrandite.
        // Dobbiamo dividerle per la scala del genitore per mantenerle piccole.
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        
        // Protezione contro divisione per zero
        if (parentScale == 0) parentScale = 1f;

        emitParams.startSize = baseLightSize / parentScale;

        for (int i = 0; i < count; i++)
        {
            // Posizione casuale sulla sfera (in coordinate locali)
            emitParams.position = Random.onUnitSphere * surfaceRadius;
            
            // SPARA!
            _ps.Emit(emitParams, 1);
        }
    }
}