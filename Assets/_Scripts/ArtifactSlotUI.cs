// --- File: _Scripts\ArtifactSlotUI.cs ---
using UnityEngine;
using UnityEngine.UI;

public class ArtifactSlotUI : MonoBehaviour
{
    [Header("Riferimenti Visuali")]
    [SerializeField] private Image rarityBackgroundImage; // <--- NUOVO: Lo sfondo colorato
    public Image artifactIcon;
    public GameObject equippedBorder; 
    public Slider dataProgressBar;

    // Aggiunto il parametro Color rarityColor
    public void Setup(PhysicalTheorySO theory, DroneManager.RuntimeTheory state, bool isEquipped, Color rarityColor)
    {
        // --- GESTIONE SFONDO RARITÀ ---
        if (rarityBackgroundImage != null)
        {
            if (state.level == 0)
            {
                // Se non è scoperta, facciamo lo sfondo molto scuro e semitrasparente, 
                // mantenendo una leggerissima tinta della sua rarità
                rarityBackgroundImage.color = new Color(rarityColor.r * 0.2f, rarityColor.g * 0.2f, rarityColor.b * 0.2f, 0.8f);
            }
            else
            {
                // Teoria posseduta: colore pieno!
                rarityBackgroundImage.color = rarityColor;
            }
        }

        // --- GESTIONE ICONA ---
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
                artifactIcon.color = state.level == 0 ? new Color(0.1f, 0.1f, 0.1f, 0.8f) : Color.white;
            }
        }

        if (equippedBorder != null) equippedBorder.SetActive(isEquipped);

        // --- GESTIONE BARRA DATI ---
        if (dataProgressBar != null)
        {
            int requiredData = theory.GetDataRequiredForLevel(state.level);
            dataProgressBar.value = Mathf.Clamp01((float)state.accumulatedData / requiredData);
            // Opzionale: Nascondi la barra se il livello è massimo
        }
    }
}