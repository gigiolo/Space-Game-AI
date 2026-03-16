// --- File: _Scripts\ArtifactsMenuUI.cs ---
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using BreakInfinity;

public class ArtifactsMenuUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    public GameObject panelRoot;
    public TextMeshProUGUI slotCountText; 

    [Header("Area Archivio")]
    public Transform archiveGridContent; 
    public ArtifactSlotUI artifactSlotPrefab;

    [Header("Dettagli Teoria")]
    public TextMeshProUGUI detailsNameText;
    public TextMeshProUGUI detailsDescText;
    public TextMeshProUGUI detailsBonusText;
    
    [Header("Progressione")]
    public TextMeshProUGUI dataProgressText; 
    public Button upgradeButton;
    public TextMeshProUGUI upgradeCostText; 

    [Header("Azione (Applica alla Matrice)")]
    public Button equipButton;
    public TextMeshProUGUI equipButtonText;

    private PhysicalTheorySO _selectedTheory;
    private List<ArtifactSlotUI> _spawnedSlots = new List<ArtifactSlotUI>();
    private bool _isOpenedByClick = false; 
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

    public void OpenFromOptions()
    {
        _returnToOptionsOnClose = true; 
        if (!panelRoot.activeSelf) ToggleMenu(); 
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
            if (effect != null) effect.Close();
            else { panelRoot.SetActive(false); OnFullyClosed(); }
        }
    }

    public void OnFullyClosed()
    {
        if (_returnToOptionsOnClose)
        {
            _returnToOptionsOnClose = false; 
            OptionsMenu optionsMenu = FindFirstObjectByType<OptionsMenu>(FindObjectsInactive.Include);
            if (optionsMenu != null) optionsMenu.ToggleMenu();
        }
    }

    private void RefreshGrid()
    {
        if (DroneManager.Instance == null) return;

        int equippedCount = DroneManager.Instance.activeTheoryIDs.Count;
        int maxSlots = DroneManager.Instance.maxActiveTheories;
        if (slotCountText) slotCountText.text = $"Matrice Dati: {equippedCount} / {maxSlots}";

        foreach (var slot in _spawnedSlots) Destroy(slot.gameObject);
        _spawnedSlots.Clear();

        foreach (var kvp in DroneManager.Instance.theoryDatabase)
        {
            string theoryId = kvp.Key;
            var state = kvp.Value;

            PhysicalTheorySO theory = DroneManager.Instance.allTheories.Find(t => t.id == theoryId);
            if (theory != null)
            {
                bool isEquipped = DroneManager.Instance.activeTheoryIDs.Contains(theoryId);
                
                ArtifactSlotUI newSlot = Instantiate(artifactSlotPrefab, archiveGridContent);
                newSlot.transform.localScale = Vector3.one;
                newSlot.Setup(theory, state, isEquipped, OnTheoryClicked);
                _spawnedSlots.Add(newSlot);
            }
        }
        
        if (_selectedTheory != null) UpdateDetails(_selectedTheory);
    }

    private void OnTheoryClicked(PhysicalTheorySO theory)
    {
        _selectedTheory = theory;
        UpdateDetails(theory);
    }

    private void UpdateDetails(PhysicalTheorySO theory)
    {
        var state = DroneManager.Instance.theoryDatabase[theory.id];
        
        string colorHex = "#FFFFFF"; 
        if (theory.rarity == TheoryRarity.Avanzata) colorHex = "#00FFFF";
        if (theory.rarity == TheoryRarity.Rivoluzionaria) colorHex = "#A020F0";
        if (theory.rarity == TheoryRarity.Unificata) colorHex = "#FFD700";

        if (detailsNameText) 
        {
            string levelDisplay = state.level == 0 ? "NON SINTETIZZATA" : $"Lv.{state.level}";
            detailsNameText.text = $"<color={colorHex}>{theory.theoryName}</color> <size=80%>({levelDisplay})</size>";
        }

        if (detailsDescText) detailsDescText.text = $"<i>\"{theory.discoveryLog}\"</i>";
        
        if (detailsBonusText) 
        {
            double currentBonus = theory.GetBonusAtLevel(state.level) * 100;
            double nextBonus = theory.GetBonusAtLevel(state.level + 1) * 100;
            
            if (state.level == 0)
            {
                detailsBonusText.text = $"Effetto Attivo: Nessuno\n<color=#888888>Dopo sintesi: +{nextBonus:F0}% {theory.bonusType}</color>";
            }
            else
            {
                detailsBonusText.text = $"Effetto: +{currentBonus:F0}% {theory.bonusType}\n<color=#888888>Prossimo livello: +{nextBonus:F0}%</color>";
            }
        }

        // Sistema UPGRADE & SINTESI (Iridio PURO)
        int requiredData = theory.GetDataRequiredForLevel(state.level);
        BigDouble requiredIridium = theory.GetIridiumCostForLevel(state.level);
        bool hasEnoughData = state.accumulatedData >= requiredData;
        bool hasEnoughIridium = GameManager.Instance.PureIridium >= requiredIridium;

        if (dataProgressText)
        {
            string dataColor = hasEnoughData ? "#00FF00" : "#FFFFFF";
            dataProgressText.text = $"Dati Rilevati: <color={dataColor}>{state.accumulatedData} / {requiredData} TB</color>";
        }

        if (upgradeButton != null && upgradeCostText != null)
        {
            upgradeButton.gameObject.SetActive(true);
            
            string actionText = state.level == 0 ? "SINTETIZZA" : "POTENZIA";
            upgradeCostText.text = $"{actionText} ({FormatNumber(requiredIridium)} Iridio Puro)";
            
            upgradeButton.interactable = hasEnoughData && hasEnoughIridium;
            
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => 
            {
                if (DroneManager.Instance.TryUpgradeTheory(theory.id)) RefreshGrid(); 
            });
        }

        // Bottone EQUIPAGGIAMENTO
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(true);
            equipButton.onClick.RemoveAllListeners();

            if (state.level == 0)
            {
                equipButtonText.text = "RICHIEDE SINTESI";
                equipButton.interactable = false;
            }
            else
            {
                bool isEquipped = DroneManager.Instance.activeTheoryIDs.Contains(theory.id);

                if (isEquipped)
                {
                    equipButtonText.text = "SCOLLEGA";
                    equipButton.interactable = true;
                    equipButton.onClick.AddListener(() => 
                    {
                        DroneManager.Instance.activeTheoryIDs.Remove(theory.id);
                        if (GameManager.Instance != null) GameManager.Instance.RecalculateCaps();
                        RefreshGrid();
                    });
                }
                else
                {
                    equipButtonText.text = "APPLICA ALLA MATRICE";
                    bool hasSpace = DroneManager.Instance.activeTheoryIDs.Count < DroneManager.Instance.maxActiveTheories;
                    equipButton.interactable = hasSpace;
                    
                    if (!hasSpace) equipButtonText.text = "MATRICE PIENA";

                    equipButton.onClick.AddListener(() => 
                    {
                        DroneManager.Instance.activeTheoryIDs.Add(theory.id);
                        if (GameManager.Instance != null) GameManager.Instance.RecalculateCaps();
                        RefreshGrid();
                    });
                }
            }
        }
    }

    private void ClearDetails()
    {
        _selectedTheory = null;
        if (detailsNameText) detailsNameText.text = "SELEZIONA UN ARCHIVIO DATI";
        if (detailsDescText) detailsDescText.text = "";
        if (detailsBonusText) detailsBonusText.text = "";
        if (dataProgressText) dataProgressText.text = "";
        if (upgradeButton) upgradeButton.gameObject.SetActive(false);
        if (equipButton) equipButton.gameObject.SetActive(false);
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F0");
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}