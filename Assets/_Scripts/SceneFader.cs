using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Durate")]
    [Tooltip("Quanto ci mette il pannello nero ad apparire/scomparire")]
    [SerializeField] private float backgroundFadeDuration = 0.5f;

    [Tooltip("Quanto ci mette l'icona/testo ad apparire/scomparire")]
    [SerializeField] private float contentFadeDuration = 0.5f;

    [Tooltip("Quanto tempo rimangono visibili icona e testo prima di sparire (dopo che la scena è caricata)")]
    [SerializeField] private float contentStayDuration = 2.0f;

    [Header("Riferimenti UI")]
    [Tooltip("Il CanvasGroup dell'oggetto PANEL (Sfondo)")]
    public CanvasGroup backgroundCanvasGroup;

    [Tooltip("Il CanvasGroup dell'oggetto LOADINGCONTENT (Icona + Testo)")]
    public CanvasGroup contentCanvasGroup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // --- FIX ERRORE ROOT ---
            // Se questo oggetto non ha genitori (è root), lo rendiamo persistente.
            // Se ha un genitore (es. Core_Systems), sarà il genitore a gestire la persistenza.
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            // Inizializzazione di sicurezza
            if (backgroundCanvasGroup) 
            {
                backgroundCanvasGroup.alpha = 0f;
                backgroundCanvasGroup.blocksRaycasts = false;
            }
            
            if (contentCanvasGroup) 
            {
                contentCanvasGroup.alpha = 0f;
                contentCanvasGroup.blocksRaycasts = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeAndLoadScene(string sceneName, System.Action onSceneLoaded = null)
    {
        StartCoroutine(SequenceRoutine(sceneName, onSceneLoaded));
    }

    private IEnumerator SequenceRoutine(string sceneName, System.Action onSceneLoaded)
    {
        // Blocchiamo i click subito
        if (backgroundCanvasGroup) backgroundCanvasGroup.blocksRaycasts = true;

        // --------------------------------------------------------
        // FASE 1: Dissolvenza SFONDO (0 -> 1)
        // --------------------------------------------------------
        yield return StartCoroutine(FadeCanvasGroup(backgroundCanvasGroup, 0f, 1f, backgroundFadeDuration));

        // --------------------------------------------------------
        // FASE 2: Dissolvenza CONTENUTO (0 -> 1)
        // --------------------------------------------------------
        // Iniziamo a caricare la scena in background MENTRE appare il logo
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false; // Blocchiamo l'attivazione finché non siamo pronti

        yield return StartCoroutine(FadeCanvasGroup(contentCanvasGroup, 0f, 1f, contentFadeDuration));

        // --------------------------------------------------------
        // FASE 3: Attesa (Stay Duration)
        // --------------------------------------------------------
        // Aspettiamo i secondi richiesti (es. 2.0s) per far leggere il testo/vedere il logo
        yield return new WaitForSecondsRealtime(contentStayDuration);

        // Nel frattempo, assicuriamoci che la scena abbia finito di caricare in memoria (90%)
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // --------------------------------------------------------
        // FASE 4: Attivazione Scena & Logica
        // --------------------------------------------------------
        op.allowSceneActivation = true;
        
        // Aspettiamo che la nuova scena sia effettivamente attiva
        while (!op.isDone)
        {
            yield return null;
        }

        // Eseguiamo la callback (es. Reset Emitter a 0) ORA che la scena è caricata ma lo schermo è ancora coperto
        onSceneLoaded?.Invoke();

        // --------------------------------------------------------
        // FASE 5: Dissolvenza Inversa CONTENUTO (1 -> 0)
        // --------------------------------------------------------
        yield return StartCoroutine(FadeCanvasGroup(contentCanvasGroup, 1f, 0f, contentFadeDuration));

        // --------------------------------------------------------
        // FASE 6: Dissolvenza Inversa SFONDO (1 -> 0)
        // --------------------------------------------------------
        yield return StartCoroutine(FadeCanvasGroup(backgroundCanvasGroup, 1f, 0f, backgroundFadeDuration));

        // Sblocchiamo i click alla fine
        if (backgroundCanvasGroup) backgroundCanvasGroup.blocksRaycasts = false;
    }

    // Helper generico per fare fade di qualsiasi CanvasGroup
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        if (cg == null) yield break;

        float timer = 0f;
        cg.alpha = start;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            cg.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }
        cg.alpha = end;
    }
}