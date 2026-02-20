using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BreakInfinity;
using System;

public class PlanetStatusButtonController : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [Tooltip("Il testo all'interno del bottone che mostrerà lo stato.")]
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Tooltip("Testo di default quando non succede nulla (es. 'SYSTEM').")]
    [SerializeField] private string defaultText = "SYSTEM";

    [Header("Colori Stato")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color prepColor = new Color(0f, 1f, 1f); // Ciano
    [SerializeField] private Color travelColor = new Color(1f, 0.8f, 0f); // Giallo/Arancio

    // Cache
    private PlanetManager _pm;

    private void Start()
    {
        _pm = PlanetManager.Instance;
        
        // Se l'utente non ha assegnato il testo, proviamo a trovarlo nel bottone stesso
        if (statusLabel == null)
            statusLabel = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (_pm == null)
        {
            _pm = PlanetManager.Instance;
            return;
        }

        if (statusLabel == null) return;

        // 1. STATO VIAGGIO (Priorità massima)
        if (_pm.isTraveling)
        {
            UpdateTravelStatus();
        }
        // 2. STATO PREPARAZIONE
        else if (_pm.isPreparingForLaunch)
        {
            UpdatePrepStatus();
        }
        // 3. STATO NORMALE / PRONTO
        else
        {
            UpdateNormalStatus();
        }
    }

    private void UpdateTravelStatus()
    {
        TimeSpan timeElapsed = DateTime.UtcNow - _pm.travelStartTime;
        double totalDuration = _pm.GetTotalTravelDuration();
        double remainingSeconds = totalDuration - timeElapsed.TotalSeconds;

        if (remainingSeconds < 0) remainingSeconds = 0;

        TimeSpan remaining = TimeSpan.FromSeconds(remainingSeconds);
        
        // Formatta il tempo come MM:SS
        string timeStr = string.Format("{0:D2}:{1:D2}", remaining.Minutes, remaining.Seconds);
        
        // Se c'è anche l'ora, la aggiungiamo
        if (remaining.TotalHours >= 1)
            timeStr = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)remaining.TotalHours, remaining.Minutes, remaining.Seconds);

        statusLabel.text = $"FLIGHT\n{timeStr}";
        statusLabel.color = travelColor;
    }

    private void UpdatePrepStatus()
    {
        BigDouble required = _pm.GetLaunchEnergyRequirement();
        
        // Evitiamo divisioni per zero
        if (required <= 0) 
        {
            statusLabel.text = "PREP\n0%";
            return;
        }

        BigDouble current = _pm.launchPreparationProgress;
        
        // Calcolo percentuale
        double ratio = (current / required).ToDouble();
        float percentage = Mathf.Clamp01((float)ratio) * 100f;

        statusLabel.text = $"PREP\n{percentage:F0}%";
        statusLabel.color = prepColor;
    }

    private void UpdateNormalStatus()
    {
        // Se c'è un pianeta successivo e abbiamo raggiunto il valore richiesto, mostriamo "READY"
        // Altrimenti mostriamo il testo di default
        var currentData = _pm.GetCurrentPlanetData();
        
        if (currentData != null && PlanetManager.Instance.CalculatePlanetValue() >= currentData.requiredPlanetValue)
        {
            statusLabel.text = "READY";
            statusLabel.color = Color.green;
        }
        else
        {
            statusLabel.text = defaultText;
            statusLabel.color = normalColor;
        }
    }
}