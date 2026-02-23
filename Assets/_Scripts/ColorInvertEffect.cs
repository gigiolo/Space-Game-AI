using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ColorInvertEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Riferimenti UI")]
    public Image backgroundFill;
    public Image iconGraphic;

    [Header("Impostazioni Animazione")]
    public float duration = 0.1f;

    private Color _colorColored; 
    private Color _colorDark;    

    private Coroutine _currentRoutine;
    
    // --- NUOVE VARIABILI ---
    private AttentionPulseEffect _pulseEffect; 
    private bool _isPressed = false; 

    private void Start()
    {
        if (backgroundFill == null) 
            backgroundFill = transform.Find("Background")?.GetComponent<Image>();
        
        if (iconGraphic == null) 
            iconGraphic = transform.Find("Icon_Img")?.GetComponent<Image>();

        if (backgroundFill == null || iconGraphic == null)
        {
            Debug.LogError($"[ColorInvertEffect] Mancano i riferimenti su {gameObject.name}!");
            enabled = false;
            return;
        }

        _colorColored = iconGraphic.color;
        _colorDark = backgroundFill.color;

        // Cerca lo script AttentionPulseEffect sullo stesso bottone
        _pulseEffect = GetComponent<AttentionPulseEffect>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
        
        // 1. Diciamo all'animazione di pulsazione di bloccarsi e resettare i colori
        if (_pulseEffect != null) _pulseEffect.Suppress(true);

        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        // 2. Avviamo la nostra animazione di click
        _currentRoutine = StartCoroutine(AnimateColors(_colorColored, _colorDark));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isPressed) RestoreColors();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isPressed) RestoreColors();
    }

    private void RestoreColors()
    {
        _isPressed = false;
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        // Avviamo l'animazione di ritorno e passiamo 'true' per dire che alla fine deve riattivare la pulsazione
        _currentRoutine = StartCoroutine(AnimateColors(_colorDark, _colorColored, true));
    }

    private IEnumerator AnimateColors(Color targetBgColor, Color targetIconColor, bool isRestoring = false)
    {
        float timer = 0f;
        Color startBg = backgroundFill.color;
        Color startIcon = iconGraphic.color;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(timer / duration);

            backgroundFill.color = Color.Lerp(startBg, targetBgColor, t);
            iconGraphic.color = Color.Lerp(startIcon, targetIconColor, t);

            yield return null;
        }

        backgroundFill.color = targetBgColor;
        iconGraphic.color = targetIconColor;

        // Se abbiamo finito di ripristinare il bottone, lasciamo che la pulsazione riprenda
        if (isRestoring && _pulseEffect != null)
        {
            _pulseEffect.Suppress(false);
        }
    }
    
    public void RefreshBaseColors()
    {
        if (backgroundFill != null) _colorDark = backgroundFill.color;
        if (iconGraphic != null) _colorColored = iconGraphic.color;
    }

    // --- FAILSAFE: Rete di sicurezza ---
    // Se il bottone viene nascosto/disattivato all'improvviso, lo resettiamo con la forza.
    private void OnDisable()
    {
        _isPressed = false;
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        if (backgroundFill != null) backgroundFill.color = _colorDark;
        if (iconGraphic != null) iconGraphic.color = _colorColored;
        
        if (_pulseEffect != null) _pulseEffect.Suppress(false);
    }
}