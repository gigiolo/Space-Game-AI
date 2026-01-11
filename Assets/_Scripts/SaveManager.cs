using UnityEngine;
using System.IO; 

public static class SaveManager
{
    private static string fileName = "savegame.json";

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true); 
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Errore durante il salvataggio: {e.Message}");
        }
    }

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

    public static void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                Debug.Log("File di salvataggio eliminato con successo.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Impossibile eliminare il salvataggio: {e.Message}");
            }
        }
        else
        {
            Debug.Log("Nessun file di salvataggio trovato da eliminare.");
        }
    }
}