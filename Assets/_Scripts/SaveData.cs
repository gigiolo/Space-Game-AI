using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string lastSaveTime;

    // ECONOMIA BASE
    public string currentEnergy;
    public string lifetimeEarnings;
    public int emitterCount;
    public int logisticsLevel;
    
    // RICERCHE
    public List<ResearchSaveData> researches = new List<ResearchSaveData>();
    
    // PERMANENTI (Soft Prestige)
    public string scientificNodes; 
    public string totalLifetimeEarnings;

    // --- NUOVO: VALUTE PREMIUM & PRE-PREMIUM ---
    // Queste persistono attraverso Planet Travel e Quantum Reset
    public string rawIridium;  // Iridio Grezzo (Accumulato in gioco)
    public string pureIridium; // Iridio Puro (Premium / Convertito)
    // -------------------------------------------

    // VISUALS
    public List<string> cityLightPositions = new List<string>();

    // REWARD NOTIFICATIONS
    public float rewardNotificationTimer;
    public int rewardNotificationCount;
    
    // DAILY GIFT
    public string dailyGiftLastClaimedTimestamp;
    public int dailyGiftCurrentDayIndex;

    // --- PLANET TRAVEL ---
    public int currentPlanetIndex;
    public bool isPreparingForLaunch;
    public string launchPreparationProgress;
    public string lockedLaunchRequirement; 
    public bool isTraveling;
    public string travelStartTimeBinary;
}

[Serializable]
public class ResearchSaveData
{
    public string id;
    public int level;
}