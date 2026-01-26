using UnityEngine;
using BreakInfinity; // Assicurati di avere questo using se necessario, anche se qui usiamo float

/*
 * ISTRUZIONI:
 * 1. Crea un Quad nella scena figlio del pianeta.
 * 2. Crea un Materiale usando lo Shader Graph "GravitationalLensShader".
 * 3. Assegna il materiale al Quad.
 * 4. Aggiungi questo script al Quad.
 */

public class GravityDistortionController : MonoBehaviour
{
    [Header("Configurazione")]
    [Tooltip("Il MeshRenderer dell'anello (il Quad). Se vuoto, cerca su se stesso.")]
    public MeshRenderer ringRenderer;

    [Header("Intensità Effetto")]
    [Tooltip("Valore minimo della distorsione (quando il tasto non è premuto).")]
    public float minDistortion = 0.0f;

    [Tooltip("Valore massimo della distorsione (quando il tasto è al massimo).")]
    public float maxDistortion = 2.0f; // Aggiusta questo valore in base a quanto è forte il tuo shader

    [Header("Animazione")]
    [Tooltip("Quanto velocemente l'effetto reagisce ai cambiamenti.")]
    public float smoothingSpeed = 5.0f;

    [Tooltip("Se vero, l'anello guarderà sempre verso la camera.")]
    public bool billboardEffect = true;

    // ID della proprietà nello shader per performance (sostituisci col nome esatto nel tuo Shader Graph)
    // Se nel Blackboard dello shader hai scritto "DistortionStrength", Unity aggiunge spesso un "_" davanti.
    private int _distortionID = Shader.PropertyToID("_DistortionStrength");
    
    private Material _materialInstance;
    private float _currentStrength = 0f;
    private Transform _cameraTransform;

    void Start()
    {
        if (ringRenderer == null) ringRenderer = GetComponent<MeshRenderer>();
        
        if (ringRenderer != null)
        {
            // Creiamo un'istanza per non modificare il materiale su disco
            _materialInstance = ringRenderer.material;
        }
        else
        {
            Debug.LogError("GravityDistortionController: Nessun Renderer trovato!");
            enabled = false;
        }

        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // 1. Calcola il target basato sul Moltiplicatore del GameManager
        // Il multiplier va da 1.0 (Idle) a EnergyButton_MaxMultiplier (es. 3.0)
        float currentMult = GameManager.Instance.CurrentEnergyMultiplier;
        float maxMult = GameManager.Instance.EffectiveMaxMultiplier;

        // Normalizziamo il valore tra 0 e 1
        // Mathf.InverseLerp restituisce 0 se current == 1, e 1 se current == max
        float normalizedPower = Mathf.InverseLerp(1.0f, maxMult, currentMult);

        // Calcoliamo la forza target interpolando tra min e max distorsione
        float targetStrength = Mathf.Lerp(minDistortion, maxDistortion, normalizedPower);

        // 2. Applica smoothing per evitare scatti bruschi
        _currentStrength = Mathf.Lerp(_currentStrength, targetStrength, Time.deltaTime * smoothingSpeed);

        // 3. Imposta il valore nello shader
        if (_materialInstance != null)
        {
            _materialInstance.SetFloat(_distortionID, _currentStrength);
        }

        // 4. Billboard Effect (L'anello guarda sempre la camera)
        if (billboardEffect && _cameraTransform != null)
        {
            transform.LookAt(transform.position + _cameraTransform.rotation * Vector3.forward,
                             _cameraTransform.rotation * Vector3.up);
        }
    }
    
    // Pulizia della memoria
    void OnDestroy()
    {
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }
}