using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using BreakInfinity;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject menuPanel;            
    public Transform listContent;            
    public ResearchSlotUI slotPrefab;    

    [Header("Database")]
    public List<ResearchDefinition> researchDatabase; 

    [Header("Stato Runtime")]
    public List<ResearchItem> allResearches;
    
    private List<ResearchSlotUI> _activeSlots = new List<ResearchSlotUI>();

    private void Awake() 
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if(menuPanel) 
        {
            menuPanel.SetActive(false);
            if (UIManager.Instance != null)
                UIManager.Instance.RegisterMenu(menuPanel);
        }
        
        // Inizializzazione sicura
        if (allResearches == null || allResearches.Count == 0) 
        {
            InitializeDatabase();
        }
        
        InitializeUI();
        
        if(GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated += UpdateAllSlots;
    }
    
    private void OnDestroy()
    {
        if(GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated -= UpdateAllSlots;
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        bool opening = !menuPanel.activeSelf;

        if (!opening)
        {
            UIPopupEffect effect = menuPanel.GetComponent<UIPopupEffect>();
            if (effect != null) effect.Close();
            else menuPanel.SetActive(false);
        }
        else
        {
            if (UIManager.Instance != null)
                UIManager.Instance.CloseAllMenusExcept(menuPanel);

            menuPanel.SetActive(true);
            UpdateAllSlots(); 
        }
    }

    // --- METODO CORRETTO CON SAFETY CHECK ---
    public void InitializeDatabase()
    {
        if (allResearches == null) allResearches = new List<ResearchItem>();
        
        // Pulizia preliminare di eventuali elementi nulli nella lista runtime
        allResearches.RemoveAll(x => x == null || x.info == null);

        if (researchDatabase == null) return;

        foreach (var def in researchDatabase)
        {
            // FIX CRASH: Se uno slot nell'inspector è vuoto, lo saltiamo
            if (def == null) continue;

            // Ora siamo sicuri che def esiste, possiamo accedere a def.id
            if (!allResearches.Exists(r => r.id == def.id))
            {
                allResearches.Add(new ResearchItem(def));
            }
        }
    }

    public void LoadResearchLevels(List<ResearchSaveData> savedData)
    {
        InitializeDatabase(); 
        
        foreach(var res in allResearches) 
        {
            if (res != null) res.currentLevel = 0;
        }

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
        _activeSlots.Clear();

        foreach (var research in allResearches)
        {
            if (research == null || research.info == null) continue;

            ResearchSlotUI newSlot = Instantiate(slotPrefab, listContent);
            newSlot.transform.localScale = Vector3.one; 
            newSlot.Setup(research, OnBuyResearch);
            _activeSlots.Add(newSlot);
        }
    }

    void OnBuyResearch(ResearchItem item)
    {
        if (item.IsMaxed()) return;
        if (GameManager.Instance.TrySpend(item.GetCost()))
        {
            item.currentLevel++;
            RecalculateAllResearches(); 
        }
    }

    void UpdateAllSlots()
    {
        if (menuPanel == null || !menuPanel.activeSelf) return;
        
        _activeSlots.RemoveAll(s => s == null);

        for (int i = 0; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].RefreshUI();
        }
    }

    public void RecalculateAllResearches()
    {
        if (GameManager.Instance == null) return;

        // Reset dei moltiplicatori prima del ricalcolo
        GameManager.Instance.ResearchMultiplier = 1;
        GameManager.Instance.LogisticsResearchBonus = 0;
        GameManager.Instance.LogisticsMultiplier = 1; // <--- RESET IMPORTANTE
        GameManager.Instance.StorageResearchBonus = 0;
        GameManager.Instance.EmitterCapResearchBonus = 0; 
        GameManager.Instance.ClickPowerResearchBonus = 0; 
        GameManager.Instance.EmitterSpeedResearchBonus = 0;
        
        for (int i = 0; i < allResearches.Count; i++)
        {
            ResearchItem item = allResearches[i];
            if (item != null && item.currentLevel > 0) ApplyEffectBasedOnTotalLevel(item);
        }
        
        GameManager.Instance.UpdateCapsFromResearch();
        UpdateAllSlots();
    }

    void ApplyEffectBasedOnTotalLevel(ResearchItem item)
    {
        // 1. GLOBAL PRODUCTION MULTIPLIER
        if (item.target == ResearchTarget.GlobalProduction && item.type == ResearchType.Multiplier)
        {
            BigDouble multiplierFromThisResearch;

            if (item.info.isExponentialBonus)
            {
                // FORMULA ESPONENZIALE (Potente)
                multiplierFromThisResearch = BigDouble.Pow(item.bonusValue, item.currentLevel);
            }
            else
            {
                // FORMULA LINEARE COMPOSTA (Standard Egg Inc.)
                BigDouble totalBonusPercent = item.bonusValue * item.currentLevel;
                multiplierFromThisResearch = 1 + totalBonusPercent;
            }

            GameManager.Instance.ResearchMultiplier *= multiplierFromThisResearch;
        }
        
        // 2. LOGISTICS CAPACITY MULTIPLIER (NUOVO!)
        else if (item.target == ResearchTarget.LogisticsCapacity && item.type == ResearchType.Multiplier)
        {
            BigDouble multiplierFromThisResearch;

            if (item.info.isExponentialBonus)
            {
                multiplierFromThisResearch = BigDouble.Pow(item.bonusValue, item.currentLevel);
            }
            else
            {
                BigDouble totalBonusPercent = item.bonusValue * item.currentLevel;
                multiplierFromThisResearch = 1 + totalBonusPercent;
            }

            GameManager.Instance.LogisticsMultiplier *= multiplierFromThisResearch;
        }

        // 3. BONUS ADDITIVI
        else if (item.type == ResearchType.Additive)
        {
            double totalAdditive = item.bonusValue * item.currentLevel;

            if (item.target == ResearchTarget.LogisticsCapacity)
                GameManager.Instance.LogisticsResearchBonus += totalAdditive;
            
            else if (item.target == ResearchTarget.StorageCapacity)
                GameManager.Instance.StorageResearchBonus += totalAdditive;
            
            else if (item.target == ResearchTarget.EmitterMaxCap)
                GameManager.Instance.EmitterCapResearchBonus += (int)totalAdditive;

            else if (item.target == ResearchTarget.ClickPower)
                GameManager.Instance.ClickPowerResearchBonus += (float)totalAdditive;
            
            else if (item.target == ResearchTarget.EmitterProductionSpeed)
                GameManager.Instance.EmitterSpeedResearchBonus += totalAdditive;
        }
    }
}