using UnityEngine;
using BreakInfinity; 
using System;

public class GameManager : MonoBehaviour
{
    // --- SINGLETON PATTERN ---
    public static GameManager Instance { get; private set; }

    // --- ECONOMY VARIABLES ---
    public BigDouble CurrentEnergy { get; set; }
    public BigDouble LifetimeEarnings { get; private set; }
    public BigDouble GenerationRate { get; set; }
    public BigDouble LogisticsCap { get; set; }
    public BigDouble StorageCap { get; private set; }

    // Variabile privata ottimizzata (cache)
    private BigDouble _cachedIncomePerSec; 

    // --- LA CORREZIONE FONDAMENTALE ---
    // Questa freccia "=>" rende pubblica la lettura della variabile privata.
    // Risolve l'errore dello UIManager che cercava "IncomePerSec".
    public BigDouble IncomePerSec => _cachedIncomePerSec;
    // ----------------------------------

    public event Action OnEconomyUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGame()
    {
        CurrentEnergy = 0;
        GenerationRate = 0; 
        LogisticsCap = 50; // Iniziamo con un po' di cavi liberi
        StorageCap = 500;
        
        // Impostiamo costi base iniziali per sicurezza
        HabitatBaseCost = 10;
        LogisticsBaseCost = 15;

        RecalculateIncome();
    }

    private void Update()
    {
        // Aggiungiamo energia passiva solo se c'è guadagno
        if (_cachedIncomePerSec > 0)
        {
            // Nota: Time.deltaTime trasforma il guadagno "per secondo" in "per frame"
            AddEnergy(_cachedIncomePerSec * Time.deltaTime);
        }
    }

    // Calcola il guadagno basandosi sul "Collo di Bottiglia" (Minimo tra Prod e Logistica)
    public void RecalculateIncome()
    {
        _cachedIncomePerSec = BigDouble.Min(GenerationRate, LogisticsCap);
        
        if (GenerationRate > LogisticsCap)
        {
            // Debug utile per capire se il sistema funziona
            // Debug.LogWarning("⚠️ BOTTLENECK: Logistica satura! Energia persa.");
        }

        // Avvisa la UI che i valori sono cambiati (es. Income/sec)
        OnEconomyUpdated?.Invoke();
    }

    public void AddEnergy(BigDouble amount)
    {
        if (CurrentEnergy + amount > StorageCap)
        {
            CurrentEnergy = StorageCap;
        }
        else
        {
            CurrentEnergy += amount;
            if(amount > 0) LifetimeEarnings += amount;
        }

        // Avvisa la UI (aggiorna il testo dell'energia)
        OnEconomyUpdated?.Invoke();
    }

    public void HandleManualTap()
    {
        BigDouble tapValue = 1; 
        AddEnergy(tapValue);
    }

    // --- SISTEMA UPGRADE (HABITAT) ---
    public int HabitatLevel { get; private set; } = 0;
    public BigDouble HabitatBaseCost { get; private set; } = 10; 
    
    // Formula costo: Base * 1.15 ^ Level
    public BigDouble HabitatCost => HabitatBaseCost * BigDouble.Pow(1.15, HabitatLevel); 

    public void BuyHabitat()
    {
        BigDouble cost = HabitatCost;

        if (CurrentEnergy >= cost)
        {
            CurrentEnergy -= cost;
            
            HabitatLevel++;
            GenerationRate += 1; // +1 Prod per ogni livello (per ora)

            RecalculateIncome(); 
            Debug.Log($"Habitat Acquistato (Lvl {HabitatLevel}). Nuova Prod: {GenerationRate}");
        }
    }

    // --- SISTEMA LOGISTICA (SHIPPING) ---
    public int LogisticsLevel { get; private set; } = 0;
    public BigDouble LogisticsBaseCost { get; private set; } = 15; 
    
    // Formula costo: Base * 1.50 ^ Level (Crescita rapida)
    public BigDouble LogisticsCost => LogisticsBaseCost * BigDouble.Pow(1.50, LogisticsLevel); 

    public void BuyLogistics()
    {
        BigDouble cost = LogisticsCost;

        if (CurrentEnergy >= cost)
        {
            CurrentEnergy -= cost;
            
            LogisticsLevel++;
            LogisticsCap += 10; // +10 Capacità per ogni livello

            RecalculateIncome();
            Debug.Log($"Logistica Potenziata (Lvl {LogisticsLevel}). Nuova Cap: {LogisticsCap}");
        }
    }
}