using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string lastSaveTime;

    // --- ECONOMIA BASE ---
    public string currentEnergy;
    public string lifetimeEarnings;
    public int emitterCount;
    public int logisticsLevel;
    
    // --- NUOVO: Rotazione Sole/Pianeta ---
    public float sunRotationY; // <--- AGGIUNTO QUI

    // --- RICERCHE & NAVI ---
    public List<ResearchSaveData> researches = new List<ResearchSaveData>();
    public List<ResearchSaveData> spaceships = new List<ResearchSaveData>();

    // --- PERMANENTI (Prestigio) ---
    public string scientificNodes; 
    public string totalLifetimeEarnings;

    // --- VALUTE SPECIALI ---
    public string rawIridium;
    public string pureIridium;

    // --- VISUALS ---
    public List<string> cityLightPositions = new List<string>();

    // --- DAILY GIFT ---
    public string dailyGiftLastClaimedTimestamp;
    public int dailyGiftCurrentDayIndex;

    // --- PLANET TRAVEL ---
    public int currentPlanetIndex;
    public bool isPreparingForLaunch;
    public string launchPreparationProgress;
    public string lockedLaunchRequirement; 
    public bool isTraveling;
    public string travelStartTimeBinary;
    public double lockedTravelDuration; 

    // --- LAUNCH SITE ---
    public string launchSitePosition; 
}

[Serializable]
public class ResearchSaveData
{
    public string id;   
    public int level;   
}