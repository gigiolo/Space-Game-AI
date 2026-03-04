using UnityEngine;
using BreakInfinity;

[System.Serializable]
public class ResearchItem
{
    // Riferimento al "Passaporto" (ScriptableObject)
    public ResearchDefinition info;

    // L'unica cosa che cambia mentre giochi
    public int currentLevel;

    // --- PROPRIETA' HELPER (Leggono dal Passaporto) ---
    public string id => info != null ? info.id : "null";
    public string title => info != null ? info.title : "Error";
    public string description => info != null ? info.description : "";
    public Sprite icon => info != null ? info.icon : null;
    public int tier => info != null ? info.tier : 1; // <--- NUOVO HELPER
    public ResearchType type => info != null ? info.type : ResearchType.Additive;
    public ResearchTarget target => info != null ? info.target : ResearchTarget.GlobalProduction;
    public double bonusValue => info != null ? info.bonusValue : 0;
    public int maxLevel => info != null ? info.maxLevel : 0;

    // Costruttore
    public ResearchItem(ResearchDefinition def)
    {
        info = def;
        currentLevel = 0;
    }

    // --- CALCOLO COSTO AVANZATO (Egg Inc Style) ---
    public BigDouble GetCost()
    {
        if (info == null) return 0;

        // 1. CONTROLLO MANUALE (Se siamo nei primi livelli definiti a mano)
        if (info.manualCosts != null && currentLevel < info.manualCosts.Count)
        {
            return info.manualCosts[currentLevel];
        }

        // 2. CALCOLO AUTOMATICO (Se abbiamo superato i livelli manuali)
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
            return startValue * BigDouble.Pow(info.costFactor, levelsBeyondManual);
        }
        else 
        {
            return startValue + (info.costFactor * levelsBeyondManual);
        }
    }

    public bool IsMaxed() => maxLevel > 0 && currentLevel >= maxLevel;
}