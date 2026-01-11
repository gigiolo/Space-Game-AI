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
            GameManager.Instance.OnOfflineProductionCalculated += ShowPopup;
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
        TimeSpan timeAway = GameManager.Instance.LastOfflineTimeSpan;
        
        // CORREZIONE: Usiamo MaxOfflineSeconds invece di StorageCap
        double maxTimeSeconds = GameManager.Instance.MaxOfflineSeconds;

        // Formattazione Tempo
        string timeStr = "";
        if (timeAway.Days > 0) timeStr += $"{timeAway.Days}d ";
        if (timeAway.Hours > 0) timeStr += $"{timeAway.Hours}h ";
        timeStr += $"{timeAway.Minutes}m";

        string earnStr = FormatNumber(earnings);

        if(timeText) timeText.text = $"Time Away: <color=yellow>{timeStr}</color>";
        if(earningsText) earningsText.text = $"Offline Production (50%):\n<size=150%><color=#00FFFF>+{earnStr}</color></size>";
        
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