using UnityEngine;
using BreakInfinity;

[CreateAssetMenu(fileName = "NewPlanetData", menuName = "Aetheris/Planet Data")]
public class PlanetData : ScriptableObject
{
    [Header("Planet Info")]
    [Tooltip("The name of the planet.")]
    public string planetName;

    [Tooltip("The name of the scene to load for this planet.")]
    public string sceneName;

    [Header("Travel Mechanics")]
    [Tooltip("Distanza dal pianeta precedente. Usata per calcolare il tempo di viaggio (Distanza / Velocità Nave). Imposta a 0 per il primo pianeta.")]
    public BigDouble travelDistance;

    [Header("Progression")]
    [Tooltip("The Planet Value required to unlock travel to the next planet.")]
    public BigDouble requiredPlanetValue;

    [Header("Economic Balance")]
    [Tooltip("Base multiplier for energy production on this planet.")]
    public BigDouble productionMultiplier = 1;

    [Tooltip("A balancing factor for the Planet Value calculation.")]
    public BigDouble balanceFactor = 1;
}