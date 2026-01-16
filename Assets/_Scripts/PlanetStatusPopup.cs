using UnityEngine;
using TMPro;
using BreakInfinity; // Necessario per gestire i BigDouble
using UnityEngine.UI;

public class PlanetStatusPopup : MonoBehaviour
{
    [Header("--- UI References ---")]
    [Tooltip("Il pannello principale del popup (da attivare/disattivare)")]
    public GameObject contentPanel;
    
    [Tooltip("Testo per il nome del pianeta corrente")]
    public TextMeshProUGUI planetNameText;
    
    [Tooltip("Testo per il moltiplicatore di produzione")]
    public TextMeshProUGUI multiplierText;
    
    [Tooltip("Testo descrittivo o di lore (opzionale)")]
    public TextMeshProUGUI descriptionText;
    
    [Tooltip("Icona o immagine del pianeta (opzionale)")]
    public Image planetIcon;

    [Header("--- Settings ---")]
    [Tooltip("Animazione di apertura (opzionale)")]
    public Animator popupAnimator;

    private void Start()
    {
        // Assicuriamoci che il popup sia chiuso all'avvio
        if(contentPanel != null) contentPanel.SetActive(false);
    }

    // Chiama questo metodo da un bottone nella UI principale
    public void OpenPopup()
    {
        if (contentPanel != null) 
        {
            contentPanel.SetActive(true);
            UpdatePlanetInfo();
            
            // Se hai un'animazione di entrata
            if (popupAnimator != null) popupAnimator.Play("PopupOpen");
        }
    }

    // Chiama questo metodo dal bottone "X" o "Chiudi" del popup
    public void ClosePopup()
    {
        if (contentPanel != null) contentPanel.SetActive(false);
    }

    private void UpdatePlanetInfo()
    {
        // Controlla se il PlanetManager esiste
        if (PlanetManager.Instance == null) return;

        // Recupera i dati del pianeta corrente
        var planetData = PlanetManager.Instance.GetCurrentPlanetData();
        int currentIndex = PlanetManager.Instance.currentPlanetIndex;

        if (planetData != null)
        {
            // 1. Imposta il Nome
            if (planetNameText != null) 
                planetNameText.text = planetData.planetName;

            // 2. Imposta il Moltiplicatore (Formattato bene)
            if (multiplierText != null) 
                multiplierText.text = $"Production Multiplier: <color=green>x{FormatMultiplier(planetData.productionMultiplier)}</color>";

            // 3. Descrizione (Generica o specifica)
            if (descriptionText != null)
            {
                descriptionText.text = $"Current Location: Planet #{currentIndex + 1}\n" +
                                       $"Gravity: Stable\n" +
                                       $"Resources: Abundant";
            }
            
            // 4. Icona (Se nel tuo PlanetData hai un campo sprite, puoi collegarlo qui)
            // if (planetIcon != null && planetData.planetIcon != null)
            //    planetIcon.sprite = planetData.planetIcon;
        }
    }

    // Helper per formattare i numeri grandi in modo leggibile
    private string FormatMultiplier(BigDouble number)
    {
        if (number < 1000) return number.ToString("F2");
        return number.ToString("F0");
    }
}