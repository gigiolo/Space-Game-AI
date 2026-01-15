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
    public int rewardNotificationCount;
    
    // DAILY GIFT
    public string dailyGiftLastClaimedTimestamp;
    public int dailyGiftCurrentDayIndex;

    // --- PLANET TRAVEL (Nuovi campi aggiunti) ---
    public int currentPlanetIndex;
    public bool isPreparingForLaunch;
    public string launchPreparationProgress; // Salviamo come stringa perché è un BigDouble
    public bool isTraveling;
    public string travelStartTimeBinary;     // Salviamo il tempo come stringa binaria
}

[Serializable]
public class ResearchSaveData
{
    public string id;
    public int level;
}