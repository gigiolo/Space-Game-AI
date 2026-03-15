using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DroneResultPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform windowRect; // <--- NUOVO: Trascina qui l'oggetto "Window" (quello col Content Size Fitter)
    [SerializeField] private TextMeshProUGUI logText; 
    [SerializeField] private Button closeButton;
    [SerializeField] private Image artifactIcon; 
    [SerializeField] private GameObject artifactContainer; 

    [Header("Animazione")]
    [SerializeField] private float charactersPerSecond = 60f;
    [SerializeField] private AudioClip typingSound; 

    public void Show(string logContent, CosmicArtifactSO artifact)
    {
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

        // --- MAGIA DEL LAYOUT ---
        // Assegniamo il testo PRIMA dell'animazione, così TextMeshPro sa già quanto sarà alto
        logText.text = logContent;
        
        // Forziamo Unity a ricalcolare immediatamente i Content Size Fitter.
        // Così la finestra prende subito la sua dimensione finale massima.
        Canvas.ForceUpdateCanvases();
        if (windowRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(windowRect);
        }

        // Ora avviamo l'animazione nascondendo i caratteri
        StartCoroutine(TypewriterEffect(logContent));
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        // Nasconde tutti i caratteri, ma lo "spazio fisico" rimane allocato!
        logText.maxVisibleCharacters = 0;

        int totalChars = fullText.Length;
        float timer = 0f;
        
        while (logText.maxVisibleCharacters < totalChars)
        {
            if (Input.GetMouseButtonDown(0))
            {
                logText.maxVisibleCharacters = totalChars;
                break; 
            }

            timer += Time.unscaledDeltaTime * charactersPerSecond;
            logText.maxVisibleCharacters = (int)timer;
            
            if (typingSound != null && AudioManager.Instance != null && UnityEngine.Random.value > 0.7f) 
            {
                AudioManager.Instance.PlaySFX(typingSound, 0.2f, 0.1f);
            }

            yield return null;
        }
        
        logText.maxVisibleCharacters = totalChars;
        closeButton.interactable = true; 
    }

    private void Close()
    {
        panelRoot.SetActive(false);
    }
}