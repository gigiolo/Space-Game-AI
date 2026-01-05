using UnityEngine;
using System.Collections.Generic;
using BreakInfinity;

public class ResearchLoader : MonoBehaviour
{
    public ResearchManager researchManager; 

    [ContextMenu("Populate Full Research List")]
    public void PopulateResearches()
    {
        if (researchManager == null) 
            researchManager = GetComponent<ResearchManager>() ?? FindFirstObjectByType<ResearchManager>();
            
        if (researchManager == null) 
        {
            Debug.LogError("ResearchManager non trovato! Collega lo script.");
            return;
        }
        
        researchManager.allResearches = new List<ResearchItem>();

        // --- TIER 1: BASE (Tecnologia Terrestre) ---
        
        // 1. ESPANSIONE HABITAT (Fondamentale ora che il Cap iniziale è 1)
        CreateResearch("habitat_1", 
            "Espansione Habitat", 
            "Aumenta il numero massimo di emettitori (+1).", 
            25, 
            10, 
            1.5f, 
            ResearchTarget.EmitterMaxCap, 
            ResearchType.Additive, 
            1 // +1 Slot per livello
        );

        // 2. NANOBOT (Velocità Produzione)
        CreateResearch("auto_emit_1", 
            "Nanobot Assemblatori", 
            "Produzione automatica Emettitori (+0.1/s).", 
            50, 
            10, 
            1.5f, 
            ResearchTarget.EmitterProductionSpeed, 
            ResearchType.Additive, 
            0.1 
        );
        
        CreateResearch("t1_cable", "Cavi in Rame", "Aumenta capacità logistica (+5).", 
            10, 50, 1.15f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 5);

        CreateResearch("t1_batt", "Condensatori", "Aumenta stoccaggio base (+50).", 
            15, 20, 1.20f, ResearchTarget.StorageCapacity, ResearchType.Additive, 50);

        CreateResearch("t1_turb", "Turbine Gas", "Aumenta produzione base (+10%).", 
            25, 30, 1.18f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.10);

        CreateResearch("t1_maint", "Manutenzione", "Ottimizza efficienza globale (+2%).", 
            500, 10, 1.50f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.02);

        CreateResearch("t1_iso", "Isolamento Cavi", "Riduce dispersione (+5 Cap).", 
            120, 50, 1.14f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 5);

        CreateResearch("t1_litio", "Batterie Litio", "Accumulatori chimici (+200 Cap).", 
            250, 30, 1.16f, ResearchTarget.StorageCapacity, ResearchType.Additive, 200);

        CreateResearch("t1_soft", "Software Rete", "Gestione flussi migliorata (+8 Cap).", 
            350, 20, 1.25f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 8);

        CreateResearch("t1_solar", "Pannelli Flex", "Cattura luce residua (+15%).", 
            600, 50, 1.15f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.15);


        // --- TIER 2: ORBITALE (Migliaia) ---
        BigDouble t2 = 5000;

        CreateResearch("t2_super", "Superconduttori", "Logistica a bassa resistenza (+10).", 
            t2, 50, 1.15f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 10);

        CreateResearch("t2_solid", "Stato Solido", "Batterie dense (+1500).", 
            t2 * 1.5, 40, 1.18f, ResearchTarget.StorageCapacity, ResearchType.Additive, 1500);

        CreateResearch("t2_fiss", "Reattori Modulari", "Produzione nucleare (+25%).", 
            t2 * 2.5, 50, 1.16f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.25);

        CreateResearch("t2_smart", "Griglia Smart", "Moltiplicatore Efficienza (+5%).", 
            t2 * 10, 10, 2.0f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.05);
        
        Debug.Log("<color=green>DATABASE RICERCHE GENERATO:</color> Importati " + researchManager.allResearches.Count + " elementi.");
    }

    // Funzione helper
    void CreateResearch(string id, string name, string desc, BigDouble baseCost, int maxLvl, float costMult, ResearchTarget target, ResearchType type, double bonusVal)
    {
        ResearchItem item = new ResearchItem();
        item.id = id;
        item.title = name;
        item.description = desc;
        
        item.baseCost = baseCost;
        item.maxLevel = maxLvl;
        item.costGrowth = costMult;
        
        item.target = target;
        item.type = type;
        item.bonusValue = bonusVal; 

        item.currentLevel = 0; 

        researchManager.allResearches.Add(item);
    }
}