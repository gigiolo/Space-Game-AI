using UnityEngine;
using TMPro;

public class UniversalIndicator : MonoBehaviour
{
    [Header("Riferimenti")]
    public GameObject uiLabelPrefab;
    public Transform ringTransform;      
    public Transform dotTransform;       
    public LineRenderer connectorLine;

    [Header("Configurazione")]
    public float minCircleSize = 0.5f;
    public float ringCameraOffset = 2.01f;

    [Header("Stile Linea")]
    public Gradient lineColorGradient = new Gradient();
    [Range(0.001f, 0.05f)] public float lineWidth = 0.005f;
    public float lineGapFromRing = 0.05f;
    public float textGap = 0.1f;         

    [Header("Stile Pallino (Dot)")]
    public float dotScale = 0.05f;       

    [Header("Stile UI")]
    public bool lockUiToScreen = true;
    public Vector2 uiOffset = new Vector2(300f, 200f);
    public Color labelColor = Color.white;

    // --- TARGETING ---
    private Transform _targetTransform;
    private bool _isActive = false;

    // Interne
    private Camera _cam;
    private RectTransform _spawnedRect;
    private TextMeshProUGUI _spawnedText;
    private Canvas _canvas;
    
    private Gradient _runtimeGradient;
    private GradientColorKey[] _cachedColorKeys;
    private GradientAlphaKey[] _cachedAlphaKeys;
    private GradientAlphaKey[] _tempAlphaKeys; 

    private void Awake()
    {
        _cam = Camera.main;
        
        if (lineColorGradient.colorKeys.Length < 2)
        {
            lineColorGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.red, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }
        
        _runtimeGradient = new Gradient();
        _runtimeGradient.mode = lineColorGradient.mode;
        _cachedColorKeys = lineColorGradient.colorKeys;
        _cachedAlphaKeys = lineColorGradient.alphaKeys;
        _tempAlphaKeys = new GradientAlphaKey[_cachedAlphaKeys.Length];

        if (connectorLine)
        {
            connectorLine.positionCount = 2;
            connectorLine.useWorldSpace = true;
            connectorLine.textureMode = LineTextureMode.Tile;
        }
    }

    private void Start()
    {
        if (UIManager.Instance != null) _canvas = UIManager.Instance.GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();

        if (uiLabelPrefab != null && _canvas != null)
        {
            GameObject newLabelObj = Instantiate(uiLabelPrefab, _canvas.transform);
            newLabelObj.transform.SetAsFirstSibling();
            _spawnedRect = newLabelObj.GetComponent<RectTransform>();
            _spawnedText = newLabelObj.GetComponent<TextMeshProUGUI>();
            newLabelObj.SetActive(false);
            if (_spawnedText) _spawnedText.alignment = TextAlignmentOptions.Right;
        }

        SetVisualsActive(false);
    }

    public void Show(Transform target, string labelText)
    {
        _targetTransform = target;
        _isActive = true;
        SetVisualsActive(true);
        if (_spawnedText) _spawnedText.text = labelText;
    }

    public void Hide()
    {
        _isActive = false;
        _targetTransform = null;
        SetVisualsActive(false);
    }

    private void OnDestroy()
    {
        if (_spawnedRect != null) Destroy(_spawnedRect.gameObject);
    }

    private void LateUpdate()
    {
        if (!_isActive || _targetTransform == null || _cam == null)
        {
            if (_isActive) Hide();
            return;
        }

        if (connectorLine) {
            connectorLine.startWidth = lineWidth;
            connectorLine.endWidth = lineWidth;
        }

        Vector3 targetCenter = _targetTransform.position;
        float targetRadius = minCircleSize;

        // 1. VISIBILITÀ
        bool isVisible = true;
        if (!lockUiToScreen)
        {
             Vector3 viewportPos = _cam.WorldToViewportPoint(targetCenter);
             if (viewportPos.z < 0) isVisible = false;
        }
        
        SetVisualsActive(isVisible);
        if (!isVisible) return;

        // 2. CERCHIO
        if (ringTransform)
        {
            Vector3 camDir = (_cam.transform.position - targetCenter).normalized;
            ringTransform.position = targetCenter + (camDir * ringCameraOffset);
            ringTransform.LookAt(ringTransform.position + _cam.transform.forward, _cam.transform.up);
            
            float diameter = targetRadius * 2f;
            ringTransform.localScale = new Vector3(diameter, diameter, 1f);
        }

        // 3. UI
        Vector3 textAttachPoint3D = Vector3.zero;
        if (_spawnedRect)
        {
            float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
            Vector3 scaledOffset = (Vector3)(uiOffset * scaleFactor);
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            
            _spawnedRect.position = screenCenter + scaledOffset;

            Vector3[] corners = new Vector3[4];
            _spawnedRect.GetWorldCorners(corners);
            Vector3 rightEdgeScreenPos = (corners[2] + corners[3]) / 2f;

            float depth = Vector3.Distance(_cam.transform.position, ringTransform.position);
            textAttachPoint3D = _cam.ScreenToWorldPoint(new Vector3(rightEdgeScreenPos.x, rightEdgeScreenPos.y, depth));
        }

        // 4. LINEA E PALLINO (ALLINEAMENTO CORRETTO)
        if (connectorLine && ringTransform && _spawnedRect)
        {
            // A. Punto Inizio (Lato Testo)
            Vector3 cameraRight = _cam.transform.right;
            Vector3 lineStartPos = textAttachPoint3D + (cameraRight * textGap);

            // B. Posizione Pallino
            if (dotTransform)
            {
                dotTransform.position = lineStartPos;
                dotTransform.LookAt(dotTransform.position + _cam.transform.forward, _cam.transform.up);
                dotTransform.localScale = Vector3.one * dotScale; 
            }

            // C. Punto Fine (Lato Anello)
            Vector3 directionToText = (lineStartPos - ringTransform.position).normalized;
            Vector3 ringEdgePos = ringTransform.position + (directionToText * (targetRadius + lineGapFromRing));

            connectorLine.SetPosition(1, lineStartPos);
            connectorLine.SetPosition(0, ringEdgePos);
        }
    }

    private void SetVisualsActive(bool active)
    {
        if (_spawnedText) {
            _spawnedText.color = labelColor;
            _spawnedText.gameObject.SetActive(active);
        }
        
        if (ringTransform) {
            ringTransform.gameObject.SetActive(active);
            var sr = ringTransform.GetComponent<SpriteRenderer>();
            if (sr) sr.color = labelColor;
        }

        if (dotTransform) {
            dotTransform.gameObject.SetActive(active);
            var sr = dotTransform.GetComponent<SpriteRenderer>();
            if (sr) sr.color = labelColor;
        }

        if (connectorLine) {
            connectorLine.enabled = active;
            if (active) {
                _runtimeGradient.SetKeys(_cachedColorKeys, _cachedAlphaKeys);
                connectorLine.colorGradient = _runtimeGradient;
            }
        }
    }
}