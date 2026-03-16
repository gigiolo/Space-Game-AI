// --- File: _Scripts\UI\HangarUI.cs ---
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HangarUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform listContent;
    [SerializeField] private DroneMissionSlotUI slotPrefab;
    [SerializeField] private TextMeshProUGUI dronesAvailableText;
    
    [Header("Collegamento Popup Finale")]
    [Tooltip("Il popup che mostra i risultati del viaggio")]
    [SerializeField] private DroneResultPopup resultPopup;

    private List<DroneMissionSlotUI> _activeSlots = new List<DroneMissionSlotUI>();
    
    // FIX: Flag per prevenire lo spegnimento istantaneo al primo avvio
    private bool _openedViaButton = false; 

    private void Start()
    {
        // Spegniamo il pannello all'avvio SOLO se non è stato appena aperto dal bottone
        if (!_openedViaButton && menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (menuPanel != null && UIManager.Instance != null) 
        {
            UIManager.Instance.RegisterMenu(menuPanel);
        }

        InitializeList();
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        
        _openedViaButton = true; // Segnaliamo allo Start che siamo stati noi ad aprirlo!

        bool opening = !menuPanel.activeSelf;

        if (opening)
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(menuPanel);
            menuPanel.SetActive(true);
            RefreshUI();
        }
        else
        {
            UIPopupEffect effect = menuPanel.GetComponent<UIPopupEffect>();
            if (effect != null) effect.Close();
            else menuPanel.SetActive(false);
        }
    }

    private void InitializeList()
    {
        if (DroneManager.Instance == null || slotPrefab == null || listContent == null) return;

        foreach (Transform child in listContent) Destroy(child.gameObject);
        _activeSlots.Clear();

        foreach (var mission in DroneManager.Instance.allMissions)
        {
            var newSlot = Instantiate(slotPrefab, listContent);
            newSlot.transform.localScale = Vector3.one;
            newSlot.Setup(mission, OnLaunchClicked, OnClaimClicked);
            _activeSlots.Add(newSlot);
        }
    }

    private void Update()
    {
        if (menuPanel != null && menuPanel.activeSelf)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (DroneManager.Instance == null) return;

        if (dronesAvailableText != null)
        {
            int busyDrones = DroneManager.Instance.activeDrones.Count;
            int totalDrones = DroneManager.Instance.unlockedSlots;
            int freeDrones = totalDrones - busyDrones;
            dronesAvailableText.text = $"Sonde in Baia: {freeDrones} / {totalDrones}";
        }

        for (int i = 0; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].RefreshState();
        }
    }

    private void OnLaunchClicked(DroneMissionSO mission)
    {
        DroneManager.Instance.LaunchDrone(0, mission);
        ToggleMenu(); // Chiude il pannello
    }

    // --- MODIFICA: Aggiornato per ricevere il Dizionario di Teorie (Capacità di Carico Multiplo) ---
    private void OnClaimClicked(DroneManager.ActiveDrone droneData)
    {
        ToggleMenu(); 

        DroneManager.Instance.ClaimDrone(droneData, (logText, theoriesDict) => 
        {
            if (resultPopup != null)
            {
                resultPopup.Show(logText, theoriesDict);
            }
        });
    }
}