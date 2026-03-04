// --- File: _Scripts\UIPopupEffect.cs ---
using UnityEngine;
using UnityEngine.Events; 
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIPopupEffect : MonoBehaviour
{
    [Header("Riferimenti Pannelli")]
    public RectTransform mainPanel;      
    public RectTransform borderPanel;    
    public CanvasGroup contentGroup;     

    [Header("---- FASE 1: APERTURA SFONDO ----")]
    public float startScale = 0.7f;
    public float openDuration = 0.25f;
    public AnimationCurve openCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.8f, 1.05f), new Keyframe(1, 1));

    [Header("---- FASE 2: BORDO E CONTENUTO ----")]
    public float borderDuration = 0.15f;
    public float contentFadeDuration = 0.15f; 

    [Header("---- CHIUSURA ----")]
    public float closeDuration = 0.2f;
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("---- EVENTI (Navigazione) ----")]
    public UnityEvent onMenuClosed; 

    public bool IsClosing { get; private set; }
    
    // Variabile per ricordare se dobbiamo scatenare l'evento alla fine
    private bool _triggerCloseEvent = true;

    private CanvasGroup _cg;
    private Vector3 _mainDefaultScale = Vector3.one;
    private Vector3 _borderDefaultScale = Vector3.one;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        
        if (mainPanel == null) mainPanel = GetComponent<RectTransform>();
        _mainDefaultScale = mainPanel.localScale;
        if (_mainDefaultScale.x < 0.1f) _mainDefaultScale = Vector3.one;
        
        if (borderPanel != null) 
        {
            _borderDefaultScale = borderPanel.localScale;
            if (_borderDefaultScale.x < 0.1f) _borderDefaultScale = Vector3.one;
        }
    }

    private void OnEnable()
    {
        IsClosing = false;
        StopAllCoroutines();
        StartCoroutine(OpenSequence());
    }

    // MODIFICA QUI: Aggiungiamo un parametro opzionale. Di base è vero.
    public void Close(bool triggerEvent = true)
    {
        if (IsClosing) return;
        IsClosing = true;
        
        // Salviamo la scelta
        _triggerCloseEvent = triggerEvent;
        
        float totalTime = closeDuration;
        float firstPhaseTime = Mathf.Max(borderDuration, contentFadeDuration);
        if ((borderPanel != null && borderPanel.gameObject.activeSelf) || contentGroup != null) 
        {
            totalTime += firstPhaseTime;
        }

        if (UIManager.Instance != null) UIManager.Instance.RegisterClosingMenu(totalTime);

        StopAllCoroutines();
        StartCoroutine(CloseSequence());
    }

    private IEnumerator OpenSequence()
    {
        if (borderPanel != null) borderPanel.gameObject.SetActive(false); 
        if (contentGroup != null) contentGroup.alpha = 0f;
        
        mainPanel.localScale = _mainDefaultScale * startScale;
        _cg.alpha = 0f;

        if (UIManager.Instance != null)
        {
            float waitTime = UIManager.Instance.GetClosingDelay();
            if (waitTime > 0) yield return new WaitForSecondsRealtime(waitTime); 
        }

        float timer = 0f;
        while (timer < openDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / openDuration);
            mainPanel.localScale = Vector3.LerpUnclamped(_mainDefaultScale * startScale, _mainDefaultScale, openCurve.Evaluate(progress));
            _cg.alpha = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }

        mainPanel.localScale = _mainDefaultScale;
        _cg.alpha = 1f;

        if (borderPanel != null)
        {
            borderPanel.gameObject.SetActive(true);
            borderPanel.localScale = Vector3.zero;
        }

        timer = 0f;
        float phase2Duration = Mathf.Max(borderDuration, contentFadeDuration);

        while (timer < phase2Duration)
        {
            timer += Time.unscaledDeltaTime;
            
            if (borderPanel != null && timer <= borderDuration)
            {
                float borderProgress = Mathf.Clamp01(timer / borderDuration);
                float easeOut = 1f - Mathf.Pow(1f - borderProgress, 3f);
                borderPanel.localScale = Vector3.LerpUnclamped(Vector3.zero, _borderDefaultScale, easeOut);
            }

            if (contentGroup != null && timer <= contentFadeDuration)
            {
                float contentProgress = Mathf.Clamp01(timer / contentFadeDuration);
                contentGroup.alpha = Mathf.Lerp(0f, 1f, contentProgress);
            }

            yield return null;
        }

        if (borderPanel != null) borderPanel.localScale = _borderDefaultScale;
        if (contentGroup != null) contentGroup.alpha = 1f;
    }

    private IEnumerator CloseSequence()
    {
        float timer = 0f;
        float phase1Duration = Mathf.Max(borderDuration, contentFadeDuration);
        Vector3 currentBorderScale = borderPanel != null ? borderPanel.localScale : Vector3.zero;

        while (timer < phase1Duration)
        {
            timer += Time.unscaledDeltaTime;

            if (borderPanel != null && borderPanel.gameObject.activeSelf && timer <= borderDuration)
            {
                float borderProgress = Mathf.Clamp01(timer / borderDuration);
                float easeIn = Mathf.Pow(borderProgress, 3f);
                borderPanel.localScale = Vector3.LerpUnclamped(currentBorderScale, Vector3.zero, easeIn);
            }

            if (contentGroup != null && timer <= contentFadeDuration)
            {
                float contentProgress = Mathf.Clamp01(timer / contentFadeDuration);
                contentGroup.alpha = Mathf.Lerp(1f, 0f, contentProgress);
            }

            yield return null;
        }
        
        if (borderPanel != null)
        {
            borderPanel.localScale = Vector3.zero;
            borderPanel.gameObject.SetActive(false);
        }

        timer = 0f;
        Vector3 currentMainScale = mainPanel.localScale;
        float currentAlpha = _cg.alpha;

        while (timer < closeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / closeDuration);
            mainPanel.localScale = Vector3.LerpUnclamped(currentMainScale, _mainDefaultScale * startScale, closeCurve.Evaluate(progress));
            _cg.alpha = Mathf.Lerp(currentAlpha, 0f, progress);
            yield return null;
        }

        _cg.alpha = 0f;
        gameObject.SetActive(false);

        // MODIFICA QUI: Scateniamo l'evento SOLO se ce l'hanno permesso
        if (_triggerCloseEvent)
        {
            onMenuClosed?.Invoke(); 
        }
    }
}