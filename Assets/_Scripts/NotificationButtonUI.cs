using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(LayoutElement))]
public class NotificationButtonUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Button myButton;
    public GameObject adsBadge;

    [Header("Animation Settings")]
    public float popInDuration = 0.4f;
    public float popOutDuration = 0.3f; // Prova ad aumentare a 0.4 o 0.5 se vuoi vederle salire più lentamente
    public float peakScale = 1.2f;

    private NotificationData _data;
    private bool _isExiting = false;
    private LayoutElement _layoutElement;
    private float _originalHeight;

    public void Setup(NotificationData data)
    {
        _data = data;

        if (iconImage != null && data.icon != null) iconImage.sprite = data.icon;
        if (adsBadge != null) adsBadge.SetActive(data.isAds);

        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(OnClick);

        // --- SETUP LAYOUT ---
        _layoutElement = GetComponent<LayoutElement>();
        
        // Disabilitiamo l'espansione flessibile per evitare comportamenti strani
        _layoutElement.flexibleHeight = 0;
        _layoutElement.flexibleWidth = 0;

        // Recuperiamo l'altezza dal RectTransform (es. 100px)
        _originalHeight = GetComponent<RectTransform>().rect.height; 
        
        // Impostiamo l'altezza fissa iniziale
        _layoutElement.preferredHeight = _originalHeight;
        _layoutElement.minHeight = _originalHeight;

        // Reset stato
        _isExiting = false;
        myButton.interactable = true;
        
        // Partiamo invisibili (Scala 0) ma occupando spazio (Height 100)
        transform.localScale = Vector3.zero; 
        
        StartCoroutine(PopInRoutine());
    }

    // --- ENTRATA (Solo Scala) ---
    private IEnumerator PopInRoutine()
    {
        float timer = 0f;
        float expandDuration = popInDuration * 0.6f; 
        
        while (timer < expandDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, timer / expandDuration);
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one * peakScale, t);
            yield return null;
        }

        timer = 0f;
        float shrinkDuration = popInDuration * 0.4f;
        while (timer < shrinkDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, timer / shrinkDuration);
            transform.localScale = Vector3.Lerp(Vector3.one * peakScale, Vector3.one, t);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    // --- USCITA (Scala + Altezza Layout) ---
    private IEnumerator PopOutAndDestroyRoutine()
    {
        float timer = 0f;
        Vector3 startScale = transform.localScale;

        while (timer < popOutDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / popOutDuration;
            
            // Usiamo una curva morbida
            float smoothT = Mathf.SmoothStep(0, 1, t); 

            // 1. Riduciamo la pallina visivamente
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothT);

            // 2. Riduciamo lo spazio fisico occupato
            // IMPORTANTE: Questo funziona solo se "Control Child Size (Height)" è ATTIVO nel Layout Group
            float currentHeight = Mathf.Lerp(_originalHeight, 0, smoothT);
            _layoutElement.preferredHeight = currentHeight;
            _layoutElement.minHeight = currentHeight;

            yield return null;
        }

        // Assicuriamoci di essere a zero alla fine
        transform.localScale = Vector3.zero;
        _layoutElement.preferredHeight = 0;
        _layoutElement.minHeight = 0;
        
        Destroy(gameObject);
    }

    void OnClick()
    {
        if (_isExiting) return;
        _isExiting = true;
        myButton.interactable = false;

        NotificationManager.Instance.OpenPopup(_data);
        StopAllCoroutines();
        StartCoroutine(PopOutAndDestroyRoutine());
    }
}