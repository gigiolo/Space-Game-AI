using UnityEngine;
using BreakInfinity; // Necessario per BigDouble

public class AsteroidEvent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f; // Rotazione estetica della roccia
    
    [Header("Rewards")]
    [Tooltip("Moltiplicatore della produzione al secondo (es. 15 = guadagni 15 secondi di produzione)")]
    [SerializeField] private float rewardMultiplier = 15f;
    [SerializeField] private bool isGolden = false; // Per future valute premium

    [Header("Visuals")]
    [SerializeField] private ParticleSystem explosionVFX;
    [SerializeField] private GameObject meshObject; // La sfera/roccia visibile

    private Vector3 _targetPosition;
    private bool _isInitialized = false;
    private System.Action<AsteroidEvent> _onDespawnCallback;

    // Chiamato dal Manager appena nasce
    public void Setup(Vector3 startPos, Vector3 endPos, System.Action<AsteroidEvent> onDespawn)
    {
        transform.position = startPos;
        _targetPosition = endPos;
        _onDespawnCallback = onDespawn;
        
        // Orientiamo l'oggetto verso l'arrivo (opzionale)
        transform.LookAt(endPos); 
        
        if (meshObject) meshObject.SetActive(true);
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        // 1. Movimento lineare verso il target (coordinate mondo)
        float step = movementSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, step);

        // 2. Rotazione estetica della roccia su se stessa
        if (meshObject)
            meshObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // 3. Controllo arrivo: se arriva a destinazione senza essere cliccato, despawna
        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            Despawn();
        }
    }

    // Chiamato dal Manager quando il giocatore clicca
    public void OnHit()
    {
        if (!_isInitialized) return; // Evita doppi click

        GiveReward();
        PlayExplosion();
        
        // Nascondi la mesh subito, ma distruggi l'oggetto dopo l'esplosione
        if (meshObject) meshObject.SetActive(false);
        _isInitialized = false; 

        if (explosionVFX != null)
            Invoke(nameof(Despawn), 1.0f); // Aspetta 1 secondo per i particellari
        else
            Despawn();
    }

    private void GiveReward()
    {
        if (GameManager.Instance == null) return;

        // Calcola ricompensa dinamica basata sull'Income attuale
        BigDouble reward = GameManager.Instance.EffectiveIncomePerSec * rewardMultiplier;
        
        // Se la produzione è 0 (inizio gioco), dai un valore minimo
        if (reward <= 0) reward = 10;

        if (isGolden)
        {
            Debug.Log("TODO: Give Premium Currency");
        }
        else
        {
            GameManager.Instance.AddEnergy(reward);
            Debug.Log($"ASTEROID PRESO! Guadagnata {reward} Energia.");
            
            // Qui in futuro aggiungerai: FloatingTextManager.ShowText(...);
        }
    }

    private void PlayExplosion()
    {
        if (explosionVFX != null) explosionVFX.Play();
    }

    private void Despawn()
    {
        // Avvisa il manager e autodistruggiti
        _onDespawnCallback?.Invoke(this);
    }
}