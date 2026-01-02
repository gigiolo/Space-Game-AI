using UnityEngine;
using System.IO; 

public static class SaveManager
{
    // Nome del file di salvataggio
    private static string fileName = "savegame.json";

    /// <summary>
    /// Salva i dati passati su disco.
    /// </summary>
    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true); 
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, json);

            // Debug.Log($"Salvataggio riuscito in: {path}"); // Decommenta se vuoi vedere il percorso
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Errore durante il salvataggio: {e.Message}");
        }
    }

    /// <summary>
    /// Carica i dati dal disco.
    /// </summary>
    public static SaveData Load()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Errore durante la lettura del file: {e.Message}");
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Cancella il file di salvataggio.
    /// </summary>
    public static void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("File di salvataggio eliminato.");
        }
    }
}