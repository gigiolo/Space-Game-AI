using UnityEngine;

// Definisce come scala il costo
public enum CostCurve
{
    Linear,
    Exponential,
    Fixed
}

// Definisce il tipo di risorsa
public enum CurrencyType
{
    Energy,
    ScientificNodes,
    ExoticMatter
}

public enum ResearchType
{
    Additive,
    Multiplier
}

public enum ResearchTarget
{
    GlobalProduction,
    ClickPower,
    EmitterMaxCap,
    LogisticsCapacity,
    EmitterProductionSpeed,
    ClickProductionPercent, // Aggiunto per futuri usi
    StorageCapacity         // <--- AGGIUNTO (Fix Errore CS0117)
}