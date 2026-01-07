using UnityEngine;
using System.Collections.Generic;

// Tipi di curva per quando finiscono i prezzi manuali
public enum CostCurve
{
    Exponential, // Moltiplica (x1.5, x2.0...) -> Standard
    Linear       // Somma (+10, +50...) -> Per slot o cap
}

[CreateAssetMenu(fileName = "NewResearch", menuName = "IdleGame/Research Definition")]
public class ResearchDefinition : ScriptableObject
{
    [Header("Identificativi")]
    public string id;
    public string title;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effetto")]
    public ResearchType type;
    public ResearchTarget target;
    public double bonusValue;

    [Header("Economia Avanzata (Stile Egg Inc)")]
    [Tooltip("Inserisci qui i prezzi esatti per i primi livelli. Es: 100, 500, 1000")]
    public List<double> manualCosts = new List<double>();

    [Header("Economia Automatica (Dopo la lista manuale)")]
    public CostCurve costType = CostCurve.Exponential;
    
    [Tooltip("Costo base usato SOLO se la lista manuale è vuota")]
    public double baseCost = 10; 
    
    [Tooltip("Moltiplicatore (es. 1.5) o Addendo (es. 10) per i livelli successivi")]
    public double costFactor = 1.50d; 
    
    [Tooltip("0 = Infinito")]
    public int maxLevel = 0;
}