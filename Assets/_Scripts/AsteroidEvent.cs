using UnityEngine;
using BreakInfinity; 

public class AsteroidEvent : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private ParticleSystem explosionVFX;
    [SerializeField] private GameObject meshObject; 
    
    [Header("Rewards")]
    [SerializeField] private float rewardMultiplier = 15f;
    [Tooltip("Se vero, questo asteroide darà premi doppi!")]
    [SerializeField] private bool isGolden = false;

    // --- VARIABILI INTERNE CURVA ---
    private Vector3 _p0; 
    private Vector3 _p1; 
    private Vector3 _p2; 
    private float _duration;
    private float _timeElapsed;
     
    // --- STATI ---
    private bool _isInitialized = false;
    private bool _hasBeenHit = false; 
    private System.Action<AsteroidEvent> _onDespawnCallback;

    public void Setup(Vector3 start, Vector3 end, Vector3 curveControlPoint, float speed, System.Action<AsteroidEvent> onDespawn)
    {
        _p0 = start;
        _p2 = end;
        _p1 = curveControlPoint;
        _onDespawnCallback = onDespawn;

        float approxDistance = Vector3.Distance(start, end);
        if (speed <= 0) speed = 1f;
        _duration = approxDistance / speed;
        _timeElapsed = 0f;

        transform.position = _p0;
        transform.rotation = Quaternion.identity; 
        _hasBeenHit = false;

        if (meshObject)
        {
            meshObject.SetActive(true);
            meshObject.transform.rotation = Random.rotation; 
        }
        
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        _timeElapsed += Time.deltaTime;
        float t = _timeElapsed / _duration;

        if (t >= 1f)
        {
            Despawn();
            return;
        }

        Vector3 newPos = CalculateBezierPoint(t, _p0, _p1, _p2);
        Vector3 direction = (newPos - transform.position).normalized;
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        transform.position = newPos;

        if (!_hasBeenHit && meshObject)
            meshObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector3 p = uu * p0; 
        p += 2 * u * t * p1; 
        p += tt * p2;        
        
        return p;
    }

    public void OnHit()
    {
        if (!_isInitialized || _hasBeenHit) return;

        _hasBeenHit = true; 

        GiveReward(); 
        PlayExplosion();
        
        if (meshObject) meshObject.SetActive(false);

        if (explosionVFX != null)
            Invoke(nameof(Despawn), 1.0f); 
        else
            Despawn();
    }

    private void GiveReward()
    {
        if (GameManager.Instance == null) return;
        
        // --- 1. Calcolo Base Energia ---
        BigDouble energyReward = GameManager.Instance.EffectiveIncomePerSec * rewardMultiplier;
        if (energyReward <= 0) energyReward = 10;

        // --- 2. Calcolo Base Iridio (2-10) ---
        int iridiumReward = Random.Range(2, 11);

        // --- 3. Bonus Golden ---
        if (isGolden)
        {
            energyReward *= 5;
            iridiumReward *= 2;
        }

        // Assegnazione valute
        GameManager.Instance.AddEnergy(energyReward);
        GameManager.Instance.AddPureIridium(iridiumReward);

        // --- 4. FEEDBACK VISIVO NEL TESTO UI ---
        // Chiama il metodo che modifica il testo "Pure Iridium" nella UI per 2 secondi
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPureIridiumFeedback(iridiumReward);
        }
    }

    private void PlayExplosion()
    {
        if (explosionVFX != null) 
        {
            explosionVFX.Play(true); 
        }
    }

    private void Despawn()
    {
        _isInitialized = false; 
        _onDespawnCallback?.Invoke(this);
    }
}