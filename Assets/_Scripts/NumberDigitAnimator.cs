// --- File: _Scripts\NumberDigitAnimator.cs ---
using UnityEngine;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(TextMeshProUGUI))]
public class NumberDigitAnimator : MonoBehaviour
{
    [Header("Animazione")]
    [Tooltip("Il colore che assume la cifra appena cambia.")]
    public Color transitionColor = Color.cyan;

    [Tooltip("Quanto velocemente il colore torna a quello originale (valore più alto = più veloce).")]
    public float fadeSpeed = 2.0f;

    // Riferimenti interni
    private TextMeshProUGUI _textComponent;
    private TMP_TextInfo _textInfo;
    
    // Memorizza lo stato PARSATO precedente per il confronto (Ignora HTML Tags)
    private string _previousParsedText = "";
    
    // Memorizza il "calore" di ogni carattere (1.0 = transitionColor, 0.0 = colore originale)
    private float[] _charHeat;
    
    // Flag per sapere se dobbiamo aggiornare la mesh
    private bool _isDirty = false;

    private void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
        // Inizializziamo l'array con una dimensione di sicurezza
        _charHeat = new float[32]; 
    }

    /// <summary>
    /// Usa questo metodo al posto di text.text = "..." per attivare l'animazione.
    /// Completamente sicuro con i Rich Text Tags (es. <color>, <mspace>).
    /// </summary>
    public void SetText(string newText)
    {
        // Se il testo grezzo è identico, non fare nulla (risparmia performance)
        if (_textComponent.text == newText) return;

        // 1. Applica il testo e forza TMP a generare la mesh
        // In questo modo TextMeshPro elimina i tag e genera i caratteri visibili
        _textComponent.text = newText;
        _textComponent.ForceMeshUpdate();

        // 2. Otteniamo SOLO il testo visibile (es. "<color=red>10</color>" diventa "10")
        string parsedText = _textComponent.GetParsedText();

        // Gestione ridimensionamento array se il testo diventa molto lungo
        if (parsedText.Length > _charHeat.Length)
        {
            System.Array.Resize(ref _charHeat, parsedText.Length + 16);
        }

        // 3. CONFRONTO: Trova quali caratteri (visibili) sono cambiati
        for (int i = 0; i < parsedText.Length; i++)
        {
            if (i >= _previousParsedText.Length || parsedText[i] != _previousParsedText[i])
            {
                // Ignoriamo spazi, virgole e punti per l'estetica, animiamo solo numeri e lettere
                if (!char.IsWhiteSpace(parsedText[i]) && parsedText[i] != '.' && parsedText[i] != ',')
                {
                    _charHeat[i] = 1.0f; // Imposta "calore" massimo
                    _isDirty = true;
                }
            }
        }

        // Aggiorna la memoria con il nuovo testo parsato
        _previousParsedText = parsedText;
    }

    private void LateUpdate()
    {
        // Se non c'è nulla da animare, usciamo
        if (!_isDirty) return;

        _textComponent.ForceMeshUpdate();
        _textInfo = _textComponent.textInfo;
        
        int charCount = _textInfo.characterCount;
        if (charCount == 0) return;

        // Recupera il colore base ATTUALE del testo (supporta i cambi di tema a runtime)
        Color32 baseColor = _textComponent.color;
        Color32 targetFlashColor = transitionColor;

        bool stillAnimating = false;

        // Itera su tutti i caratteri visibili (che ora combaciano perfettamente con _charHeat)
        for (int i = 0; i < charCount; i++)
        {
            // Skip se il carattere non è visibile
            if (!_textInfo.characterInfo[i].isVisible) continue;

            // Se questo carattere ha "calore", calcoliamo il colore interpolato
            if (_charHeat[i] > 0.01f)
            {
                // Riduci il calore
                _charHeat[i] -= Time.deltaTime * fadeSpeed;
                if (_charHeat[i] < 0) _charHeat[i] = 0;
                else stillAnimating = true;

                // Calcola colore (Lerp tra base e flash)
                float t = _charHeat[i];
                Color32 displayColor = Color32.Lerp(baseColor, targetFlashColor, t);

                // Applica ai 4 vertici del carattere
                int materialIndex = _textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = _textInfo.characterInfo[i].vertexIndex;
                Color32[] vertexColors = _textInfo.meshInfo[materialIndex].colors32;

                if (vertexColors != null && vertexIndex + 3 < vertexColors.Length)
                {
                    vertexColors[vertexIndex + 0] = displayColor;
                    vertexColors[vertexIndex + 1] = displayColor;
                    vertexColors[vertexIndex + 2] = displayColor;
                    vertexColors[vertexIndex + 3] = displayColor;
                }
            }
        }

        // Applica le modifiche alla mesh
        _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        // Se nessun carattere sta più animando, spegniamo il flag per risparmiare CPU
        _isDirty = stillAnimating;
    }
}