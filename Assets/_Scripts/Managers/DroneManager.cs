// --- File: _Scripts\Managers\DroneManager.cs ---
using UnityEngine;
using System;
using System.Collections.Generic;
using BreakInfinity;
using System.Text;

public class DroneManager : MonoBehaviour
{
    public static DroneManager Instance { get; private set; }

    [Header("Database (Assegna da Editor)")]
    public List<DroneMissionSO> allMissions;
    public List<PhysicalTheorySO> allTheories;

    [Header("Visuals (Assegna da Editor)")]
    public SpaceshipFlight droneLaunchPrefab;
    public SpaceshipLanding droneLandingPrefab;
    public float droneScale = 0.3f;

    [Header("Impostazioni Hangar & Teorie")]
    public int unlockedSlots = 1; 
    public int maxActiveTheories = 3; 

    public class ActiveDrone
    {
        public int slotIndex;
        public DroneMissionSO missionData;
        public DateTime launchTime; 
        public DateTime returnTime;
        public bool isCompleted; 
        
        [NonSerialized] public bool isLanding; 
    }

    public class RuntimeTheory
    {
        public int level = 0; 
        public int accumulatedData = 0;
    }

    public List<ActiveDrone> activeDrones = new List<ActiveDrone>();
    public Dictionary<string, RuntimeTheory> theoryDatabase = new Dictionary<string, RuntimeTheory>();
    public List<string> activeTheoryIDs = new List<string>(); 

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        for (int i = 0; i < activeDrones.Count; i++)
        {
            var drone = activeDrones[i];
            if (!drone.isCompleted && !drone.isLanding && DateTime.UtcNow >= drone.returnTime)
            {
                drone.isLanding = true;
                SpawnVisualDroneLanding(() => 
                {
                    drone.isCompleted = true;
                    drone.isLanding = false;
                });
            }
        }
    }

    public void LoadData(SaveData data)
    {
        activeDrones.Clear();
        theoryDatabase.Clear();
        activeTheoryIDs.Clear();

        if (data.discoveredTheories != null)
        {
            foreach (var t in data.discoveredTheories)
            {
                theoryDatabase[t.id] = new RuntimeTheory { level = t.level, accumulatedData = t.accumulatedData };
            }
        }
        
        if (data.activeTheories != null) activeTheoryIDs.AddRange(data.activeTheories); 

        if (data.activeDrones != null)
        {
            foreach (var savedDrone in data.activeDrones)
            {
                DroneMissionSO mission = allMissions.Find(m => m.id == savedDrone.missionID);
                if (mission != null)
                {
                    long returnBin = long.Parse(savedDrone.returnTimeBinary);
                    long launchBin = string.IsNullOrEmpty(savedDrone.launchTimeBinary) ? returnBin : long.Parse(savedDrone.launchTimeBinary); 
                    bool timeIsUp = DateTime.UtcNow >= DateTime.FromBinary(returnBin);

                    activeDrones.Add(new ActiveDrone
                    {
                        slotIndex = savedDrone.slotIndex,
                        missionData = mission,
                        launchTime = DateTime.FromBinary(launchBin), 
                        returnTime = DateTime.FromBinary(returnBin),
                        isCompleted = timeIsUp, 
                        isLanding = false
                    });
                }
            }
        }
    }

    public void SaveData(SaveData data)
    {
        data.discoveredTheories = new List<TheorySaveData>();
        foreach (var kvp in theoryDatabase)
        {
            data.discoveredTheories.Add(new TheorySaveData { id = kvp.Key, level = kvp.Value.level, accumulatedData = kvp.Value.accumulatedData });
        }

        data.activeTheories = new List<string>(activeTheoryIDs); 
        data.activeDrones = new List<DroneSaveData>();

        foreach (var drone in activeDrones)
        {
            data.activeDrones.Add(new DroneSaveData
            {
                slotIndex = drone.slotIndex,
                missionID = drone.missionData.id,
                launchTimeBinary = drone.launchTime.ToBinary().ToString(),
                returnTimeBinary = drone.returnTime.ToBinary().ToString()
            });
        }
    }

    public bool TryUpgradeTheory(string theoryId)
    {
        if (!theoryDatabase.ContainsKey(theoryId)) return false;

        PhysicalTheorySO info = allTheories.Find(t => t.id == theoryId);
        RuntimeTheory state = theoryDatabase[theoryId];

        int requiredData = info.GetDataRequiredForLevel(state.level);
        BigDouble requiredIridium = info.GetIridiumCostForLevel(state.level);

        if (state.accumulatedData >= requiredData && GameManager.Instance.PureIridium >= requiredIridium)
        {
            state.accumulatedData -= requiredData;
            GameManager.Instance.TrySpendPureIridium(requiredIridium); 
            state.level++;
            GameManager.Instance.SaveGame();
            GameManager.Instance.RecalculateCaps();
            return true;
        }
        return false;
    }

    // --- METODO AGGIORNATO: LANCIO A COSTO FISSO ---
    public void LaunchDrone(int slotIndex, DroneMissionSO mission)
    {
        if (GameManager.Instance == null) return;
        
        // 1. Legge il costo fisso dal SO e lo converte in BigDouble
        BigDouble cost = BigDouble.Parse(mission.fixedEnergyCost);
        
        // 2. Tenta di spendere l'energia
        if (!GameManager.Instance.TrySpend(cost)) return;

        ActiveDrone newDrone = new ActiveDrone
        {
            slotIndex = slotIndex,
            missionData = mission,
            launchTime = DateTime.UtcNow, 
            returnTime = DateTime.UtcNow.AddSeconds(mission.durationSeconds),
            isCompleted = false,
            isLanding = false
        };
        activeDrones.Add(newDrone);

        SpawnVisualDroneLaunch();
        GameManager.Instance.SaveGame();
        
        if (LocalNotificationController.Instance != null)
        {
            LocalNotificationController.Instance.ScheduleNotification(
                "Sonda Rientrata! 🛰️", 
                $"La spedizione {mission.missionName} ha completato l'analisi.", 
                newDrone.returnTime, 
                4000 + slotIndex 
            );
        }
    }

    // --- MODIFICA: Ora restituisce un DIZIONARIO di risultati ---
    public void ClaimDrone(ActiveDrone drone, Action<string, Dictionary<PhysicalTheorySO, int>> onLogReadyCallback)
    {
        if (!drone.isCompleted) return;

        // La ricompensa in energia è basata sul costo fisso della missione
        BigDouble investedEnergy = BigDouble.Parse(drone.missionData.fixedEnergyCost);
        float mult = UnityEngine.Random.Range(drone.missionData.minRewardMult, drone.missionData.maxRewardMult);
        BigDouble energyReward = investedEnergy * mult;

        GameManager.Instance.AddEnergy(energyReward);

        if (UnityEngine.Random.Range(0, 100) < drone.missionData.iridiumChance)
        {
            int iridium = UnityEngine.Random.Range(drone.missionData.minIridium, drone.missionData.maxIridium);
            GameManager.Instance.AddRawIridium(iridium);
        }

        // Il "Bagagliaio" della sonda
        Dictionary<PhysicalTheorySO, int> foundTheories = new Dictionary<PhysicalTheorySO, int>();
        
        int capacity = drone.missionData.cargoCapacity > 0 ? drone.missionData.cargoCapacity : 1;
        for (int i = 0; i < capacity; i++)
        {
            if (UnityEngine.Random.Range(0, 100f) <= drone.missionData.artifactChance)
            {
                PhysicalTheorySO extracted = ExtractTheoryData(out int amount);
                if (extracted != null)
                {
                    if (foundTheories.ContainsKey(extracted))
                        foundTheories[extracted] += amount;
                    else
                        foundTheories[extracted] = amount;
                }
            }
        }

        activeDrones.Remove(drone);
        GameManager.Instance.SaveGame();

        string logText = GenerateFlightLog(drone, foundTheories, energyReward);
        onLogReadyCallback?.Invoke(logText, foundTheories);
    }

    private PhysicalTheorySO ExtractTheoryData(out int dataAmountObtained)
    {
        if (allTheories == null || allTheories.Count == 0) 
        {
            dataAmountObtained = 0;
            return null;
        }

        int totalWeight = 0;
        foreach (var t in allTheories) totalWeight += t.dropWeight;

        int randomVal = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;
        PhysicalTheorySO chosenTheory = null;

        foreach (var t in allTheories)
        {
            currentWeight += t.dropWeight;
            if (randomVal < currentWeight)
            {
                chosenTheory = t;
                break;
            }
        }

        dataAmountObtained = UnityEngine.Random.Range(1, 4);

        if (chosenTheory != null)
        {
            if (!theoryDatabase.ContainsKey(chosenTheory.id))
            {
                theoryDatabase[chosenTheory.id] = new RuntimeTheory();
            }
            theoryDatabase[chosenTheory.id].accumulatedData += dataAmountObtained;
        }
        return chosenTheory;
    }

    private void SpawnVisualDroneLaunch()
    {
        if (droneLaunchPrefab == null) return;
        Vector3 startPos;
        Vector3 startDir;

        SpaceportHub spaceport = UnityEngine.Object.FindFirstObjectByType<SpaceportHub>();
        if (spaceport != null)
        {
            Transform pad = spaceport.GetRandomPad();
            startPos = pad.position;
            startDir = pad.forward;
        }
        else
        {
            if (Camera.main == null) return;
            startPos = (Camera.main.transform.position + Camera.main.transform.forward * 10f).normalized * 1.6f; 
            startDir = startPos.normalized;
        }

        var drone = Instantiate(droneLaunchPrefab);
        drone.transform.localScale = Vector3.one * droneScale;
        drone.Launch(startPos, startDir);
    }

    private void SpawnVisualDroneLanding(Action onLandedCallback)
    {
        if (droneLandingPrefab == null) { onLandedCallback?.Invoke(); return; }

        Vector3 landPos;
        Vector3 spacePos;

        SpaceportHub spaceport = UnityEngine.Object.FindFirstObjectByType<SpaceportHub>();
        if (spaceport != null)
        {
            Transform pad = spaceport.GetRandomPad();
            landPos = pad.position;
            spacePos = landPos + (pad.forward * 15f); 
        }
        else
        {
            if (Camera.main == null) return;
            landPos = (Camera.main.transform.position + Camera.main.transform.forward * 10f).normalized * 1.6f;
            spacePos = Camera.main.transform.position + (Camera.main.transform.up * 8f) - (Camera.main.transform.forward * 2f);
        }

        var drone = Instantiate(droneLandingPrefab);
        drone.transform.localScale = Vector3.one * droneScale;
        drone.BeginLanding(spacePos, landPos, (pos) => { onLandedCallback?.Invoke(); });
    }

    // --- MODIFICA: Il log genera uno scontrino se ci sono più dati ---
    private string GenerateFlightLog(ActiveDrone drone, Dictionary<PhysicalTheorySO, int> foundTheories, BigDouble energyGained)
    {
        float distLY = UnityEngine.Random.Range(drone.missionData.minLightYears, drone.missionData.maxLightYears);
        string distanceString = distLY < 0.0001f ? $"{(distLY * 9460730.0):F1} Million km" : $"{distLY:F4} Light Years";
        string sector = $"SEC-{UnityEngine.Random.Range(10, 99):00}/{((char)UnityEngine.Random.Range(65, 90))}";
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=#888888>> ACCESSING FLIGHT RECORDER...</color>");
        sb.AppendLine($"<color=#FFFFFF>> MISSION ID:</color> {drone.missionData.missionName.ToUpper()}");
        sb.AppendLine($"<color=#FFFFFF>> TARGET SECTOR:</color> {sector}");
        sb.AppendLine($"<color=#00FFFF>> MAX DISTANCE:</color> <b>{distanceString}</b>");
        sb.AppendLine($"<color=#888888>> VOID EXPOSURE:</color> {(drone.missionData.durationSeconds / 60):F0} CYCLES\n");
        
        if (foundTheories != null && foundTheories.Count > 0)
        {
            sb.AppendLine($"<color=#00FF00>> CARICO DATI DECIFRATO:</color>");
            foreach (var kvp in foundTheories)
            {
                PhysicalTheorySO theory = kvp.Key;
                int amount = kvp.Value;
                
                string rarityColor = "#FFFFFF";
                if (theory.rarity == TheoryRarity.Avanzata) rarityColor = "#00FFFF";
                if (theory.rarity == TheoryRarity.Rivoluzionaria) rarityColor = "#A020F0";
                if (theory.rarity == TheoryRarity.Unificata) rarityColor = "#FFD700";

                sb.AppendLine($"<color={rarityColor}>  [+] {amount} TB: {theory.theoryName}</color>");
            }
            sb.AppendLine();
        }
        else
        {
            string[] standardLogs = {
                "> Scansione completata. Silenzio assoluto rilevato.",
                "> Tracce di radiazione cosmica di fondo nei filtri.",
                "> Nessuna anomalia strutturale. Dati insufficienti."
            };
            sb.AppendLine(standardLogs[UnityEngine.Random.Range(0, standardLogs.Length)] + "\n");
        }

        sb.AppendLine($"<color=#00FF00>> RESOURCES EXTRACTED: +{FormatNumber(energyGained)} Energy</color>"); 
        return sb.ToString();
    }
    
    public double GetTheoryBonus(TheoryBonusType type)
    {
        double total = 0;
        foreach (string id in activeTheoryIDs) 
        {
            if (theoryDatabase.TryGetValue(id, out RuntimeTheory state))
            {
                var theory = allTheories.Find(t => t.id == id);
                if (theory != null && theory.bonusType == type) 
                {
                    total += theory.GetBonusAtLevel(state.level);
                }
            }
        }
        return total;
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F0");
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}