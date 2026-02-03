using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class IntroSequenceController : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup blackScreen;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI introText;

    [Header("Tutorial Prompt")]
    public CanvasGroup tapPromptPanel; 

    [Header("Reparenting Logic")]
    [Tooltip("Il vero bottone Energy che sta momentaneamente fuori.")]
    public Transform energyButtonObj; 
    [Tooltip("L'oggetto vuoto 'Placeholder' che sta dentro il BottomPanel.")]
    public Transform placeholderObj; 

    [Header("Settings")]
    public float fadeInTime = 1.0f;
    public float stayTime = 2.0f;
    public float fadeOutTime = 1.0f;
    public float delayBetweenTexts = 0.5f;

    private void Start()
    {
        // Se NON è la prima sessione:
        if (GameManager.Instance != null && !GameManager.Instance.IsFirstSession)
        {
            // Ripristina subito la posizione del bottone se necessario
            if (energyButtonObj != null && placeholderObj != null)
            {
                ReparentButtonNow();
            }

            if (UIManager.Instance != null) UIManager.Instance.SetHUDVisibility(true, 0f);
            Destroy(gameObject);
            return;
        }

        // Setup Iniziale
        if (blackScreen) blackScreen.alpha = 1f;
        if (titleText) titleText.alpha = 0f;
        if (introText) introText.alpha = 0f;
        if (tapPromptPanel) tapPromptPanel.alpha = 0f; 

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetHUDVisibility(false, 0f);
        }

        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        // ... (SEQUENZA IDENTICA A PRIMA) ...
        yield return StartCoroutine(FadeText(titleText, 0f, 1f, fadeInTime));
        yield return new WaitForSeconds(stayTime);
        yield return StartCoroutine(FadeText(titleText, 1f, 0f, fadeOutTime));
        yield return new WaitForSeconds(delayBetweenTexts);
        yield return StartCoroutine(FadeText(introText, 0f, 1f, fadeInTime));
        yield return new WaitForSeconds(stayTime);
        yield return StartCoroutine(FadeText(introText, 1f, 0f, fadeOutTime));
        yield return new WaitForSeconds(delayBetweenTexts);

        StartCoroutine(FadeCanvasGroup(blackScreen, 1f, 0f, 2.0f));
        if (tapPromptPanel) StartCoroutine(FadeCanvasGroup(tapPromptPanel, 0f, 1f, 1.5f));

        yield return new WaitForSeconds(2.0f);
        
        blackScreen.blocksRaycasts = false; 

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFirstInput += OnFirstInteraction;
        }
    }

    private void OnFirstInteraction()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFirstInput -= OnFirstInteraction;
            GameManager.Instance.IsFirstSession = false;
            GameManager.Instance.SaveGame();
        }

        if (tapPromptPanel)
        {
            StartCoroutine(FadeCanvasGroup(tapPromptPanel, tapPromptPanel.alpha, 0f, 0.2f));
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetHUDVisibility(true, 1.0f);
        }

        // --- IL TRUCCO: Ripristina la gerarchia corretta ---
        ReparentButtonNow();
        // ---------------------------------------------------

        Destroy(gameObject, 1.5f);
    }

    private void ReparentButtonNow()
    {
        if (energyButtonObj != null && placeholderObj != null)
        {
            // 1. Sposta il bottone dentro il genitore del placeholder (BottomPanel)
            energyButtonObj.SetParent(placeholderObj.parent);
            
            // 2. Mettilo allo stesso indice del placeholder (così mantiene la posizione tra gli altri tasti)
            energyButtonObj.SetSiblingIndex(placeholderObj.GetSiblingIndex());
            
            // 3. Resetta scala e posizione per sicurezza (LayoutGroup farà il resto)
            energyButtonObj.localScale = Vector3.one;
            
            // 4. Distruggi il placeholder ormai inutile
            Destroy(placeholderObj.gameObject);
        }
    }

    // ... (Helper Coroutines rimangono uguali) ...
    private IEnumerator FadeText(TextMeshProUGUI text, float start, float end, float duration)
    {
        if (text == null) yield break;
        float timer = 0f;
        text.alpha = start;
        while (timer < duration) { timer += Time.deltaTime; text.alpha = Mathf.Lerp(start, end, timer / duration); yield return null; }
        text.alpha = end;
    }
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        if (cg == null) yield break;
        float timer = 0f;
        cg.alpha = start;
        while (timer < duration) { timer += Time.deltaTime; cg.alpha = Mathf.Lerp(start, end, timer / duration); yield return null; }
        cg.alpha = end;
    }
}