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
    private GameManager _gameManager;

    private float[] _targetMinutes = new float[] { 30, 90, 270, 810, 2430 };

    private const double TIER_JUMP_FACTOR = 9.5;
    private const double MULTIPLIER_PERCENTAGE = 0.15;
    private const double BUFFER_FACTOR = 0.5; // Adjusted down to 50% to be very conservative for playability

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

        _gameManager = FindFirstObjectByType<GameManager>();
        if (_gameManager == null)
        {
            // Try to find the prefab if not in scene
            string[] gmGuids = AssetDatabase.FindAssets("GameManager t:GameObject");
            foreach (var guid in gmGuids)
            {
                GameObject gmObj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (gmObj != null && gmObj.GetComponent<GameManager>() != null)
                {
                    _gameManager = gmObj.GetComponent<GameManager>();
                    break;
                }
            }
        }
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
        _gameManager = (GameManager)EditorGUILayout.ObjectField("Game Manager Reference", _gameManager, typeof(GameManager), true);
        if (_gameManager == null)
        {
            EditorGUILayout.HelpBox("Please assign a GameManager (from Scene or Prefab) to sync base values!", MessageType.Warning);
        }

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
        if (GUILayout.Button("Create Basic Research Structure (T1-T6)"))
        {
            CreateBasicResearches();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Simulation Status", EditorStyles.boldLabel);
        GUILayout.Label(_statusMessage);

        EditorGUILayout.EndScrollView();
    }

    private void RunBalance()
    {
        _statusMessage = "Balancing...";
        LoadData();

        try {
            PerformMathematicalBalancing();
            AssetDatabase.SaveAssets();
            _statusMessage = "Balance Complete!";
        } catch (Exception e) {
            _statusMessage = "Error: " + e.Message;
            Debug.LogError(e);
        }
    }

    private void PerformMathematicalBalancing()
    {
        // SYNC BASE VALUES FROM GAMEMANAGER
        BigDouble currentBaseIncomePerEmitter = (_gameManager != null) ? _gameManager.baseEmissionPerUnit : 0.01;
        int currentEmitters = 1;
        BigDouble currentIncomePerSec = currentBaseIncomePerEmitter * currentEmitters;
        BigDouble lastTierMaxCost = 0.01;
        BigDouble cumulativeEarnings = 0;

        var researchesByTier = _researches.GroupBy(r => r.tier).OrderBy(g => g.Key).ToList();

        for (int i = 0; i < _planets.Count; i++)
        {
            PlanetData planet = _planets[i];
            int tierLevel = i + 1;
            var tierResearches = researchesByTier.FirstOrDefault(g => g.Key == tierLevel)?.ToList() ?? new List<ResearchDefinition>();

            BigDouble planetAdjustedIncome = currentIncomePerSec * planet.productionMultiplier;

            double targetSeconds = ((i < _targetMinutes.Length) ? _targetMinutes[i] : _targetMinutes[_targetMinutes.Length - 1] * 3) * 60;

            // Growth factor: T1 needs a big boost to overcome the initial hurdle
            double growthFactor = (i == 0) ? 25.0 : 15.0;
            double k = Math.Log(growthFactor) / targetSeconds;

            BigDouble phaseEnergyBudget = (planetAdjustedIncome / k) * (Math.Exp(k * targetSeconds) - 1) * BUFFER_FACTOR;
            cumulativeEarnings += phaseEnergyBudget;

            BalanceTier(tierResearches, ref currentIncomePerSec, ref lastTierMaxCost, growthFactor, phaseEnergyBudget);

            if (i < _planets.Count - 1)
            {
                int maxEmitters = 10 + (i * 25);
                planet.requiredPlanetValue = (currentIncomePerSec * planet.productionMultiplier) * maxEmitters;
                EditorUtility.SetDirty(planet);

                if (i + 1 < _planets.Count) {
                    _planets[i+1].productionMultiplier = planet.productionMultiplier * 5;
                    EditorUtility.SetDirty(_planets[i+1]);
                }
            }

            // PERSISTENT PRESTIGE DIVISOR UPDATE
            if (i == 2 && _gameManager != null) {
                Undo.RecordObject(_gameManager, "Update Prestige Divisor");
                _gameManager.prestigeDivisor = cumulativeEarnings.ToDouble();
                EditorUtility.SetDirty(_gameManager);
            }

            Debug.Log($"Planet {i+1} balanced. Time: {targetSeconds/60}m. Final Income (Base): {currentIncomePerSec.ToString()}/s");
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

        BigDouble prodBudgetTotal = energyBudget * 0.6;
        BigDouble logBudgetTotal = energyBudget * 0.3;

        BigDouble researchBudget = prodBudgetTotal / Math.Max(1, prodRes.Count);
        foreach (var res in prodRes.OrderBy(r => r.type == ResearchType.Multiplier))
        {
            res.baseCost = tierStartCost;
            res.costCurve = CostCurve.Exponential;
            res.maxLevel = (res.type == ResearchType.Additive) ? 40 : 15;
            res.costFactor = CalculateAffordableFactor(res.baseCost, res.maxLevel, researchBudget);

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

        BigDouble logResearchBudget = logBudgetTotal / Math.Max(1, logRes.Count);
        foreach (var res in logRes)
        {
            res.baseCost = tierStartCost * 0.8;
            res.costCurve = CostCurve.Exponential;
            res.maxLevel = 50;
            res.costFactor = CalculateAffordableFactor(res.baseCost, res.maxLevel, logResearchBudget);
            res.bonusValue = (currentIncome.ToDouble() * 1.1) / res.maxLevel;
            EditorUtility.SetDirty(res);
        }
    }

    private float CalculateAffordableFactor(BigDouble baseCost, int levels, BigDouble budget)
    {
        float r = 1.01f;
        while (r < 2.5f)
        {
            BigDouble total = baseCost * (Math.Pow(r, levels + 1) - 1) / (r - 1);
            if (total > budget) break;
            r += 0.01f;
        }
        return Math.Max(1.05f, r - 0.01f);
    }

    private void CreateBasicResearches()
    {
        string folderPath = "Assets/Resources/ResearchData";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        for (int t = 1; t <= 6; t++)
        {
            CreateRes(t, "Prod", ResearchTarget.GlobalProduction, ResearchType.Additive);
            CreateRes(t, "Mult", ResearchTarget.GlobalProduction, ResearchType.Multiplier);
            CreateRes(t, "Log", ResearchTarget.LogisticsCapacity, ResearchType.Additive);
        }
        AssetDatabase.Refresh();
        LoadData();
        _statusMessage = "Basic Research structure created!";
    }

    private void CreateRes(int tier, string suffix, ResearchTarget target, ResearchType type)
    {
        string id = $"T{tier}_{suffix}";
        string path = $"Assets/Resources/ResearchData/{id}.asset";
        if (File.Exists(path)) return;

        ResearchDefinition res = ScriptableObject.CreateInstance<ResearchDefinition>();
        res.id = id;
        res.title = $"{target} T{tier}";
        res.tier = tier;
        res.target = target;
        res.type = type;
        res.costType = CurrencyType.Energy;
        res.costCurve = CostCurve.Exponential;

        AssetDatabase.CreateAsset(res, path);
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
