using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ArtifactSlotUI : MonoBehaviour
{
    public Image artifactIcon;
    public GameObject equippedBorder; // Un contorno o un badge verde che appare se è equipaggiato
    public Button myButton;

    private CosmicArtifactSO _artifact;
    private Action<CosmicArtifactSO> _onClickCallback;

    public void Setup(CosmicArtifactSO artifact, bool isEquipped, Action<CosmicArtifactSO> onClick)
    {
        _artifact = artifact;
        _onClickCallback = onClick;

        if (artifactIcon != null)
        {
            artifactIcon.sprite = artifact.icon;
            // Se l'artefatto è null (slot vuoto), nascondi l'icona
            if (artifact.icon == null) artifactIcon.color = new Color(1,1,1,0); 
            else artifactIcon.color = Color.white;
        }

        if (equippedBorder != null)
        {
            equippedBorder.SetActive(isEquipped);
        }

        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(() => _onClickCallback?.Invoke(_artifact));
    }
}