using UnityEngine;
using BreakInfinity; 
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Research", menuName = "Research/Research Definition")]
public class ResearchDefinition : ScriptableObject
{
    [Header("Identità")]
    public string id;
    public string title;
    [TextArea] public string description;
    
    // Icona e Tipo Valuta
    public Sprite icon;             
    public CurrencyType costType;   // Che valuta usa? (Energy, Iridium...)

    [Header("Logica Effetto")]
    public ResearchType type;
    public ResearchTarget target;
    public double bonusValue; 

    [Tooltip("SE VERO: Il bonus si moltiplica esponenzialmente (es. Bonus 10 al liv 3 = 1000x).\nSE FALSO (Default): Il bonus si somma (es. Bonus 0.10 al liv 3 = +30% ovvero 1.3x).")]
    public bool isExponentialBonus = false; // <--- NUOVO CAMPO

    [Header("Logica Costi")]
    public CostCurve costCurve = CostCurve.Exponential; // Default Esponenziale

    public BigDouble baseCost; 
    public float costFactor;
    public int maxLevel;
    
    // Lista per costi manuali (opzionale)
    public List<BigDouble> manualCosts = new List<BigDouble>();

    // Metodo helper per calcolare il costo attuale
    public BigDouble GetCost(int currentLevel)
    {
        if (currentLevel < manualCosts.Count)
        {
            return manualCosts[currentLevel];
        }
        
        BigDouble calculationBase = manualCosts.Count > 0 ? manualCosts[manualCosts.Count - 1] : baseCost;
        int levelsBeyond = manualCosts.Count > 0 ? currentLevel - (manualCosts.Count - 1) : currentLevel;
        
        if (costCurve == CostCurve.Exponential)
            return calculationBase * BigDouble.Pow(costFactor, levelsBeyond);
        else
            return calculationBase + (costFactor * levelsBeyond);
    }
}