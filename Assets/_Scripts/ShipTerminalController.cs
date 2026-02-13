using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class ShipTerminalController : MonoBehaviour
{
    public static ShipTerminalController Instance;

    [Header("Dati")]
    public LoreDatabase database;

    [Header("UI References")]
    public TextMeshProUGUI terminalText;
    [Tooltip("Il RectTransform del pannello che si deve allargare/stringere.")]
    public RectTransform panelRect;

    [Header("Configurazione Cursore")]
    public string cursorSymbol = "█";
    public float blinkSpeed = 0.5f;

    [Header("Settings Animazione Pannello")]
    public float expandDuration = 0.5f;
    public float messageStayTime = 5.0f;
    public float intervalMin = 20f;
    public float intervalMax = 45f;

    [Header("Settings AI Typing (Generative Style)")]
    [Tooltip("Tempo in secondi per il fade-in di un singolo carattere.")]
    public float charFadeDuration = 0.2f; // Durata della dissolvenza del singolo carattere
    
    [Tooltip("Velocità di avanzamento tra un carattere e l'altro.")]
    public float typingDelay = 0.03f; 

    [Tooltip("Ritardo extra aggiunto dopo la punteggiatura (.,?!).")]
    public float punctuationPause = 0.2f;

    [Tooltip("Probabilità (0-1) di una esitazione TRA LE PAROLE.")]
    public float hesitationChance = 0.15f; // Aumentata un po' dato che capita solo sugli spazi
    public float hesitationDuration = 0.4f;

    // Variabili interne
    private float _targetWidth;
    private Coroutine _currentRoutine;
    private TMP_TextInfo _textInfo;

    private void Awake()
    {
        Instance = this;
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
    }

    private IEnumerator Start()
    {
        // 1. Pulizia testo
        if (terminalText) terminalText.text = "";

        // 2. SALVATAGGIO DIMENSIONI
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        yield return new WaitForEndOfFrame();

        if (panelRect != null)
        {
            _targetWidth = panelRect.rect.width;
            if (_targetWidth < 10) _targetWidth = 500f; // Fallback di sicurezza
            SetWidth(0);
        }

        if (PlanetManager.Instance != null)
            PlanetManager.Instance.OnTravelStarted += OnTravelStarted;

        StartCoroutine(RandomMessageLoop());
    }

    private void OnDestroy()
    {
        if (PlanetManager.Instance != null)
            PlanetManager.Instance.OnTravelStarted -= OnTravelStarted;
    }

    private void SetWidth(float width)
    {
        if (panelRect)
        {
            panelRect.sizeDelta = new Vector2(width, panelRect.sizeDelta.y);
        }
    }

    private IEnumerator RandomMessageLoop()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            float waitTime = Random.Range(intervalMin, intervalMax);
            yield return new WaitForSeconds(waitTime);

            if (database != null)
            {
                ShowLog(database.GetRandomLog());
            }
        }
    }

    private void OnTravelStarted()
    {
        if (database) ShowLog(database.travelLog, true);
    }

    public void ShowSystemMessage(string message)
    {
        ShowLog(message, true);
    }

    private void ShowLog(string content, bool priority = false)
    {
        if (_currentRoutine != null)
        {
            if (priority) StopCoroutine(_currentRoutine);
            else return;
        }

        _currentRoutine = StartCoroutine(AISequenceRoutine(content));
    }

    // --- NUOVA LOGICA: Vertex Fade per effetto "Generativo" ---
    private IEnumerator AISequenceRoutine(string fullText)
    {
        // 1. Pulizia e Preparazione
        terminalText.text = "";
        terminalText.color = new Color(terminalText.color.r, terminalText.color.g, terminalText.color.b, 0); // Nascondi tutto inizialmente

        // 2. ESPANSIONE (Apre la tendina)
        yield return StartCoroutine(AnimateWidth(0f, _targetWidth, expandDuration));

        // 3. Setup del Testo completo ma INVISIBILE
        // Aggiungiamo il cursore alla fine della stringa
        string displayText = fullText + cursorSymbol;
        terminalText.text = displayText;
        terminalText.color = new Color(terminalText.color.r, terminalText.color.g, terminalText.color.b, 1); // Rendi base visibile
        terminalText.ForceMeshUpdate();

        _textInfo = terminalText.textInfo;
        int totalChars = _textInfo.characterCount;

        // Rendiamo tutti i caratteri trasparenti (Alpha 0) manipolando i vertici
        Color32[] newVertexColors;
        for (int i = 0; i < totalChars; i++)
        {
            SetCharAlpha(i, 0);
        }
        terminalText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        // 4. CICLO DI SCRITTURA (FADE-IN)
        // Iteriamo fino a totalChars - 1 (l'ultimo è il cursore, che trattiamo diversamente)
        int contentLen = fullText.Length;
        
        for (int i = 0; i < contentLen; i++)
        {
            // --- A. Fade-In del carattere corrente ---
            // Avviamo una coroutine separata per il fade di QUESTO carattere
            StartCoroutine(FadeInChar(i));

            // --- B. Gestione Cursore ---
            // Il cursore (che è l'ultimo char della stringa) deve essere sempre visibile o lampeggiare?
            // Per semplicità, lo teniamo spento durante la scrittura o lo accendiamo solo alla fine.
            // Oppure, possiamo farlo "saltare" ma è complesso coi vertex. 
            // In questo stile "flow", il cursore appare spesso solo quando l'AI si ferma.
            
            // --- C. Ritardo di Scrittura (Typing Speed) ---
            float currentDelay = typingDelay;

            // Logica Punteggiatura
            char c = fullText[i];
            if (c == '.' || c == '?' || c == '!' || c == ':') currentDelay += punctuationPause;
            else if (c == ',') currentDelay += punctuationPause * 0.5f;

            // --- D. Logica Hesitation (Solo sugli SPAZI) ---
            if (c == ' ')
            {
                // L'AI ha appena finito una parola. Esita prima della prossima?
                if (Random.value < hesitationChance)
                {
                    // Mostriamo il cursore mentre pensa?
                    SetCharAlpha(totalChars - 1, 255); // Accendi cursore (ultimo char)
                    terminalText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                    
                    yield return new WaitForSeconds(hesitationDuration);
                    
                    SetCharAlpha(totalChars - 1, 0); // Spegni cursore
                    terminalText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                }
            }

            yield return new WaitForSeconds(currentDelay);
        }

        // 5. ATTESA LETTURA (Cursore lampeggiante alla fine)
        int cursorIndex = totalChars - 1; // L'indice del simbolo cursore
        float elapsedWait = 0f;
        bool cursorOn = true;

        while (elapsedWait < messageStayTime)
        {
            // Blink Cursore
            SetCharAlpha(cursorIndex, cursorOn ? (byte)255 : (byte)0);
            terminalText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            
            cursorOn = !cursorOn;
            yield return new WaitForSeconds(blinkSpeed);
            elapsedWait += blinkSpeed;
        }

        // 6. CHIUSURA
        terminalText.text = "";
        yield return StartCoroutine(AnimateWidth(_targetWidth, 0f, expandDuration));
        _currentRoutine = null;
    }

    // Coroutine per sfumare un singolo carattere da Alpha 0 a 255
    private IEnumerator FadeInChar(int charIndex)
    {
        float timer = 0f;
        while (timer < charFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / charFadeDuration);
            byte alpha = (byte)(t * 255);

            SetCharAlpha(charIndex, alpha);
            
            // Nota: Aggiornare i dati dei vertici in continuazione per ogni carattere può essere pesante
            // se ci sono centinaia di caratteri contemporanei, ma per un terminale va bene.
            terminalText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            
            yield return null;
        }
        // Assicura visibilità finale
        SetCharAlpha(charIndex, 255);
        terminalText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    // Helper per settare l'alpha dei 4 vertici di un carattere
    private void SetCharAlpha(int charIndex, byte alpha)
    {
        if (charIndex >= _textInfo.characterCount) return;

        TMP_CharacterInfo cInfo = _textInfo.characterInfo[charIndex];
        if (!cInfo.isVisible) return; // Salta spazi vuoti o caratteri invisibili

        int materialIndex = cInfo.materialReferenceIndex;
        int vertexIndex = cInfo.vertexIndex;
        Color32[] vertexColors = _textInfo.meshInfo[materialIndex].colors32;

        // Un carattere ha 4 vertici (Top-Left, Top-Right, Bottom-Right, Bottom-Left)
        // Dobbiamo cambiare l'alpha a tutti mantenendo il colore originale (solitamente bianco/verde terminale)
        // Nota: Assumiamo che il colore base sia già settato correttamente nel componente UI
        Color32 baseColor = terminalText.color; 

        for (int i = 0; i < 4; i++)
        {
            vertexColors[vertexIndex + i].a = alpha;
        }
    }

    private IEnumerator AnimateWidth(float startW, float endW, float duration)
    {
        if (panelRect == null) yield break;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            t = Mathf.SmoothStep(0, 1, t);

            float currentW = Mathf.Lerp(startW, endW, t);
            SetWidth(currentW);

            yield return null;
        }
        SetWidth(endW);
    }
}