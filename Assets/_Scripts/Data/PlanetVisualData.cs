using UnityEngine;

[CreateAssetMenu(fileName = "Visual_NewPlanet", menuName = "Aetheris/Planet Visual Data")]
public class PlanetVisualData : ScriptableObject
{
    [Header("Geometria")]
    [Range(2, 6)] public int resolution = 4; // 4 è ottimo per mobile
    public float radius = 1.6f; // Combacia con il tuo default attuale

    [Header("Superficie")]
    public Texture2D albedoMap;
    public Texture2D normalMap;
    public Texture2D emissionMap; // Per le luci città statiche (opzionale)
    public Color tintColor = Color.white;

    [Header("Atmosfera")]
    public bool hasAtmosphere = true;
    public Color atmosphereColor = new Color(0.3f, 0.6f, 1f, 1f);
    public float atmosphereHeight = 1.2f; // +20% del raggio
    public float fresnelPower = 4.0f;

    [Header("Nuvole")]
    public bool hasClouds = false;
    public Texture2D cloudTexture; // Texture bianca con sfondo trasparente
    public Color cloudColor = new Color(1f, 1f, 1f, 0.9f); // Bianco quasi opaco
    [Tooltip("Scala relativa al raggio (1.0 = superficie). Consigliato: 1.02 - 1.05")]
    public float cloudHeight = 1.03f; 
    [Tooltip("Velocità di rotazione in gradi al secondo (Usa valori negativi per contro-ruotare)")]
    public float cloudRotationSpeed = -5f; 
}