// --- File: _Scripts\ArtifactSlotUI.cs ---
using UnityEngine;
using UnityEngine.UI;

public class ArtifactSlotUI : MonoBehaviour
{
    public Image artifactIcon;
    public GameObject equippedBorder; 
    public Slider dataProgressBar;

    public void Setup(PhysicalTheorySO theory, DroneManager.RuntimeTheory state, bool isEquipped)
    {
        if (artifactIcon != null)
        {
            artifactIcon.sprite = theory.icon;
            if (theory.icon == null) 
            {
                artifactIcon.color = new Color(1, 1, 1, 0); 
            }
            else 
            {
                // Silhouette scura se non è ancora sintetizzata (Lv 0)
                // Se posseduta (Lv > 0), mostra l'icona normale (che comunicherà visivamente il livello)
                artifactIcon.color = state.level == 0 ? new Color(0.1f, 0.1f, 0.1f, 0.8f) : Color.white;
            }
        }

        if (equippedBorder != null) equippedBorder.SetActive(isEquipped);

        if (dataProgressBar != null)
        {
            int requiredData = theory.GetDataRequiredForLevel(state.level);
            dataProgressBar.value = Mathf.Clamp01((float)state.accumulatedData / requiredData);
            // Opzionale: Nascondi la barra se il livello è massimo
        }
    }
}