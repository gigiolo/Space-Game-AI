using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BreakInfinity;

public class AetherisBalancer : EditorWindow
{
    [MenuItem("Aetheris/Aetheris Balancer")]
    public static void ShowWindow()
    {
        GetWindow<AetherisBalancer>("Aetheris Balancer");
    }

    private List<PlanetData> _planets = new List<PlanetData>();
    private List<ResearchDefinition> _researches = new List<ResearchDefinition>();

    // Target times in minutes to reach the NEXT planet
    private float[] _targetMinutes = new float[] { 30, 90, 270, 810, 2430 };

    private const double TIER_JUMP_FACTOR = 9.5;
    private const double MULTIPLIER_PERCENTAGE = 0.15;

    private Vector2 _scrollPos;
    private string _statusMessage = "Ready";

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        _planets = AssetDatabase.FindAssets("t:PlanetData")
            .Select(guid => AssetDatabase.LoadAssetAtPath<PlanetData>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(p => ExtractNumber(p.name))
            .ToList();

        _researches = AssetDatabase.FindAssets("t:ResearchDefinition")
            .Select(guid => AssetDatabase.LoadAssetAtPath<ResearchDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
            .ToList();
    }

    private int ExtractNumber(string name)
    {
        string digits = new string(name.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int result) ? result : 0;
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        GUILayout.Label("Aetheris Balancer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Reload Data")) LoadData();

        EditorGUILayout.Space();
        GUILayout.Label("Target Times (Minutes to reach next planet)", EditorStyles.boldLabel);
        for (int i = 0; i < _targetMinutes.Length; i++)
        {
            _targetMinutes[i] = EditorGUILayout.FloatField($"Planet {i + 1} -> {i + 2}:", _targetMinutes[i]);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Run Full Auto-Balance", GUILayout.Height(40)))
        {
            RunBalance();
        }

        if (GUILayout.Button("Export to CSV"))
        {
            ExportToCSV();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Simulation Status", EditorStyles.boldLabel);
        GUILayout.Label(_statusMessage);

        EditorGUILayout.EndScrollView();
    }

    private void RunBalance()
    {
        _statusMessage = "Balancing...";

        try {
            PerformMathematicalBalancing();
            AssetDatabase.SaveAssets();
            _statusMessage = "Balance Complete! Check Console for simulation logs.";
        } catch (Exception e) {
            _statusMessage = "Error: " + e.Message;
            Debug.LogError(e);
        }
    }

    private void PerformMathematicalBalancing()
    {
        // 1. Setup Initial Environment
        BigDouble currentIncome = 0.1;
        BigDouble cumulativeEarnings = 0;
        BigDouble lastTierMaxCost = 0.05;

        var researchesByTier = _researches.GroupBy(r => r.tier).OrderBy(g => g.Key).ToList();

        Debug.Log("<color=cyan>[Aetheris Balancer] Starting Deterministic Simulation</color>");

        for (int i = 0; i < _planets.Count; i++)
        {
            PlanetData planet = _planets[i];
            int tierLevel = i + 1;
            var tierResearches = researchesByTier.FirstOrDefault(g => g.Key == tierLevel)?.ToList() ?? new List<ResearchDefinition>();

            double targetSeconds = ((i < _targetMinutes.Length) ? _targetMinutes[i] : _targetMinutes[_targetMinutes.Length - 1] * 3) * 60;

            // Deterministic growth factor for this planet phase
            double growthFactor = 15.0;
            double k = Math.Log(growthFactor) / targetSeconds;

            // Phase budget calculation (Integral of P0 * e^kt)
            BigDouble phaseEnergy = (currentIncome / k) * (Math.Exp(k * targetSeconds) - 1);
            cumulativeEarnings += phaseEnergy;

            // Balance Researches for this Tier
            BalanceTier(tierResearches, ref currentIncome, ref lastTierMaxCost, growthFactor, phaseEnergy);

            // Set Planet Data
            if (i < _planets.Count - 1)
            {
                int estimatedEmitters = 10 + (i * 20);
                planet.requiredPlanetValue = currentIncome * estimatedEmitters;
                EditorUtility.SetDirty(planet);

                if (i + 1 < _planets.Count) {
                    _planets[i+1].productionMultiplier = planet.productionMultiplier * 4;
                    EditorUtility.SetDirty(_planets[i+1]);
                }
            }

            Debug.Log($"<b>Planet {i+1}</b> | Time: {targetSeconds/60}m | Phase Earnings: {phaseEnergy.ToString()} | Cumulative: {cumulativeEarnings.ToString()} | Income: {currentIncome.ToString()}/s");
        }
    }

    private void BalanceTier(List<ResearchDefinition> researches, ref BigDouble currentIncome, ref BigDouble lastTierMaxCost, double growthFactor, BigDouble energyBudget)
    {
        if (researches.Count == 0) return;

        var prodRes = researches.Where(r => r.target == ResearchTarget.GlobalProduction || r.target == ResearchTarget.EmitterMaxCap).ToList();
        var logRes = researches.Where(r => r.target == ResearchTarget.LogisticsCapacity).ToList();

        double multiplierGrowth = Math.Pow(growthFactor, MULTIPLIER_PERCENTAGE);
        double additiveGrowth = Math.Pow(growthFactor, 1.0 - MULTIPLIER_PERCENTAGE);

        BigDouble tierStartCost = lastTierMaxCost * TIER_JUMP_FACTOR;

        // --- PRODUCTION ---
        foreach (var res in prodRes.OrderBy(r => r.type == ResearchType.Multiplier))
        {
            res.baseCost = tierStartCost;
            res.costCurve = CostCurve.Exponential;
            res.costFactor = 1.15f + (res.tier * 0.01f);
            res.maxLevel = (res.type == ResearchType.Additive) ? 40 : 15;

            if (res.type == ResearchType.Additive) {
                double bonusPerLevel = (additiveGrowth / Math.Max(1, prodRes.Count(r => r.target == ResearchTarget.GlobalProduction && r.type == ResearchType.Additive))) / res.maxLevel;
                res.bonusValue = Math.Max(0.1, bonusPerLevel);
                res.isExponentialBonus = false;
            } else {
                double totalMult = Math.Pow(multiplierGrowth, 1.0 / Math.Max(1, prodRes.Count(r => r.type == ResearchType.Multiplier)));
                res.bonusValue = Math.Pow(totalMult, 1.0 / res.maxLevel);
                res.isExponentialBonus = true;
            }

            lastTierMaxCost = BigDouble.Max(lastTierMaxCost, res.GetCost(res.maxLevel));
            EditorUtility.SetDirty(res);
        }

        currentIncome *= growthFactor;

        // --- LOGISTICS ---
        foreach (var res in logRes)
        {
            res.baseCost = tierStartCost * 0.9;
            res.costCurve = CostCurve.Exponential;
            res.costFactor = 1.12f;
            res.maxLevel = 50;
            res.bonusValue = (currentIncome.ToDouble() * 1.1) / res.maxLevel;
            EditorUtility.SetDirty(res);
        }
    }

    private void ExportToCSV()
    {
        string path = EditorUtility.SaveFilePanel("Save Research Data CSV", "", "ResearchBalance.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("ID,Title,Description,Type,Target,BonusValue,IsExponential,H,I,J,BaseCost,CostFactor,MaxLevel,Tier");
            foreach (var res in _researches.OrderBy(r => r.tier).ThenBy(r => r.id))
            {
                writer.WriteLine($"{res.id},{res.title},{res.description},{res.type},{res.target},{res.bonusValue.ToString(System.Globalization.CultureInfo.InvariantCulture)},{res.isExponentialBonus},,,,{res.baseCost.ToString()},{res.costFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)},{res.maxLevel},{res.tier}");
            }
        }
        _statusMessage = "CSV Exported to " + path;
    }
}
