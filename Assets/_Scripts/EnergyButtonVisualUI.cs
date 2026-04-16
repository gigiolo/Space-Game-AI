// --- File: _Scripts\EnergyButtonVisualUI.cs ---
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class EnergyButtonVisualUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [Tooltip("L'immagine impostata su Filled -> Radial 360")]
    public Image progressRing;
    
    [Header("Impostazioni Colori")]
    [Tooltip("Colore quando tieni premuto l'energia sta salendo")]
    public Color colorCharging = new Color(0f, 1f, 1f, 1f); // Ciano
    
    [Tooltip("Colore quando hai raggiunto il moltiplicatore massimo (Stamina critica)")]
    public Color colorMaxHold = new Color(1f, 0.5f, 0f, 1f); // Arancione
    
    [Tooltip("Colore quando rilasci il bottone e si sta ricaricando (Cooldown)")]
    public Color colorCoolingDown = new Color(0.4f, 0.4f, 0.4f, 0.8f); // Grigio semitrasparente

    [Header("Animazione")]
    [Tooltip("Quanto velocemente compare/scompare il cerchio (Fade)")]
    public float fadeSpeed = 5.0f;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f; // Nasconde all'avvio
    }

    private void Update()
    {
        if (GameManager.Instance == null || progressRing == null) return;

        // Recupera i valori in tempo reale dal GameManager
        float fillAmount = GameManager.Instance.EnergyButtonFillAmount;
        GameManager.EnergyButtonState state = GameManager.Instance.CurrentEnergyButtonState;

        // 1. Aggiorna la quantità visiva
        progressRing.fillAmount = fillAmount;

        // 2. Gestisci il Fade (Trasparenza)
        // Se siamo in Idle ed è completamente carico, lo nascondiamo. Altrimenti lo rendiamo visibile.
        float targetAlpha = (state == GameManager.EnergyButtonState.Idle && fillAmount >= 0.99f) ? 0f : 1f;
        _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // Se è invisibile, è inutile calcolare i colori
        if (_canvasGroup.alpha <= 0.01f) return;

        // 3. Gestisci i colori in base allo stato
        Color targetColor = progressRing.color;

        switch (state)
        {
            case GameManager.EnergyButtonState.RampingUp:
                targetColor = colorCharging;
                break;
            case GameManager.EnergyButtonState.HoldingMax:
                // Quando la barra è quasi vuota, fa lampeggiare di rosso come warning visivo
                if (fillAmount < 0.2f)
                    targetColor = Color.Lerp(colorMaxHold, Color.red, Mathf.PingPong(Time.time * 8f, 1f));
                else
                    targetColor = colorMaxHold;
                break;
            case GameManager.EnergyButtonState.RampingDown:
            case GameManager.EnergyButtonState.Cooldown:
                targetColor = colorCoolingDown;
                break;
        }

        // Transizione morbida del colore
        progressRing.color = Color.Lerp(progressRing.color, targetColor, Time.deltaTime * 10f);
    }
}