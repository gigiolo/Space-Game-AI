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
    [Tooltip("Il vero bottone Energy.")]
    public RectTransform energyButtonRect; 
    
    [Header("Hierarchy Targets")]
    public RectTransform fakeBottomPanel;
    public RectTransform realBottomPanel;
    public RectTransform placeholderInRealPanel; 

    [Header("Generative AI Text Settings (Intro Text Only)")]
    public float charFadeDuration = 0.3f; 
    public float typingDelay = 0.04f; 
    public float punctuationPause = 0.2f;
    public float hesitationChance = 0.2f;
    public float hesitationDuration = 0.3f;

    [Header("Cursor Settings")]
    public string cursorSymbol = "|";
    public float cursorBlinkSpeed = 0.3f;

    [Header("Timing Sequenza")]
    public float titleFadeInDuration = 2.0f; 
    public float stayTime = 2.0f;            
    public float fadeOutTime = 1.0f;         
    public float delayBetweenTexts = 0.5f;

    [Header("Cinematic Camera")] // <--- NUOVA SEZIONE
    [Tooltip("Offset in gradi per l'inquadratura finale (es. 15 per non centrare perfettamente l'emitter).")]
    public float cameraFocusOffset = 15f; 
    [Tooltip("Durata della rotazione della camera.")]
    public float cameraRotationDuration = 1.5f;

    // Cache
    private TMP_TextInfo _currentTextInfo;

    private void Start()
    {
        bool isFirst = GameManager.Instance != null && GameManager.Instance.IsFirstSession;

        if (!isFirst)
        {
            ReparentButtonNow();
            if (fakeBottomPanel) fakeBottomPanel.gameObject.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.SetHUDVisibility(true, 0f);
            Destroy(gameObject);
            return;
        }

        SetupIntroState();
        StartCoroutine(IntroRoutine());
    }

    private void SetupIntroState()
    {
        if (blackScreen) { blackScreen.alpha = 1f; blackScreen.blocksRaycasts = true; }
        
        if (titleText) titleText.alpha = 0f; 
        if (introText) introText.alpha = 0f;
        
        if (tapPromptPanel) tapPromptPanel.alpha = 0f; 

        if (UIManager.Instance != null)
            UIManager.Instance.SetHUDVisibility(false, 0f);

        if (fakeBottomPanel) fakeBottomPanel.gameObject.SetActive(true);
    }

    private IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        // 1. TITOLO
        if (titleText != null)
        {
            yield return StartCoroutine(FadeTextAlpha(titleText, 0f, 1f, titleFadeInDuration));
            yield return new WaitForSeconds(stayTime);
            yield return StartCoroutine(FadeTextAlpha(titleText, 1f, 0f, fadeOutTime));
        }
        
        yield return new WaitForSeconds(delayBetweenTexts);
        
        // 2. INTRO LORE
        if (introText != null)
        {
            string originalIntro = introText.text;
            introText.alpha = 1f; 
            
            yield return StartCoroutine(TypewriterAI(introText, originalIntro));
            yield return StartCoroutine(BlinkCursorDuringStay(introText, stayTime));
            yield return StartCoroutine(FadeTextAlpha(introText, 1f, 0f, fadeOutTime));
        }

        // 3. RIVELAZIONE
        StartCoroutine(FadeCanvasGroup(blackScreen, 1f, 0f, 2.0f));
        if (tapPromptPanel) StartCoroutine(FadeCanvasGroup(tapPromptPanel, 0f, 1f, 1.5f));

        yield return new WaitForSeconds(1.5f);
        blackScreen.blocksRaycasts = false; 

        if (GameManager.Instance != null)
            GameManager.Instance.OnFirstInput += OnFirstInteraction;
    }

    // --- LOGICA AI GENERATIVA ---
    private IEnumerator TypewriterAI(TextMeshProUGUI targetText, string content)
    {
        targetText.text = content + cursorSymbol;
        targetText.ForceMeshUpdate();
        
        _currentTextInfo = targetText.textInfo;
        int totalChars = _currentTextInfo.characterCount;

        for (int i = 0; i < totalChars; i++)
        {
            SetCharAlpha(targetText, i, 0);
        }
        targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        int contentLen = content.Length; 

        for (int i = 0; i < contentLen; i++)
        {
            StartCoroutine(FadeInChar(targetText, i));

            float currentDelay = typingDelay;
            char c = content[i];

            if (c == '.' || c == '?' || c == '!' || c == ':') currentDelay += punctuationPause;
            else if (c == ',' || c == ';') currentDelay += punctuationPause * 0.5f;

            if (c == ' ' && Random.value < hesitationChance) yield return new WaitForSeconds(hesitationDuration);

            yield return new WaitForSeconds(currentDelay);
        }
    }

    private IEnumerator BlinkCursorDuringStay(TextMeshProUGUI targetText, float duration)
    {
        int cursorIndex = targetText.textInfo.characterCount - 1;
        float elapsed = 0f;
        bool isVisible = true;

        SetCharAlpha(targetText, cursorIndex, 255);
        targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(cursorBlinkSpeed);
            elapsed += cursorBlinkSpeed;
            isVisible = !isVisible;
            SetCharAlpha(targetText, cursorIndex, isVisible ? (byte)255 : (byte)0);
            targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
        SetCharAlpha(targetText, cursorIndex, 0);
        targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private IEnumerator FadeInChar(TextMeshProUGUI txt, int charIndex)
    {
        float timer = 0f;
        while (timer < charFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / charFadeDuration);
            byte alpha = (byte)(t * 255);
            SetCharAlpha(txt, charIndex, alpha);
            txt.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return null;
        }
        SetCharAlpha(txt, charIndex, 255);
        txt.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void SetCharAlpha(TextMeshProUGUI txt, int charIndex, byte alpha)
    {
        if (charIndex >= txt.textInfo.characterCount) return;
        TMP_CharacterInfo cInfo = txt.textInfo.characterInfo[charIndex];
        if (!cInfo.isVisible) return; 
        int materialIndex = cInfo.materialReferenceIndex;
        int vertexIndex = cInfo.vertexIndex;
        Color32[] vertexColors = txt.textInfo.meshInfo[materialIndex].colors32;
        for (int i = 0; i < 4; i++) {
            Color32 baseColor = vertexColors[vertexIndex + i]; 
            baseColor.a = alpha;
            vertexColors[vertexIndex + i] = baseColor;
        }
    }
    // --- FINE LOGICA AI ---

    private void OnFirstInteraction()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFirstInput -= OnFirstInteraction;
            GameManager.Instance.IsFirstSession = false;
            GameManager.Instance.SaveGame();
        }

        if (UIManager.Instance != null)
            UIManager.Instance.SetHUDVisibility(true, 1.0f);

        ReparentButtonNow();

        if (tapPromptPanel) StartCoroutine(FadeCanvasGroup(tapPromptPanel, tapPromptPanel.alpha, 0f, 0.5f));

        StartCoroutine(FocusCameraOnFirstEmitter());

        Destroy(gameObject, 2.0f);
    }

    // --- COROUTINE MODIFICATA ---
    private IEnumerator FocusCameraOnFirstEmitter()
    {
        yield return new WaitForEndOfFrame();

        var visuals = FindFirstObjectByType<PlanetPopulationVisuals>();
        var orbitCam = FindFirstObjectByType<PlanetOrbitCamera>();

        if (visuals != null && orbitCam != null)
        {
            Vector3 targetPos = visuals.GetLastAddedWorldPosition();

            if (targetPos != Vector3.zero)
            {
                // Qui passiamo 'cameraFocusOffset' invece di 0f
                orbitCam.AnimateToLookAt(targetPos, cameraFocusOffset, cameraRotationDuration, null);
                Debug.Log($"[IntroSequence] Camera focusing with offset: {cameraFocusOffset}");
            }
        }
    }

    private void ReparentButtonNow()
    {
        if (energyButtonRect == null || realBottomPanel == null || placeholderInRealPanel == null) return;
        int targetIndex = placeholderInRealPanel.GetSiblingIndex();
        energyButtonRect.SetParent(realBottomPanel);
        energyButtonRect.SetSiblingIndex(targetIndex);
        energyButtonRect.localScale = Vector3.one;
        energyButtonRect.localPosition = Vector3.zero;
        Destroy(placeholderInRealPanel.gameObject);
        if (fakeBottomPanel) fakeBottomPanel.gameObject.SetActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(realBottomPanel);
    }

    private IEnumerator FadeTextAlpha(TextMeshProUGUI text, float start, float end, float duration)
    {
        if (text == null) yield break;
        float timer = 0f;
        text.alpha = start;
        while (timer < duration) { 
            timer += Time.deltaTime; 
            text.alpha = Mathf.Lerp(start, end, timer / duration); 
            yield return null; 
        }
        text.alpha = end;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        if (cg == null) yield break;
        float timer = 0f;
        while (timer < duration) { 
            timer += Time.deltaTime; 
            cg.alpha = Mathf.Lerp(start, end, timer / duration); 
            yield return null; 
        }
        cg.alpha = end;
    }
}