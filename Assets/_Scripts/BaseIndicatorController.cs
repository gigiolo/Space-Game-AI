using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BaseIndicatorController : MonoBehaviour
{
    [Header("Riferimenti")]
    public GameObject uiLabelPrefab;
    public Transform ringTransform;
    public Transform dotTransform;        
    public LineRenderer connectorLine;

    [Header("Configurazione")]
    public int hideAtCount = 10;
    public float circleMargin = 0.1f;
    public float minCircleSize = 0.3f;
    public float ringCameraOffset = 2.01f;

    [Header("Stile Linea")]
    public Gradient lineColorGradient = new Gradient();
    [Range(0.001f, 0.05f)] public float lineWidth = 0.005f;
    public float lineGapFromRing = 0.05f;
    public float textGap = 0.1f;          

    [Header("Stile Pallino (Dot)")]
    [Tooltip("Dimensione del cerchietto pieno.")]
    public float dotScale = 0.05f;

    [Header("Stile UI (HUD)")]
    public bool lockUiToScreen = true;
    public Vector2 uiOffset = new Vector2(300f, 200f);
    public Color labelColor = new Color(0f, 1f, 1f, 1f);

    // Interne
    private PlanetPopulationVisuals _visuals;
    private float _currentAlpha = 0f;
    private Vector3 _currentCenterPos;
    private float _currentRadius;
    private Camera _cam;
    
    private RectTransform _spawnedRect;
    private TextMeshProUGUI _spawnedText;
    private Canvas _canvas;
    
    private Gradient _runtimeGradient;
    private GradientColorKey[] _cachedColorKeys;
    private GradientAlphaKey[] _cachedAlphaKeys;
    private GradientAlphaKey[] _tempAlphaKeys; 

    private void Start()
    {
        _cam = Camera.main;
        _visuals = FindFirstObjectByType<PlanetPopulationVisuals>();

        // Setup Gradiente
        if (lineColorGradient.colorKeys.Length < 2)
        {
            lineColorGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.cyan, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }
        
        _runtimeGradient = new Gradient();
        _runtimeGradient.mode = lineColorGradient.mode;
        _cachedColorKeys = lineColorGradient.colorKeys;
        _cachedAlphaKeys = lineColorGradient.alphaKeys;
        _tempAlphaKeys = new GradientAlphaKey[_cachedAlphaKeys.Length];

        // Setup UI
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

        // Setup Linea
        if (connectorLine)
        {
            connectorLine.positionCount = 2;
            connectorLine.useWorldSpace = true;
            connectorLine.textureMode = LineTextureMode.Tile;
        }

        SetAlpha(0f);
    }

    private void OnDestroy()
    {
        if (_spawnedRect != null) Destroy(_spawnedRect.gameObject);
    }

    private void LateUpdate()
    {
        if (GameManager.Instance == null || _visuals == null || _cam == null) return;

        if (connectorLine) {
            connectorLine.startWidth = lineWidth;
            connectorLine.endWidth = lineWidth;
        }

        int count = GameManager.Instance.EmitterCount;
        var positions = _visuals.GetOccupiedPositions();

        // 1. VISIBILITÀ
        bool shouldShow = count > 0 && count < hideAtCount && positions != null && positions.Count > 0 && _spawnedRect != null;
        
        if (shouldShow && !lockUiToScreen)
        {
             Vector3 viewportPos = _cam.WorldToViewportPoint(_currentCenterPos);
             if (viewportPos.z < 0) shouldShow = false;
        }

        float targetAlpha = shouldShow ? 1f : 0f;
        if (Mathf.Abs(_currentAlpha - targetAlpha) < 0.01f) _currentAlpha = targetAlpha;
        else _currentAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, Time.deltaTime * 5f);

        if (_currentAlpha <= 0.001f)
        {
            SetAlpha(0f);
            return;
        }
        SetAlpha(_currentAlpha);

        // 2. POSIZIONE
        CalculateBounds(positions, out Vector3 targetCenter, out float targetRadius);
        _currentCenterPos = targetCenter;
        _currentRadius = targetRadius;

        // 3. CERCHIO
        if (ringTransform)
        {
            Vector3 camDir = (_cam.transform.position - _currentCenterPos).normalized;
            ringTransform.position = _currentCenterPos + (camDir * ringCameraOffset);
            ringTransform.LookAt(ringTransform.position + _cam.transform.forward, _cam.transform.up);
            
            float diameter = _currentRadius * 2f;
            ringTransform.localScale = new Vector3(diameter, diameter, 1f);
        }

        // 4. UI
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

        // 5. LINEA E PALLINO (ALLINEAMENTO CORRETTO)
        if (connectorLine && ringTransform && _spawnedRect)
        {
            // A. Punto Inizio (Lato Testo) + Gap
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
            Vector3 ringEdgePos = ringTransform.position + (directionToText * (_currentRadius + lineGapFromRing));

            connectorLine.SetPosition(1, lineStartPos);
            connectorLine.SetPosition(0, ringEdgePos);
        }
    }

    private void CalculateBounds(System.Collections.Generic.IReadOnlyList<Vector3> localPositions, out Vector3 centerWorld, out float radius)
    {
        if (localPositions.Count == 0) { centerWorld = Vector3.zero; radius = minCircleSize; return; }
        
        Vector3 sum = Vector3.zero;
        Transform visualsTransform = _visuals.transform;
        foreach (var localPos in localPositions) sum += visualsTransform.TransformPoint(localPos);
        centerWorld = sum / localPositions.Count;
        
        float maxDist = 0f;
        foreach (var localPos in localPositions) {
            Vector3 worldPos = visualsTransform.TransformPoint(localPos);
            float dist = Vector3.Distance(centerWorld, worldPos);
            if (dist > maxDist) maxDist = dist;
        }
        radius = Mathf.Max(maxDist + circleMargin, minCircleSize);
    }

    private void SetAlpha(float globalAlpha)
    {
        Color c = labelColor; c.a = globalAlpha;
        if (_spawnedText) _spawnedText.color = c;
        if (ringTransform) {
            var sr = ringTransform.GetComponent<SpriteRenderer>();
            if (sr) sr.color = c;
        }

        if (dotTransform) {
            dotTransform.gameObject.SetActive(globalAlpha > 0.01f);
            var sr = dotTransform.GetComponent<SpriteRenderer>();
            if (sr) sr.color = c;
        }

        if (connectorLine) 
        {
            for (int i = 0; i < _cachedAlphaKeys.Length; i++)
            {
                _tempAlphaKeys[i] = new GradientAlphaKey(_cachedAlphaKeys[i].alpha * globalAlpha, _cachedAlphaKeys[i].time);
            }
            _runtimeGradient.SetKeys(_cachedColorKeys, _tempAlphaKeys);
            connectorLine.colorGradient = _runtimeGradient;
        }
        
        bool isActive = globalAlpha > 0.01f;
        if (_spawnedRect) _spawnedRect.gameObject.SetActive(isActive);
        if (connectorLine) connectorLine.enabled = isActive;
        if (ringTransform) ringTransform.gameObject.SetActive(isActive);
    }
}