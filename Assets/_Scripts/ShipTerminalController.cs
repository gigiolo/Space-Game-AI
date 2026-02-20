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
    Tutorial
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
    // Questo valore viene sovrascritto automaticamente all'avvio dalla larghezza reale del RectTransform
    [HideInInspector] public float panelWidth; 
    public float minHeight = 60f;
    
    [Tooltip("Spazio extra calcolato automaticamente dai margini del testo.")]
    private float _textVerticalMargin = 0f; 
    
    [Tooltip("Tempo di adattamento dell'altezza. Consigliato basso (0.05 - 0.08) per reattività immediata.")]
    public float heightSmoothTime = 0.08f; 

    [Header("Velocità Scrittura")]
    public float typingDelay = 0.03f;
    public float messageStayTime = 4.0f;
    public float intervalMin = 20f;
    public float intervalMax = 45f;

    // Variabili interne
    private float _currentPanelHeight;
    private float _targetPanelHeight;
    private float _heightVelocity;
    private bool _isPanelOpen = false;
    private Coroutine _currentRoutine;

    private void Awake()
    {
        Instance = this;
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 1. AUTO-RILEVAMENTO LARGHEZZA
        // Cattura la larghezza esatta che hai impostato visivamente nell'Editor
        if (panelRect != null)
        {
            panelWidth = panelRect.rect.width;
        }
        else
        {
            panelWidth = 500f; // Fallback
        }

        if (terminalText)
        {
            terminalText.text = "";
            terminalText.overflowMode = TextOverflowModes.Overflow;
            terminalText.alignment = TextAlignmentOptions.TopLeft; 
            
            // Calcolo Margini (Padding) basato sugli Anchors Unity
            if (terminalText.rectTransform != null)
            {
                float topGap = Mathf.Abs(terminalText.rectTransform.offsetMax.y);
                float bottomGap = Mathf.Abs(terminalText.rectTransform.offsetMin.y);
                _textVerticalMargin = topGap + bottomGap;

                if (_textVerticalMargin < 20) _textVerticalMargin = 40f; 
            }
        }

        // Setup Stili di default
        if (styles.Count == 0)
        {
            styles.Add(new LogStyle { category = LogCategory.System, color = Color.white, prefix = "SYS >" });
            styles.Add(new LogStyle { category = LogCategory.Tutorial, color = new Color(1f, 0.8f, 0f), prefix = "GUIDE >" });
            styles.Add(new LogStyle { category = LogCategory.Logistics, color = Color.cyan, prefix = "LOG >" });
            styles.Add(new LogStyle { category = LogCategory.Alert, color = new Color(1f, 0.3f, 0.3f), prefix = "WARN >" });
            styles.Add(new LogStyle { category = LogCategory.Philosophy, color = new Color(0.8f, 0.5f, 1f), prefix = "MEMO >" });
        }

        // CHIUSURA INIZIALE
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
            // Adatta l'altezza dinamicamente verso il target
            // Usiamo SmoothDamp per fluidità, ma con un controllo di tolleranza
            if (Mathf.Abs(_currentPanelHeight - _targetPanelHeight) > 0.1f)
            {
                _currentPanelHeight = Mathf.SmoothDamp(_currentPanelHeight, _targetPanelHeight, ref _heightVelocity, heightSmoothTime);
                SetPanelSize(panelWidth, _currentPanelHeight);
            }
            else
            {
                // Se siamo vicinissimi, scatta al valore esatto per evitare micro-movimenti
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

    private IEnumerator RandomMessageLoop()
    {
        yield return new WaitForSeconds(3f);
        while (true)
        {
            float waitTime = Random.Range(intervalMin, intervalMax);
            yield return new WaitForSeconds(waitTime);
            if (database != null && !_isPanelOpen)
                ShowLog(database.GetRandomLog(), LogCategory.Philosophy);
        }
    }

    private void OnTravelStarted()
    {
        if (database) ShowLog(database.travelLog, LogCategory.Logistics, true);
    }

    // --- Metodo Helper per GameManager ---
    public void ShowSystemMessage(string message)
    {
        ShowLog(message, LogCategory.System, true);
    }

    public void ShowLog(string content, LogCategory category = LogCategory.System, bool priority = false)
    {
        if (_isPanelOpen)
        {
            if (priority) StopAllCoroutines();
            else return;
        }
        _currentRoutine = StartCoroutine(TypingRoutine(content, category));
    }

    private IEnumerator TypingRoutine(string content, LogCategory category)
    {
        _isPanelOpen = true;

        // 1. Configurazione Stile
        LogStyle style = styles.Find(x => x.category == category);
        if (string.IsNullOrEmpty(style.prefix) && styles.Count > 0) style = styles[0];
        
        string fullText = $"{style.prefix} {content}";
        
        // Colore Pieno (Alpha 1)
        Color finalColor = style.color; 
        if (finalColor.a <= 0.05f) finalColor.a = 1f;
        terminalText.color = finalColor;
        terminalText.text = ""; 

        // 2. Animazione Apertura (Da sinistra verso destra)
        float timer = 0f;
        float openDuration = 0.25f;
        
        while (timer < openDuration)
        {
            timer += Time.deltaTime;
            float t = timer / openDuration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // EaseOut

            float currentW = Mathf.Lerp(0, panelWidth, t);
            float currentH = Mathf.Lerp(0, minHeight, t);
            SetPanelSize(currentW, currentH);
            yield return null;
        }
        
        _currentPanelHeight = minHeight;
        SetPanelSize(panelWidth, minHeight);

        // 3. Setup Testo per Typing
        terminalText.text = fullText;
        terminalText.maxVisibleCharacters = 99999; 
        terminalText.ForceMeshUpdate(); 
        terminalText.maxVisibleCharacters = 0; 

        int totalChars = fullText.Length;
        _targetPanelHeight = minHeight;

        // 4. Ciclo di Scrittura SINCRONIZZATO
        for (int i = 0; i <= totalChars; i++)
        {
            // --- A. CALCOLO PREVENTIVO DELL'ALTEZZA ---
            // Calcoliamo quanto sarà alto il testo SE mostriamo il carattere 'i'
            float textAvailableWidth = panelWidth - 20f; 
            float currentTextHeight = terminalText.GetPreferredValues(fullText.Substring(0, i), textAvailableWidth, float.PositiveInfinity).y;
            float requiredHeight = Mathf.Max(minHeight, currentTextHeight + _textVerticalMargin);
            
            // Impostiamo il target per l'Update
            _targetPanelHeight = requiredHeight;

            // --- B. SINCRONIZZAZIONE (Blocco di sicurezza) ---
            // Se il pannello è ancora troppo piccolo (più di 2 pixel di differenza),
            // mettiamo in PAUSA la scrittura finché l'Update non ha allargato il pannello.
            // Questo impedisce al testo di apparire "fuori" dal bordo.
            while (_currentPanelHeight < _targetPanelHeight - 2f)
            {
                yield return null; // Aspetta un frame
            }

            // --- C. RIVELAZIONE ---
            terminalText.maxVisibleCharacters = i;

            // Ritardo Punteggiatura
            float delay = typingDelay;
            if (i > 0 && i < totalChars)
            {
                char c = fullText[i - 1];
                if (c == '.' || c == '?' || c == '!') delay *= 6f;
                else if (c == ',') delay *= 3f;
            }
            yield return new WaitForSeconds(delay);
        }

        // 5. Cursore Lampeggiante
        terminalText.text = fullText + cursorSymbol;
        bool cursorOn = true;
        float elapsed = 0f;
        float readTime = Mathf.Max(messageStayTime, totalChars * 0.05f);

        while (elapsed < readTime)
        {
            terminalText.maxVisibleCharacters = cursorOn ? totalChars + 1 : totalChars;
            cursorOn = !cursorOn;
            yield return new WaitForSeconds(blinkSpeed);
            elapsed += blinkSpeed;
        }

        // 6. Chiusura (Collapse)
        terminalText.text = ""; 
        timer = 0f;
        float startH = _currentPanelHeight;
        float closeDuration = 0.25f;
        
        while (timer < closeDuration)
        {
            timer += Time.deltaTime;
            float t = 1f - (timer / closeDuration); 
            t = t * t; // EaseIn

            SetPanelSize(panelWidth * t, startH * t);
            yield return null;
        }

        SetPanelSize(0, 0);
        _isPanelOpen = false;
        _currentRoutine = null;
    }
}