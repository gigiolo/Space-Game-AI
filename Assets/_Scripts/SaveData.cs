using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // --- INFO GENERALI ---
    public string lastSaveTime;

    // --- ECONOMIA (Salvati come stringhe per sicurezza con BreakInfinity) ---
    public string currentEnergy;
    public string lifetimeEarnings;
    
    // --- PROGRESSIONE ---
    public int emitterCount;
    public int logisticsLevel;
    
    // --- RICERCHE ---
    // Salviamo ID e Livello per ogni ricerca
    public List<ResearchSaveData> researches = new List<ResearchSaveData>();
}

[Serializable]
public class ResearchSaveData
{
    public string id;
    public int level;
}