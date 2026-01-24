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
    
    // --- RICERCHE & NAVI ---
    // Usano la classe definita in fondo a questo file
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
    
    // Durata bloccata del viaggio (calcolata alla partenza)
    public double lockedTravelDuration; 

    // --- NUOVO: Posizione salvata del sito di lancio ---
    // Memorizza la coordinata "X|Y|Z" della particella lampeggiante
    public string launchSitePosition; 
}

// =================================================================================
// CLASSE HELPER PER SALVARE LISTE DI OGGETTI (Ricerche, Navi, ecc.)
// Deve stare fuori dalla classe SaveData per essere vista dagli altri script.
// =================================================================================
[Serializable]
public class ResearchSaveData
{
    public string id;   // ID univoco (es. "res_production_1")
    public int level;   // Livello attuale
}