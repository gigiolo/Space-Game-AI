using UnityEngine;
using BreakInfinity; // Assicurati che BreakInfinity sia installato

[System.Serializable]
public class ResearchItem
{
    public string id;              // ID unico (es. "res_solar_1")
    public string title;           // Nome visibile
    public string description;     // Descrizione
    public Sprite icon;            // Immagine
    
    public BigDouble baseCost;     // Costo iniziale
    public int currentLevel;       // Livello attuale
    public int maxLevel;           // Livello massimo (0 se infinito)
    
    // Formula costo: Base * (1.15 ^ Livello) [cite: 36]
    public BigDouble GetCost()
    {
        // Se BreakInfinity non è riconosciuto, questo darà errore.
        // Assicurati di avere la libreria nel progetto.
        return baseCost * BigDouble.Pow(1.15, currentLevel);
    }
}