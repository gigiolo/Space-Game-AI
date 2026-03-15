using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class ArtifactsMenuUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    public GameObject panelRoot;
    public TextMeshProUGUI slotCountText; 

    [Header("Area Archivio (Tutti quelli scoperti)")]
    public Transform archiveGridContent; 
    public ArtifactSlotUI artifactSlotPrefab;

    [Header("Dettagli (Pannello laterale o basso)")]
    public TextMeshProUGUI detailsNameText;
    public TextMeshProUGUI detailsDescText;
    public TextMeshProUGUI detailsBonusText;
    public Button equipButton;
    public TextMeshProUGUI equipButtonText;

    private CosmicArtifactSO _selectedArtifact;
    private List<ArtifactSlotUI> _spawnedSlots = new List<ArtifactSlotUI>();
    
    private bool _isOpenedByClick = false; 

    // --- NUOVA VARIABILE: La "Memoria" di navigazione ---
    private bool _returnToOptionsOnClose = false;

    private void Start()
    {
        if (panelRoot != null)
        {
            if (!_isOpenedByClick) panelRoot.SetActive(false);
            
            if (UIManager.Instance != null) UIManager.Instance.RegisterMenu(panelRoot);
        }

        ClearDetails();
    }

    // --- NUOVO METODO: Chiamato SOLO dal menù Opzioni ---
    public void OpenFromOptions()
    {
        _returnToOptionsOnClose = true; // Imposta il biglietto di ritorno
        if (!panelRoot.activeSelf)
        {
            ToggleMenu(); // Lo apre normalmente
        }
    }

    public void ToggleMenu()
    {
        if (panelRoot == null) return;
        
        _isOpenedByClick = true; 
        
        bool opening = !panelRoot.activeSelf;

        if (opening)
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(panelRoot);
            panelRoot.SetActive(true);
            ClearDetails();
            RefreshGrid();
        }
        else
        {
            UIPopupEffect effect = panelRoot.GetComponent<UIPopupEffect>();
            if (effect != null) 
            {
                effect.Close();
                // L'evento OnMenuClosed nell'Inspector chiamerà OnFullyClosed() alla fine dell'animazione
            }
            else 
            {
                panelRoot.SetActive(false);
                OnFullyClosed(); // Se non c'è l'animazione, lo chiamiamo subito
            }
        }
    }

    // --- NUOVO METODO: Controlla se deve riaprire le opzioni alla chiusura ---
    public void OnFullyClosed()
    {
        if (_returnToOptionsOnClose)
        {
            _returnToOptionsOnClose = false; // Brucia il biglietto di ritorno

            // Cerca il menù opzioni anche se è spento e lo riapre
            OptionsMenu optionsMenu = FindFirstObjectByType<OptionsMenu>(FindObjectsInactive.Include);
            if (optionsMenu != null)
            {
                optionsMenu.ToggleMenu();
            }
        }
    }

    private void RefreshGrid()
    {
        if (DroneManager.Instance == null) return;

        int equippedCount = DroneManager.Instance.equippedArtifactIDs.Count;
        int maxSlots = DroneManager.Instance.maxEquippedArtifacts;
        if (slotCountText) slotCountText.text = $"Slot Matrix: {equippedCount} / {maxSlots}";

        foreach (var slot in _spawnedSlots) Destroy(slot.gameObject);
        _spawnedSlots.Clear();

        foreach (string id in DroneManager.Instance.discoveredArtifactIDs)
        {
            CosmicArtifactSO art = DroneManager.Instance.allArtifacts.Find(a => a.id == id);
            if (art != null)
            {
                bool isEquipped = DroneManager.Instance.equippedArtifactIDs.Contains(id);
                
                ArtifactSlotUI newSlot = Instantiate(artifactSlotPrefab, archiveGridContent);
                newSlot.transform.localScale = Vector3.one;
                newSlot.Setup(art, isEquipped, OnArtifactClicked);
                _spawnedSlots.Add(newSlot);
            }
        }
        
        if (_selectedArtifact != null) UpdateDetails(_selectedArtifact);
    }

    private void OnArtifactClicked(CosmicArtifactSO artifact)
    {
        _selectedArtifact = artifact;
        UpdateDetails(artifact);
    }

    private void UpdateDetails(CosmicArtifactSO artifact)
    {
        if (detailsNameText) detailsNameText.text = artifact.artifactName;
        if (detailsDescText) detailsDescText.text = $"<i>\"{artifact.discoveryLog}\"</i>";
        
        if (detailsBonusText) 
        {
            string bonusString = (artifact.bonusValue * 100).ToString("F0");
            detailsBonusText.text = $"Effetto: +{bonusString}% {artifact.bonusType.ToString()}";
        }

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(true);
            equipButton.onClick.RemoveAllListeners();

            bool isEquipped = DroneManager.Instance.equippedArtifactIDs.Contains(artifact.id);

            if (isEquipped)
            {
                equipButtonText.text = "RIMUOVI";
                equipButton.interactable = true;
                equipButton.onClick.AddListener(() => 
                {
                    DroneManager.Instance.UnequipArtifact(artifact.id);
                    RefreshGrid();
                });
            }
            else
            {
                equipButtonText.text = "INSERISCI NELLA MATRIX";
                bool hasSpace = DroneManager.Instance.equippedArtifactIDs.Count < DroneManager.Instance.maxEquippedArtifacts;
                equipButton.interactable = hasSpace;
                
                if (!hasSpace) equipButtonText.text = "MATRIX PIENA";

                equipButton.onClick.AddListener(() => 
                {
                    DroneManager.Instance.EquipArtifact(artifact.id);
                    RefreshGrid();
                });
            }
        }
    }

    private void ClearDetails()
    {
        _selectedArtifact = null;
        if (detailsNameText) detailsNameText.text = "SELEZIONA UN ARTEFATTO";
        if (detailsDescText) detailsDescText.text = "";
        if (detailsBonusText) detailsBonusText.text = "";
        if (equipButton) equipButton.gameObject.SetActive(false);
    }
}