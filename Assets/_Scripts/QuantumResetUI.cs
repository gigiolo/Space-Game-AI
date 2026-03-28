// --- File: _Scripts\QuantumResetUI.cs ---
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;

public class QuantumResetUI : MonoBehaviour
{
    [Header("Riferimenti Pannello")]
    [Tooltip("Il pannello principale da attivare/disattivare")]
    [SerializeField] private GameObject menuPanel;

    [Header("Zona: Nodi Scientifici")]
    [Tooltip("Mostra quanti nodi possiedi attualmente")]
    [SerializeField] private TextMeshProUGUI ownedNodesText;

    [Header("Zona: Manipolatori Quantistici")]
    [Tooltip("Mostra quanti manipolatori possiedi attualmente")]
    [SerializeField] private TextMeshProUGUI ownedManipulatorsText;

    [Header("Zona: Statistiche Bonus")]
    [Tooltip("Testo descrittivo: 'Ogni nodo fornisce il X%'")]
    [SerializeField] private TextMeshProUGUI singleNodeBonusDescText;
    
    [Tooltip("Testo che mostra il bonus totale attuale: '+X%'")]
    [SerializeField] private TextMeshProUGUI totalEarningBonusText;

    [Header("Zona: Azione (Reset)")]
    [Tooltip("Il testo dentro o sopra il bottone che dice quanti nodi guadagnerai")]
    [SerializeField] private TextMeshProUGUI gainNodesText;
    [Tooltip("Il bottone da premere per resettare")]
    [SerializeField] private Button performResetButton;
    [Tooltip("Bottone per chiudere la finestra")]
    [SerializeField] private Button closeButton;

    // FIX: Flag per evitare la chiusura istantanea al primo avvio
    private bool _isOpenedByClick = false;
    
    // --- NUOVO: La "Memoria" del pannello ---
    private bool _returnToPlanetStatusOnClose = false;

    private void Start()
    {
        if (menuPanel != null)
        {
            if (!_isOpenedByClick) menuPanel.SetActive(false);
            
            if (UIManager.Instance != null) UIManager.Instance.RegisterMenu(menuPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ToggleMenu);
        }

        if (performResetButton != null)
        {
            performResetButton.onClick.AddListener(OnResetClicked);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEconomyUpdated += RefreshUI;
            
            if (singleNodeBonusDescText != null)
            {
                double bonusPercent = GameManager.Instance.nodesBonusPerUnit * 100;
                singleNodeBonusDescText.text = $"Ogni Nodo Scientifico fornisce un bonus di Earning del {bonusPercent}%";
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEconomyUpdated -= RefreshUI;
        }
    }

    // --- NUOVO: Apriamo il pannello sapendo da dove veniamo ---
    public void OpenFromPlanetStatus()
    {
        _returnToPlanetStatusOnClose = true; // Accendiamo la memoria!
        if (menuPanel != null && !menuPanel.activeSelf)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        
        _isOpenedByClick = true; 

        bool opening = !menuPanel.activeSelf;

        if (opening)
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(menuPanel);
            menuPanel.SetActive(true);
            PlanetOrbitCamera.IsInputBlocked = true; 
            RefreshUI();
        }
        else
        {
            UIPopupEffect effect = menuPanel.GetComponent<UIPopupEffect>();
            if (effect != null) 
            {
                // IL TRUCCO È QUI: Se dobbiamo tornare al pianeta, diciamo al popup di 
                // NON scatenare l'evento di chiusura dell'Inspector (che aprirebbe le opzioni).
                bool triggerInspectorEvent = !_returnToPlanetStatusOnClose;
                effect.Close(triggerInspectorEvent);
                
                // Se la memoria è accesa, richiamiamo la nostra funzione
                if (_returnToPlanetStatusOnClose) Invoke(nameof(OnFullyClosed), 0.25f);
            }
            else 
            {
                menuPanel.SetActive(false);
                OnFullyClosed();
            }
            
            PlanetOrbitCamera.IsInputBlocked = false;
        }
    }

    // --- NUOVO: La funzione chiamata quando il pannello ha finito di chiudersi ---
    public void OnFullyClosed()
    {
        if (_returnToPlanetStatusOnClose)
        {
            _returnToPlanetStatusOnClose = false; // Spegniamo la memoria
            
            // Cerchiamo il pannello del pianeta e lo forziamo ad aprirsi
            PlanetStatusPopup planetPopup = FindFirstObjectByType<PlanetStatusPopup>(FindObjectsInactive.Include);
            if (planetPopup != null) planetPopup.OpenPopup();
        }
    }

    private void RefreshUI()
    {
        if (GameManager.Instance == null || !menuPanel.activeSelf) return;

        if (ownedNodesText != null)
        {
            ownedNodesText.text = FormatNumber(GameManager.Instance.ScientificNodes);
        }

        if (ownedManipulatorsText != null)
        {
            ownedManipulatorsText.text = FormatNumber(GameManager.Instance.QuantumManipulators);
        }

        if (totalEarningBonusText != null)
        {
            BigDouble extraBonusPercent = (GameManager.Instance.EarningsBonus - 1) * 100;
            totalEarningBonusText.text = $"+{FormatNumber(extraBonusPercent)}%";
        }

        BigDouble potentialNodes = GameManager.Instance.CalculatePotentialNodes();
        
        if (gainNodesText != null)
        {
            if (potentialNodes > 0)
            {
                gainNodesText.text = $"<color=#00FFFF>+{FormatNumber(potentialNodes)}</color>";
            }
            else
            {
                gainNodesText.text = "<color=red>0</color>";
            }
        }

        if (performResetButton != null)
        {
            performResetButton.interactable = potentialNodes > 0;
        }
    }

    private string FormatNumber(BigDouble number)
    {
        if (number < 1000) return number.ToString("F0");
        long exponent = (long)BigDouble.Log10(number);
        
        if (exponent < 6) return (number / 1000).ToString("F1") + "k";
        if (exponent < 9) return (number / 1e6).ToString("F2") + "M";
        if (exponent < 12) return (number / 1e9).ToString("F2") + "B";
        if (exponent < 15) return (number / 1e12).ToString("F2") + "T";
        
        return $"{number.Mantissa:F2}e{number.Exponent}";
    }

    private void OnResetClicked()
    {
        if (GameManager.Instance != null)
        {
            // Se il giocatore preme il tasto di reset effettivo, disattiviamo il ritorno automatico 
            // perché il gioco verrà riavviato e l'animazione verrebbe interrotta brutalmente.
            _returnToPlanetStatusOnClose = false; 
            
            ToggleMenu(); 
            GameManager.Instance.PerformQuantumReset();
        }
    }
}