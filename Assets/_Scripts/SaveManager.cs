using UnityEngine;
using System.IO;
using System;
using System.Text;

public static class SaveManager
{
    private static string FileName = "savegame.dat";
    
    // Percorso: %AppData%/LocalLow/TuoNomeCompagnia/TuoGioco/savegame.dat
    private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            
            // Opzionale: Offuscamento semplice (Base64)
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            
            File.WriteAllText(Path, encoded);
            Debug.Log($"<color=green>GIOCO SALVATO</color> in: {Path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore salvataggio: {e.Message}");
        }
    }

    public static SaveData Load()
    {
        if (!File.Exists(Path))
        {
            Debug.Log("Nessun salvataggio trovato. Creazione nuova partita.");
            return null;
        }

        try
        {
            string encoded = File.ReadAllText(Path);
            
            // Decodifica Base64
            byte[] bytes = Convert.FromBase64String(encoded);
            string json = Encoding.UTF8.GetString(bytes);
            
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore caricamento (File corrotto?): {e.Message}");
            return null; // Ritorna null per forzare una nuova partita
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(Path)) File.Delete(Path);
        PlayerPrefs.DeleteAll();
    }
}