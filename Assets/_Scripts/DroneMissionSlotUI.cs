// --- File: _Scripts\UI\DroneMissionSlotUI.cs ---
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BreakInfinity;
using System;

public class DroneMissionSlotUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI statusText; 
    [SerializeField] private TextMeshProUGUI speedText; 
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    private DroneMissionSO _mission;
    private DroneManager.ActiveDrone _activeDroneData; 
    private Action<DroneMissionSO> _onLaunchClick;
    private Action<DroneManager.ActiveDrone> _onClaimClick;

    public void Setup(DroneMissionSO mission, Action<DroneMissionSO> onLaunch, Action<DroneManager.ActiveDrone> onClaim)
    {
        _mission = mission;
        _onLaunchClick = onLaunch;
        _onClaimClick = onClaim;

        if (titleText != null) titleText.text = mission.missionName;
        if (descText != null) descText.text = mission.description; // Potresti voler aggiungere 'cargoCapacity' nella descrizione testuale

        RefreshState();
    }

    public void RefreshState()
    {
        if (_mission == null || DroneManager.Instance == null || GameManager.Instance == null) return;

        _activeDroneData = DroneManager.Instance.activeDrones.Find(d => d.missionData.id == _mission.id);
        actionButton.onClick.RemoveAllListeners();

        if (_activeDroneData != null)
        {
            // 1. STATO: A TERRA E PRONTA (Il giocatore deve solo leggere il log)
            if (_activeDroneData.isCompleted)
            {
                statusText.text = "<color=#00FF00>ANALISI PRONTA</color>";
                actionButtonText.text = "LEGGI LOG";
                actionButton.interactable = true;
                actionButton.onClick.AddListener(() => _onClaimClick?.Invoke(_activeDroneData));
                
                if (speedText != null) speedText.gameObject.SetActive(false); 
            }
            // 2. STATO: IN ATTERRAGGIO (Timer scaduto, animazione in corso)
            else if (_activeDroneData.isLanding)
            {
                statusText.text = "<color=orange>IN ATTERRAGGIO...</color>";
                actionButtonText.text = "ATTENDERE";
                actionButton.interactable = false;
                
                if (speedText != null) speedText.gameObject.SetActive(false); 
            }
            // 3. STATO: IN VIAGGIO (Timer attivo)
            else
            {
                TimeSpan remaining = _activeDroneData.returnTime - DateTime.UtcNow;
                if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;
                
                string timeStr = string.Format("{0:D2}:{1:D2}", remaining.Minutes, remaining.Seconds);
                if (remaining.TotalHours >= 1) timeStr = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)remaining.TotalHours, remaining.Minutes, remaining.Seconds);

                statusText.text = $"In Viaggio... <color=yellow>{timeStr}</color>";
                actionButtonText.text = "IN ORBITA";
                actionButton.interactable = false;

                if (speedText != null)
                {
                    speedText.gameObject.SetActive(true);

                    double totalSeconds = _mission.durationSeconds;
                    double elapsedSeconds = (DateTime.UtcNow - _activeDroneData.launchTime).TotalSeconds;
                    float progress = Mathf.Clamp01((float)(elapsedSeconds / totalSeconds));

                    float speedCurve = Mathf.Pow(Mathf.Sin(progress * Mathf.PI), 2f);
                    float theoreticalMaxSpeed = _mission.maxLightYears * 500000f; 
                    float currentSpeed = theoreticalMaxSpeed * speedCurve;

                    if (currentSpeed < 0.01f) currentSpeed = 0f;

                    if (currentSpeed > 300000f)
                    {
                        float speedInC = currentSpeed / 300000f;
                        speedText.text = $"Velocità: <color=#00FFFF>{speedInC:F5} c</color>";
                    }
                    else
                    {
                        speedText.text = $"Velocità: <color=#00FFFF>{currentSpeed:N2} km/s</color>";
                    }
                }
            }
        }
        else
        {
            // --- STATO: DISPONIBILE PER IL LANCIO (MODIFICATO PER IL COSTO FISSO) ---
            
            // 1. Legge la stringa dal SO e la converte nel numero gigante
            BigDouble cost = BigDouble.Parse(_mission.fixedEnergyCost);
            
            bool canAfford = GameManager.Instance.CurrentEnergy >= cost;
            bool hasFreeDrones = DroneManager.Instance.activeDrones.Count < DroneManager.Instance.unlockedSlots;

            // 2. Mostra il numero usando il tuo metodo di formattazione
            statusText.text = $"Costo Lancio: {FormatNumber(cost)} Energy";
            
            // Colora il testo (Bianco se hai i soldi, Rosso se sei povero)
            statusText.color = canAfford ? Color.white : Color.red;
            
            if (speedText != null) speedText.gameObject.SetActive(false); 

            if (!hasFreeDrones)
            {
                actionButtonText.text = "NESSUN DRONE";
                actionButton.interactable = false;
            }
            else
            {
                actionButtonText.text = "LANCIA";
                actionButton.interactable = canAfford;
                actionButton.onClick.AddListener(() => _onLaunchClick?.Invoke(_mission));
            }
        }
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F0");
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}