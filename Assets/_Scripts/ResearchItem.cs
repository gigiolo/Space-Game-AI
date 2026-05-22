// --- File: _Scripts\ResearchItem.cs ---
using UnityEngine;
using BreakInfinity;

[System.Serializable]
public class ResearchItem
{
    public ResearchDefinition info;
    public int currentLevel;

    public string id => info != null ? info.id : "null";
    public string title => info != null ? info.title : "Error";
    public string description => info != null ? info.description : "";
    public Sprite icon => info != null ? info.icon : null;
    public int tier => info != null ? info.tier : 1;
    public ResearchType type => info != null ? info.type : ResearchType.Additive;
    public ResearchTarget target => info != null ? info.target : ResearchTarget.GlobalProduction;
    public double bonusValue => info != null ? info.bonusValue : 0;
    public int maxLevel => info != null ? info.maxLevel : 0;

    public ResearchItem(ResearchDefinition def)
    {
        info = def;
        currentLevel = 0;
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

            if (info.costCurve == CostCurve.Exponential)
            {
                finalCost = startValue * BigDouble.Pow(info.costFactor, levelsBeyondManual);
            }
            else 
            {
                finalCost = startValue + (info.costFactor * levelsBeyondManual);
            }
        }

        // --- APPLICAZIONE BONUS TEORIA: Sconto Ricerche ---
        double discount = DroneManager.Instance != null ? DroneManager.Instance.GetTheoryBonus(TheoryBonusType.ResearchCostDiscount) : 0;
        discount = System.Math.Min(discount, 0.99); // Hard cap al 99% di sconto
        
        return finalCost * (1.0 - discount);
    }

    public bool IsMaxed() => maxLevel > 0 && currentLevel >= maxLevel;
}