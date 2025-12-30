using UnityEngine;
using TMPro;
using BreakInfinity;
using System;

public class OfflinePopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;       // Il pannello intero (Background + Finestra)
    public TextMeshProUGUI titleText;   // "Welcome Back!"
    public TextMeshProUGUI timeText;    // "You were away for: 2h 15m"
    public TextMeshProUGUI earningsText;// "You earned: 1.50k Energy"
    public TextMeshProUGUI capWarningText; // "Storage Full!" (opzionale)

    private void Start()
    {
        // Iscrizione all'evento (codice che avevi già)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnOfflineProductionCalculated += ShowPopup;
        }

        // --- NUOVO CODICE: Il controllo manuale di sicurezza ---
        // Controlliamo se il Manager ha già calcolato tutto PRIMA che noi ci svegliassimo
        CheckPendingOfflineEarnings();
    }

    private void CheckPendingOfflineEarnings()
    {
        // Se il GameManager esiste E ha dei guadagni offline salvati in memoria...
        if (GameManager.Instance != null && GameManager.Instance.LastOfflineEarnings > 0)
        {
            // ...ma il pannello è ancora spento (quindi ci siamo persi l'evento)
            if (popupPanel != null && !popupPanel.activeSelf)
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
        TimeSpan timeAway = GameManager.Instance.LastOfflineTimeSpan;

        // Formattazione Tempo
        string timeStr = "";
        if (timeAway.Days > 0) timeStr += $"{timeAway.Days}d ";
        if (timeAway.Hours > 0) timeStr += $"{timeAway.Hours}h ";
        timeStr += $"{timeAway.Minutes}m {timeAway.Seconds}s";

        string earnStr = FormatNumber(earnings);

        // Aggiorna Testi UI
        if(timeText) timeText.text = $"Time Away: <color=yellow>{timeStr}</color>";
        
        // >>> LOGICA PER LO STORAGE PIENO <<<
        if (earnings <= 0)
        {
            // Caso: Batteria Piena -> Guadagno Zero
            if(earningsText) earningsText.text = "<color=red>BATTERIES FULL!</color>\n<size=80%>Upgrade Storage to earn offline.</size>";
        }
        else
        {
            // Caso Normale
            if(earningsText) earningsText.text = $"Offline Production (50%):\n<size=150%><color=#00FFFF>+{earnStr}</color></size>";
        }
        
        // Gestione Avviso Extra (Opzionale)
        if (capWarningText)
        {
            // Mostra l'avviso se siamo pieni O se abbiamo guadagnato 0
            bool isFull = GameManager.Instance.CurrentEnergy >= GameManager.Instance.StorageCap;
            capWarningText.gameObject.SetActive(isFull || earnings <= 0);
            
            if (isFull || earnings <= 0) 
                capWarningText.text = "<color=red>Storage Capacity Reached!</color>";
        }

        // Mostra Pannello
        popupPanel.SetActive(true); // Ricordati che ora questo accende "Visuals"
        
        // ... (Blocco camera rimasto uguale) ...
    }

    public void ClosePopup()
    {
        if(popupPanel) popupPanel.SetActive(false);
        PlanetOrbitCamera.IsInputBlocked = false;
    }

    // Helper per formattazione (copiato da UIManager per consistenza)
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