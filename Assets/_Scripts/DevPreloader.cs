using UnityEngine;

public class DevPreloader : MonoBehaviour
{
    [Header("Sistema Unico")]
    [Tooltip("Trascina qui il Prefab 'Core_Systems' che contiene GameManager, UI, ecc.")]
    public GameObject coreSystemsPrefab;

    [Header("Configurazione Test")]
    [Tooltip("L'indice del pianeta che stiamo testando (0=Terra, 1=Marte, ecc)")]
    public int debugPlanetIndex = 1;

    void Awake()
    {
        // 1. Controllo se il GameManager esiste già (tramite il Singleton)
        if (GameManager.Instance == null)
        {
            Debug.Log("<color=yellow>[DevPreloader]</color> Core Systems mancanti! Inizializzo...");

            // Istanzio il Prefab Unico (che contiene GameManager, UI, EventSystem, ecc.)
            if (coreSystemsPrefab) 
            {
                Instantiate(coreSystemsPrefab);
            }
            else
            {
                Debug.LogError("[DevPreloader] Hai dimenticato di assegnare il prefab Core_Systems!");
            }

            // --- CONFIGURAZIONE SPECIFICA PER IL TEST ---
            // Impostiamo l'indice del pianeta corretto per questa scena
            if (PlanetManager.Instance != null)
            {
                PlanetManager.Instance.currentPlanetIndex = debugPlanetIndex;
                
                // Opzionale: Diamo un po' di risorse per non partire da zero durante i test
                // GameManager.Instance.AddEnergy(500); 
            }
        }
        else
        {
            Debug.Log("<color=green>[DevPreloader]</color> Core Systems già presenti (arrivi dalla Scena 1). Non faccio nulla.");
        }
        
        // Il loader ha finito il suo lavoro, si autodistrugge per non sporcare la scena
        Destroy(gameObject);
    }
}