using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using BreakInfinity;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("--- 1. CANVAS GROUPS (Per Dissolvenze) ---")]
    public CanvasGroup energyButtonCanvasGroup;
    public CanvasGroup topHudCanvasGroup;
    public CanvasGroup researchButtonCanvasGroup;
    public CanvasGroup planetStatusCanvasGroup;
    public CanvasGroup spaceshipButtonCanvasGroup;
    public CanvasGroup hangarButtonCanvasGroup;
    public CanvasGroup shopButtonCanvasGroup;
    public CanvasGroup optionsButtonCanvasGroup;

    [Header("--- 2. PULSE EFFECTS ---")]
    public AttentionPulseEffect energyPulseEffect;
    public AttentionPulseEffect researchPulseEffect;
    public AttentionPulseEffect planetPulseEffect;
    public AttentionPulseEffect spaceshipPulseEffect;
    public AttentionPulseEffect hangarPulseEffect;

    [Header("--- 3. BOTTONI (Per rilevare il click) ---")]
    public Button researchMenuButton;
    public Button planetStatusButton;
    public Button spaceshipButton;
    public Button hangarButton;

    [Header("--- 4. PANNELLI (Per rilevare la chiusura) ---")]
    public GameObject researchPanel;
    public GameObject planetStatusPanel;
    public GameObject spaceshipPanel;
    public GameObject hangarPanel;

    [Header("--- ICONE TEMPORANEE ---")]
    public Image energyButtonIcon;
    [Tooltip("L'icona temporanea per la prima fase (es. la fabbrica/emitter)")]
    public Sprite tutorialEmitterIcon;
    [Tooltip("L'icona originale dell'energia a cui tornare dopo il primo click")]
    public Sprite defaultEnergyIcon; // <--- NUOVO CAMPO ESPLICITO

    [Header("--- TESTI DEL TUTORIAL ---")]
    [TextArea(2,3)] public string step1_ClickEnergy = "INIZIALIZZAZIONE COMPLETATA. TOCCARE [ENERGY] PER AVVIARE LA PRODUZIONE DI NANOBOT.";
    [TextArea(2,3)] public string step2_ClickResearch = "PRODUZIONE STABILE. ENERGIA SUFFICIENTE RILEVATA. ACCEDERE AL TERMINALE [RICERCHE].";
    [TextArea(2,3)] public string step3_ExplainResearch = "MENU RICERCHE. DA QUI SI POSSONO ACQUISTARE POTENZIAMENTI PER AUMENTARE IL NUMERO DEI GENERATORI E LA LORO PRODUZIONE DI ENERGIA. CHIUDERE IL TERMINALE PER CONTINUARE.";
    [TextArea(2,3)] public string step4_HoldEnergy = "NECESSARIO MAGGIORE FLUSSO. TENERE PREMUTO IL PULSANTE [ENERGY] PER GENERARE UN IMPULSO GRAVITAZIONALE.";
    
    [TextArea(2,3)] public string step5_ClickPlanet = "IMPULSO COMPLETATO. ACCEDERE AL PANNELLO [STATO PIANETA].";
    [TextArea(2,3)] public string step6_ExplainPlanet = "QUESTO E' IL PANNELLO DI STATO DEL PIANETA. DA QUI SI PUO' AVVIARE LA PROCEDURA PER I VIAGGI INTERPLANETARI. CHIUDERE PER CONTINUARE.";
    
    [TextArea(2,3)] public string step7_ClickSpaceship = "PER VIAGGIARE SERVONO MEZZI DI TRASPORTO. ACCEDERE AL TERMINALE [NAVI SPAZIALI].";
    [TextArea(2,3)] public string step8_ExplainSpaceship = "MENU NAVI SPAZIALI. DA QUI SI POSSONO ACQUISTARE LE NAVI E I POTENZIAMENTI PER VIAGGIARE TRA I PIANETI. CHIUDERE PER CONTINUARE.";
    
    [TextArea(2,3)] public string step9_ClickHangar = "I SENSORI SONO CIECHI. ACCEDERE ALL'HANGAR SONDE.";
    [TextArea(2,3)] public string step10_ExplainHangar = "LE INFORMAZIONI SUI VARI SETTORI DELLO SPAZIO SONO ANDATE PERSE. DOBBIAMO INVIARE DELLE SONDE E RIPRISTINARE IL DATABASE. CHIUDERE PER CONTINUARE.";
    
    [TextArea(2,3)] public string step11_Final = "CONTINUA AD ESPLORARE LO SPAZIO E PRODURRE ENERGIA. CE NE SERVIRA' MOLTA PER RAGGIUNGERE IL CENTRO DELLA GALASSIA.";

    // Flags di progresso
    private bool _hasClickedEnergy = false;
    private bool _hasClickedResearch = false;
    private bool _hasClickedPlanet = false;
    private bool _hasClickedSpaceship = false;
    private bool _hasClickedHangar = false;

    // Gestione Hold Button
    private bool _isHoldingEnergy = false;
    private float _energyHoldTime = 0f;
    private bool _energyHoldCompleted = false;

    // Sistemi bloccati
    private MenuNotificationController _menuNotif;
    private RewardNotificationManager _rewardNotif;
    private DailyGiftManager _dailyGiftManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsFirstSession)
        {
            SetAllUIVisible();
            Destroy(gameObject);
            return;
        }

        // --- 1. BLOCCA I SISTEMI AUTONOMI ---
        _menuNotif = FindFirstObjectByType<MenuNotificationController>();
        _rewardNotif = FindFirstObjectByType<RewardNotificationManager>();
        _dailyGiftManager = FindFirstObjectByType<DailyGiftManager>();

        if (_menuNotif) 
        {
            _menuNotif.StopAllCoroutines(); 
            _menuNotif.enabled = false;
        }
        if (_rewardNotif) _rewardNotif.enabled = false;
        if (_dailyGiftManager) _dailyGiftManager.enabled = false;

        if (NotificationManager.Instance != null && NotificationManager.Instance.notificationContainer != null)
        {
            NotificationManager.Instance.notificationContainer.gameObject.SetActive(false);
        }

        // --- 2. NASCONDI LA UI ---
        HideCanvasGroup(energyButtonCanvasGroup);
        HideCanvasGroup(topHudCanvasGroup);
        HideCanvasGroup(researchButtonCanvasGroup);
        HideCanvasGroup(planetStatusCanvasGroup);
        HideCanvasGroup(spaceshipButtonCanvasGroup);
        HideCanvasGroup(hangarButtonCanvasGroup);
        HideCanvasGroup(shopButtonCanvasGroup);
        HideCanvasGroup(optionsButtonCanvasGroup);

        // --- 3. LISTENER CLICK SEMPLICI ---
        GameManager.Instance.OnFirstInput += () => _hasClickedEnergy = true;
        if(researchMenuButton) researchMenuButton.onClick.AddListener(() => _hasClickedResearch = true);
        if(planetStatusButton) planetStatusButton.onClick.AddListener(() => _hasClickedPlanet = true);
        if(spaceshipButton) spaceshipButton.onClick.AddListener(() => _hasClickedSpaceship = true);
        if(hangarButton) hangarButton.onClick.AddListener(() => _hasClickedHangar = true);

        // --- 4. EVENT TRIGGER PER IL TIENI PREMUTO ---
        Invoke(nameof(SetupEnergyHoldTracker), 0.2f);
    }

    private void SetupEnergyHoldTracker()
    {
        if (UIManager.Instance == null || UIManager.Instance.mainEnergyButtonObj == null) return;
        
        EventTrigger trigger = UIManager.Instance.mainEnergyButtonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = UIManager.Instance.mainEnergyButtonObj.AddComponent<EventTrigger>();

        EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((data) => { 
            _isHoldingEnergy = true; 
            if (energyPulseEffect != null && energyPulseEffect.isActiveAndEnabled) 
                energyPulseEffect.SetActive(false); 
        });
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entryUp.callback.AddListener((data) => { _isHoldingEnergy = false; });
        trigger.triggers.Add(entryUp);
    }

    private void Update()
    {
        if (_isHoldingEnergy && !_energyHoldCompleted)
        {
            _energyHoldTime += Time.deltaTime;
            if (_energyHoldTime >= 3.0f)
            {
                _energyHoldCompleted = true;
            }
        }
        else if (!_isHoldingEnergy && !_energyHoldCompleted)
        {
            _energyHoldTime = 0f;
        }
    }

    public void StartTutorialSequence()
    {
        StartCoroutine(TutorialRoutine());
    }

    private IEnumerator TutorialRoutine()
    {
        // ==========================================
        // FASE 1: PRIMO CLICK ENERGIA
        // ==========================================
        
        // Assegniamo esplicitamente l'icona del tutorial
        if (energyButtonIcon != null && tutorialEmitterIcon != null)
        {
            energyButtonIcon.sprite = tutorialEmitterIcon;
        }

        ShipTerminalController.Instance.ShowLog(step1_ClickEnergy, LogCategory.Tutorial, true);
        
        yield return StartCoroutine(WaitForLogToFinishTyping());
        
        yield return StartCoroutine(FadeCanvasGroup(energyButtonCanvasGroup, 0f, 1f, 1.0f));
        yield return new WaitForSeconds(2.0f);
        if (energyPulseEffect) energyPulseEffect.SetActive(true);
        
        yield return new WaitUntil(() => _hasClickedEnergy);
        
        ShipTerminalController.Instance.CloseTerminal();
        if (energyPulseEffect) energyPulseEffect.SetActive(false);
        
        // RIPRISTINO ESPLICITO ICONA ORIGINALE
        if (energyButtonIcon != null && defaultEnergyIcon != null) 
        {
            energyButtonIcon.sprite = defaultEnergyIcon;
        }

        GameManager.Instance.TrySpend(GameManager.Instance.CurrentEnergy);
        
        yield return StartCoroutine(FadeCanvasGroup(topHudCanvasGroup, 0f, 1f, 1.0f));
        yield return new WaitForSeconds(0.6f); 

        // ==========================================
        // FASE 2: BOTTONE RICERCA
        // ==========================================
        BigDouble cheapestResearchCost = GetCheapestResearchCost();
        yield return new WaitUntil(() => GameManager.Instance.CurrentEnergy >= cheapestResearchCost);
        
        yield return new WaitForSeconds(0.8f);

        ShipTerminalController.Instance.ShowLog(step2_ClickResearch, LogCategory.Tutorial, true);
        
        yield return StartCoroutine(WaitForLogToFinishTyping());
        
        yield return StartCoroutine(FadeCanvasGroup(researchButtonCanvasGroup, 0f, 1f, 1.0f));
        yield return new WaitForSeconds(2.0f);
        if (researchPulseEffect) researchPulseEffect.SetActive(true);

        yield return new WaitUntil(() => _hasClickedResearch);
        
        if (researchPulseEffect) researchPulseEffect.SetActive(false);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        // ==========================================
        // FASE 3: MENU RICERCHE CHIUSO
        // ==========================================
        ShipTerminalController.Instance.ShowLog(step3_ExplainResearch, LogCategory.Tutorial, true);
        
        yield return new WaitUntil(() => researchPanel != null && !researchPanel.activeInHierarchy);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        // ==========================================
        // FASE 4: OVERCHARGE (Tieni Premuto)
        // ==========================================
        ShipTerminalController.Instance.ShowLog(step4_HoldEnergy, LogCategory.Tutorial, true);
        if (energyPulseEffect) energyPulseEffect.SetActive(true);

        yield return new WaitUntil(() => _energyHoldCompleted);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        // ==========================================
        // FASE 5: PLANET STATUS
        // ==========================================
        ShipTerminalController.Instance.ShowLog(step5_ClickPlanet, LogCategory.Tutorial, true);
        
        yield return StartCoroutine(WaitForLogToFinishTyping());
        
        yield return StartCoroutine(FadeCanvasGroup(planetStatusCanvasGroup, 0f, 1f, 1.0f));
        if (planetPulseEffect) planetPulseEffect.SetActive(true);

        yield return new WaitUntil(() => _hasClickedPlanet);
        if (planetPulseEffect) planetPulseEffect.SetActive(false);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        ShipTerminalController.Instance.ShowLog(step6_ExplainPlanet, LogCategory.Tutorial, true);
        
        yield return new WaitUntil(() => planetStatusPanel != null && !planetStatusPanel.activeInHierarchy);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        // ==========================================
        // FASE 6: SPACESHIP INVITO
        // ==========================================
        ShipTerminalController.Instance.ShowLog(step7_ClickSpaceship, LogCategory.Tutorial, true);
        
        yield return StartCoroutine(WaitForLogToFinishTyping());
        
        yield return StartCoroutine(FadeCanvasGroup(spaceshipButtonCanvasGroup, 0f, 1f, 1.0f));
        yield return new WaitForSeconds(2.0f);
        if (spaceshipPulseEffect) spaceshipPulseEffect.SetActive(true);

        yield return new WaitUntil(() => _hasClickedSpaceship);
        if (spaceshipPulseEffect) spaceshipPulseEffect.SetActive(false);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        // ==========================================
        // FASE 7: SPACESHIP MENU CHIUSO
        // ==========================================
        ShipTerminalController.Instance.ShowLog(step8_ExplainSpaceship, LogCategory.Tutorial, true);
        
        yield return new WaitUntil(() => spaceshipPanel != null && !spaceshipPanel.activeInHierarchy);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        // ==========================================
        // FASE 8: HANGAR INVITO
        // ==========================================
        ShipTerminalController.Instance.ShowLog(step9_ClickHangar, LogCategory.Tutorial, true);
        
        yield return StartCoroutine(WaitForLogToFinishTyping());
        
        yield return StartCoroutine(FadeCanvasGroup(hangarButtonCanvasGroup, 0f, 1f, 1.0f));
        yield return new WaitForSeconds(2.0f);
        if (hangarPulseEffect) hangarPulseEffect.SetActive(true);

        yield return new WaitUntil(() => _hasClickedHangar);
        if (hangarPulseEffect) hangarPulseEffect.SetActive(false);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        // ==========================================
        // FASE 9: HANGAR CHIUSO
        // ==========================================
        ShipTerminalController.Instance.ShowLog(step10_ExplainHangar, LogCategory.Tutorial, true);
        
        yield return new WaitUntil(() => hangarPanel != null && !hangarPanel.activeInHierarchy);
        ShipTerminalController.Instance.CloseTerminal();
        yield return new WaitForSeconds(0.6f);

        // ==========================================
        // FASE 10: CONCLUSIONE
        // ==========================================
        ShipTerminalController.Instance.ShowLog(step11_Final, LogCategory.Tutorial, true);
        
        if(shopButtonCanvasGroup) StartCoroutine(FadeCanvasGroup(shopButtonCanvasGroup, 0f, 1f, 1.0f));
        if(optionsButtonCanvasGroup) StartCoroutine(FadeCanvasGroup(optionsButtonCanvasGroup, 0f, 1f, 1.0f));

        yield return new WaitForSeconds(6.0f);
        ShipTerminalController.Instance.CloseTerminal();

        // --- RIATTIVAZIONE SISTEMI IN BACKGROUND ---
        if (NotificationManager.Instance != null && NotificationManager.Instance.notificationContainer != null)
        {
            NotificationManager.Instance.notificationContainer.gameObject.SetActive(true);
        }

        if (_menuNotif) 
        {
            _menuNotif.enabled = true;
            _menuNotif.StartCoroutine("CheckRoutine"); 
        }
        
        if (_rewardNotif) _rewardNotif.enabled = true;
        if (_dailyGiftManager) _dailyGiftManager.enabled = true;

        Debug.Log("Tutorial Completato e Sistemi Ripristinati!");
    }

    private IEnumerator WaitForLogToFinishTyping()
    {
        yield return new WaitForSeconds(0.3f);

        if (ShipTerminalController.Instance != null && ShipTerminalController.Instance.terminalText != null)
        {
            yield return new WaitUntil(() => 
                ShipTerminalController.Instance.terminalText.maxVisibleCharacters >= 
                ShipTerminalController.Instance.terminalText.text.Length - 1
            );
        }
        
        yield return new WaitForSeconds(0.5f);
    }

    private BigDouble GetCheapestResearchCost()
    {
        if (ResearchManager.Instance == null || ResearchManager.Instance.allResearches.Count == 0) return 10;
        BigDouble minCost = BigDouble.PositiveInfinity;
        foreach (var res in ResearchManager.Instance.allResearches)
        {
            if (res.GetCost() < minCost) minCost = res.GetCost();
        }
        return minCost;
    }

    private void HideCanvasGroup(CanvasGroup cg)
    {
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        if (cg == null) yield break;
        if (end > 0f) cg.blocksRaycasts = true;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, timer / duration);
            yield return null;
        }
        cg.alpha = end;
        if (end == 0f) cg.blocksRaycasts = false;
    }

    private void SetAllUIVisible()
    {
        SetVisible(energyButtonCanvasGroup);
        SetVisible(topHudCanvasGroup);
        SetVisible(researchButtonCanvasGroup);
        SetVisible(planetStatusCanvasGroup);
        SetVisible(spaceshipButtonCanvasGroup);
        SetVisible(hangarButtonCanvasGroup);
        SetVisible(shopButtonCanvasGroup);
        SetVisible(optionsButtonCanvasGroup);
    }

    private void SetVisible(CanvasGroup cg)
    {
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }
    }
}