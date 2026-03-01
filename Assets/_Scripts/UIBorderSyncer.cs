// --- File: _Scripts\UIBorderSyncer.cs ---
using UnityEngine;

[ExecuteAlways] // Rende lo script attivo nell'Editor per farti vedere le modifiche in tempo reale
[RequireComponent(typeof(RectTransform))]
public class UIBorderSyncer : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("Il pannello principale da cui copiare dimensioni, pivot e posizione")]
    public RectTransform targetPanel;

    [Header("Impostazioni Bordo")]
    [Tooltip("Spazio extra (in pixel) da aggiungere per creare il bordo colorato (es. X: 10, Y: 10)")]
    public Vector2 borderPadding = new Vector2(10f, 10f);

    private RectTransform _myRect;

    private void Awake()
    {
        _myRect = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (targetPanel == null || _myRect == null) return;

        // 1. Sincronizza Anchor e Pivot (così l'animazione partirà sempre dallo stesso punto per entrambi)
        if (_myRect.anchorMin != targetPanel.anchorMin) _myRect.anchorMin = targetPanel.anchorMin;
        if (_myRect.anchorMax != targetPanel.anchorMax) _myRect.anchorMax = targetPanel.anchorMax;
        if (_myRect.pivot != targetPanel.pivot) _myRect.pivot = targetPanel.pivot;

        // 2. Calcola e applica la dimensione basandosi su quella reale del Target
        Vector2 targetSize = targetPanel.rect.size + borderPadding;
        if (_myRect.sizeDelta != targetSize)
        {
            _myRect.sizeDelta = targetSize;
        }

        // 3. Compensazione Posizione (Il vero trucco!)
        // Se il pivot non è perfettamente al centro (0.5, 0.5), aggiungere spessore 
        // farebbe crescere il bordo in modo storto. Questa formula lo spinge nella 
        // direzione opposta per mantenerlo visivamente centrato.
        Vector2 posOffset = new Vector2(
            borderPadding.x * (targetPanel.pivot.x - 0.5f),
            borderPadding.y * (targetPanel.pivot.y - 0.5f)
        );

        Vector2 targetPosition = targetPanel.anchoredPosition + posOffset;
        
        // I controlli "!=" servono per non sporcare la scena inutilmente e risparmiare CPU
        if (_myRect.anchoredPosition != targetPosition)
        {
            _myRect.anchoredPosition = targetPosition;
        }
    }
}