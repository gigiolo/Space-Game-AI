using UnityEngine;
using UnityEditor;
using System.IO;
using BreakInfinity;
using System.Globalization;

public class ResearchCSVImporter : EditorWindow
{
    // ORA USIAMO UN OGGETTO UNITY, NON UNA STRINGA
    private TextAsset csvFile; 

    [MenuItem("Tools/Import Research CSV")]
    public static void ShowWindow()
    {
        GetWindow<ResearchCSVImporter>("Research Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Importatore Ricerche", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // --- CAMPO DRAG & DROP ---
        // Questo crea il box dove puoi trascinare il file .csv dal Project
        csvFile = (TextAsset)EditorGUILayout.ObjectField("File CSV", csvFile, typeof(TextAsset), false);

        GUILayout.Space(10);

        if (GUILayout.Button("IMPORTA ORA", GUILayout.Height(40)))
        {
            ImportData();
        }
        
        GUILayout.Space(5);
        if (csvFile == null)
        {
            EditorGUILayout.HelpBox("Trascina un file .csv nel campo sopra per iniziare.", MessageType.Info);
        }
    }

    private void ImportData()
    {
        // 1. CONTROLLO FILE ASSEGNATO
        if (csvFile == null)
        {
            Debug.LogError("Nessun file CSV assegnato! Trascinalo nel campo apposito.");
            return;
        }

        // 2. RECUPERO PERCORSO REALE
        string assetPath = AssetDatabase.GetAssetPath(csvFile);
        // Convertiamo il percorso relativo di Unity in percorso assoluto di sistema per File.ReadAllLines
        // (necessario perché TextAsset.text a volte gestisce male i caporiga su OS diversi)
        string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

        if (!File.Exists(absolutePath))
        {
            Debug.LogError($"Errore lettura file in: {absolutePath}");
            return;
        }

        string[] lines = File.ReadAllLines(absolutePath);
        int importedCount = 0;

        // Inizia da i = 1 per saltare l'intestazione
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Usa la virgola per Google Sheets
            string[] data = line.Split(','); 

            // CONTROLLO COLONNE (Serve almeno fino alla colonna M = indice 12)
            if (data.Length < 13) 
            {
                // Se la riga è incompleta ma non vuota, avvisa
                if (data.Length > 1) 
                    Debug.LogWarning($"Riga {i + 1} saltata: Colonne insufficienti ({data.Length}/13).");
                continue;
            }

            try
            {
                ResearchDefinition research = ScriptableObject.CreateInstance<ResearchDefinition>();

                // --- MAPPATURA ---
                research.id = data[0].Trim();
                if (string.IsNullOrEmpty(research.id)) continue;

                research.title = data[1].Trim();
                research.description = data[2].Trim();
                
                // ENUM PARSING
                research.type = (ResearchType)System.Enum.Parse(typeof(ResearchType), data[3].Trim(), true);
                research.target = (ResearchTarget)System.Enum.Parse(typeof(ResearchTarget), data[4].Trim(), true);
                
                // BONUS
                research.bonusValue = double.Parse(data[5].Trim(), CultureInfo.InvariantCulture);

                // --- SALTIAMO COLONNE EXCEL CALCOLATE (6, 7, 8, 9) ---

                // COSTO BASE (BigDouble)
                string costStr = data[10].Trim();
                if (string.IsNullOrEmpty(costStr)) costStr = "0";
                research.baseCost = BigDouble.Parse(costStr);

                // COST FACTOR (Float)
                research.costFactor = float.Parse(data[11].Trim(), CultureInfo.InvariantCulture);

                // MAX LEVEL (Int)
                if (int.TryParse(data[12].Trim(), out int lvl)) research.maxLevel = lvl;
                else research.maxLevel = 0;

                // --- NUOVA COLONNA TIER (Colonna N, Indice 13) ---
                if (data.Length > 13 && int.TryParse(data[13].Trim(), out int t)) research.tier = t;
                else research.tier = 1; // Default di sicurezza

                // DEFAULT AGGIUNTIVI
                research.costType = CurrencyType.Energy;
                research.costCurve = CostCurve.Exponential; 
                research.icon = null; 

                // --- SALVATAGGIO ---
                string folderPath = "Assets/Resources/ResearchData";
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string assetPathSave = $"{folderPath}/{research.id}.asset";
                AssetDatabase.CreateAsset(research, assetPathSave);
                importedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Errore riga {i + 1} ({data[0]}): {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>SUCCESS!</color> Importate {importedCount} ricerche.");
    }
}