using UnityEngine;
using System.Collections.Generic;
using BreakInfinity;

public class ResearchManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel;        
    public Transform listContent;       
    public ResearchSlotUI slotPrefab;    

    [Header("Database")]
    public List<ResearchItem> allResearches; 

    private void Start()
    {
        if(menuPanel) menuPanel.SetActive(false); 
        InitializeResearches();
        
        if(GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated += UpdateAllSlots;
    }
    
    private void OnDestroy()
    {
        if(GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated -= UpdateAllSlots;
    }

    void InitializeResearches()
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
            // Applichiamo l'effetto incrementale (solo per questo livello)
            ApplySingleLevelEffect(item); 
            
            GameManager.Instance.UpdateCapsFromResearch();
            UpdateAllSlots(); 
        }
    }

    // Usato quando compri UN livello (aggiunge solo il bonus di quel livello)
    void ApplySingleLevelEffect(ResearchItem item)
    {
        if (item.target == ResearchTarget.GlobalProduction && item.type == ResearchType.Multiplier)
        {
            GameManager.Instance.ResearchMultiplier *= (1 + item.bonusValue);
        }
        else if (item.type == ResearchType.Additive)
        {
            if (item.target == ResearchTarget.LogisticsCapacity)
                GameManager.Instance.LogisticsResearchBonus += item.bonusValue;
            else if (item.target == ResearchTarget.StorageCapacity)
                GameManager.Instance.StorageResearchBonus += item.bonusValue;
        }
    }

    void UpdateAllSlots()
    {
        if(!menuPanel.activeSelf) return;

        foreach(Transform child in listContent)
        {
            child.GetComponent<ResearchSlotUI>().RefreshUI();
        }
    }

    public void ToggleMenu()
    {
        menuPanel.SetActive(!menuPanel.activeSelf);
        if(menuPanel.activeSelf) UpdateAllSlots();
    }

    // --- LOGICA DI RICALCOLO (CHIAMATA DAL LOADGAME) ---
    // Questa è la parte critica che mancava o era incompleta
    public void RecalculateAllResearches()
    {
        // 1. Resetta TUTTI i bonus nel GameManager a zero/base
        GameManager.Instance.ResearchMultiplier = 1;
        GameManager.Instance.LogisticsResearchBonus = 0;
        GameManager.Instance.StorageResearchBonus = 0;
        
        // 2. Ricalcola basandosi sul livello attuale di ogni ricerca
        foreach (var item in allResearches)
        {
            if (item.currentLevel > 0)
            {
                ApplyEffectBasedOnTotalLevel(item);
            }
        }
        
        // 3. Aggiorna UI e Caps
        GameManager.Instance.ForceUIUpdate();
        UpdateAllSlots();
    }

    void ApplyEffectBasedOnTotalLevel(ResearchItem item)
    {
        // A. MOLTIPLICATORI (Produzione)
        if (item.target == ResearchTarget.GlobalProduction && item.type == ResearchType.Multiplier)
        {
            // Formula: (1 + bonus)^livello
            BigDouble totalMult = BigDouble.Pow(1 + item.bonusValue, item.currentLevel);
            GameManager.Instance.ResearchMultiplier *= totalMult;
        }

        // B. ADDITIVI (Logistica / Storage)
        else if (item.type == ResearchType.Additive)
        {
            // Formula: Bonus * Livello
            double totalAdditive = item.bonusValue * item.currentLevel;

            if (item.target == ResearchTarget.LogisticsCapacity)
            {
                GameManager.Instance.LogisticsResearchBonus += totalAdditive;
            }
            else if (item.target == ResearchTarget.StorageCapacity)
            {
                GameManager.Instance.StorageResearchBonus += totalAdditive;
            }
        }
    }
}