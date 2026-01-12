using System;
using System.Collections.Generic; 

[Serializable]
public class SaveData
{
    public string lastSaveTime;

    // ECONOMIA
    public string currentEnergy;
    public string lifetimeEarnings;
    public int emitterCount;
    public int logisticsLevel;
    
    // RICERCHE
    public List<ResearchSaveData> researches = new List<ResearchSaveData>();
    
    // PERMANENTI
    public string scientificNodes; 
    public string totalLifetimeEarnings;

    // VISUALS
    public List<string> cityLightPositions = new List<string>();

    // REWARD NOTIFICATIONS
    public float rewardNotificationTimer;
}

[Serializable]
public class ResearchSaveData
{
    public string id;
    public int level;
}