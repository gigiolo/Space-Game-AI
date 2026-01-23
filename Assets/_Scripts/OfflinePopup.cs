using UnityEngine;
using TMPro;
using BreakInfinity;
using System;

public class OfflinePopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI earningsText;
    public TextMeshProUGUI capWarningText; 

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // 1. Iscriviti all'evento per il futuro (se avviene mentre il gioco è già acceso)
            GameManager.Instance.OnOfflineProductionCalculated += ShowPopup;

            // --- FIX START ---
            // 2. CONTROLLO MANUALE:
            // Se il GameManager ha già calcolato i guadagni (LastOfflineEarnings > 0)
            // e il popup non è ancora visibile, mostriamolo subito!
            if (GameManager.Instance.LastOfflineEarnings > 0 && !popupPanel.activeSelf)
            {
                ShowPopup();
            }
            // --- FIX END ---
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnOfflineProductionCalculated -= ShowPopup;
        }
    }

    private void ShowPopup()
    {
        BigDouble earnings = GameManager.Instance.LastOfflineEarnings;
        
        // Se per qualche motivo i guadagni sono 0, non mostrare nulla (evita popup vuoti)
        if (earnings <= 0) return;

        TimeSpan timeAway = GameManager.Instance.LastOfflineTimeSpan;
        
        // CORREZIONE: Usiamo MaxOfflineSeconds invece di StorageCap
        double maxTimeSeconds = GameManager.Instance.MaxOfflineSeconds;

        // Formattazione Tempo
        string timeStr = "";
        if (timeAway.Days > 0) timeStr += $"{timeAway.Days}d ";
        if (timeAway.Hours > 0) timeStr += $"{timeAway.Hours}h ";
        timeStr += $"{timeAway.Minutes}m {timeAway.Seconds}s"; // Aggiunti i secondi per precisione

        string earnStr = FormatNumber(earnings);

        if(timeText) timeText.text = $"Time Away: <color=yellow>{timeStr}</color>";
        if(earningsText) earningsText.text = $"Offline Production ({GameManager.Instance.offlineProductionRatio * 100}%):\n<size=150%><color=#00FFFF>+{earnStr}</color></size>";
        
        // NUOVO CONTROLLO: BATTERIE ESAURITE
        if (capWarningText)
        {
            // Se siamo stati via (timeAway) più di quanto le batterie reggessero (maxTimeSeconds)
            bool batteriesDied = timeAway.TotalSeconds > maxTimeSeconds;
            
            capWarningText.gameObject.SetActive(batteriesDied);
            
            if (batteriesDied) 
            {
                TimeSpan maxTs = TimeSpan.FromSeconds(maxTimeSeconds);
                string maxStr = $"{maxTs.Hours}h {maxTs.Minutes}m";
                capWarningText.text = $"<color=red>Batteries died after {maxStr}!</color>\nUpgrade storage to last longer.";
            }
        }

        popupPanel.SetActive(true);
        
        // Blocchiamo l'input della camera planetaria per non ruotare il pianeta mentre leggiamo
        PlanetOrbitCamera.IsInputBlocked = true;
    }

    public void ClosePopup()
    {
        // Quando chiudiamo, resettiamo il valore nel GameManager per evitare 
        // che il popup si riapra se ricarichiamo la scena senza chiudere il gioco
        if (GameManager.Instance != null)
        {
            // Nota: LastOfflineEarnings è read-only pubblicamente nel tuo codice attuale.
            // Se volessi azzerarlo servirebbe un metodo pubblico nel GameManager.
            // Per ora va bene così, perché il controllo in Start verifica !popupPanel.activeSelf
        }

        if(popupPanel) popupPanel.SetActive(false);
        PlanetOrbitCamera.IsInputBlocked = false;
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F0");
        long exponent = (long)BigDouble.Log10(number);
        if (exponent < 6) return (number / 1000).ToString("F2") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        return number.ToString("e2");
    }
}