// --- File: _Scripts\Data\DroneMissionSO.cs ---
using UnityEngine;

[CreateAssetMenu(fileName = "NewDroneMission", menuName = "Aetheris/Drone Mission")]
public class DroneMissionSO : ScriptableObject
{
    public string id;
    public string missionName;
    
    [TextArea(2,4)]
    public string description; // <--- ECCOLA QUI! Era lei la grande assente!
    
    [Header("Costi e Tempi")]
    [Tooltip("Costo fisso in Energia. Usa stringhe per numeri enormi, es: 50000 o 1.5e6")]
    public string fixedEnergyCost = "1000"; 
    public int durationSeconds = 60;
    
    [Header("Ricompense")]
    public float minRewardMult = 0.5f;
    public float maxRewardMult = 1.5f;
    
    public float artifactChance = 20f;
    public float iridiumChance = 10f;
    public int minIridium = 1;
    public int maxIridium = 5;
    
    [Header("Log di Viaggio")]
    public float minLightYears = 0.1f;
    public float maxLightYears = 1.5f;

    [Header("Logistica e Carico")]
    [Tooltip("Quanti pacchetti dati indipendenti può estrarre al massimo questa sonda?")]
    public int cargoCapacity = 1; 
}