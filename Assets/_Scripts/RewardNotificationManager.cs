using UnityEngine;
using BreakInfinity;

public class RewardNotificationManager : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("Il NotificationManager a cui inviare le notifiche generate.")]
    public NotificationManager notificationManager;
    [Tooltip("Il GameManager per accedere ai dati economici.")]
    public GameManager gameManager;

    [Header("Configurazione Ricompensa")]
    [Tooltip("Ogni quanti secondi di gioco attivo generare una notifica premio.")]
    public float rewardIntervalSeconds = 60f;

    [Tooltip("Fattore per cui moltiplicare la produzione di energia al secondo per calcolare il premio.")]
    public float rewardMultiplier = 15f;

    [Tooltip("Quante notifiche premio si possono accumulare prima di smettere di generarne.")]
    public int maxAccumulableNotifications = 2;

    public float Timer { get; private set; }
    public int CurrentNotificationCount { get; private set; } = 0;

    private void Start()
    {
        // Trova i manager se non sono stati assegnati nell'inspector
        if (notificationManager == null) notificationManager = NotificationManager.Instance;
        if (gameManager == null) gameManager = GameManager.Instance;
    }

    private void Update()
    {
        Timer += Time.deltaTime;

        if (Timer >= rewardIntervalSeconds)
        {
            Timer -= rewardIntervalSeconds;
            GenerateRewardNotification();
        }
    }

    public void LoadState(float timer)
    {
        Timer = timer;
    }

    private void GenerateRewardNotification()
    {
        if (CurrentNotificationCount >= maxAccumulableNotifications)
        {
            Debug.Log("Limite notifiche raggiunto. Nessuna nuova notifica verrà generata.");
            return;
        }

        // Calcolo della ricompensa
        BigDouble energyProductionRate = gameManager.EffectiveIncomePerSec;
        BigDouble rewardAmount = energyProductionRate * rewardMultiplier;

        if (rewardAmount <= 0)
        {
            Debug.Log("Produzione di energia a zero. Nessuna ricompensa generata.");
            return;
        }

        // Creazione dei dati per la notifica
        NotificationData data = new NotificationData(
            "Bonus Energia!",
            $"Hai ricevuto {rewardAmount.ToString("F2")} energia bonus!",
            notificationManager.giftIcon, // Usiamo un'icona standard dal manager
            () => {
                gameManager.AddEnergy(rewardAmount);
                CurrentNotificationCount--; // Decrementa quando il premio viene riscosso
            },
            false
        );

        // Invio della notifica al sistema
        notificationManager.SpawnNotification(data);
        CurrentNotificationCount++; // Incrementa quando la notifica viene creata
        Debug.Log($"Notifica premio generata con {rewardAmount.ToString("F2")} energia.");
    }
}
