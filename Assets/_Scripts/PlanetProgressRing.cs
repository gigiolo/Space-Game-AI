using UnityEngine;
using UnityEngine.UI;
using BreakInfinity;

[RequireComponent(typeof(Image))]
public class PlanetProgressRing : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Colore della barra mentre carica.")]
    [SerializeField] private Color progressColor = new Color(0f, 1f, 1f, 1f); // Ciano
    
    [Tooltip("Colore della barra quando il pianeta è pronto per il viaggio.")]
    [SerializeField] private Color readyColor = new Color(0f, 1f, 0f, 1f); // Verde

    [Tooltip("Velocità di riempimento visivo (smoothing).")]
    [SerializeField] private float smoothSpeed = 5.0f;

    [Header("Optional Feedback")]
    [Tooltip("Un oggetto opzionale (es. icona lucchetto aperto o particelle) da attivare quando pronto.")]
    [SerializeField] private GameObject readyIndicatorObj;

    // Riferimenti interni
    private Image _ringImage;
    private float _currentFill = 0f;

    private void Awake()
    {
        _ringImage = GetComponent<Image>();
        _ringImage.type = Image.Type.Filled; // Forza il tipo Filled per sicurezza
        _ringImage.fillMethod = Image.FillMethod.Radial360;
        _ringImage.fillOrigin = (int)Image.Origin360.Top;
        
        // Reset iniziale
        _ringImage.fillAmount = 0f;
        if (readyIndicatorObj) readyIndicatorObj.SetActive(false);
    }

    private void Update()
    {
        // 1. Controlli di sicurezza
        if (PlanetManager.Instance == null) return;

        // 2. Recupero Dati
        PlanetData currentPlanet = PlanetManager.Instance.GetCurrentPlanetData();
        
        // Se non c'è un pianeta o siamo all'ultimo, barra piena o vuota a scelta
        if (currentPlanet == null) 
        {
            _ringImage.fillAmount = 0f;
            return;
        }

        // 3. Calcolo Progresso (Current / Required)
        // Usiamo ToDouble() perché BigDouble / BigDouble restituisce un BigDouble, 
        // ma fillAmount vuole un float.
        BigDouble currentValue = PlanetManager.Instance.CalculatePlanetValue();
        BigDouble requiredValue = currentPlanet.requiredPlanetValue;

        float targetFill = 0f;

        if (requiredValue > 0)
        {
            // Dividiamo e convertiamo in float. Clampiamo a 1 per non rompere la UI.
            double ratio = (currentValue / requiredValue).ToDouble();
            targetFill = (float)ratio;
            if (targetFill > 1f) targetFill = 1f;
        }

        // 4. Animazione Fluida (Lerp)
        _currentFill = Mathf.Lerp(_currentFill, targetFill, Time.deltaTime * smoothSpeed);
        _ringImage.fillAmount = _currentFill;

        // 5. Gestione Colori e Stati
        bool isReady = targetFill >= 1.0f;

        if (isReady)
        {
            _ringImage.color = readyColor;
            if (readyIndicatorObj && !readyIndicatorObj.activeSelf) 
                readyIndicatorObj.SetActive(true);
        }
        else
        {
            _ringImage.color = progressColor;
            if (readyIndicatorObj && readyIndicatorObj.activeSelf) 
                readyIndicatorObj.SetActive(false);
        }
    }
}