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
    
    [Tooltip("Collega qui il testo per mostrare i nuovi Emitter guadagnati")]
    public TextMeshProUGUI emittersText; // <--- NUOVO RIFERIMENTO

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnOfflineProductionCalculated += ShowPopup;

            if (GameManager.Instance.LastOfflineEarnings > 0 && !popupPanel.activeSelf)
            {
                ShowPopup();
            }
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
        
        if (earnings <= 0) return;

        TimeSpan timeAway = GameManager.Instance.LastOfflineTimeSpan;
        double maxTimeSeconds = GameManager.Instance.MaxOfflineSeconds;

        string timeStr = "";
        if (timeAway.Days > 0) timeStr += $"{timeAway.Days}d ";
        if (timeAway.Hours > 0) timeStr += $"{timeAway.Hours}h ";
        timeStr += $"{timeAway.Minutes}m {timeAway.Seconds}s"; 

        string earnStr = FormatNumber(earnings);

        if(timeText) timeText.text = $"Time Away: <color=yellow>{timeStr}</color>";
        if(earningsText) earningsText.text = $"Offline Production ({GameManager.Instance.offlineProductionRatio * 100}%):\n<size=150%><color=#00FFFF>+{earnStr}</color></size>";
        
        // --- NUOVO: MOSTRA EMITTERS GUADAGNATI ---
        if (emittersText != null)
        {
            int gainedEmitters = GameManager.Instance.LastOfflineEmittersGained;
            if (gainedEmitters > 0)
            {
                emittersText.gameObject.SetActive(true);
                emittersText.text = $"New Emitters: <color=#00FF00>+{gainedEmitters}</color>";
            }
            else
            {
                // Se non ne abbiamo guadagnati (perché cap raggiunto o tempo troppo breve), nascondiamo il testo
                emittersText.gameObject.SetActive(false);
            }
        }
        // ----------------------------------------

        if (capWarningText)
        {
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
        PlanetOrbitCamera.IsInputBlocked = true;
    }

    public void ClosePopup()
    {
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