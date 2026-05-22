// --- File: _Scripts\Data\PhysicalTheorySO.cs ---
using UnityEngine;
using BreakInfinity;

public enum TheoryBonusType
{
    StorageCapacity,
    EmitterGrowthSpeed,
    GlobalIncome,
    LogisticsCap,
    ScientificNodesGain,
    OfflineProduction,
    AsteroidFrequency,
    AsteroidIridiumReward,
    ResearchCostDiscount,
    SpacecraftAndTravelDiscount,
    DroneCostDiscount // <--- NUOVO BONUS SPECIFICO PER LE SONDE
}

public enum TheoryRarity
{
    Base,
    Avanzata,
    Rivoluzionaria,
    Unificata
}

[CreateAssetMenu(fileName = "NewTheory", menuName = "Aetheris/Physical Theory")]
public class PhysicalTheorySO : ScriptableObject
{
    [Header("Identità")]
    public string id; 
    public string theoryName;
    
    [TextArea(3,5)] 
    public string discoveryLog; 

    public Sprite icon;

    [Header("Ricerca & Gacha")]
    public TheoryRarity rarity;
    public int dropWeight = 100;

    [Header("Economia & Upgrade")]
    public int baseDataRequired = 5;
    public int baseIridiumCost = 10;

    [Header("Effetto")]
    public TheoryBonusType bonusType;
    public double baseBonusValue; 
    public double bonusPerLevel;

    public int GetDataRequiredForLevel(int currentLevel)
    {
        return baseDataRequired * (currentLevel + 1);
    }

    public BigDouble GetIridiumCostForLevel(int currentLevel)
    {
        return baseIridiumCost * (currentLevel + 1);
    }

    public double GetBonusAtLevel(int level)
    {
        if (level <= 0) return 0; 
        return baseBonusValue + (bonusPerLevel * (level - 1));
    }
}