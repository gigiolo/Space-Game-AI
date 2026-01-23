using UnityEngine;
using System;
using System.Collections.Generic;
using BreakInfinity; // Necessario per BigDouble

public class DailyGiftManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The list of 28 daily rewards, in order.")]
    public List<DailyRewardSO> dailyRewards = new List<DailyRewardSO>(28);

    [Header("References")]
    [Tooltip("Reference to the NotificationManager to show alerts.")]
    public NotificationManager notificationManager;

    // ID univoco per la notifica locale del Daily Gift (per non sovrascrivere quella dei viaggi)
    private const int DAILY_GIFT_NOTIF_ID = 200;

    // Player State
    private DateTime lastClaimedTimestamp;
    private int currentDayIndex;
    private bool isRewardAvailable = false;
    private bool isNotificationShowing = false;
    private BigDouble _cachedRewardAmount;
    
    // Real-time Check
    private float checkTimer = 0f;
    private const float CHECK_INTERVAL = 5f; // Check every 5 seconds
    
    // This method will be called by GameManager to initialize the system
    public void Initialize(SaveData data)
    {
        if (data != null && !string.IsNullOrEmpty(data.dailyGiftLastClaimedTimestamp))
        {
            lastClaimedTimestamp = DateTime.Parse(data.dailyGiftLastClaimedTimestamp);
            currentDayIndex = data.dailyGiftCurrentDayIndex;
        }
        else
        {
            // First time playing or corrupted data, start fresh
            lastClaimedTimestamp = DateTime.MinValue; // Never claimed
            currentDayIndex = 0;
        }
        
        CheckForDailyGift();
    }
    
    private void Update()
    {
        // Only run the check periodically for performance
        checkTimer += Time.deltaTime;
        if (checkTimer >= CHECK_INTERVAL)
        {
            checkTimer = 0f;
            
            // If a reward is not yet available, check again.
            if (!isRewardAvailable)
            {
                CheckForDailyGift();
            }
            
            // If a reward is available but the notification hasn't been shown, show it (In-Game UI).
            if (isRewardAvailable && !isNotificationShowing)
            {
                CreateNotification();
            }
        }
    }

    // --- NUOVO: Schedula la notifica quando l'app va in pausa ---
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) // L'app sta andando in background
        {
            // Se abbiamo già riscosso (o stiamo aspettando), assicuriamoci che la notifica per domani sia pronta
            ScheduleNextGiftNotification();
        }
    }

    // This method will be called by GameManager to save the system's state
    public void Save(SaveData data)
    {
        data.dailyGiftLastClaimedTimestamp = lastClaimedTimestamp.ToString();
        data.dailyGiftCurrentDayIndex = currentDayIndex;
    }

    private void CheckForDailyGift()
    {
        // If the rewards list is not configured, do nothing.
        if (dailyRewards == null || dailyRewards.Count == 0)
        {
            isRewardAvailable = false;
            return;
        }

        // Case 1: First time ever claiming. The reward is immediately available.
        if (lastClaimedTimestamp == DateTime.MinValue)
        {
            isRewardAvailable = true;
            return;
        }
        
        // The time when the "day" resets.
        const int resetHour = 3;
        DateTime now = DateTime.Now;
        
        // Determine the start of the current "logical day" (which is 3 AM today).
        DateTime currentLogicalDayStart = now.Date.AddHours(resetHour);
        if (now.Hour < resetHour)
        {
            // If it's currently between midnight and 3 AM, the logical day started at 3 AM yesterday.
            currentLogicalDayStart = currentLogicalDayStart.AddDays(-1);
        }

        // If the last claim was made before the start of the current logical day, a new reward is available.
        if (lastClaimedTimestamp < currentLogicalDayStart)
        {
            isRewardAvailable = true;
        }
        else
        {
            isRewardAvailable = false;
        }
    }

    private void CreateNotification()
    {
        if (notificationManager == null)
        {
            notificationManager = NotificationManager.Instance;
            if (notificationManager == null)
            {
                // Se non c'è il manager in scena (es. scena di caricamento), usciamo
                return; 
            }
        }
        
        // Ensure the current day index is valid.
        if (currentDayIndex < 0 || currentDayIndex >= dailyRewards.Count)
        {
            Debug.LogError($"Invalid daily reward index: {currentDayIndex}");
            return;
        }

        DailyRewardSO currentReward = dailyRewards[currentDayIndex];
        if (currentReward == null)
        {
            Debug.LogError($"Daily Reward for day {currentDayIndex + 1} is not assigned!");
            return;
        }

        var notificationData = new NotificationData(
            "Daily Gift Available!",
            currentReward.description,
            currentReward.icon,
            () => { ShowRewardPopup(); },
            false
        );

        notificationManager.SpawnNotification(notificationData);
        isNotificationShowing = true; // Mark that the notification is now visible
    }

    public void ShowRewardPopup()
    {
        if (notificationManager == null || notificationManager.popupWindow == null)
        {
            Debug.LogError("Notification Popup reference is missing!");
            return;
        }

        DailyRewardSO currentReward = dailyRewards[currentDayIndex];
        _cachedRewardAmount = CalculateCurrentRewardAmount();
        
        string description = $"{currentReward.description}\n<size=120%><b>{_cachedRewardAmount.ToString("F2")}</b></size>";

        var popupData = new NotificationData(
            $"Daily Gift - Day {currentDayIndex + 1}",
            description,
            currentReward.icon,
            () => { ClaimReward(); },
            false
        );
        
        notificationManager.popupWindow.Show(popupData);
    }

    public void ClaimReward()
    {
        if (!isRewardAvailable) return;

        DailyRewardSO rewardSO = dailyRewards[currentDayIndex];

        switch (rewardSO.type)
        {
            case DailyRewardSO.RewardType.Energy:
                GameManager.Instance.AddEnergy(_cachedRewardAmount);
                break;
            case DailyRewardSO.RewardType.PremiumCurrency:
                // Implementazione futura Iridio Puro
                GameManager.Instance.AddPureIridium(_cachedRewardAmount);
                break;
            case DailyRewardSO.RewardType.ScienceNodes:
                // Implementazione futura Nodi
                break;
            case DailyRewardSO.RewardType.QuantumMultiplier:
                 // Implementazione futura
                break;
        }

        // Update state
        lastClaimedTimestamp = DateTime.Now;
        currentDayIndex = (currentDayIndex + 1) % dailyRewards.Count;
        isRewardAvailable = false;
        isNotificationShowing = false; // Reset notification flag for the next day

        // Save progress immediately
        GameManager.Instance.SaveGame();
        
        Debug.Log($"Claimed reward for day {currentDayIndex}. Next reward is day {currentDayIndex + 1}.");

        // --- NUOVO: Schedula la notifica per domani ---
        ScheduleNextGiftNotification();
    }

    private BigDouble CalculateCurrentRewardAmount()
    {
        DailyRewardSO rewardSO = dailyRewards[currentDayIndex];

        if (!rewardSO.isDynamic)
        {
            return rewardSO.staticAmount;
        }
        else
        {
            switch (rewardSO.type)
            {
                case DailyRewardSO.RewardType.Energy:
                    // Example: Production per second * multiplier (e.g., 20 seconds of production)
                    return GameManager.Instance.EffectiveIncomePerSec * rewardSO.dynamicMultiplier;
                
                case DailyRewardSO.RewardType.PremiumCurrency:
                    return 10; // Placeholder
                case DailyRewardSO.RewardType.ScienceNodes:
                    return 5; // Placeholder
                case DailyRewardSO.RewardType.QuantumMultiplier:
                    return 1; // Placeholder
            }
        }

        return 0;
    }

    // --- NUOVO METODO: Gestione Notifica Locale ---
    private void ScheduleNextGiftNotification()
    {
        // Se il sistema di notifiche non è pronto, usciamo
        if (LocalNotificationController.Instance == null) return;

        // 1. Calcoliamo quando scatta il prossimo reset (ore 03:00)
        DateTime now = DateTime.Now;
        DateTime nextReset = now.Date.AddHours(3); // Oggi alle 03:00

        // Se sono già passate le 3 di notte di oggi, il prossimo reset è domani alle 3
        if (now >= nextReset)
        {
            nextReset = nextReset.AddDays(1);
        }

        // 2. Programmiamo la notifica usando l'ID dedicato (200)
        LocalNotificationController.Instance.ScheduleNotification(
            "Regalo Disponibile! 🎁",
            "Il rifornimento giornaliero è arrivato. Torna in gioco per riscattarlo!",
            nextReset,
            DAILY_GIFT_NOTIF_ID
        );
        
        Debug.Log($"[DailyGift] Notifica schedulata per: {nextReset}");
    }
}