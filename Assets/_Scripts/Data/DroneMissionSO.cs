using UnityEngine;

[CreateAssetMenu(fileName = "NewDroneMission", menuName = "Aetheris/Drone Mission")]
public class DroneMissionSO : ScriptableObject
{
    [Header("Identità")]
    public string id; // Es: "miss_moon_01"
    public string missionName;
    [TextArea(2, 4)] public string description;

    [Header("Parametri Viaggio")]
    [Tooltip("Durata in secondi (es. 300 = 5 minuti)")]
    public double durationSeconds;
    
    [Tooltip("Moltiplicatore sul guadagno al secondo attuale per definire il costo. Es: 60 = costa 1 minuto di produzione.")]
    public double energyCostMultiplier;

    [Header("Senso di Enormità (Distanza)")]
    [Tooltip("Distanza minima in Anni Luce (es. 0.00001 per milioni di km, 4.2 per Alpha Centauri)")]
    public float minLightYears = 0.00001f;
    public float maxLightYears = 0.00005f;

    [Header("Ricompense (Energia)")]
    [Tooltip("Moltiplicatore minimo del premio (Es: se il costo era 60s, e minReward è 2, vinci almeno 120s di energia)")]
    public float minRewardMult = 2.0f;
    public float maxRewardMult = 5.0f;

    [Header("Ricompense (Speciali)")]
    [Tooltip("Percentuale (0-100) di trovare Iridio Grezzo")]
    public int iridiumChance = 20;
    public int minIridium = 1;
    public int maxIridium = 5;

    [Tooltip("Percentuale (0-100) di trovare un Artefatto Cosmico")]
    public float artifactChance = 10f;
}