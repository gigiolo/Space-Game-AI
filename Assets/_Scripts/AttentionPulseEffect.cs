using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AttentionPulseEffect : MonoBehaviour
{
    [Header("Riferimenti Grafici")]
    [Tooltip("L'immagine di sfondo (attualmente nera/scura).")]
    public Image backgroundImage;

    [Tooltip("L'immagine dell'icona (attualmente colorata).")]
    public Image iconImage;

    [Header("Configurazione Animazione")]
    [Tooltip("Quante volte deve invertire i colori in una sequenza (es. 3).")]
    public int pulseCount = 3;

    [Tooltip("Quanto tempo passa tra una sequenza e l'altra (es. 5 secondi).")]
    public float intervalDelay = 5.0f;

    [Tooltip("Quanto dura un singolo flash (Andata e Ritorno).")]
    public float singleFlashDuration = 0.3f;

    [Header("Correzione Colori")]
    [Tooltip("Se VERO, usa sempre il Nero Puro (0,0,0) come colore scuro, ignorando eventuali tinte grigie del bottone.")]
    public bool forcePureBlack = true;

    // Stato interno
    private bool _shouldAnimate = false;
    private bool _isSuppressed = false; // <--- NUOVO: Gestisce l'interruzione dal click
    private Coroutine _animationRoutine;
    
    // Colori originali
    private Color _colorDark;   // Colore dello sfondo
    private Color _colorTheme;  // Colore dell'icona

    private void Start()
    {
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        
        if (iconImage == null)
        {
            foreach (var img in GetComponentsInChildren<Image>())
            {
                if (img != backgroundImage)
                {
                    iconImage = img;
                    break;
                }
            }
        }

        if (backgroundImage) 
        {
            if (forcePureBlack)
                _colorDark = Color.black;
            else
                _colorDark = backgroundImage.color;
        }

        if (iconImage) _colorTheme = iconImage.color;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _animationRoutine = null;
        _isSuppressed = false;
        ResetColors();
    }

    public void SetActive(bool isActive)
    {
        if (_shouldAnimate == isActive) return; 

        _shouldAnimate = isActive;

        if (_shouldAnimate)
        {
            if (_animationRoutine == null && gameObject.activeInHierarchy)
            {
                if (iconImage) _colorTheme = iconImage.color;
                _animationRoutine = StartCoroutine(PulseRoutine());
            }
        }
        else
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _animationRoutine = null;
            ResetColors();
        }
    }

    // --- NUOVO: Metodo per sospendere visivamente la pulsazione ---
    public void Suppress(bool suppress)
    {
        _isSuppressed = suppress;
        if (suppress)
        {
            // Forza i colori originali subito, così il ColorInvertEffect
            // inizia la sua animazione partendo dai colori giusti, non sfumati.
            ResetColors();
        }
    }

    private void ResetColors()
    {
        if (backgroundImage) backgroundImage.color = _colorDark;
        if (iconImage) iconImage.color = _colorTheme;
    }

    private IEnumerator PulseRoutine()
    {
        while (_shouldAnimate)
        {
            for (int i = 0; i < pulseCount; i++)
            {
                yield return StartCoroutine(SingleFlash());
            }

            if (!_isSuppressed) ResetColors();

            yield return new WaitForSeconds(intervalDelay);
        }
    }

    private IEnumerator SingleFlash()
    {
        float halfDuration = singleFlashDuration / 2f;
        float timer = 0f;

        // FASE 1: Inversione
        while (timer < halfDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);

            // Modifica i colori SOLO se non stiamo cliccando il bottone
            if (!_isSuppressed)
            {
                if (backgroundImage) backgroundImage.color = Color.Lerp(_colorDark, _colorTheme, t);
                if (iconImage) iconImage.color = Color.Lerp(_colorTheme, _colorDark, t);
            }

            yield return null;
        }

        // FASE 2: Ritorno
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);

            if (!_isSuppressed)
            {
                if (backgroundImage) backgroundImage.color = Color.Lerp(_colorTheme, _colorDark, t);
                if (iconImage) iconImage.color = Color.Lerp(_colorDark, _colorTheme, t);
            }

            yield return null;
        }

        if (!_isSuppressed) ResetColors();
    }
}