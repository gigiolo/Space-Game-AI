using UnityEngine;
using UnityEditor;
using BreakInfinity;
using System.Collections.Generic;
using System.Text;

public class GameSimulationTool : EditorWindow
{
    private float simulationStepSeconds = 60f;
    private bool autoBuyUpgrades = true;

    // --- DATI REPORT ---
    private double _totalSimulatedSeconds = 0;
    private List<string> _reportLog = new List<string>();
    private Vector2 _scrollPos;

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

        // --- HEADER STATS ---
        GUILayout.Label("STATISTICHE ATTUALI", EditorStyles.boldLabel);
        try
        {
            EditorGUILayout.LabelField("Tempo Simulato:", FormatTime(_totalSimulatedSeconds));
            EditorGUILayout.LabelField("Energy:", FormatNumber(GameManager.Instance.CurrentEnergy));
            EditorGUILayout.LabelField("Income (Reale):", FormatNumber(GameManager.Instance.EffectiveIncomePerSec));
            EditorGUILayout.LabelField("Prod. Lorda (No Cap):", FormatNumber(GameManager.Instance.RawProductionRate));
            EditorGUILayout.LabelField("Logistics Cap:", FormatNumber(GameManager.Instance.LogisticsCap));
            EditorGUILayout.LabelField("Emitters:", $"{GameManager.Instance.EmitterCount} / {GameManager.Instance.EmitterCap}");
            EditorGUILayout.LabelField("Nodi Potenziali:", FormatNumber(GameManager.Instance.CalculatePotentialNodes()));
        }
        catch { }

        GUILayout.Space(10);
        autoBuyUpgrades = EditorGUILayout.Toggle("Auto-Buy Upgrades", autoBuyUpgrades);
        
        // --- CONTROLLI SIMULAZIONE ---
        GUILayout.Space(10);
        GUILayout.Label("SIMULAZIONE", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 1 Min", GUILayout.Height(30))) SimulateTime(60); 
        if (GUILayout.Button("+ 10 Min", GUILayout.Height(30))) SimulateTime(600); 
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 1 Ora", GUILayout.Height(30))) SimulateTime(3600);
        if (GUILayout.Button("+ 6 Ore", GUILayout.Height(30))) SimulateTime(21600);
        if (GUILayout.Button("+ 24 Ore", GUILayout.Height(30))) SimulateTime(86400);
        GUILayout.EndHorizontal();

        GUILayout.Space(15);

        // --- GESTIONE REPORT ---
        GUILayout.Label($"REPORT ({_reportLog.Count} righe)", EditorStyles.boldLabel);
        
        if (GUILayout.Button("COPIA CSV (Per Excel)", GUILayout.Height(40)))
        {
            CopyToClipboard();
        }

        if (GUILayout.Button("Reset Dati Simulazione"))
        {
            _totalSimulatedSeconds = 0;
            _reportLog.Clear();
            Debug.Log("Dati simulazione resettati.");
        }

        // Preview del log (ultime 5 righe) - MODIFICATO PER EVITARE CRASH
        GUILayout.Label("Anteprima Log (Ultime righe):", EditorStyles.miniLabel);
        
        string preview = "";
        int start = Mathf.Max(0, _reportLog.Count - 5);
        for (int i = start; i < _reportLog.Count; i++) preview += _reportLog[i] + "\n";
        
        // Usa HelpBox invece di TextArea per evitare allocazioni massicce di memoria modificabile
        EditorGUILayout.HelpBox(preview, MessageType.None);
    }

    private void SimulateTime(float totalSeconds)
    {
        float step = totalSeconds <= 60 ? 1f : simulationStepSeconds; 
        int steps = Mathf.CeilToInt(totalSeconds / step);
        
        int startUpgrades = CountTotalUpgrades();
        
        // Accumulatore locale per la crescita emitter
        double simEmitterAccumulator = 0;

        for (int i = 0; i < steps; i++)
        {
            // 1. Produzione & Logistica
            BigDouble income = GameManager.Instance.EffectiveIncomePerSec * step;
            GameManager.Instance.AddEnergy(income);

            // 2. Crescita Emitter
            if (GameManager.Instance.EmitterCount < GameManager.Instance.EmitterCap)
            {
                double growth = GameManager.Instance.EmitterAutoGrowthSpeed * step;
                simEmitterAccumulator += growth;
                if (simEmitterAccumulator >= 1.0)
                {
                    int toSpawn = (int)simEmitterAccumulator;
                    int space = GameManager.Instance.EmitterCap - GameManager.Instance.EmitterCount;
                    int actual = Mathf.Min(toSpawn, space);
                    if (actual > 0) GameManager.Instance.AddInstantEmitters(actual);
                    simEmitterAccumulator -= toSpawn;
                }
            }

            // 3. Auto-Buy
            if (autoBuyUpgrades) BuyUpgrades();
        }

        // Aggiorna tempo totale
        _totalSimulatedSeconds += totalSeconds;
        GameManager.Instance.ForceUIUpdate();

        // REGISTRAZIONE DATI NEL REPORT
        RecordDataLog(totalSeconds, CountTotalUpgrades() - startUpgrades);
    }

    private int CountTotalUpgrades()
    {
        int total = 0;
        if (ResearchManager.Instance == null) return 0;
        foreach (var r in ResearchManager.Instance.allResearches) total += r.currentLevel;
        return total;
    }

    private int BuyUpgrades()
    {
        int bought = 0;
        if (ResearchManager.Instance == null) return 0;

        foreach (var item in ResearchManager.Instance.allResearches)
        {
            if (item.IsMaxed()) continue;
            if (GameManager.Instance.CurrentEnergy >= item.GetCost())
            {
                GameManager.Instance.TrySpend(item.GetCost());
                item.currentLevel++;
                bought++;
                ResearchManager.Instance.RecalculateAllResearches();
            }
        }
        return bought;
    }

    // --- SISTEMA DI LOGGING CORRETTO ---
    private void RecordDataLog(float secondsSkipped, int upgradesBought)
    {
        if (_reportLog.Count == 0)
        {
            // Header del CSV
            _reportLog.Add("Time_Total_Sec;Time_Formatted;Energy;Income_Net;Income_Raw;Logistics_Cap;Emitters;Upgrades_Bought_In_Step;Potential_Nodes");
        }

        var gm = GameManager.Instance;
        
        // FIX: Usiamo FormatNumber (scientifico) invece di ToString("F0") per evitare stringhe giganti
        string line = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
            _totalSimulatedSeconds,
            FormatTime(_totalSimulatedSeconds),
            FormatNumber(gm.CurrentEnergy),      
            FormatNumber(gm.EffectiveIncomePerSec),
            FormatNumber(gm.RawProductionRate),
            FormatNumber(gm.LogisticsCap),
            gm.EmitterCount,
            upgradesBought,
            FormatNumber(gm.CalculatePotentialNodes())
        );

        _reportLog.Add(line);
    }

    private void CopyToClipboard()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var line in _reportLog) sb.AppendLine(line);
        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Report copiato negli appunti! Incollalo in Excel/Sheets.");
    }

    // --- FORMATTAZIONE VISUALE ---
    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F2");
        // Usa la notazione scientifica (es. 1.25e10) che è sicura per la memoria e leggibile da Excel
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }

    private string FormatTime(double totalSeconds)
    {
        int days = (int)(totalSeconds / 86400);
        int hours = (int)((totalSeconds % 86400) / 3600);
        int mins = (int)((totalSeconds % 3600) / 60);
        return $"{days}d {hours}h {mins}m";
    }
}