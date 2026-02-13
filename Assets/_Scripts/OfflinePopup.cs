using UnityEngine;
using TMPro;
using BreakInfinity;
using System;
using UnityEngine.UI; 

public class OfflinePopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI earningsText;
    public TextMeshProUGUI emittersText;
    public TextMeshProUGUI capWarningText;

    private void Start()
    {
        if (popupPanel != null) popupPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnOfflineProductionCalculated += UpdateUI;
            if (GameManager.Instance.LastOfflineEarnings > 0)
            {
                UpdateUI();
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnOfflineProductionCalculated -= UpdateUI;
    }

    public void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        BigDouble earnings = GameManager.Instance.LastOfflineEarnings;
        TimeSpan timeAway = GameManager.Instance.LastOfflineTimeSpan;

        string timeStr = "";
        if (timeAway.Days > 0) timeStr += $"{timeAway.Days}d ";
        if (timeAway.Hours > 0) timeStr += $"{timeAway.Hours}h ";
        timeStr += $"{timeAway.Minutes}m {timeAway.Seconds}s";

        string earnStr = FormatNumber(earnings);

        if (timeText != null)
        {
            timeText.text = $"Tempo Offline: <color=yellow>{timeStr}</color>";
            timeText.SetAllDirty(); 
        }

        if (earningsText != null)
        {
            earningsText.text = $"Guadagno:\n<size=120%><color=#00FFFF>+{earnStr}</color></size>";
            earningsText.SetAllDirty();
        }

        if (emittersText != null)
        {
            int gained = GameManager.Instance.LastOfflineEmittersGained;
            if (gained > 0)
            {
                emittersText.gameObject.SetActive(true);
                emittersText.text = $"Nuovi Emitter: <color=#00FF00>+{gained}</color>";
                emittersText.SetAllDirty();
            }
            else
            {
                emittersText.gameObject.SetActive(false);
            }
        }

        if (capWarningText != null)
        {
            bool died = timeAway.TotalSeconds >= GameManager.Instance.MaxOfflineSeconds;
            capWarningText.gameObject.SetActive(died);
            if (died) capWarningText.SetAllDirty();
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupPanel.GetComponent<RectTransform>());
            PlanetOrbitCamera.IsInputBlocked = true;
        }
    }

    public void ClosePopup()
    {
        if (popupPanel) popupPanel.SetActive(false);
        PlanetOrbitCamera.IsInputBlocked = false;
    }

    // --- METODO CORRETTO ---
    private string FormatNumber(BigDouble number)
    {
        if (number < 10) return number.ToString("F2");
        if (number < 1000) return number.ToString("F0");

        long exponent = (long)BigDouble.Log10(number);
        if (exponent < 6) return (number / 1000).ToString("F2") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        
        // CORREZIONE QUI
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}