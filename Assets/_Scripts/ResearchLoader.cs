using UnityEngine;
using System.Collections.Generic;
using BreakInfinity;

public class ResearchLoader : MonoBehaviour
{
    public ResearchManager researchManager; 

    // Esegui questo metodo facendo clic destro sul componente nell'Inspector!
    [ContextMenu("Populate Full Research List")]
    public void PopulateResearches()
    {
        // Se non è collegato, cerchiamolo usando il NUOVO comando (fix warning CS0618)
        if (researchManager == null) 
            researchManager = GetComponent<ResearchManager>() ?? FindFirstObjectByType<ResearchManager>();
            
        if (researchManager == null) 
        {
            Debug.LogError("ResearchManager non trovato! Collega lo script.");
            return;
        }
        
        // Pulisci la lista esistente per evitare duplicati
        researchManager.allResearches = new List<ResearchItem>();

        // --- TIER 1: BASE (Tecnologia Terrestre) ---
        
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

        CreateResearch("t2_micro", "Trasmissione Microonde", "Logistica wireless (+12).", 
            t2 * 4, 40, 1.17f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 12);

        CreateResearch("t2_fly", "Volani Inerziali", "Stoccaggio cinetico (+3000).", 
            t2 * 5, 30, 1.20f, ResearchTarget.StorageCapacity, ResearchType.Additive, 3000);

        CreateResearch("t2_liq", "Raffreddamento Liquido", "Overclock sistemi (+30%).", 
            t2 * 8, 40, 1.15f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.30);


        // --- TIER 3: INTERPLANETARIO (Milioni) ---
        BigDouble t3 = 1000000; // 1M

        CreateResearch("t3_nano", "Nanotubi Carbonio", "Logistica ultra-leggera (+15).", 
            t3, 80, 1.12f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 15);

        CreateResearch("t3_fus", "Fusione Fredda", "Produzione pulita massiva (+50%).", 
            t3 * 2, 50, 1.15f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.50);

        CreateResearch("t3_data", "Banche Dati", "Energia digitalizzata (+50k).", 
            t3 * 1.5, 50, 1.14f, ResearchTarget.StorageCapacity, ResearchType.Additive, 50000);

        CreateResearch("t3_mirr", "Specchi Orbitali", "Riflessione solare (+40%).", 
            t3 * 5, 40, 1.18f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.40);

        CreateResearch("t3_ai", "AI Quantistica", "Logistica predittiva (+20).", 
            t3 * 8, 30, 1.25f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 20);

        CreateResearch("t3_dil", "Cristalli Dilitio", "Reazioni stabili (+60%).", 
            t3 * 15, 20, 1.30f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 0.60);

        CreateResearch("t3_0g", "Protocollo Zero-G", "Boost efficienza spaziale (x2).", 
            t3 * 50, 5, 3.0f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 1.0); 


        // --- TIER 4: STELLARE (Miliardi) ---
        BigDouble t4 = 1000000000; // 1B

        CreateResearch("t4_dyson", "Sfera Dyson", "Cattura output stellare (+200%).", 
            t4, 100, 1.10f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 2.0);

        CreateResearch("t4_worm", "Ponti Einstein-Rosen", "Logistica istantanea (+50).", 
            t4 * 3, 50, 1.15f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 50);

        CreateResearch("t4_flux", "Condensatori Flusso", "Stoccaggio temporale (+1M).", 
            t4 * 4, 50, 1.16f, ResearchTarget.StorageCapacity, ResearchType.Additive, 1000000);

        CreateResearch("t4_res", "Risonanza Armonica", "Vibrazioni energetiche (+300%).", 
            t4 * 10, 60, 1.14f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 3.0);

        CreateResearch("t4_grav", "Tether Gravitazionali", "Fionda gravitazionale (+60).", 
            t4 * 8, 40, 1.18f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 60);

        CreateResearch("t4_black", "Reattori Buco Nero", "Estrazione orizzonte eventi (+500%).", 
            t4 * 20, 50, 1.15f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 5.0);


        // --- TIER 5: GALATTICO (Quadrilioni 1e15) ---
        BigDouble t5 = BigDouble.Parse("1e15");

        CreateResearch("t5_ent", "Entanglement", "Logistica ovunque (+100).", 
            t5, 50, 1.15f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 100);

        CreateResearch("t5_zero", "Energia Punto Zero", "Estrazione dal vuoto (+1000%).", 
            t5 * 5, 100, 1.12f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 10.0);

        CreateResearch("t5_time", "Chrono-Boost", "Moltiplicatore Temporale (x5).", 
            t5 * 100, 10, 4.0f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 4.0);

        CreateResearch("t5_matt", "Conversione Materia", "Tutto è energia (+2000%).", 
            t5 * 10, 80, 1.14f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 20.0);

        CreateResearch("t5_net", "Rete Galattica", "Universo connesso (+200).", 
            t5 * 20, 40, 1.20f, ResearchTarget.LogisticsCapacity, ResearchType.Additive, 200);

        CreateResearch("t5_real", "Matrice Realtà", "Salva nella realtà (+1B).", 
            t5 * 30, 50, 1.18f, ResearchTarget.StorageCapacity, ResearchType.Additive, 1000000000);

        CreateResearch("t5_asc", "Ascensione", "Oltre la materia (Moltiplicatore x100).", 
            BigDouble.Parse("1e20"), 1, 1.0f, ResearchTarget.GlobalProduction, ResearchType.Multiplier, 99.0);

        
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