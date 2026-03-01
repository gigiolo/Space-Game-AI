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

    private void Start()
    {
        // 1. Inizializzazione Pannello
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.RegisterMenu(menuPanel);
        }

        // 2. Collegamento Eventi Bottoni
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ToggleMenu);
        }

        if (performResetButton != null)
        {
            performResetButton.onClick.AddListener(OnResetClicked);
        }

        // 3. Iscrizione all'aggiornamento dell'economia per avere i numeri sempre freschi
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEconomyUpdated += RefreshUI;
            
            // Impostiamo una volta sola il testo descrittivo del singolo nodo
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

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        
        bool opening = !menuPanel.activeSelf;

        if (opening)
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(menuPanel);
            menuPanel.SetActive(true);
            PlanetOrbitCamera.IsInputBlocked = true; // Blocca input dietro il pannello
            RefreshUI();
        }
        else
        {
            UIPopupEffect effect = menuPanel.GetComponent<UIPopupEffect>();
            if (effect != null) effect.Close();
            else menuPanel.SetActive(false);
            
            PlanetOrbitCamera.IsInputBlocked = false;
        }
    }

    private void RefreshUI()
    {
        if (GameManager.Instance == null || !menuPanel.activeSelf) return;

        // 1. Aggiorna Nodi Posseduti
        if (ownedNodesText != null)
        {
            ownedNodesText.text = FormatNumber(GameManager.Instance.ScientificNodes);
        }

        // 2. Aggiorna Manipolatori Posseduti
        if (ownedManipulatorsText != null)
        {
            ownedManipulatorsText.text = FormatNumber(GameManager.Instance.QuantumManipulators);
        }

        // 3. Aggiorna Bonus Totale
        if (totalEarningBonusText != null)
        {
            // EarningsBonus parte da 1 (che significa 100% della produzione, nessun bonus). 
            // Se ho 1 nodo, diventa 1.5 (+50% bonus). Calcoliamo solo il "plus".
            BigDouble extraBonusPercent = (GameManager.Instance.EarningsBonus - 1) * 100;
            totalEarningBonusText.text = $"+{FormatNumber(extraBonusPercent)}%";
        }

        // 4. Aggiorna Bottone di Reset e Guadagno Potenziale (Modificato)
        BigDouble potentialNodes = GameManager.Instance.CalculatePotentialNodes();
        
        if (gainNodesText != null)
        {
            if (potentialNodes > 0)
            {
                // Mostra solo il numero in azzurro con il "+"
                gainNodesText.text = $"<color=#00FFFF>+{FormatNumber(potentialNodes)}</color>";
            }
            else
            {
                // Se non si guadagna nulla, mostra uno 0 rosso
                gainNodesText.text = "<color=red>0</color>";
            }
        }

        if (performResetButton != null)
        {
            performResetButton.interactable = potentialNodes > 0;
        }
    }

    // Helper per formattare i grandi numeri
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
            ToggleMenu(); // Chiudiamo il pannello per goderci l'animazione visiva
            GameManager.Instance.PerformQuantumReset();
        }
    }
}