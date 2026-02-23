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
    public List<CosmicArtifactSO> allArtifacts;

    [Header("Visuals (Assegna da Editor)")]
    public SpaceshipFlight droneLaunchPrefab;
    public SpaceshipLanding droneLandingPrefab;
    public float droneScale = 0.3f;

    [Header("Impostazioni Hangar & Artefatti")]
    public int unlockedSlots = 1; 
    public int maxEquippedArtifacts = 3; 

    // --- CLASSE RUNTIME ---
    public class ActiveDrone
    {
        public int slotIndex;
        public DroneMissionSO missionData;
        public DateTime launchTime; 
        public DateTime returnTime;
        public bool isCompleted; // True se è a terra e pronto per essere letto
        
        [NonSerialized] public bool isLanding; // True mentre si sta riproducendo l'animazione di atterraggio
    }

    // Stato Runtime
    public List<ActiveDrone> activeDrones = new List<ActiveDrone>();
    public List<string> discoveredArtifactIDs = new List<string>();
    public List<string> equippedArtifactIDs = new List<string>(); 

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
            
            // Se il timer è scaduto, NON ha ancora completato l'atterraggio e NON sta già atterrando...
            if (!drone.isCompleted && !drone.isLanding && DateTime.UtcNow >= drone.returnTime)
            {
                drone.isLanding = true;
                
                // Avvia automaticamente l'animazione visiva
                SpawnVisualDroneLanding(() => 
                {
                    // Quando tocca terra fisicamente, diventa "Pronto al recupero"
                    drone.isCompleted = true;
                    drone.isLanding = false;
                    Debug.Log($"[DroneManager] Drone {drone.slotIndex} atterrato! In attesa di lettura log.");
                });
            }
        }
    }

    // --- GESTIONE SALVATAGGIO ---
    public void LoadData(SaveData data)
    {
        activeDrones.Clear();
        discoveredArtifactIDs.Clear();
        equippedArtifactIDs.Clear();

        if (data.discoveredArtifacts != null) discoveredArtifactIDs.AddRange(data.discoveredArtifacts);
        if (data.equippedArtifacts != null) equippedArtifactIDs.AddRange(data.equippedArtifacts); 

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
                        // Se eravamo offline e il tempo è scaduto, consideriamolo già atterrato e completato
                        isCompleted = timeIsUp, 
                        isLanding = false
                    });
                }
            }
        }
    }

    public void SaveData(SaveData data)
    {
        data.discoveredArtifacts = new List<string>(discoveredArtifactIDs);
        data.equippedArtifacts = new List<string>(equippedArtifactIDs); 
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

    // --- LOGICA EQUIPAGGIAMENTO ARTEFATTI ---
    public bool EquipArtifact(string artifactId)
    {
        if (!discoveredArtifactIDs.Contains(artifactId)) return false; 
        if (equippedArtifactIDs.Contains(artifactId)) return false; 
        if (equippedArtifactIDs.Count >= maxEquippedArtifacts) return false; 

        equippedArtifactIDs.Add(artifactId);
        if (GameManager.Instance != null) GameManager.Instance.RecalculateCaps();
        return true;
    }

    public void UnequipArtifact(string artifactId)
    {
        if (equippedArtifactIDs.Contains(artifactId))
        {
            equippedArtifactIDs.Remove(artifactId);
            if (GameManager.Instance != null) GameManager.Instance.RecalculateCaps();
        }
    }

    // --- CORE LOGIC ---
    public void LaunchDrone(int slotIndex, DroneMissionSO mission)
    {
        if (GameManager.Instance == null) return;
        BigDouble cost = GameManager.Instance.EffectiveIncomePerSec * mission.energyCostMultiplier;
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

    // Metodo chiamato dalla UI per leggere il log. NON lancia più l'animazione.
    public void ClaimDrone(ActiveDrone drone, Action<string, CosmicArtifactSO> onLogReadyCallback)
    {
        if (!drone.isCompleted) return;

        BigDouble income = GameManager.Instance.EffectiveIncomePerSec;
        float mult = UnityEngine.Random.Range(drone.missionData.minRewardMult, drone.missionData.maxRewardMult);
        BigDouble energyReward = income * drone.missionData.energyCostMultiplier * mult;

        GameManager.Instance.AddEnergy(energyReward);

        if (UnityEngine.Random.Range(0, 100) < drone.missionData.iridiumChance)
        {
            int iridium = UnityEngine.Random.Range(drone.missionData.minIridium, drone.missionData.maxIridium);
            GameManager.Instance.AddRawIridium(iridium);
        }

        CosmicArtifactSO foundArtifact = null;
        if (UnityEngine.Random.Range(0, 100f) <= drone.missionData.artifactChance)
        {
            foundArtifact = TryGetNewArtifact();
        }

        activeDrones.Remove(drone);
        GameManager.Instance.SaveGame();

        string logText = GenerateFlightLog(drone, foundArtifact, energyReward);

        // Restituisce subito il testo, la nave è già atterrata in background!
        onLogReadyCallback?.Invoke(logText, foundArtifact);
    }

    private CosmicArtifactSO TryGetNewArtifact()
    {
        if (allArtifacts == null || allArtifacts.Count == 0) return null;

        List<CosmicArtifactSO> shuffled = new List<CosmicArtifactSO>(allArtifacts);
        for (int i = 0; i < shuffled.Count; i++) {
            CosmicArtifactSO temp = shuffled[i];
            int randomIndex = UnityEngine.Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        foreach (var art in shuffled)
        {
            if (!discoveredArtifactIDs.Contains(art.id))
            {
                discoveredArtifactIDs.Add(art.id);
                return art; 
            }
        }
        return null;
    }

    private void SpawnVisualDroneLaunch()
    {
        if (droneLaunchPrefab == null || Camera.main == null) return;
        Vector3 startPos = GetCameraForwardPointOnPlanet(); 
        var drone = Instantiate(droneLaunchPrefab);
        drone.transform.localScale = Vector3.one * droneScale;
        drone.Launch(startPos, startPos.normalized);
    }

    private void SpawnVisualDroneLanding(Action onLandedCallback)
    {
        if (droneLandingPrefab == null || Camera.main == null)
        {
            onLandedCallback?.Invoke();
            return;
        }

        Vector3 landPos = GetCameraForwardPointOnPlanet();
        Vector3 spacePos = Camera.main.transform.position + (Camera.main.transform.up * 8f) - (Camera.main.transform.forward * 2f);

        var drone = Instantiate(droneLandingPrefab);
        drone.transform.localScale = Vector3.one * droneScale;
        
        drone.BeginLanding(spacePos, landPos, (pos) => 
        {
            onLandedCallback?.Invoke();
        });
    }

    private Vector3 GetCameraForwardPointOnPlanet()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit)) return hit.point;
        return (Camera.main.transform.position + Camera.main.transform.forward * 10f).normalized * 1.6f; 
    }

    private string GenerateFlightLog(ActiveDrone drone, CosmicArtifactSO artifact, BigDouble energyGained)
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
        
        if (artifact != null)
        {
            sb.AppendLine($"<color=red>> ALERT: GRAVITATIONAL ANOMALY DETECTED.</color>");
            sb.AppendLine($"<color=#FFaa00>> OBJECT RECOVERED:</color> {artifact.artifactName}");
            sb.AppendLine($"<i>\"{artifact.discoveryLog}\"</i>\n");
        }
        else
        {
            string[] standardLogs = {
                "> Scansione completata. Silenzio assoluto rilevato.",
                "> Tracce di radiazione cosmica di fondo nei filtri.",
                "> Nessuna forma di vita. Parametri orbitali stabili.",
                "> I sensori ottici hanno registrato solo oscurità."
            };
            sb.AppendLine(standardLogs[UnityEngine.Random.Range(0, standardLogs.Length)] + "\n");
        }

        sb.AppendLine($"<color=#00FF00>> RESOURCES EXTRACTED: +{FormatNumber(energyGained)} Energy</color>"); 
        
        return sb.ToString();
    }
    
    public double GetArtifactBonus(ArtifactBonusType type)
    {
        double total = 0;
        foreach (string id in equippedArtifactIDs) 
        {
            var art = allArtifacts.Find(a => a.id == id);
            if (art != null && art.bonusType == type) total += art.bonusValue;
        }
        return total;
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F0");
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}