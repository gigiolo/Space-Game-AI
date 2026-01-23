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
    
    // RICERCHE (Tecnologie)
    public List<ResearchSaveData> researches = new List<ResearchSaveData>();
    
    // SPACESHIPS (Hangar)
    public List<ResearchSaveData> spaceships = new List<ResearchSaveData>();

    // PERMANENTI (Soft Prestige)
    public string scientificNodes; 
    public string totalLifetimeEarnings;

    // VALUTE
    public string rawIridium;
    public string pureIridium;

    // VISUALS
    public List<string> cityLightPositions = new List<string>();

    // DAILY GIFT
    public string dailyGiftLastClaimedTimestamp;
    public int dailyGiftCurrentDayIndex;

    // PLANET TRAVEL
    public int currentPlanetIndex;
    public bool isPreparingForLaunch;
    public string launchPreparationProgress;
    public string lockedLaunchRequirement; 
    public bool isTraveling;
    public string travelStartTimeBinary;

    // --- NUOVO: Tempo di viaggio bloccato al momento della partenza ---
    public double lockedTravelDuration; 
}

// --- QUESTA È LA CLASSE CHE MANCAVA ---
[Serializable]
public class ResearchSaveData
{
    public string id;
    public int level;
}