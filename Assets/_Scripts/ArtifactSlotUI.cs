// --- File: _Scripts\ArtifactSlotUI.cs ---
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ArtifactSlotUI : MonoBehaviour
{
    public Image artifactIcon;
    public GameObject equippedBorder; 
    public Button myButton;
    
    public TextMeshProUGUI levelText;
    public Slider dataProgressBar;

    private PhysicalTheorySO _theory;
    private Action<PhysicalTheorySO> _onClickCallback;

    public void Setup(PhysicalTheorySO theory, DroneManager.RuntimeTheory state, bool isEquipped, Action<PhysicalTheorySO> onClick)
    {
        _theory = theory;
        _onClickCallback = onClick;

        if (artifactIcon != null)
        {
            artifactIcon.sprite = theory.icon;
            if (theory.icon == null) artifactIcon.color = new Color(1,1,1,0); 
            else 
            {
                // Mostra l'icona "spenta" se non è ancora sintetizzata (Lv 0)
                artifactIcon.color = state.level == 0 ? new Color(0.4f, 0.4f, 0.4f, 0.8f) : Color.white;
            }
        }

        if (equippedBorder != null) equippedBorder.SetActive(isEquipped);
        
        if (levelText != null) 
        {
            if (state.level == 0) levelText.text = "<color=#FF6B6B>DATI GREZZI</color>";
            else levelText.text = $"Lv. {state.level}";
        }

        if (dataProgressBar != null)
        {
            int requiredData = theory.GetDataRequiredForLevel(state.level);
            dataProgressBar.value = Mathf.Clamp01((float)state.accumulatedData / requiredData);
        }

        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(() => _onClickCallback?.Invoke(_theory));
    }
}