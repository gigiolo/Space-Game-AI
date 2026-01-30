using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using BreakInfinity;

public class SpaceshipManager : MonoBehaviour
{
    public static SpaceshipManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject menuPanel;
    public Transform listContent;
    public SpaceshipSlotUI slotPrefab;

    [Header("Database")]
    public List<SpaceshipDefinition> spaceshipDatabase;

    [Header("Runtime State")]
    public List<SpaceshipItem> fleet = new List<SpaceshipItem>();
    
    private List<SpaceshipSlotUI> _activeSlots = new List<SpaceshipSlotUI>();

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
        if (menuPanel) 
        {
            menuPanel.SetActive(false);
            
            // --- REGISTRAZIONE MENU ---
            if (UIManager.Instance != null)
                UIManager.Instance.RegisterMenu(menuPanel);
        }
        
        if (fleet == null || fleet.Count == 0) 
        {
            InitializeDatabase();
        }
        
        InitializeUI();

        if (GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated += RefreshAllSlots;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated -= RefreshAllSlots;
    }

    public BigDouble GetTotalSpaceshipSpeed()
    {
        BigDouble total = 0;
        foreach (var ship in fleet)
        {
            total += ship.GetCurrentSpeed();
        }
        return total;
    }

    public void ToggleMenu()
    {
        if (menuPanel)
        {
            bool opening = !menuPanel.activeSelf;
            
            if (!opening)
            {
                // CHIUSURA
                UIPopupEffect effect = menuPanel.GetComponent<UIPopupEffect>();
                if (effect != null) effect.Close();
                else menuPanel.SetActive(false);
            }
            else
            {
                // APERTURA - Chiudi gli altri!
                if (UIManager.Instance != null)
                    UIManager.Instance.CloseAllMenusExcept(menuPanel);

                menuPanel.SetActive(true);
                RefreshAllSlots();
            }
        }
    }

    private void InitializeDatabase()
    {
        if (fleet == null) fleet = new List<SpaceshipItem>();
        
        foreach (var def in spaceshipDatabase)
        {
            if (!fleet.Exists(x => x.info.id == def.id))
            {
                fleet.Add(new SpaceshipItem(def));
            }
        }
    }

    public void LoadFleetLevels(List<ResearchSaveData> savedData)
    {
        InitializeDatabase(); 
        foreach (var ship in fleet) ship.currentLevel = 0;

        if (savedData != null)
        {
            foreach (var saved in savedData)
            {
                var item = fleet.Find(x => x.info.id == saved.id);
                if (item != null) item.currentLevel = saved.level;
            }
        }
        RefreshAllSlots();
    }

    private void InitializeUI()
    {
        if (!listContent || !slotPrefab) return;

        foreach (Transform child in listContent) Destroy(child.gameObject);
        _activeSlots.Clear();

        foreach (var ship in fleet)
        {
            var newSlot = Instantiate(slotPrefab, listContent);
            newSlot.transform.localScale = Vector3.one; 
            newSlot.Setup(ship, OnBuyShip);
            _activeSlots.Add(newSlot);
        }
    }

    private void OnBuyShip(SpaceshipItem ship)
    {
        if (ship.IsMaxed()) return;

        BigDouble cost = ship.GetCost();
        bool purchaseSuccess = false;

        if (ship.info.currencyType == SpaceshipCurrency.Energy)
        {
            if (GameManager.Instance.TrySpend(cost)) purchaseSuccess = true;
        }
        else
        {
            if (GameManager.Instance.TrySpendPureIridium(cost)) purchaseSuccess = true;
        }

        if (purchaseSuccess)
        {
            ship.currentLevel++;
            RefreshAllSlots();
            GameManager.Instance.ForceUIUpdate();
        }
    }

    private void RefreshAllSlots()
    {
        if (!menuPanel || !menuPanel.activeSelf) return;

        _activeSlots.RemoveAll(s => s == null);

        for(int i = 0; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].RefreshUI();
        }
    }
}