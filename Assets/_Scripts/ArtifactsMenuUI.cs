// --- File: _Scripts\ArtifactsMenuUI.cs ---
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using BreakInfinity;

public class ArtifactsMenuUI : MonoBehaviour
{
    [Header("Riferimenti Root")]
    public GameObject panelRoot;
    public Canvas mainCanvas; 

    [Header("Matrice Attiva (Drop Zones)")]
    public Transform[] topMatrixSlots; 
    public ArtifactSlotUI matrixSlotPrefab; 

    [Header("Area Archivio (Scroll View)")]
    public Transform archiveGridContent; 
    public DraggableTheoryUI draggableSlotPrefab; 

    [Header("Popup Modale (Dettagli e Upgrade)")]
    public GameObject detailsPopupPanel;
    public Image detailsIconImage; 
    public TextMeshProUGUI detailsNameText;
    public TextMeshProUGUI detailsDescText;
    public TextMeshProUGUI detailsBonusText;
    public TextMeshProUGUI dataProgressText; 
    public Button upgradeButton;
    public TextMeshProUGUI upgradeCostText; 
    
    [Header("Azione (Equipaggia da Popup)")]
    public Button equipButton; 
    public TextMeshProUGUI equipButtonText; 
    
    public Button closePopupButton;

    private PhysicalTheorySO _selectedTheory;
    private bool _isOpenedByClick = false; 
    private bool _returnToOptionsOnClose = false;

    private void Start()
    {
        if (panelRoot != null)
        {
            if (!_isOpenedByClick) panelRoot.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.RegisterMenu(panelRoot);
        }
        
        if (detailsPopupPanel) detailsPopupPanel.SetActive(false);
        
        if (closePopupButton) 
        {
            closePopupButton.onClick.RemoveAllListeners();
            closePopupButton.onClick.AddListener(CloseDetailsPopup);
        }
    }

    public void OpenFromOptions()
    {
        _returnToOptionsOnClose = true; 
        if (panelRoot != null && !panelRoot.activeSelf) ToggleMenu(); 
    }

    public void ToggleMenu()
    {
        if (panelRoot == null) return;
        _isOpenedByClick = true; 
        
        if (detailsPopupPanel != null && detailsPopupPanel.activeSelf)
        {
            CloseDetailsPopup();
            return;
        }

        bool opening = !panelRoot.activeSelf;

        if (opening)
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(panelRoot);
            panelRoot.SetActive(true);
            if (detailsPopupPanel) detailsPopupPanel.SetActive(false);
            RefreshAll();
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

    private void RefreshAll()
    {
        RefreshArchive();
        RefreshTopMatrix();
    }

    private void RefreshArchive()
    {
        for (int i = archiveGridContent.childCount - 1; i >= 0; i--)
        {
            Destroy(archiveGridContent.GetChild(i).gameObject);
        }

        foreach (var kvp in DroneManager.Instance.theoryDatabase)
        {
            string theoryId = kvp.Key;
            var state = kvp.Value;

            PhysicalTheorySO theory = DroneManager.Instance.allTheories.Find(t => t.id == theoryId);
            if (theory != null)
            {
                bool isEquipped = DroneManager.Instance.activeTheoryIDs.Contains(theoryId);
                
                DraggableTheoryUI newDraggable = Instantiate(draggableSlotPrefab, archiveGridContent);
                newDraggable.transform.localScale = Vector3.one;
                
                newDraggable.Setup(theory, this, mainCanvas);
                newDraggable.GetComponent<ArtifactSlotUI>().Setup(theory, state, isEquipped);
            }
        }
    }

    private void RefreshTopMatrix()
    {
        var activeList = DroneManager.Instance.activeTheoryIDs;

        for (int i = 0; i < topMatrixSlots.Length; i++)
        {
            for (int j = topMatrixSlots[i].childCount - 1; j >= 0; j--)
            {
                Destroy(topMatrixSlots[i].GetChild(j).gameObject);
            }

            if (i < activeList.Count)
            {
                PhysicalTheorySO theory = DroneManager.Instance.allTheories.Find(t => t.id == activeList[i]);
                var state = DroneManager.Instance.theoryDatabase[theory.id];

                ArtifactSlotUI equippedSlot = Instantiate(matrixSlotPrefab, topMatrixSlots[i]);
                equippedSlot.transform.localScale = Vector3.one;
                equippedSlot.Setup(theory, state, false); 
                
                Button btn = equippedSlot.gameObject.GetComponent<Button>();
                if (btn == null) btn = equippedSlot.gameObject.AddComponent<Button>();
                
                string idToRemove = theory.id;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => UnequipTheory(idToRemove));
            }
        }
    }

    public void TryEquipTheory(PhysicalTheorySO newTheory, int dropIndex)
    {
        if (DroneManager.Instance.theoryDatabase[newTheory.id].level == 0) return;

        var activeList = DroneManager.Instance.activeTheoryIDs;

        if (activeList.Contains(newTheory.id)) return;

        if (dropIndex < activeList.Count)
        {
            activeList[dropIndex] = newTheory.id;
        }
        else
        {
            if (activeList.Count < DroneManager.Instance.maxActiveTheories)
            {
                activeList.Add(newTheory.id);
            }
        }

        if (GameManager.Instance != null) GameManager.Instance.RecalculateCaps();
        RefreshAll();
    }

    private void UnequipTheory(string theoryId)
    {
        DroneManager.Instance.activeTheoryIDs.Remove(theoryId);
        if (GameManager.Instance != null) GameManager.Instance.RecalculateCaps();
        RefreshAll();
        
        if (detailsPopupPanel.activeSelf && _selectedTheory != null && _selectedTheory.id == theoryId)
        {
            UpdatePopupDetails();
        }
    }

    public void OpenDetailsPopup(PhysicalTheorySO theory)
    {
        _selectedTheory = theory;
        if (panelRoot != null) panelRoot.SetActive(false);
        if (detailsPopupPanel != null) detailsPopupPanel.SetActive(true);
        UpdatePopupDetails();
    }

    public void CloseDetailsPopup()
    {
        if (detailsPopupPanel != null) detailsPopupPanel.SetActive(false);
        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshAll();
    }

    private void UpdatePopupDetails()
    {
        if (_selectedTheory == null) return;

        var state = DroneManager.Instance.theoryDatabase[_selectedTheory.id];
        
        if (detailsIconImage != null)
        {
            detailsIconImage.sprite = _selectedTheory.icon;
            detailsIconImage.color = state.level == 0 ? new Color(0.1f, 0.1f, 0.1f, 0.8f) : Color.white;
        }
        
        string colorHex = "#FFFFFF"; 
        if (_selectedTheory.rarity == TheoryRarity.Avanzata) colorHex = "#00FFFF";
        if (_selectedTheory.rarity == TheoryRarity.Rivoluzionaria) colorHex = "#A020F0";
        if (_selectedTheory.rarity == TheoryRarity.Unificata) colorHex = "#FFD700";

        if (detailsNameText) 
        {
            detailsNameText.text = $"<color={colorHex}>{_selectedTheory.theoryName}</color>";
        }

        if (detailsDescText) detailsDescText.text = $"<i>\"{_selectedTheory.discoveryLog}\"</i>";
        
        if (detailsBonusText) 
        {
            double currentBonus = _selectedTheory.GetBonusAtLevel(state.level) * 100;
            double nextBonus = _selectedTheory.GetBonusAtLevel(state.level + 1) * 100;
            
            if (state.level == 0)
                detailsBonusText.text = $"Active effect: None\n<color=#888888>Post theorize: +{nextBonus:F0}% {_selectedTheory.bonusType}</color>";
            else
                detailsBonusText.text = $"Effect: +{currentBonus:F0}% {_selectedTheory.bonusType}\n<color=#888888>Next upgrade: +{nextBonus:F0}%</color>";
        }

        int requiredData = _selectedTheory.GetDataRequiredForLevel(state.level);
        BigDouble requiredIridium = _selectedTheory.GetIridiumCostForLevel(state.level);
        bool hasEnoughData = state.accumulatedData >= requiredData;
        bool hasEnoughIridium = GameManager.Instance.PureIridium >= requiredIridium;

        if (dataProgressText)
        {
            string dataColor = hasEnoughData ? "#00FF00" : "#FFFFFF";
            dataProgressText.text = $"Data needed: <color={dataColor}>{state.accumulatedData} / {requiredData} </color>";
        }

        if (upgradeButton != null && upgradeCostText != null)
        {
            // --- MODIFICA: Logica di controllo per i pacchetti dati ---
            if (!hasEnoughData)
            {
                upgradeCostText.text = "Insufficient data";
            }
            else
            {
                string actionText = state.level == 0 ? "Theorize" : "Expand";
                upgradeCostText.text = $"{actionText} ({FormatNumber(requiredIridium)} Pure Iridium)";
            }
            
            upgradeButton.interactable = hasEnoughData && hasEnoughIridium;
            
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => 
            {
                if (DroneManager.Instance.TryUpgradeTheory(_selectedTheory.id)) 
                {
                    UpdatePopupDetails(); 
                }
            });
        }

        if (equipButton != null && equipButtonText != null)
        {
            equipButton.onClick.RemoveAllListeners();

            if (state.level == 0)
            {
                equipButtonText.text = "Untheorized";
                equipButton.interactable = false;
            }
            else
            {
                bool isEquipped = DroneManager.Instance.activeTheoryIDs.Contains(_selectedTheory.id);

                if (isEquipped)
                {
                    equipButtonText.text = "Deactivate";
                    equipButton.interactable = true;
                    equipButton.onClick.AddListener(() => 
                    {
                        UnequipTheory(_selectedTheory.id);
                        UpdatePopupDetails(); 
                    });
                }
                else
                {
                    bool hasSpace = DroneManager.Instance.activeTheoryIDs.Count < DroneManager.Instance.maxActiveTheories;
                    
                    if (hasSpace)
                    {
                        equipButtonText.text = "Activate";
                        equipButton.interactable = true;
                        equipButton.onClick.AddListener(() => 
                        {
                            TryEquipTheory(_selectedTheory, 999); 
                            UpdatePopupDetails(); 
                        });
                    }
                    else
                    {
                        equipButtonText.text = "Matrix full";
                        equipButton.interactable = false;
                    }
                }
            }
        }
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F0");
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }
}