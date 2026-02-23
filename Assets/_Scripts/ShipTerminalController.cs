using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public enum LogCategory
{
    System,
    Philosophy,
    Logistics,
    Alert,
    Tutorial // <--- I messaggi Tutorial ora sono SEMPRE persistenti
}

[System.Serializable]
public struct LogStyle
{
    public LogCategory category;
    public Color color;
    public string prefix;
}

public class ShipTerminalController : MonoBehaviour
{
    public static ShipTerminalController Instance;

    [Header("Dati")]
    public LoreDatabase database;

    [Header("UI References")]
    public TextMeshProUGUI terminalText;
    public RectTransform panelRect;

    [Header("Stili")]
    public List<LogStyle> styles = new List<LogStyle>();

    [Header("Cursore")]
    public string cursorSymbol = "_";
    public float blinkSpeed = 0.5f;

    [Header("Dimensioni & Animazione")]
    [HideInInspector] public float panelWidth; 
    public float minHeight = 60f;
    private float _textVerticalMargin = 0f; 
    public float heightSmoothTime = 0.08f; 

    [Header("Velocità Scrittura")]
    public float typingDelay = 0.03f;
    public float messageStayTime = 4.0f;
    public float intervalMin = 20f;
    public float intervalMax = 45f;

    private float _currentPanelHeight;
    private float _targetPanelHeight;
    private float _heightVelocity;
    private bool _isPanelOpen = false;
    private bool _isPersistentMessage = false;
    private Coroutine _currentRoutine;
    
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        Instance = this;
        if (panelRect == null) panelRect = GetComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) 
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (panelRect != null) panelWidth = panelRect.rect.width;
        else panelWidth = 500f;

        if (terminalText)
        {
            terminalText.text = "";
            terminalText.overflowMode = TextOverflowModes.Overflow;
            terminalText.alignment = TextAlignmentOptions.TopLeft; 
            
            if (terminalText.rectTransform != null)
            {
                float topGap = Mathf.Abs(terminalText.rectTransform.offsetMax.y);
                float bottomGap = Mathf.Abs(terminalText.rectTransform.offsetMin.y);
                _textVerticalMargin = topGap + bottomGap;
                if (_textVerticalMargin < 20) _textVerticalMargin = 40f; 
            }
        }

        if (styles.Count == 0)
        {
            styles.Add(new LogStyle { category = LogCategory.System, color = Color.white, prefix = "SYS >" });
            styles.Add(new LogStyle { category = LogCategory.Tutorial, color = new Color(1f, 0.8f, 0f), prefix = "GUIDE >" });
            styles.Add(new LogStyle { category = LogCategory.Logistics, color = Color.cyan, prefix = "LOG >" });
            styles.Add(new LogStyle { category = LogCategory.Alert, color = new Color(1f, 0.3f, 0.3f), prefix = "WARN >" });
            styles.Add(new LogStyle { category = LogCategory.Philosophy, color = new Color(0.8f, 0.5f, 1f), prefix = "MEMO >" });
        }

        SetPanelSize(0, 0);
        _currentPanelHeight = 0;

        if (PlanetManager.Instance != null)
            PlanetManager.Instance.OnTravelStarted += OnTravelStarted;

        StartCoroutine(RandomMessageLoop());
    }

    private void Update()
    {
        if (_isPanelOpen)
        {
            if (Mathf.Abs(_currentPanelHeight - _targetPanelHeight) > 0.1f)
            {
                _currentPanelHeight = Mathf.SmoothDamp(_currentPanelHeight, _targetPanelHeight, ref _heightVelocity, heightSmoothTime);
                SetPanelSize(panelWidth, _currentPanelHeight);
            }
            else
            {
                _currentPanelHeight = _targetPanelHeight;
                SetPanelSize(panelWidth, _currentPanelHeight);
            }
        }
    }

    private void OnDestroy()
    {
        if (PlanetManager.Instance != null)
            PlanetManager.Instance.OnTravelStarted -= OnTravelStarted;
    }

    private void SetPanelSize(float width, float height)
    {
        if (panelRect) panelRect.sizeDelta = new Vector2(width, height);
    }

    public void SetOverrideVisibility(bool overrideVisibility)
    {
        if (_canvasGroup != null)
            _canvasGroup.ignoreParentGroups = overrideVisibility;
    }

    private IEnumerator RandomMessageLoop()
    {
        yield return new WaitForSeconds(3f);
        while (true)
        {
            float waitTime = Random.Range(intervalMin, intervalMax);
            yield return new WaitForSeconds(waitTime);
            // Non interrompe i tutorial o i messaggi di sistema attivi
            if (database != null && !_isPanelOpen)
                ShowLog(database.GetRandomLog(), LogCategory.Philosophy);
        }
    }

    private void OnTravelStarted()
    {
        if (database) ShowLog(database.travelLog, LogCategory.Logistics, true);
    }

    public void ShowSystemMessage(string message)
    {
        ShowLog(message, LogCategory.System, true);
    }

    // --- NUOVO: Chiusura immediata attivata da bottoni esterni ---
    public void CloseTerminal()
    {
        if (!_isPanelOpen) return;
        
        // Fermiamo la routine attuale (battitura o lampeggio)
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        _isPersistentMessage = false;
        
        // Avviamo direttamente la chiusura
        _currentRoutine = StartCoroutine(CloseRoutine());
    }

    public void ShowLog(string content, LogCategory category = LogCategory.System, bool priority = false)
    {
        if (_isPanelOpen)
        {
            if (priority) 
            {
                if (_currentRoutine != null) StopCoroutine(_currentRoutine);
            }
            else return;
        }

        // SE È UN TUTORIAL, LO RENDIAMO AUTOMATICAMENTE PERSISTENTE
        _isPersistentMessage = (category == LogCategory.Tutorial);

        _currentRoutine = StartCoroutine(TypingRoutine(content, category));
    }

    private IEnumerator TypingRoutine(string content, LogCategory category)
    {
        _isPanelOpen = true;

        int styleIndex = styles.FindIndex(x => x.category == category);
        LogStyle style = styleIndex >= 0 ? styles[styleIndex] : (styles.Count > 0 ? styles[0] : new LogStyle { color = Color.white, prefix = ">" });
        
        string fullText = string.IsNullOrEmpty(style.prefix) ? content : $"{style.prefix} {content}";
        
        Color finalColor = style.color; 
        if (finalColor.a <= 0.05f) finalColor.a = 1f;
        terminalText.color = finalColor;
        terminalText.text = ""; 

        float timer = 0f;
        float openDuration = 0.25f;
        
        while (timer < openDuration)
        {
            timer += Time.deltaTime;
            float t = timer / openDuration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            float currentW = Mathf.Lerp(0, panelWidth, t);
            float currentH = Mathf.Lerp(0, minHeight, t);
            SetPanelSize(currentW, currentH);
            yield return null;
        }
        
        _currentPanelHeight = minHeight;
        SetPanelSize(panelWidth, minHeight);

        terminalText.text = fullText;
        terminalText.maxVisibleCharacters = 99999; 
        terminalText.ForceMeshUpdate(); 
        terminalText.maxVisibleCharacters = 0; 

        int totalChars = fullText.Length;
        _targetPanelHeight = minHeight;

        for (int i = 0; i <= totalChars; i++)
        {
            float textAvailableWidth = panelWidth - 20f; 
            float currentTextHeight = terminalText.GetPreferredValues(fullText.Substring(0, i), textAvailableWidth, float.PositiveInfinity).y;
            float requiredHeight = Mathf.Max(minHeight, currentTextHeight + _textVerticalMargin);
            
            _targetPanelHeight = requiredHeight;

            while (_currentPanelHeight < _targetPanelHeight - 2f)
            {
                yield return null; 
            }

            terminalText.maxVisibleCharacters = i;

            float delay = typingDelay;
            if (i > 0 && i < totalChars)
            {
                char c = fullText[i - 1];
                if (c == '.' || c == '?' || c == '!') delay *= 6f;
                else if (c == ',') delay *= 3f;
            }
            yield return new WaitForSeconds(delay);
        }

        terminalText.text = fullText + cursorSymbol;
        bool cursorOn = true;
        float elapsed = 0f;
        float readTime = Mathf.Max(messageStayTime, totalChars * 0.05f);

        // Finché è persistente, gira all'infinito qui dentro
        while (elapsed < readTime || _isPersistentMessage)
        {
            terminalText.maxVisibleCharacters = cursorOn ? totalChars + 1 : totalChars;
            cursorOn = !cursorOn;
            yield return new WaitForSeconds(blinkSpeed);
            
            if (!_isPersistentMessage) 
            {
                elapsed += blinkSpeed;
            }
        }

        // Se arriva qui naturalmente (non era un tutorial), si chiude
        _currentRoutine = StartCoroutine(CloseRoutine());
    }

    // --- NUOVO: Routine di chiusura isolata per poter essere chiamata da fuori ---
    private IEnumerator CloseRoutine()
    {
        terminalText.text = ""; 
        float timer = 0f;
        float startH = _currentPanelHeight;
        
        // Usiamo un tempo più veloce (0.15s invece di 0.25s) per reattività immediata
        float closeDuration = 0.15f; 
        
        while (timer < closeDuration)
        {
            timer += Time.deltaTime;
            float t = 1f - (timer / closeDuration); 
            t = t * t; 

            SetPanelSize(panelWidth * t, startH * t);
            yield return null;
        }

        SetPanelSize(0, 0);
        _isPanelOpen = false;
        _currentRoutine = null;
        _isPersistentMessage = false;
    }
}