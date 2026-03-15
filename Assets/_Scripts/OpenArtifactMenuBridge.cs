using UnityEngine;

public class OpenArtifactMenuBridge : MonoBehaviour
{
    public void TriggerArtifactMenu()
    {
        // Cerca il menù includendo anche gli oggetti disattivati
        ArtifactsMenuUI artifactsMenu = FindFirstObjectByType<ArtifactsMenuUI>(FindObjectsInactive.Include);

        if (artifactsMenu != null)
        {
            artifactsMenu.ToggleMenu();
        }
        else
        {
            Debug.LogError("[Bridge] ERRORE: Impossibile trovare ArtifactsMenuUI. Assicurati che lo script sia presente nel Core_Systems.");
        }
    }
}