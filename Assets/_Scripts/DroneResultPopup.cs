using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DroneResultPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI logText; 
    [SerializeField] private Button closeButton;
    [SerializeField] private Image artifactIcon; 
    [SerializeField] private GameObject artifactContainer; // Un pannello/oggetto che contiene l'icona e la scritta "Nuovo Artefatto"

    [Header("Animazione")]
    [SerializeField] private float charactersPerSecond = 60f;
    [SerializeField] private AudioClip typingSound; 

    public void Show(string logContent, CosmicArtifactSO artifact)
    {
        // Se il manager UI esiste, chiudiamo le altre finestre
        if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(panelRoot);
        
        panelRoot.SetActive(true);
        
        // Setup Grafica Artefatto
        if (artifactContainer != null)
        {
            bool hasArtifact = artifact != null;
            artifactContainer.SetActive(hasArtifact);

            if (hasArtifact && artifactIcon != null) 
            {
                artifactIcon.sprite = artifact.icon;
            }
        }

        // Blocca il bottone di chiusura finché non finisce di scrivere
        closeButton.interactable = false;
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);

        StartCoroutine(TypewriterEffect(logContent));
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        logText.text = fullText;
        logText.maxVisibleCharacters = 0;

        int totalChars = fullText.Length;
        float timer = 0f;
        
        while (logText.maxVisibleCharacters < totalChars)
        {
            // SKIP: Se il giocatore tocca lo schermo, stampa tutto subito
            if (Input.GetMouseButtonDown(0))
            {
                logText.maxVisibleCharacters = totalChars;
                break; 
            }

            // Usiamo unscaledDeltaTime così funziona anche se il gioco fosse in pausa
            timer += Time.unscaledDeltaTime * charactersPerSecond;
            logText.maxVisibleCharacters = (int)timer;
            
            // Effetto sonoro stile terminale (suona randomicamente per non assordare)
            if (typingSound != null && AudioManager.Instance != null && UnityEngine.Random.value > 0.7f) 
            {
                AudioManager.Instance.PlaySFX(typingSound, 0.2f, 0.1f);
            }

            yield return null;
        }
        
        logText.maxVisibleCharacters = totalChars;
        closeButton.interactable = true; // Permetti la chiusura
    }

    private void Close()
    {
        panelRoot.SetActive(false);
    }
}