using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ColorInvertEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Riferimenti UI")]
    [Tooltip("L'immagine di sfondo (quella nera) che diventerà colorata.")]
    public Image backgroundFill;

    [Tooltip("L'immagine dell'icona (quella colorata) che diventerà nera.")]
    public Image iconGraphic;

    [Header("Impostazioni Animazione")]
    [Tooltip("Durata della transizione di colore.")]
    public float duration = 0.1f;

    // Colori rilevati automaticamente allo Start
    private Color _colorColored; // Il colore "Tema" (preso dall'icona)
    private Color _colorDark;    // Il colore "Sfondo" (preso dal background, solitamente nero)

    private Coroutine _currentRoutine;

    private void Start()
    {
        // Se i riferimenti mancano, proviamo a trovarli nei figli (fallback intelligente)
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

        // SALVIAMO I COLORI INIZIALI
        // Nota: Lo facciamo in Start così se ThemedUIElement ha settato i colori, noi li leggiamo corretti.
        _colorColored = iconGraphic.color;
        _colorDark = backgroundFill.color;
    }

    // Quando premi il dito/mouse
    public void OnPointerDown(PointerEventData eventData)
    {
        // Interrompiamo eventuali animazioni precedenti
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        // Avviamo l'animazione verso lo stato "PREMUTO" (Invertito)
        // Background -> Colore Acceso
        // Icona -> Colore Scuro
        _currentRoutine = StartCoroutine(AnimateColors(_colorColored, _colorDark));
    }

    // Quando rilasci il dito/mouse
    public void OnPointerUp(PointerEventData eventData)
    {
        RestoreColors();
    }

    // Quando il dito esce dal bottone mentre premi (per evitare che rimanga bloccato)
    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreColors();
    }

    private void RestoreColors()
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        // Avviamo l'animazione verso lo stato "NORMALE"
        // Background -> Colore Scuro
        // Icona -> Colore Acceso
        _currentRoutine = StartCoroutine(AnimateColors(_colorDark, _colorColored));
    }

    private IEnumerator AnimateColors(Color targetBgColor, Color targetIconColor)
    {
        float timer = 0f;
        Color startBg = backgroundFill.color;
        Color startIcon = iconGraphic.color;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Unscaled per funzionare anche in pausa
            float t = Mathf.Clamp01(timer / duration);

            backgroundFill.color = Color.Lerp(startBg, targetBgColor, t);
            iconGraphic.color = Color.Lerp(startIcon, targetIconColor, t);

            yield return null;
        }

        // Assicuriamo i valori finali precisi
        backgroundFill.color = targetBgColor;
        iconGraphic.color = targetIconColor;
    }
    
    // Metodo opzionale se il tema cambia a runtime (es. cambi pianeta)
    public void RefreshBaseColors()
    {
        if (backgroundFill != null) _colorDark = backgroundFill.color;
        if (iconGraphic != null) _colorColored = iconGraphic.color;
    }
}