using UnityEngine;
using System.Collections.Generic;
using BreakInfinity;

public class ResearchManager : MonoBehaviour
{
    [Header("Configurazione UI")]
    public GameObject menuPanel;        // Il pannello nero intero
    public Transform listContent;       // L'oggetto "Content" della ScrollView
    public ResearchSlotUI slotPrefab;   // Il prefab creato in Fase 2

    [Header("Database Ricerche")]
    // Qui creeremo le ricerche direttamente dall'Inspector
    public List<ResearchItem> allResearches; 

    private void Start()
    {
        menuPanel.SetActive(false); // Nascondi menu all'avvio
        InitializeResearches();     // Crea la lista
    }

    // Crea le righe visive basandosi sulla lista dati
    void InitializeResearches()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);

        foreach (var research in allResearches)
        {
            // 1. Crea l'oggetto
            GameObject newSlot = Instantiate(slotPrefab.gameObject, listContent);

            // ---------------------------------------------------------
            // 👇 AGGIUNGI QUESTE 3 RIGHE ESATTE 👇
            // ---------------------------------------------------------
            
            // Forza la scala a 1 (risolve il problema "invisibile")
            newSlot.transform.localScale = Vector3.one; 
            
            // Forza la posizione Z a 0 (risolve il problema "lontano/dietro")
            newSlot.transform.localPosition = new Vector3(newSlot.transform.localPosition.x, newSlot.transform.localPosition.y, 0);
            
            // ---------------------------------------------------------

            // 2. Configura lo script
            newSlot.GetComponent<ResearchSlotUI>().Setup(research, OnBuyResearch);
        }
    }

    // Questa funzione viene chiamata quando clicchi "Acquista"
    void OnBuyResearch(ResearchItem item)
    {
        BigDouble cost = item.GetCost();

        // 1. Controlla se hai abbastanza soldi (dal GameManager)
        if (GameManager.Instance.CurrentEnergy >= cost)
        {
            // 2. Paga
            GameManager.Instance.CurrentEnergy -= cost; // O usa un metodo SpendEnergy()
            
            // 3. Aumenta livello e potenzia
            item.currentLevel++;
            ApplyEffect(item); 

            // 4. Aggiorna la grafica di TUTTI gli slot (perché i prezzi cambiano o i soldi scendono)
            UpdateAllSlots();
        }
        else
        {
            Debug.Log("Non hai abbastanza energia!");
        }
    }

    void ApplyEffect(ResearchItem item)
    {
        // Qui colleghi l'effetto al gioco vero
        if(item.id == "habitat_speed")
        {
            GameManager.Instance.GenerationRate *= 1.15; // Esempio
        }
        // Aggiungi altri if per altre ricerche
    }

    void UpdateAllSlots()
    {
        foreach(Transform child in listContent)
        {
            child.GetComponent<ResearchSlotUI>().RefreshUI();
        }
    }

    // Chiama questa funzione dal Tasto Arancione in basso a sx
    public void ToggleMenu()
    {
        menuPanel.SetActive(!menuPanel.activeSelf);
    }
}