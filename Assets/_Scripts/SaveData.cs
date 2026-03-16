// --- File: _Scripts\SaveData.cs ---
using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string lastSaveTime;

    // --- PRIMA SESSIONE ---
    public bool isFirstSession = true;

    // --- ECONOMIA BASE ---
    public string currentEnergy;
    public string lifetimeEarnings;
    public int emitterCount;
    public int logisticsLevel;
    
    // --- ROTAZIONE ---
    public float sunRotationY;

    // --- RICERCHE & NAVI ---
    public List<ResearchSaveData> researches = new List<ResearchSaveData>();
    public List<ResearchSaveData> spaceships = new List<ResearchSaveData>();

    // --- PERMANENTI (Prestigio) ---
    public string scientificNodes; 
    public string totalLifetimeEarnings;

    // --- VALUTE SPECIALI ---
    public string rawIridium;
    public string pureIridium;
    public string quantumManipulators; 

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

    // --- TEORIE FISICHE ED ESPLORAZIONE ---
    public List<TheorySaveData> discoveredTheories = new List<TheorySaveData>();
    public List<string> activeTheories = new List<string>(); 
    public List<DroneSaveData> activeDrones = new List<DroneSaveData>();
}

[Serializable]
public class ResearchSaveData
{
    public string id;   
    public int level;   
}

[Serializable]
public class DroneSaveData
{
    public int slotIndex;
    public string missionID; 
    public string launchTimeBinary; 
    public string returnTimeBinary; 
}

[Serializable]
public class TheorySaveData
{
    public string id;
    public int level;
    public int accumulatedData; 
}