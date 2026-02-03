using UnityEngine;
using System.Collections;
using System;

public class QuantumEffectManager : MonoBehaviour
{
    [Header("--- TIMING GLOBALE ---")]
    [Tooltip("Durata TOTALE dell'animazione.")]
    public float totalDuration = 4.0f;

    [Tooltip("Quanto tempo prima della fine deve partire la dissolvenza nera?")]
    public float fadeOverlapDuration = 1.0f;

    [Tooltip("Asse di rotazione globale (es. Y negativo per antiorario).")]
    public Vector3 rotationAxis = new Vector3(0, -1, 0);

    [Space(20)]
    [Header("--- 1. PIANETA (La Mesh) ---")]
    public Transform planetRoot;
    public float planetMaxSpeed = 3000f;
    public float planetExponent = 6.0f;

    [Space(10)]
    [Header("--- 2. CIELO (Rotazione) ---")]
    public Transform skySphere;
    public Vector3 skyRotationAxis = new Vector3(1, 0.5f, 0); 
    public float skyMaxSpeed = 500f; 
    public float skyExponent = 4.0f;

    [Space(10)]
    [Header("--- 3. LUCI (Sole) ---")]
    public Transform lightingRig;
    public float lightMaxSpeed = 3000f;
    public float lightExponent = 6.0f;

    [Space(20)]
    [Header("--- 4. BLUESHIFT & CMB (Colore Cielo) ---")]
    [Tooltip("Trascina qui l'oggetto che ha il MeshRenderer del cielo.")]
    public MeshRenderer skyRenderer;
    
    [Tooltip("La sequenza di colori che il cielo assumerà. \nEsempio: Bianco (Start) -> Blu (Galassie) -> Arancio (CMB) -> Bianco (Fine).")]
    public Gradient colorShiftSequence;

    [Tooltip("Quanto diventa luminosa la luce? (HDR Intensity).")]
    public float maxEmissionIntensity = 4.0f;

    [Tooltip("Come cresce la luminosità. 0 = Normale, 1 = Accecante.")]
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Variabili interne per gestione materiale
    private Material _skyMatInstance;
    private int _baseColorID;
    private int _emissionColorID;
    private Color _originalColor;

    private void Awake()
    {
        // Se non assegnato manualmente, proviamo a prenderlo da skySphere
        if (skyRenderer == null && skySphere != null)
            skyRenderer = skySphere.GetComponent<MeshRenderer>();

        if (skyRenderer != null)
        {
            // Creiamo un'istanza per non rovinare il materiale originale
            _skyMatInstance = skyRenderer.material;
            
            // Cache degli ID shader per performance e compatibilità (URP/Built-in)
            _baseColorID = Shader.PropertyToID("_BaseColor"); // URP standard
            if (!_skyMatInstance.HasProperty(_baseColorID)) 
                _baseColorID = Shader.PropertyToID("_Color"); // Built-in standard
            
            _emissionColorID = Shader.PropertyToID("_EmissionColor");

            // Salviamo il colore originale (anche se la scena si ricarica, è buona pratica)
            if (_skyMatInstance.HasProperty(_baseColorID))
                _originalColor = _skyMatInstance.GetColor(_baseColorID);
        }
    }

    // Metodo pubblico chiamato dal GameManager
    public void PlayRewindEffect(Action onTriggerFade, Action onAnimationComplete)
    {
        StartCoroutine(RewindRoutine(onTriggerFade, onAnimationComplete));
    }

    private IEnumerator RewindRoutine(Action onTriggerFade, Action onAnimationComplete)
    {
        // 1. Setup Iniziale
        if (PlanetSunRotator.Instance != null) PlanetSunRotator.Instance.enabled = false;
        PlanetOrbitCamera.IsInputBlocked = true;

        float timer = 0f;
        bool fadeTriggered = false;

        // Normalizziamo vettori
        Vector3 planetAxisNorm = rotationAxis.normalized;
        Vector3 skyAxisNorm = skyRotationAxis.normalized;

        float triggerFadeTime = Mathf.Max(0, totalDuration - fadeOverlapDuration);

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;
            
            // --- LOGICA FADE ---
            if (!fadeTriggered && timer >= triggerFadeTime)
            {
                onTriggerFade?.Invoke(); 
                fadeTriggered = true;
            }

            // Progression 0 -> 1
            float progress = Mathf.Clamp01(timer / totalDuration);

            // ----------------------------------------------------
            // A. ROTAZIONI (Esponenziali)
            // ----------------------------------------------------
            
            if (planetRoot != null)
            {
                float pSpeed = Mathf.Pow(progress, planetExponent) * planetMaxSpeed;
                planetRoot.Rotate(planetAxisNorm, pSpeed * Time.deltaTime, Space.Self);
            }

            if (skySphere != null)
            {
                float sSpeed = Mathf.Pow(progress, skyExponent) * skyMaxSpeed;
                skySphere.Rotate(skyAxisNorm, sSpeed * Time.deltaTime, Space.World);
            }

            if (lightingRig != null)
            {
                float lSpeed = Mathf.Pow(progress, lightExponent) * lightMaxSpeed;
                lightingRig.Rotate(planetAxisNorm, lSpeed * Time.deltaTime, Space.World);
            }

            // ----------------------------------------------------
            // B. BLUESHIFT & BIG CRUNCH (Colore & Luce)
            // ----------------------------------------------------
            if (_skyMatInstance != null)
            {
                // 1. Campiona il colore dal gradiente in base al progresso
                Color targetTint = colorShiftSequence.Evaluate(progress);

                // 2. Calcola l'intensità (Boost luminoso)
                float intensityMult = 1.0f + (intensityCurve.Evaluate(progress) * maxEmissionIntensity);

                // 3. Applica il colore finale (HDR)
                Color finalColor = targetTint * intensityMult;

                // Applichiamo sia al colore base che all'emissione per sicurezza
                _skyMatInstance.SetColor(_baseColorID, finalColor);
                
                if (_skyMatInstance.HasProperty(_emissionColorID))
                {
                    _skyMatInstance.EnableKeyword("_EMISSION");
                    _skyMatInstance.SetColor(_emissionColorID, finalColor);
                }
            }

            yield return null;
        }

        if (!fadeTriggered) onTriggerFade?.Invoke();
        onAnimationComplete?.Invoke();
    }
}