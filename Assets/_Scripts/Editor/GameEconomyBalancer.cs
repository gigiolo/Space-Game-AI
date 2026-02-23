using UnityEngine;
using BreakInfinity;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode] // Funziona anche senza premere Play!
public class GameEconomyBalancer : MonoBehaviour
{
    [Header("--- MASTER SCALAR ---")]
    [Tooltip("1.0 = Nessun cambiamento.\n> 1.0 = Gioco più lento (Costi più alti).\n< 1.0 = Gioco più veloce (Costi più bassi).")]
    public float globalDifficultyMultiplier = 1.0f;

    [Header("--- TARGETS ---")]
    public GameManager gameManager;
    [Tooltip("La lista di tutte le ricerche da bilanciare (ScriptableObjects).")]
    public List<ResearchDefinition> allResearches;
    [Tooltip("La lista di tutte le navi da bilanciare.")]
    public List<SpaceshipDefinition> allSpaceships;

    [Header("--- DEBUG COMMANDS ---")]
    public BigDouble debugEnergyToAdd = 1000;

    // --- FUNZIONI DI DEBUG RAPIDO (Funzionano in Play Mode) ---
    
    public void AddDebugEnergy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddEnergy(debugEnergyToAdd);
            Debug.Log($"[Balancer] Aggiunta {debugEnergyToAdd} Energia.");
        }
    }

    public void ResetSave()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PerformFullHardReset();
        }
    }

    // --- FUNZIONI DI BILANCIAMENTO (Funzionano in Edit Mode) ---

    public void ApplyCostMultiplier()
    {
        if (Mathf.Approximately(globalDifficultyMultiplier, 1.0f))
        {
            Debug.LogWarning("[Balancer] Moltiplicatore a 1.0. Nessuna modifica applicata.");
            return;
        }

        int count = 0;

        // 1. Bilancia RICERCHE
        foreach (var res in allResearches)
        {
            if (res == null) continue;
            
            // Moltiplica il costo base
            res.baseCost *= globalDifficultyMultiplier;
            
            // Moltiplica anche i costi manuali se presenti
            for (int i = 0; i < res.manualCosts.Count; i++)
            {
                res.manualCosts[i] *= globalDifficultyMultiplier;
            }
            
            EditorUtility.SetDirty(res); // Segna che il file è cambiato per salvarlo su disco
            count++;
        }

        // 2. Bilancia NAVI
        foreach (var ship in allSpaceships)
        {
            if (ship == null) continue;
            ship.baseCost *= globalDifficultyMultiplier;
            for (int i = 0; i < ship.manualCosts.Count; i++)
            {
                ship.manualCosts[i] *= globalDifficultyMultiplier;
            }
            EditorUtility.SetDirty(ship);
            count++;
        }

        Debug.Log($"[Balancer] Applicato moltiplicatore {globalDifficultyMultiplier}x a {count} elementi.");
        
        // Resetta lo slider a 1 per sicurezza
        globalDifficultyMultiplier = 1.0f;
    }

    public void ApplyProductionMultiplier()
    {
        // Qui invertiamo la logica: se il gioco è più difficile (Multiplier > 1), la produzione deve SCENDERE.
        // Quindi dividiamo per il moltiplicatore.
        if (gameManager != null)
        {
            gameManager.baseEmissionPerUnit /= globalDifficultyMultiplier;
            Debug.Log($"[Balancer] Nuova Emissione Base: {gameManager.baseEmissionPerUnit}");
            EditorUtility.SetDirty(gameManager);
        }
    }

    // --- PULIZIA DATI ---
    // Cerca automaticamente tutti gli asset nel progetto se la lista è vuota
    [ContextMenu("Find All Definitions")]
    public void FindAllDefinitions()
    {
#if UNITY_EDITOR
        allResearches.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ResearchDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            allResearches.Add(AssetDatabase.LoadAssetAtPath<ResearchDefinition>(path));
        }

        allSpaceships.Clear();
        string[] shipGuids = AssetDatabase.FindAssets("t:SpaceshipDefinition");
        foreach (string guid in shipGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            allSpaceships.Add(AssetDatabase.LoadAssetAtPath<SpaceshipDefinition>(path));
        }
        
        Debug.Log($"[Balancer] Trovate {allResearches.Count} Ricerche e {allSpaceships.Count} Navi.");
#endif
    }
}

// --- CUSTOM EDITOR (Per mostrare i bottoni nell'Inspector) ---
#if UNITY_EDITOR
[CustomEditor(typeof(GameEconomyBalancer))]
public class GameEconomyBalancerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameEconomyBalancer script = (GameEconomyBalancer)target;

        GUILayout.Space(20);
        GUILayout.Label("--- TOOLS DI BILANCIAMENTO ---", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Trova Tutti gli Asset (Ricerche/Navi)"))
        {
            script.FindAllDefinitions();
        }

        GUILayout.Space(10);
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button($"Applica MOLTIPLICATORE COSTI ({script.globalDifficultyMultiplier}x)"))
        {
            if (EditorUtility.DisplayDialog("Conferma Bilanciamento", 
                $"Stai per moltiplicare il costo base di TUTTE le ricerche e navi per {script.globalDifficultyMultiplier}.\nQuesta operazione modifica i file su disco permanentemente.\n\nSei sicuro?", "Sì, Procedi", "Annulla"))
            {
                script.ApplyCostMultiplier();
                AssetDatabase.SaveAssets();
            }
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button($"Applica a PRODUZIONE BASE (1/{script.globalDifficultyMultiplier}x)"))
        {
             script.ApplyProductionMultiplier();
        }

        GUILayout.Space(20);
        GUILayout.Label("--- RUNTIME CHEATS (Solo in Play) ---", EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Add Energy (+Debug Amount)"))
            {
                script.AddDebugEnergy();
            }
            
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("HARD RESET (Cancella tutto)"))
            {
                script.ResetSave();
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            // CORRETTO QUI: Usiamo EditorGUILayout invece di GUILayout
            EditorGUILayout.HelpBox("Entra in Play Mode per usare i Cheat.", MessageType.Info);
        }
    }
}
#endif