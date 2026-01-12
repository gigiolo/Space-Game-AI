using UnityEngine;
using BreakInfinity;

[CreateAssetMenu(fileName = "NewDailyReward", menuName = "Aetheris/Daily Reward", order = 1)]
public class DailyRewardSO : ScriptableObject
{
    public enum RewardType
    {
        Energy,
        PremiumCurrency,
        ScienceNodes,
        QuantumMultiplier
    }

    [Header("Reward Configuration")]
    [Tooltip("The type of reward.")]
    public RewardType type;

    [Tooltip("A brief description of the reward for the UI.")]
    public string description;
    
    [Tooltip("Icon to display in the UI.")]
    public Sprite icon;

    [Header("Reward Value")]
    [Tooltip("Is the reward amount calculated dynamically (e.g., based on production)?")]
    public bool isDynamic;

    [Tooltip("The static amount for the reward. Only used if isDynamic is false.")]
    public BigDouble staticAmount;

    [Tooltip("The multiplier for dynamic rewards. E.g., 20 for 20 seconds of production. Only used if isDynamic is true.")]
    public double dynamicMultiplier;
}
