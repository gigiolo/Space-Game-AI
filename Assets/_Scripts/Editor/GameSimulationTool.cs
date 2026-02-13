using UnityEngine;
using UnityEditor;
using BreakInfinity;
using System.Collections.Generic;

public class GameSimulationTool : EditorWindow
{
    private float simulationStepSeconds = 60f;
    private bool autoBuyUpgrades = true;

    [MenuItem("Tools/Space Inc/Game Simulator")] 
    public static void ShowWindow()
    {
        GetWindow<GameSimulationTool>("Game Simulator");
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Premi PLAY per usare questo tool!", MessageType.Info);
            return;
        }

        if (GameManager.Instance == null || ResearchManager.Instance == null)
        {
            GUILayout.Label("In attesa del GameManager...");
            return;
        }

        GUILayout.Label("SIMULATORE TEMPO", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Usiamo un try-catch nella GUI per evitare che un errore di formattazione blocchi tutto l'editor
        try
        {
            EditorGUILayout.LabelField("Energy Attuale:", FormatNumber(GameManager.Instance.CurrentEnergy));
            EditorGUILayout.LabelField("Prod/Sec:", FormatNumber(GameManager.Instance.EffectiveIncomePerSec));
        }
        catch 
        {
            EditorGUILayout.LabelField("Energy Attuale:", "Error calculating...");
        }

        GUILayout.Space(10);
        autoBuyUpgrades = EditorGUILayout.Toggle("Compra Auto-Upgrade", autoBuyUpgrades);
        
        GUILayout.Space(10);

        if (GUILayout.Button("Simula 1 Ora (Veloce)", GUILayout.Height(30)))
        {
            SimulateTime(3600);
        }
        
        if (GUILayout.Button("Simula 24 Ore (Giorno)", GUILayout.Height(40)))
        {
            SimulateTime(86400);
        }
    }

    private void SimulateTime(float totalSeconds)
    {
        int steps = Mathf.CeilToInt(totalSeconds / simulationStepSeconds);
        int bought = 0;

        for (int i = 0; i < steps; i++)
        {
            // Guadagno
            BigDouble income = GameManager.Instance.EffectiveIncomePerSec * simulationStepSeconds;
            GameManager.Instance.AddEnergy(income);

            // Acquisto
            if (autoBuyUpgrades)
            {
                bought += BuyUpgrades();
            }
        }
        
        GameManager.Instance.ForceUIUpdate();
        Debug.Log($"Simulazione completata: {totalSeconds/3600:F1} ore simulate. Upgrade comprati: {bought}");
    }

    private int BuyUpgrades()
    {
        int count = 0;
        if (ResearchManager.Instance == null) return 0;

        foreach (var item in ResearchManager.Instance.allResearches)
        {
            if (item.IsMaxed()) continue;
            BigDouble cost = item.GetCost();
            
            if (GameManager.Instance.CurrentEnergy >= cost)
            {
                GameManager.Instance.TrySpend(cost);
                item.currentLevel++;
                count++;
                ResearchManager.Instance.RecalculateAllResearches();
            }
        }
        return count;
    }

    // --- CORREZIONE QUI ---
    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) 
        {
            // Numeri piccoli: Formattazione standard (es. 150.00)
            return number.ToString("F2");
        }
        
        // Numeri grandi: Costruzione manuale (es. 1.52e12)
        // Questo evita l'errore "Unknown string format 'e'" costruendo la stringa a mano
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}