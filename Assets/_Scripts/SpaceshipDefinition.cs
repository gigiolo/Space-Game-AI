using UnityEngine;
using System.Collections.Generic;
using BreakInfinity;

[CreateAssetMenu(fileName = "NewSpaceship", menuName = "Aetheris/Spaceship Definition")]
public class SpaceshipDefinition : ScriptableObject
{
    [Header("Identificativi")]
    public string id;
    public string title;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Statistiche Base")]
    [Tooltip("La velocità che ottieni sbloccando la nave (Livello 1).")]
    public double baseSpeed = 100f;

    [Header("Potenziamenti")]
    public SpaceshipUpgradeType upgradeType;
    [Tooltip("Quanto aumenta la velocità per ogni livello DOPO il primo.")]
    public double upgradeValue; 

    [Header("Economia")]
    public SpaceshipCurrency currencyType = SpaceshipCurrency.Energy;
    
    [Header("Costi (Stile Egg Inc)")]
    [Tooltip("Prezzi manuali per i primi livelli (incluso lo sblocco).")]
    public List<double> manualCosts = new List<double>();
    
    public CostCurve costCurveType = CostCurve.Exponential;
    public double baseCost = 1000; 
    public double costFactor = 1.5d;
    
    [Tooltip("0 = Infinito")]
    public int maxLevel = 0;
}