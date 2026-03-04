using UnityEngine;
using TMPro;

public class ResearchTierHeaderUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public CanvasGroup canvasGroup; // Per sfumare il colore se è bloccato

    // Salviamo l'indice del tier per quando facciamo il refresh
    public int TierIndex { get; private set; } 

    public void Setup(int tier)
    {
        TierIndex = tier;
        if (titleText) titleText.text = $"--- TIER {tier} ---";
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (ResearchManager.Instance == null) return;

        bool isUnlocked = ResearchManager.Instance.IsTierUnlocked(TierIndex);
        int missingUpgrades = ResearchManager.Instance.UpgradesNeededForTier(TierIndex);

        if (statusText != null)
        {
            if (isUnlocked)
            {
                statusText.text = "<color=#00FF00>SBLOCCATO</color>";
            }
            else
            {
                statusText.text = $"<color=red>Acquista altri {missingUpgrades} upgrade per sbloccare</color>";
            }
        }

        // Opzionale: scurisce l'header se è bloccato
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isUnlocked ? 1f : 0.6f;
        }
    }
}