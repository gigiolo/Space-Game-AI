using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchTierHeaderUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private CanvasGroup canvasGroup; // Per sfumare il colore se è bloccato

    [Header("Icone Stato")]
    [SerializeField] private Image statusIcon; 
    [SerializeField] private Sprite unlockedIcon; 
    [SerializeField] private Sprite lockedIcon; 

    [Header("Colori Icona")]
    // Esporre i colori nell'Inspector è una best practice: 
    // permette ai game designer di cambiare le tonalità senza toccare il codice!
    [SerializeField] private Color unlockedColor = Color.white; 
    [SerializeField] private Color lockedColor = Color.red; 

    // Salviamo l'indice del tier per quando facciamo il refresh
    public int TierIndex { get; private set; } 

    public void Setup(int tier)
    {
        TierIndex = tier;
        if (titleText != null) titleText.text = $"Livello di conoscenza {tier}";
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (ResearchManager.Instance == null) return;

        // --- 1. LOGICA DI VISIBILITA' ---
        int highestUnlocked = ResearchManager.Instance.GetHighestUnlockedTier();
        
        // Magia: Questo Header deve esistere solo se il suo indice è <= al massimo sbloccato + 1.
        bool shouldBeVisible = TierIndex <= highestUnlocked + 1;

        if (gameObject.activeSelf != shouldBeVisible)
        {
            gameObject.SetActive(shouldBeVisible);
        }

        // Se lo abbiamo appena spento, inutile consumare CPU
        if (!shouldBeVisible) return;

        // --- 2. AGGIORNAMENTO TESTI, ICONE E COLORI ---
        bool isUnlocked = ResearchManager.Instance.IsTierUnlocked(TierIndex);
        int missingUpgrades = ResearchManager.Instance.UpgradesNeededForTier(TierIndex);

        if (isUnlocked)
        {
            // SBLOCCATO
            if (statusText != null) statusText.text = ""; 
            
            if (statusIcon != null)
            {
                statusIcon.sprite = unlockedIcon;
                statusIcon.color = unlockedColor; // Applica il colore per lo stato sbloccato
                statusIcon.gameObject.SetActive(true); 
            }
        }
        else
        {
            // BLOCCATO
            if (statusText != null) statusText.text = $"<color=red>{missingUpgrades} upgrade necessari</color>";
            
            if (statusIcon != null)
            {
                statusIcon.sprite = lockedIcon;
                statusIcon.color = lockedColor; // Applica il colore rosso (o quello scelto nell'Inspector)
                statusIcon.gameObject.SetActive(true); 
            }
        }

        // --- 3. EFFETTO VISIVO (SCURIMENTO) ---
        // Scurisce l'header se è bloccato
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isUnlocked ? 1f : 0.6f;
        }
    }
}