using UnityEngine;
using System.IO;

public class SaveDebugger : MonoBehaviour
{
    private void Start()
    {
        string path = Path.Combine(Application.persistentDataPath, "savegame.json");
        
        Debug.Log($"[SAVE DEBUG] Percorso salvataggio: {path}");

        if (File.Exists(path))
        {
            string content = File.ReadAllText(path);
            Debug.Log($"[SAVE DEBUG] File Trovato! Dimensione: {content.Length} bytes.");
            Debug.Log($"[SAVE DEBUG] Contenuto parziale: {content.Substring(0, Mathf.Min(content.Length, 200))}...");
            
            // Prova a deserializzare per vedere se i dati sono validi
            try 
            {
                SaveData data = JsonUtility.FromJson<SaveData>(content);
                Debug.Log($"[SAVE DEBUG] Dati Letti Correttamente:");
                Debug.Log($"   - Energy: {data.currentEnergy}");
                Debug.Log($"   - LastTime: {data.lastSaveTime}");
                Debug.Log($"   - IsFirstSession: {data.isFirstSession}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SAVE DEBUG] ERRORE CRITICO: Il file esiste ma è corrotto! {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[SAVE DEBUG] Nessun file di salvataggio trovato (normale se è la prima volta assoluta).");
        }
    }

    // Tasto destro sul componente in Inspector per eseguire questo test manualmente
    [ContextMenu("Test Scrittura")]
    public void TestWrite()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            Debug.Log("[SAVE DEBUG] Comando di salvataggio inviato al GameManager.");
        }
        else
        {
            Debug.LogError("[SAVE DEBUG] Impossibile salvare: GameManager assente.");
        }
    }
}