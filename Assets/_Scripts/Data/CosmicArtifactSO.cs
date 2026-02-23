using UnityEngine;

public enum ArtifactBonusType
{
    StorageCapacity,
    EmitterGrowthSpeed,
    GlobalIncome
}

[CreateAssetMenu(fileName = "NewArtifact", menuName = "Aetheris/Cosmic Artifact")]
public class CosmicArtifactSO : ScriptableObject
{
    [Header("Identità")]
    public string id; // Es: "art_blackhole"
    public string artifactName;
    
    [TextArea(3,5)] 
    [Tooltip("Il testo narrativo che appare nel log di viaggio.")]
    public string discoveryLog; 

    public Sprite icon; // Per il museo in futuro

    [Header("Effetto Passivo Permanente")]
    public ArtifactBonusType bonusType;
    
    [Tooltip("Es: 0.10 significa +10% al calcolo finale")]
    public double bonusValue; 
}