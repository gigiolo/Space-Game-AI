// --- File: _Scripts\DroneResultPopup.cs ---
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DroneResultPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelRoot;
    public RectTransform windowRect; 
    public TextMeshProUGUI logText; 
    public Button closeButton;
    public Image artifactIcon; 
    public GameObject artifactContainer; 
    public TextMeshProUGUI dataAmountText; 

    [Header("Animazione")]
    public float charactersPerSecond = 60f;
    public AudioClip typingSound; 

    // MODIFICA: La firma ora accetta il Dictionary
    public void Show(string logContent, Dictionary<PhysicalTheorySO, int> foundTheories)
    {
        if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(panelRoot);
        
        panelRoot.SetActive(true);
        
        if (artifactContainer != null)
        {
            bool hasAnyTheory = foundTheories != null && foundTheories.Count > 0;
            artifactContainer.SetActive(hasAnyTheory);

            if (hasAnyTheory) 
            {
                // Se c'è una sola teoria, mostriamo l'icona. Altrimenti nascondiamo l'immagine singola.
                if (foundTheories.Count == 1)
                {
                    var firstTheory = new List<PhysicalTheorySO>(foundTheories.Keys)[0];
                    if (artifactIcon != null) 
                    {
                        artifactIcon.gameObject.SetActive(true);
                        artifactIcon.sprite = firstTheory.icon;
                    }
                    if (dataAmountText != null) dataAmountText.text = $"+{foundTheories[firstTheory]} TB";
                }
                else
                {
                    if (artifactIcon != null) artifactIcon.gameObject.SetActive(false); // Troppe da mostrare in una sola icona
                    if (dataAmountText != null) dataAmountText.text = "DATI MULTIPLI";
                }
            }
        }

        closeButton.interactable = false;
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);

        logText.text = logContent;
        
        Canvas.ForceUpdateCanvases();
        if (windowRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(windowRect);

        StartCoroutine(TypewriterEffect(logContent));
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
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