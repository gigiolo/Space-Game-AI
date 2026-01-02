using System;
using System.Collections.Generic; // Fondamentale per le Liste

[Serializable]
public class SaveData
{
    // --- INFO GENERALI ---
    public string lastSaveTime;

    // --- ECONOMIA DELLA RUN CORRENTE ---
    public string currentEnergy;
    public string lifetimeEarnings;
    public int emitterCount;
    public int logisticsLevel;
    
    // --- RICERCHE ---
    // Questa lista usa la classe definita qui sotto (ResearchSaveData)
    public List<ResearchSaveData> researches = new List<ResearchSaveData>();
    
    // --- DATI PERMANENTI (RESET QUANTISTICO) ---
    public string scientificNodes; 
    public string totalLifetimeEarnings;
}

// QUESTA È LA CLASSE CHE TI MANCAVA:
[Serializable]
public class ResearchSaveData
{
    public string id;
    public int level;
}