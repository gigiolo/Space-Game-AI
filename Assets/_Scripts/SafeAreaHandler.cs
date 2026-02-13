using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea = Rect.zero;
    private Vector2 _lastScreenSize = Vector2.zero;
    private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        // Controlliamo se la Safe Area o lo schermo sono cambiati (es. rotazione del telefono)
        if (_lastSafeArea != Screen.safeArea || 
            _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height || 
            _lastOrientation != Screen.orientation)
        {
            Refresh();
        }
    }

    void Refresh()
    {
        Rect safeArea = Screen.safeArea;

        // Memorizziamo lo stato attuale per il prossimo controllo
        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        _lastOrientation = Screen.orientation;

        // Convertiamo le coordinate dei pixel in coordinate relative (0.0 a 1.0)
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Applichiamo gli anchor al RectTransform
        // Questo farà sì che il pannello si restringa per stare dentro la zona sicura
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
        
        Debug.Log($"[Safe Area] Applicata: {safeArea} su schermo {Screen.width}x{Screen.height}");
    }
}