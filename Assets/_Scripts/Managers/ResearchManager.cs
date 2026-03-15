// --- File: _Scripts\Managers\ResearchManager.cs ---
using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using System.Linq; 
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
            Debug.LogWarning($"[ResearchManager] Rilevato duplicato su {gameObject.name}. Lo distruggo per mantenere il Singleton.");
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

            if (_activeSlots.Count == 0 && allResearches != null && allResearches.Count > 0)
            {
                Debug.Log("<color=yellow>[ResearchManager] La UI era vuota, forzo la ricostruzione.</color>");
                InitializeUI();
            }

            UpdateAllSlots(); 

            Canvas.ForceUpdateCanvases();
            if (listContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(listContent.GetComponent<RectTransform>());
            }
        }
    }

    public int GetTotalUpgrades()
    {
        int total = 0;
        if (allResearches == null) return 0;
        foreach(var res in allResearches) total += res.currentLevel;
        return total;
    }

    public bool IsTierUnlocked(int tier)
    {
        if (tier <= 1) return true; 
        
        var req = tierRequirements.Find(t => t.tierLevel == tier);
        if (req != null) return GetTotalUpgrades() >= req.requiredTotalUpgrades;
        return true; 
    }

    public int UpgradesNeededForTier(int tier)
    {
        var req = tierRequirements.Find(t => t.tierLevel == tier);
        if (req != null) return Mathf.Max(0, req.requiredTotalUpgrades - GetTotalUpgrades());
        return 0;
    }

    public int GetHighestUnlockedTier()
    {
        int maxTier = 1;
        if (allResearches != null && allResearches.Count > 0)
        {
            maxTier = allResearches.Max(r => r.tier);
        }

        int highest = 1;
        for (int i = 1; i <= maxTier; i++)
        {
            if (IsTierUnlocked(i)) highest = i;
            else break; 
        }
        return highest;
    }

    // TASTO DESTRO SULLO SCRIPT IN EDITOR -> "Test: Carica Database"
    [ContextMenu("Test: Carica Database")]
    public void InitializeDatabase()
    {
        if (allResearches == null) allResearches = new List<ResearchItem>();
        allResearches.RemoveAll(x => x == null || x.info == null);

        if (researchDatabase == null || researchDatabase.Count == 0) 
        {
            Debug.LogError("<color=red>[ResearchManager] ERRORE: La lista 'Research Database' è vuota! Trascina le ricerche nell'Inspector!</color>");
            return;
        }

        int added = 0;
        foreach (var def in researchDatabase)
        {
            if (def == null) continue;

            if (!allResearches.Exists(r => r.id == def.id))
            {
                allResearches.Add(new ResearchItem(def));
                added++;
            }
        }
        Debug.Log($"<color=green>[ResearchManager] Database caricato! Aggiunte {added} nuove ricerche. Totale in Runtime: {allResearches.Count}</color>");
    }

    public void LoadResearchLevels(List<ResearchSaveData> savedData)
    {
        InitializeDatabase(); 
        foreach(var res in allResearches) if (res != null) res.currentLevel = 0;

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

    // TASTO DESTRO SULLO SCRIPT IN EDITOR -> "Test: Costruisci UI"
    [ContextMenu("Test: Costruisci UI")]
    void InitializeUI()
    {
        if (slotPrefab == null)
        {
            Debug.LogError("<color=red>[ResearchManager] ERRORE: Manca lo 'Slot Prefab'!</color>");
            return;
        }

        foreach (Transform child in listContent) DestroyImmediate(child.gameObject);
        _activeSlots.Clear();
        _activeHeaders.Clear();

        if (allResearches == null || allResearches.Count == 0)
        {
            Debug.LogWarning("[ResearchManager] Impossibile costruire la UI: nessuna ricerca nel database Runtime.");
            return;
        }

        var groupedResearches = allResearches
            .Where(r => r != null && r.info != null)
            .GroupBy(r => r.tier)
            .OrderBy(g => g.Key);

        foreach (var group in groupedResearches)
        {
            int currentTier = group.Key;

            if (headerPrefab != null)
            {
                ResearchTierHeaderUI header = Instantiate(headerPrefab, listContent);
                header.transform.localScale = Vector3.one;
                header.Setup(currentTier);
                _activeHeaders.Add(header);
            }

            foreach (var research in group)
            {
                ResearchSlotUI newSlot = Instantiate(slotPrefab, listContent);
                newSlot.transform.localScale = Vector3.one; 
                newSlot.Setup(research, OnBuyResearch);
                _activeSlots.Add(newSlot);
            }
        }
        Debug.Log($"<color=cyan>[ResearchManager] UI Costruita con {_activeHeaders.Count} Titoli e {_activeSlots.Count} Slot.</color>");
    }

    void OnBuyResearch(ResearchItem item)
    {
        if (item.IsMaxed()) return;
        if (!IsTierUnlocked(item.tier)) return; 

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

        foreach (var header in _activeHeaders) header.RefreshUI();
        foreach (var slot in _activeSlots) slot.RefreshUI();
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
        UpdateAllSlots(); 
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