using UnityEngine;
using UnityEngine.UI; // Necessario per gestire Layout e Button
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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if(menuPanel) menuPanel.SetActive(false); 
        
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

    // --- LOGICA DI APERTURA / CHIUSURA SEMPLIFICATA (V2) ---
    public void ToggleMenu()
    {
        if (menuPanel == null) return;

        if (menuPanel.activeSelf)
        {
            // --- CHIUSURA ---
            // Cerchiamo lo script dell'effetto animato
            UIPopupEffect effect = menuPanel.GetComponent<UIPopupEffect>();
            
            if (effect != null)
            {
                // Se c'è lo script, usiamo la sua chiusura elegante
                effect.Close();
            }
            else
            {
                // Fallback: se non hai messo lo script, chiude e basta (nessun errore)
                menuPanel.SetActive(false);
            }
        }
        else
        {
            // --- APERTURA ---
            // 1. Attiva il pannello (Questo fa partire l'OnEnable di UIPopupEffect automaticamente)
            menuPanel.SetActive(true);

            // 2. Fix per le ScrollView: Forza Unity a ricalcolare le dimensioni della lista
            // (Utile se l'animazione parte da scala piccola e la lista si confonde)
            Canvas.ForceUpdateCanvases();
            if (listContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(listContent.GetComponent<RectTransform>());
            
            // 3. Aggiorna i dati (prezzi, livelli, colori)
            UpdateAllSlots();
        }
    }

    // -------------------------------------------------------
    // --- IL RESTO DEL CODICE RIMANE INVARIATO ---
    // -------------------------------------------------------

    public void InitializeDatabase()
    {
        if (allResearches == null) allResearches = new List<ResearchItem>();

        foreach (var def in researchDatabase)
        {
            if (!allResearches.Exists(r => r.id == def.id))
            {
                allResearches.Add(new ResearchItem(def));
            }
        }
    }

    public void LoadResearchLevels(List<ResearchSaveData> savedData)
    {
        InitializeDatabase(); 
        
        foreach(var res in allResearches) res.currentLevel = 0;

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
        // Pulisce vecchi slot
        foreach (Transform child in listContent) Destroy(child.gameObject);

        // Crea nuovi slot
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
        if (menuPanel == null || !menuPanel.activeSelf) return;
        
        foreach(Transform child in listContent)
        {
            if (child != null)
                child.GetComponent<ResearchSlotUI>().RefreshUI();
        }
    }

    public void RecalculateAllResearches()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.ResearchMultiplier = 1;
        GameManager.Instance.LogisticsResearchBonus = 0;
        GameManager.Instance.StorageResearchBonus = 0;
        GameManager.Instance.EmitterAutoGrowthSpeed = GameManager.Instance.BaseAutoGrowthSpeed;
        GameManager.Instance.EmitterCapResearchBonus = 0; 
        
        foreach (var item in allResearches)
        {
            if (item.currentLevel > 0) ApplyEffectBasedOnTotalLevel(item);
        }
        
        GameManager.Instance.UpdateCapsFromResearch();
        UpdateAllSlots();
    }

    void ApplyEffectBasedOnTotalLevel(ResearchItem item)
    {
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