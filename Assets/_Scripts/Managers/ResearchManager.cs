using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using System.Linq; // NECESSARIO PER RAGGRUPPARE I TIER
using BreakInfinity;

[System.Serializable]
public class TierRequirement
{
    [Tooltip("Il Tier da sbloccare (es. 2)")]
    public int tierLevel;
    [Tooltip("Quanti upgrade totali (somma dei livelli di tutte le ricerche) servono per sbloccarlo.")]
    public int requiredTotalUpgrades;
}

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject menuPanel;            
    public Transform listContent;            
    public ResearchSlotUI slotPrefab;    
    
    [Tooltip("Il nuovo prefab che fa da titolo per ogni Tier")]
    public ResearchTierHeaderUI headerPrefab; 

    [Header("Database & Requisiti")]
    public List<ResearchDefinition> researchDatabase; 
    
    [Tooltip("Imposta qui le regole di sblocco. Es: Tier 2 richiede 10 upgrade.")]
    public List<TierRequirement> tierRequirements = new List<TierRequirement>();

    [Header("Stato Runtime")]
    public List<ResearchItem> allResearches;
    
    private List<ResearchSlotUI> _activeSlots = new List<ResearchSlotUI>();
    private List<ResearchTierHeaderUI> _activeHeaders = new List<ResearchTierHeaderUI>();

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

    // --- NUOVE FUNZIONI PER I TIER ---
    
    // Calcola il numero totale di acquisti effettuati (somma dei livelli)
    public int GetTotalUpgrades()
    {
        int total = 0;
        if (allResearches == null) return 0;
        foreach(var res in allResearches) total += res.currentLevel;
        return total;
    }

    // Verifica se un Tier specifico è sbloccato
    public bool IsTierUnlocked(int tier)
    {
        if (tier <= 1) return true; // Il Tier 1 è sempre aperto
        
        var req = tierRequirements.Find(t => t.tierLevel == tier);
        if (req != null)
        {
            return GetTotalUpgrades() >= req.requiredTotalUpgrades;
        }
        // Se ti scordi di configurare un requisito, lo lasciamo sbloccato per sicurezza
        return true; 
    }

    // Restituisce quanti upgrade mancano per sbloccare il Tier (utile per la UI)
    public int UpgradesNeededForTier(int tier)
    {
        var req = tierRequirements.Find(t => t.tierLevel == tier);
        if (req != null)
        {
            return Mathf.Max(0, req.requiredTotalUpgrades - GetTotalUpgrades());
        }
        return 0;
    }

    public void InitializeDatabase()
    {
        if (allResearches == null) allResearches = new List<ResearchItem>();
        
        allResearches.RemoveAll(x => x == null || x.info == null);

        if (researchDatabase == null) return;

        foreach (var def in researchDatabase)
        {
            if (def == null) continue;

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
        _activeHeaders.Clear();

        // 1. Raggruppiamo tutte le ricerche per "Tier" e le ordiniamo dal Tier 1 in su
        var groupedResearches = allResearches
            .Where(r => r != null && r.info != null)
            .GroupBy(r => r.tier)
            .OrderBy(g => g.Key);

        // 2. Creiamo l'interfaccia a blocchi
        foreach (var group in groupedResearches)
        {
            int currentTier = group.Key;

            // Spawna l'Header del Tier (se hai assegnato il prefab)
            if (headerPrefab != null)
            {
                ResearchTierHeaderUI header = Instantiate(headerPrefab, listContent);
                header.transform.localScale = Vector3.one;
                header.Setup(currentTier);
                _activeHeaders.Add(header);
            }

            // Spawna tutti i bottoni di ricerca appartenenti a questo Tier
            foreach (var research in group)
            {
                ResearchSlotUI newSlot = Instantiate(slotPrefab, listContent);
                newSlot.transform.localScale = Vector3.one; 
                newSlot.Setup(research, OnBuyResearch);
                _activeSlots.Add(newSlot);
            }
        }
    }

    void OnBuyResearch(ResearchItem item)
    {
        if (item.IsMaxed()) return;
        if (!IsTierUnlocked(item.tier)) return; // Doppio controllo di sicurezza

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
        _activeHeaders.RemoveAll(h => h == null);

        // Aggiorniamo prima gli Headers (che mostrano i testi "LOCKED")
        foreach (var header in _activeHeaders)
        {
            header.RefreshUI();
        }

        // Poi aggiorniamo le singole ricerche (che diventano grigie se il tier è locked)
        foreach (var slot in _activeSlots)
        {
            slot.RefreshUI();
        }
    }

    public void RecalculateAllResearches()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.ResearchMultiplier = 1;
        GameManager.Instance.LogisticsResearchBonus = 0;
        GameManager.Instance.LogisticsMultiplier = 1; 
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
        UpdateAllSlots(); // Aggiorna anche per mostrare l'eventuale sblocco di un nuovo Tier!
    }

    void ApplyEffectBasedOnTotalLevel(ResearchItem item)
    {
        if (item.target == ResearchTarget.GlobalProduction && item.type == ResearchType.Multiplier)
        {
            BigDouble multiplierFromThisResearch;
            if (item.info.isExponentialBonus) multiplierFromThisResearch = BigDouble.Pow(item.bonusValue, item.currentLevel);
            else multiplierFromThisResearch = 1 + (item.bonusValue * item.currentLevel);

            GameManager.Instance.ResearchMultiplier *= multiplierFromThisResearch;
        }
        else if (item.target == ResearchTarget.LogisticsCapacity && item.type == ResearchType.Multiplier)
        {
            BigDouble multiplierFromThisResearch;
            if (item.info.isExponentialBonus) multiplierFromThisResearch = BigDouble.Pow(item.bonusValue, item.currentLevel);
            else multiplierFromThisResearch = 1 + (item.bonusValue * item.currentLevel);

            GameManager.Instance.LogisticsMultiplier *= multiplierFromThisResearch;
        }
        else if (item.type == ResearchType.Additive)
        {
            double totalAdditive = item.bonusValue * item.currentLevel;

            if (item.target == ResearchTarget.LogisticsCapacity) GameManager.Instance.LogisticsResearchBonus += totalAdditive;
            else if (item.target == ResearchTarget.StorageCapacity) GameManager.Instance.StorageResearchBonus += totalAdditive;
            else if (item.target == ResearchTarget.EmitterMaxCap) GameManager.Instance.EmitterCapResearchBonus += (int)totalAdditive;
            else if (item.target == ResearchTarget.ClickPower) GameManager.Instance.ClickPowerResearchBonus += (float)totalAdditive;
            else if (item.target == ResearchTarget.EmitterProductionSpeed) GameManager.Instance.EmitterSpeedResearchBonus += totalAdditive;
        }
    }
}