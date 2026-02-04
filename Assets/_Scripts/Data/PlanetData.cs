using UnityEngine;
using BreakInfinity;

[CreateAssetMenu(fileName = "NewPlanetData", menuName = "Aetheris/Planet Data")]
public class PlanetData : ScriptableObject
{
    [Header("Planet Info")]
    [Tooltip("The name of the planet.")]
    public string planetName;

    [Tooltip("L'icona da mostrare nella schermata di caricamento.")]
    public Sprite planetIcon;

    [Tooltip("The name of the scene to load for this planet.")]
    public string sceneName;

    [Header("Audio")] // <--- NUOVO
    [Tooltip("La musica di sottofondo per questo pianeta.")]
    public AudioClip planetThemeMusic;

    // --- NUOVA SEZIONE PER GRAFICA PROCEDURALE ---
    [Header("Procedural Generation")]
    [Tooltip("Se assegnato, il pianeta verrà generato via codice usando questi dati visivi.")]
    public PlanetVisualData visualData;
    // ---------------------------------------------

    [Header("Travel Mechanics")]
    [Tooltip("Distanza dal pianeta precedente.")]
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