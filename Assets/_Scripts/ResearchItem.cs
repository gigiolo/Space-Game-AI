using UnityEngine;
using BreakInfinity;

public enum ResearchType
{
    Multiplier,      // Moltiplica (x1.10)
    Additive,        // Aggiunge (+10)
    Unlock           // Sblocca funzionalità
}

public enum ResearchTarget
{
    GlobalProduction,
    Emitter,                // (Non usato in questa lista, ma tienilo)
    LogisticsCapacity,      // Usato
    StorageCapacity,        // Usato
    ClickPower,             // NUOVO
    EmitterProductionSpeed, // NUOVO
    EmitterMaxCap           // AGGIUNTO: Fondamentale per far compilare il codice
}

[System.Serializable]
public class ResearchItem
{
    [Header("Identificativi")]
    public string id;               
    public string title;            
    [TextArea] public string description;        
    public Sprite icon;              
    
    [Header("Configurazione Effetto")]
    public ResearchType type;        
    public ResearchTarget target;    
    
    public double bonusValue;        

    [Header("Economia")]
    public BigDouble baseCost;       
    public double costGrowth = 1.50d;

    [Header("Progresso")]
    public int currentLevel;        
    public int maxLevel;            
    
    public BigDouble GetCost()
    {
        BigDouble cost = baseCost > 0 ? baseCost : 10;
        return cost * BigDouble.Pow(costGrowth, currentLevel);
    }

    public bool IsMaxed() => maxLevel > 0 && currentLevel >= maxLevel;
}