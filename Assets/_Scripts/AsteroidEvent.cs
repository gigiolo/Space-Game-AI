using UnityEngine;
using BreakInfinity; // Assicurati di avere questo namespace

public class AsteroidEvent : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private ParticleSystem explosionVFX;
    [SerializeField] private GameObject meshObject; // La sfera/grafica

    [Header("Rewards")]
    [SerializeField] private float rewardMultiplier = 15f;
    [SerializeField] private bool isGolden = false;

    // --- VARIABILI INTERNE CURVA ---
    private Vector3 _p0; // Start
    private Vector3 _p1; // Controllo (Curva)
    private Vector3 _p2; // End
    private float _duration;
    private float _timeElapsed;
    
    // --- STATI ---
    private bool _isInitialized = false;
    private bool _hasBeenHit = false; // Impedisce doppi click
    private System.Action<AsteroidEvent> _onDespawnCallback;

    // Funzione di Setup chiamata dal Manager
    public void Setup(Vector3 start, Vector3 end, Vector3 curveControlPoint, float speed, System.Action<AsteroidEvent> onDespawn)
    {
        _p0 = start;
        _p2 = end;
        _p1 = curveControlPoint;
        _onDespawnCallback = onDespawn;

        // Calcolo durata viaggio
        float approxDistance = Vector3.Distance(start, end);
        if (speed <= 0) speed = 1f;
        _duration = approxDistance / speed;
        _timeElapsed = 0f;

        // Reset posizione e stati
        transform.position = _p0;
        transform.rotation = Quaternion.identity; // Reset rotazione del padre
        _hasBeenHit = false;

        // Attivazione grafica
        if (meshObject)
        {
            meshObject.SetActive(true);
            meshObject.transform.rotation = Random.rotation; // Rotazione iniziale casuale della roccia
        }
        
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        // 1. Avanzamento del tempo
        _timeElapsed += Time.deltaTime;
        float t = _timeElapsed / _duration;

        // Se il tempo è scaduto (e non l'abbiamo preso), despawna
        if (t >= 1f)
        {
            Despawn();
            return;
        }

        // 2. Calcolo nuova posizione sulla curva (Bezier)
        Vector3 newPos = CalculateBezierPoint(t, _p0, _p1, _p2);

        // 3. Orientamento Dinamico (FONDAMENTALE PER L'INERZIA)
        // Calcoliamo la direzione verso cui ci stiamo muovendo
        Vector3 direction = (newPos - transform.position).normalized;
        
        // Ruotiamo l'INTERO oggetto padre verso la direzione di viaggio
        // Così le particelle "Inherit Velocity" sanno dov'è il "davanti"
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // Applichiamo la posizione
        transform.position = newPos;

        // 4. Rotazione Mesh (Estetica aggiuntiva)
        // Ruota solo se l'oggetto è ancora integro (non colpito)
        if (!_hasBeenHit && meshObject)
            meshObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    // Matematica della curva
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
        // Se non è pronto o è già esploso, ignoriamo il click
        if (!_isInitialized || _hasBeenHit) return;

        _hasBeenHit = true; // Segniamo che è stato colpito

        GiveReward();
        PlayExplosion();
        
        // TRUCCO INERZIA:
        // Nascondiamo solo la grafica, ma lasciamo attivo il padre (_isInitialized resta true)
        // Così continua a muoversi invisibile e le particelle ereditano la velocità corretta.
        if (meshObject) meshObject.SetActive(false);

        if (explosionVFX != null)
            Invoke(nameof(Despawn), 1.0f); // Aspetta 1 secondo per far finire l'esplosione
        else
            Despawn();
    }

    private void GiveReward()
    {
        if (GameManager.Instance == null) return;
        
        BigDouble reward = GameManager.Instance.EffectiveIncomePerSec * rewardMultiplier;
        if (reward <= 0) reward = 10;

        if (isGolden) Debug.Log("TODO: Premium Currency");
        else GameManager.Instance.AddEnergy(reward);
    }

    private void PlayExplosion()
    {
        if (explosionVFX != null) 
        {
            // 'true' forza l'avvio anche di tutti i particle system figli (Debris, Core, Sparks)
            explosionVFX.Play(true); 
        }
    }

    private void Despawn()
    {
        _isInitialized = false; // Blocca l'update
        _onDespawnCallback?.Invoke(this);
    }
}