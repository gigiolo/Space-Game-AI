using UnityEngine;
using System.Collections.Generic;
using BreakInfinity;

public class ResearchManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel;        
    public Transform listContent;        
    public ResearchSlotUI slotPrefab;    

    [Header("Database (Trascina qui i file ricerca)")]
    public List<ResearchDefinition> researchDatabase; 

    [Header("Stato Runtime (Si riempie da solo)")]
    public List<ResearchItem> allResearches;

    private void Start()
    {
        if(menuPanel) menuPanel.SetActive(false); 
        
        // Inizializza se non è stato già fatto dal LoadGame
        if (allResearches == null || allResearches.Count == 0)
            InitializeDatabase();

        InitializeUI();
        
        if(GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated += UpdateAllSlots;
    }
    
    private void OnDestroy()
    {
        if(GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated -= UpdateAllSlots;
    }

    // Trasforma i file SO in oggetti di gioco
    public void InitializeDatabase()
    {
        if (allResearches == null) allResearches = new List<ResearchItem>();

        foreach (var def in researchDatabase)
        {
            // Evita duplicati se chiamiamo questa funzione più volte
            if (!allResearches.Exists(r => r.id == def.id))
            {
                allResearches.Add(new ResearchItem(def));
            }
        }
    }

    // Chiamata dal GameManager quando carica il salvataggio
    public void LoadResearchLevels(List<ResearchSaveData> savedData)
    {
        InitializeDatabase(); // Assicura che la lista esista
        
        // Reset a 0
        foreach(var res in allResearches) res.currentLevel = 0;

        // Applica salvataggi
        if (savedData != null)
        {
            foreach (var saved in savedData)
            {
                var item = allResearches.Find(r => r.id == saved.id);
                if (item != null) item.currentLevel = saved.level;
            }
        }
        RecalculateAllResearches();
    }

    void InitializeUI()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);

        foreach (var research in allResearches)
        {
            GameObject newSlot = Instantiate(slotPrefab.gameObject, listContent);
            newSlot.transform.localScale = Vector3.one; 
            newSlot.GetComponent<ResearchSlotUI>().Setup(research, OnBuyResearch);
        }
    }

    void OnBuyResearch(ResearchItem item)
    {
        if (item.IsMaxed()) return;

        BigDouble cost = item.GetCost();

        if (GameManager.Instance.TrySpend(cost))
        {
            item.currentLevel++;
            RecalculateAllResearches(); 
        }
    }

    void UpdateAllSlots()
    {
        if(!menuPanel.activeSelf) return;
        foreach(Transform child in listContent)
            child.GetComponent<ResearchSlotUI>().RefreshUI();
    }

    public void ToggleMenu()
    {
        menuPanel.SetActive(!menuPanel.activeSelf);
        if(menuPanel.activeSelf) UpdateAllSlots();
    }

    public void RecalculateAllResearches()
    {
        if (GameManager.Instance == null) return;

        // 1. Reset Bonus
        GameManager.Instance.ResearchMultiplier = 1;
        GameManager.Instance.LogisticsResearchBonus = 0;
        GameManager.Instance.StorageResearchBonus = 0;
        GameManager.Instance.EmitterAutoGrowthSpeed = 0;
        GameManager.Instance.EmitterCapResearchBonus = 0; 
        
        // 2. Ricalcola
        foreach (var item in allResearches)
        {
            if (item.currentLevel > 0) ApplyEffectBasedOnTotalLevel(item);
        }
        
        // 3. Aggiorna Caps
        GameManager.Instance.UpdateCapsFromResearch();
        UpdateAllSlots();
    }

    void ApplyEffectBasedOnTotalLevel(ResearchItem item)
    {
        // NOTA: item.target e item.type ora vengono letti dal SO tramite ResearchItem
        if (item.target == ResearchTarget.GlobalProduction && item.type == ResearchType.Multiplier)
        {
            BigDouble totalMult = BigDouble.Pow(1 + item.bonusValue, item.currentLevel);
            GameManager.Instance.ResearchMultiplier *= totalMult;
        }
        else if (item.type == ResearchType.Additive)
        {
            double totalAdditive = item.bonusValue * item.currentLevel;

            if (item.target == ResearchTarget.LogisticsCapacity)
                GameManager.Instance.LogisticsResearchBonus += totalAdditive;
            
            else if (item.target == ResearchTarget.StorageCapacity)
                GameManager.Instance.StorageResearchBonus += totalAdditive;
            
            else if (item.target == ResearchTarget.EmitterProductionSpeed)
                GameManager.Instance.EmitterAutoGrowthSpeed += totalAdditive;
                
            else if (item.target == ResearchTarget.EmitterMaxCap)
                GameManager.Instance.EmitterCapResearchBonus += (int)totalAdditive;
        }
    }
}