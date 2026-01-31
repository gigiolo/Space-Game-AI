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

    [Tooltip("Asse di rotazione per PIANETA e LUCI (solitamente Y negativo per antiorario).")]
    public Vector3 mainRotationAxis = new Vector3(0, -1, 0);

    [Space(20)]
    [Header("--- 1. PIANETA (La Mesh) ---")]
    public Transform planetRoot;
    [Tooltip("Velocità massima finale per il pianeta.")]
    public float planetMaxSpeed = 3000f;
    [Tooltip("Curva accelerazione pianeta (1=Lineare, 6=Esponenziale forte).")]
    public float planetExponent = 6.0f;

    [Space(10)]
    [Header("--- 2. CIELO (Sky Sphere) ---")]
    public Transform skySphere;
    [Tooltip("Asse di rotazione SPECIFICO per il cielo. (Es: 1, 0, 0 ruota verticalmente).")]
    public Vector3 skyRotationAxis = new Vector3(1, 0.5f, 0); // Default inclinato
    [Tooltip("Velocità massima finale per il cielo.")]
    public float skyMaxSpeed = 500f; 
    [Tooltip("Curva accelerazione cielo.")]
    public float skyExponent = 4.0f;

    [Space(10)]
    [Header("--- 3. LUCI (Lighting Rig) ---")]
    public Transform lightingRig;
    [Tooltip("Velocità massima finale per le luci (Sole).")]
    public float lightMaxSpeed = 3000f;
    [Tooltip("Curva accelerazione luci.")]
    public float lightExponent = 6.0f;

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

        // Normalizziamo i vettori per sicurezza (così la velocità dipende solo da MaxSpeed)
        Vector3 planetAxisNorm = mainRotationAxis.normalized;
        Vector3 skyAxisNorm = skyRotationAxis.normalized;

        // Calcoliamo quando far partire il fade
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

            // --- CALCOLO ROTAZIONI INDIPENDENTI ---
            float progress = Mathf.Clamp01(timer / totalDuration);

            // 1. Pianeta (Usa asse principale)
            if (planetRoot != null)
            {
                float pSpeed = Mathf.Pow(progress, planetExponent) * planetMaxSpeed;
                planetRoot.Rotate(planetAxisNorm, pSpeed * Time.deltaTime, Space.Self);
            }

            // 2. Cielo (Usa il NUOVO asse specifico)
            if (skySphere != null)
            {
                float sSpeed = Mathf.Pow(progress, skyExponent) * skyMaxSpeed;
                // Space.World è solitamente meglio per il cielo per ignorare la rotazione del padre
                skySphere.Rotate(skyAxisNorm, sSpeed * Time.deltaTime, Space.World);
            }

            // 3. Luci (Usa asse principale per coerenza col pianeta)
            if (lightingRig != null)
            {
                float lSpeed = Mathf.Pow(progress, lightExponent) * lightMaxSpeed;
                lightingRig.Rotate(planetAxisNorm, lSpeed * Time.deltaTime, Space.World);
            }

            yield return null;
        }

        // Sicurezza finale
        if (!fadeTriggered) onTriggerFade?.Invoke();

        // 3. Fine totale
        onAnimationComplete?.Invoke();
    }
}