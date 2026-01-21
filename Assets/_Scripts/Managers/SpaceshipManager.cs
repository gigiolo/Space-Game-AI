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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (menuPanel) menuPanel.SetActive(false);
        InitializeDatabase();
        InitializeUI();

        // Iscriviti agli eventi del GameManager per aggiornare la UI quando cambiano i soldi
        if (GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated += RefreshAllSlots;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnEconomyUpdated -= RefreshAllSlots;
    }

    // --- LOGICA CORE ---

    // Calcola la velocità TOTALE di tutte le navi possedute
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
            bool isActive = !menuPanel.activeSelf;
            menuPanel.SetActive(isActive);
            if (isActive) RefreshAllSlots();
        }
    }

    private void InitializeDatabase()
    {
        if (fleet == null) fleet = new List<SpaceshipItem>();
        
        foreach (var def in spaceshipDatabase)
        {
            // Se non abbiamo già questa nave in lista, aggiungiamola
            if (!fleet.Exists(x => x.info.id == def.id))
            {
                fleet.Add(new SpaceshipItem(def));
            }
        }
    }

    public void LoadFleetLevels(List<ResearchSaveData> savedData)
    {
        InitializeDatabase(); // Assicura che la lista esista
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

    // --- UI & INTERACTION ---

    private void InitializeUI()
    {
        if (!listContent || !slotPrefab) return;

        foreach (Transform child in listContent) Destroy(child.gameObject);

        foreach (var ship in fleet)
        {
            var newSlot = Instantiate(slotPrefab, listContent);
            newSlot.Setup(ship, OnBuyShip);
        }
    }

    private void OnBuyShip(SpaceshipItem ship)
    {
        if (ship.IsMaxed()) return;

        BigDouble cost = ship.GetCost();
        bool purchaseSuccess = false;

        if (ship.info.currencyType == SpaceshipCurrency.Energy)
        {
            // Paga con Energia
            if (GameManager.Instance.TrySpend(cost)) purchaseSuccess = true;
        }
        else
        {
            // Paga con Iridio Puro
            if (GameManager.Instance.TrySpendPureIridium(cost)) purchaseSuccess = true;
        }

        if (purchaseSuccess)
        {
            ship.currentLevel++;
            RefreshAllSlots();
            // Aggiorna anche la UI principale se necessario
            GameManager.Instance.ForceUIUpdate();
        }
    }

    private void RefreshAllSlots()
    {
        if (!menuPanel || !menuPanel.activeSelf) return;

        foreach (Transform child in listContent)
        {
            var slot = child.GetComponent<SpaceshipSlotUI>();
            if (slot) slot.RefreshUI();
        }
    }
}