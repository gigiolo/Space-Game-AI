// --- File: _Scripts\UIPopupEffect.cs ---
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIPopupEffect : MonoBehaviour
{
    [Header("Riferimenti Pannelli")]
    [Tooltip("Il pannello scuro principale (quello davanti)")]
    public RectTransform mainPanel;
    
    [Tooltip("Il pannello colorato che fa da bordo (quello dietro)")]
    public RectTransform borderPanel;

    [Header("---- FASE 1: APERTURA PRINCIPALE ----")]
    public float startScale = 0.7f;
    public float openDuration = 0.25f;
    public AnimationCurve openCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.8f, 1.05f), new Keyframe(1, 1));

    [Header("---- FASE 2: COMPARSA BORDO ----")]
    public float borderDuration = 0.15f;

    [Header("---- CHIUSURA ----")]
    public float closeDuration = 0.2f;
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool IsClosing { get; private set; } // Flag di sicurezza

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

    public void Close()
    {
        if (IsClosing) return; // Evita riavvii accidentali se stai spammando il click
        IsClosing = true;
        
        // 1. Calcola quanto tempo impiegherà a chiudersi
        float totalTime = closeDuration;
        if (borderPanel != null && borderPanel.gameObject.activeSelf) 
        {
            totalTime += borderDuration;
        }

        // 2. Lo comunica all'UIManager (il vigile urbano)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterClosingMenu(totalTime);
        }

        StopAllCoroutines();
        StartCoroutine(CloseSequence());
    }

    private IEnumerator OpenSequence()
    {
        // PREPARAZIONE: Nascondiamo il bordo, prepariamo il pannello principale
        if (borderPanel != null) borderPanel.gameObject.SetActive(false); 
        mainPanel.localScale = _mainDefaultScale * startScale;
        _cg.alpha = 0f;

        // --- MAGIA: ATTESA SINCRONIZZATA ---
        // Se l'UIManager ci dice che c'è un pannello che si sta chiudendo, aspettiamo!
        if (UIManager.Instance != null)
        {
            float waitTime = UIManager.Instance.GetClosingDelay();
            if (waitTime > 0)
            {
                // Usiamo Realtime così funziona anche se il gioco è in pausa (TimeScale = 0)
                yield return new WaitForSecondsRealtime(waitTime); 
            }
        }

        // FASE 1: DISSOLVENZA E SCALA
        float timer = 0f;
        while (timer < openDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / openDuration);
            float curveValue = openCurve.Evaluate(progress);

            mainPanel.localScale = Vector3.LerpUnclamped(_mainDefaultScale * startScale, _mainDefaultScale, curveValue);
            _cg.alpha = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        mainPanel.localScale = _mainDefaultScale;
        _cg.alpha = 1f;

        // FASE 2: COMPARSA RAPIDA DEL BORDO
        if (borderPanel != null)
        {
            borderPanel.gameObject.SetActive(true);
            borderPanel.localScale = Vector3.zero;

            timer = 0f;
            while (timer < borderDuration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(timer / borderDuration);
                float easeOut = 1f - Mathf.Pow(1f - progress, 3f);

                borderPanel.localScale = Vector3.LerpUnclamped(Vector3.zero, _borderDefaultScale, easeOut);
                yield return null;
            }
            borderPanel.localScale = _borderDefaultScale;
        }
    }

    private IEnumerator CloseSequence()
    {
        float timer = 0f;

        // FASE 1: SCOMPARSA RAPIDA DEL BORDO
        if (borderPanel != null && borderPanel.gameObject.activeSelf)
        {
            Vector3 currentBorderScale = borderPanel.localScale;
            
            while (timer < borderDuration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(timer / borderDuration);
                float easeIn = Mathf.Pow(progress, 3f);

                borderPanel.localScale = Vector3.LerpUnclamped(currentBorderScale, Vector3.zero, easeIn);
                yield return null;
            }
            
            borderPanel.localScale = Vector3.zero;
            borderPanel.gameObject.SetActive(false);
        }

        // FASE 2: DISSOLVENZA E CHIUSURA
        timer = 0f;
        Vector3 currentMainScale = mainPanel.localScale;
        float currentAlpha = _cg.alpha;

        while (timer < closeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / closeDuration);
            float curveValue = closeCurve.Evaluate(progress);

            mainPanel.localScale = Vector3.LerpUnclamped(currentMainScale, _mainDefaultScale * startScale, curveValue);
            _cg.alpha = Mathf.Lerp(currentAlpha, 0f, progress);

            yield return null;
        }

        _cg.alpha = 0f;
        gameObject.SetActive(false);
    }
}