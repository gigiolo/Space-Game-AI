using UnityEngine;
using TMPro;

public class UIFloatingText : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 100f; // Pixel al secondo (essendo UI, valori alti come 100-200 vanno bene)
    public float fadeDuration = 1.5f;

    private TextMeshProUGUI _textMesh;
    private float _timer;
    private Color _startColor;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
        // Salviamo il colore iniziale impostato nell'editor per mantenerne la tinta
        if (_textMesh != null) _startColor = _textMesh.color;
    }

    public void Setup(string text, Color color)
    {
        if (_textMesh == null) return;

        _textMesh.text = text;
        _textMesh.color = color;
        _startColor = color;
        _timer = 0f;
    }

    private void Update()
    {
        // 1. Muovi verso l'alto (in coordinate UI)
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 2. Dissolvenza (Fade Out)
        _timer += Time.deltaTime;
        float progress = _timer / fadeDuration;

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
        else
        {
            if (_textMesh != null)
            {
                // Sfumiamo l'alpha da 1 a 0 mantenendo il colore originale
                Color newColor = _startColor;
                newColor.a = Mathf.Lerp(1f, 0f, progress);
                _textMesh.color = newColor;
            }
        }
    }
}