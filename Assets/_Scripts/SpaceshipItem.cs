// --- File: _Scripts\SpaceshipItem.cs ---
using UnityEngine;
using BreakInfinity;

[System.Serializable]
public class SpaceshipItem
{
    public SpaceshipDefinition info;
    public int currentLevel;

    public SpaceshipItem(SpaceshipDefinition def)
    {
        info = def;
        currentLevel = 0;
    }

    public BigDouble GetCurrentSpeed()
    {
        if (info == null || currentLevel == 0) return 0;

        BigDouble speed = info.baseSpeed;

        if (currentLevel > 1)
        {
            int upgrades = currentLevel - 1;

            if (info.upgradeType == SpaceshipUpgradeType.Additive)
            {
                speed += (info.upgradeValue * upgrades);
            }
            else if (info.upgradeType == SpaceshipUpgradeType.Multiplier)
            {
                speed *= BigDouble.Pow(info.upgradeValue, upgrades);
            }
        }
        return speed;
    }

    public BigDouble GetCost()
    {
        if (info == null) return 0;

        BigDouble finalCost;

        if (info.manualCosts != null && currentLevel < info.manualCosts.Count)
        {
            finalCost = info.manualCosts[currentLevel];
        }
        else
        {
            BigDouble startValue;
            int levelsBeyondManual;

            if (info.manualCosts != null && info.manualCosts.Count > 0)
            {
                startValue = info.manualCosts[info.manualCosts.Count - 1];
                levelsBeyondManual = currentLevel - (info.manualCosts.Count - 1);
            }
            else
            {
                startValue = info.baseCost;
                levelsBeyondManual = currentLevel;
            }

            if (info.costCurveType == CostCurve.Exponential)
                finalCost = startValue * BigDouble.Pow(info.costFactor, levelsBeyondManual);
            else 
                finalCost = startValue + (info.costFactor * levelsBeyondManual);
        }

        // --- APPLICAZIONE BONUS TEORIA: Sconto Veicoli e Viaggi ---
        double discount = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.SpacecraftAndTravelDiscount) : 0;
        discount = System.Math.Min(discount, 0.99);
        
        return finalCost * (1.0 - discount);
    }

    public bool IsMaxed() => info.maxLevel > 0 && currentLevel >= info.maxLevel;
}