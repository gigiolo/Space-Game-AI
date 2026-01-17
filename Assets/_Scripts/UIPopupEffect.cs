using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIPopupEffect : MonoBehaviour
{
    [Header("Impostazioni Generali")]
    [Tooltip("La scala da cui parte l'oggetto (0 = invisibile, 1 = grandezza naturale)")]
    public float startScale = 0.7f;

    [Header("---- APERTURA ----")]
    public float openDuration = 0.3f;
    [Tooltip("Disegna qui il rimbalzo. L'asse Y è la progressione (0 a 1). Puoi andare sopra 1 per fare 'bounce'.")]
    public AnimationCurve openCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.8f, 1.05f), new Keyframe(1, 1));

    [Header("---- CHIUSURA ----")]
    public float closeDuration = 0.2f;
    [Tooltip("Curva di chiusura. Solitamente una curva morbida 'Ease In'.")]
    public AnimationCurve closeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    private CanvasGroup _cg;
    private Vector3 _defaultScale = Vector3.one;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        // Memorizza la scala che l'oggetto ha nell'editor (di solito 1,1,1)
        _defaultScale = transform.localScale; 
    }

    // --- SETUP AUTOMATICO PER L'EDITOR ---
    // Questa funzione viene chiamata da Unity appena aggiungi lo script all'oggetto.
    // Imposta dei valori di default carini così non devi farlo a mano.
    private void Reset()
    {
        // Curva Apertura: Rimbalzo elastico (va da 0, supera 1.1, torna a 1)
        openCurve = new AnimationCurve(
            new Keyframe(0, 0), 
            new Keyframe(0.7f, 1.1f), 
            new Keyframe(1, 1)
        );
        
        // Curva Chiusura: Accelerazione morbida (Anticipation)
        closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    private void OnEnable()
    {
        // 1. Reset immediato (piccolo e trasparente)
        transform.localScale = _defaultScale * startScale;
        _cg.alpha = 0f;

        // 2. Avvia apertura
        StopAllCoroutines();
        StartCoroutine(RunAnimation(true));
    }

    public void Close()
    {
        StopAllCoroutines();
        StartCoroutine(RunAnimation(false));
    }

    private IEnumerator RunAnimation(bool isOpening)
    {
        float timer = 0f;
        float duration = isOpening ? openDuration : closeDuration;
        AnimationCurve curve = isOpening ? openCurve : closeCurve;

        // Valori di partenza e arrivo per la Scala
        Vector3 startSize = _defaultScale * startScale;
        Vector3 endSize = _defaultScale;

        // Valori di partenza e arrivo per l'Alpha
        float startAlpha = 0f;
        float endAlpha = 1f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // Valutiamo la curva (0 -> 1)
            float curveValue = curve.Evaluate(progress);

            if (isOpening)
            {
                // APERTURA: Seguiamo la curva (che può andare oltre 1 per il rimbalzo)
                // Usiamo LerpUnclamped per permettere il bounce
                transform.localScale = Vector3.LerpUnclamped(startSize, endSize, curveValue);
                _cg.alpha = Mathf.Lerp(startAlpha, endAlpha, progress); // Alpha sempre lineare o quasi
            }
            else
            {
                // CHIUSURA: Invertiamo la logica. 
                // La curva va da 0 a 1, ma noi vogliamo andare da GRANDE a PICCOLO.
                // Quindi usiamo (1 - curveValue) oppure invertiamo Start/End nel Lerp.
                
                // Qui usiamo la curva per interpolare "indietro"
                transform.localScale = Vector3.LerpUnclamped(endSize, startSize, curveValue);
                _cg.alpha = Mathf.Lerp(endAlpha, startAlpha, progress);
            }

            yield return null;
        }

        // Fine sicura
        if (isOpening)
        {
            transform.localScale = endSize;
            _cg.alpha = 1f;
        }
        else
        {
            transform.localScale = startSize;
            _cg.alpha = 0f;
            gameObject.SetActive(false); // Spegni tutto
        }
    }
}