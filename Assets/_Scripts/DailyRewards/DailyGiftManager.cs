using UnityEngine;
using System;
using System.Collections.Generic;

public class DailyGiftManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The list of 28 daily rewards, in order.")]
    public List<DailyRewardSO> dailyRewards = new List<DailyRewardSO>(28);

    [Header("References")]
    [Tooltip("Reference to the NotificationManager to show alerts.")]
    public NotificationManager notificationManager;

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
            
            // If a reward is available but the notification hasn't been shown, show it.
            if (isRewardAvailable && !isNotificationShowing)
            {
                CreateNotification();
            }
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
                Debug.LogError("NotificationManager not found in the scene!");
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
                Debug.Log($"TODO: Implement logic to add {_cachedRewardAmount} Premium Currency.");
                break;
            case DailyRewardSO.RewardType.ScienceNodes:
                Debug.Log($"TODO: Implement logic to add {_cachedRewardAmount} Science Nodes.");
                break;
            case DailyRewardSO.RewardType.QuantumMultiplier:
                 Debug.Log($"TODO: Implement logic to add a Quantum Multiplier.");
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
                
                // --- PLACEHOLDERS FOR FUTURE DYNAMIC REWARDS ---
                case DailyRewardSO.RewardType.PremiumCurrency:
                     Debug.LogWarning("Dynamic Premium Currency calculation not yet implemented. Returning placeholder value.");
                    return 10; // Placeholder
                case DailyRewardSO.RewardType.ScienceNodes:
                    Debug.LogWarning("Dynamic Science Nodes calculation not yet implemented. Returning placeholder value.");
                    return 5; // Placeholder
                case DailyRewardSO.RewardType.QuantumMultiplier:
                     Debug.LogWarning("Dynamic Quantum Multiplier not yet implemented. Returning placeholder value.");
                    return 1; // Placeholder
            }
        }

        return 0;
    }
}
