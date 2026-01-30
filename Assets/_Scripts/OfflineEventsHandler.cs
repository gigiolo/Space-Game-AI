using UnityEngine;
using System;

public class OfflineEventsHandler : MonoBehaviour
{
    // ID univoco per evitare conflitti con Viaggi (100) o Regali (200)
    private const int OFFLINE_FULL_ID = 300;

    private void Start()
    {
        // Appena il gioco parte, cancelliamo eventuali notifiche vecchie di "batteria piena"
        // dato che il giocatore è tornato online.
        CancelOfflineNotification();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // L'app sta andando in background: calcoliamo quando si riempirà il deposito
            ScheduleOfflineNotification();
        }
        else
        {
            // L'app è tornata in primo piano: cancelliamo l'avviso
            CancelOfflineNotification();
        }
    }

    private void OnApplicationQuit()
    {
        // L'app si sta chiudendo completamente
        ScheduleOfflineNotification();
    }

    private void ScheduleOfflineNotification()
    {
        // Controlli di sicurezza
        if (GameManager.Instance == null || LocalNotificationController.Instance == null) return;

        // 1. Otteniamo la capacità massima in secondi (es. 2 ore = 7200s)
        // Questa variabile esiste già nel tuo GameManager ed è aggiornata dalle ricerche.
        double maxDurationSeconds = GameManager.Instance.MaxOfflineSeconds;

        // Se per qualche motivo è 0, non facciamo nulla
        if (maxDurationSeconds <= 60) return; // Ignoriamo se la capacità è meno di 1 minuto

        // 2. Calcoliamo l'ora esatta in cui il deposito sarà pieno
        // DateTime.Now è il momento in cui usciamo dal gioco.
        DateTime fullCapacityTime = DateTime.Now.AddSeconds(maxDurationSeconds);

        // 3. Programmiamo la notifica
        LocalNotificationController.Instance.ScheduleNotification(
            "Deposito Energia Pieno! ⚡", 
            "I tuoi accumulatori sono al 100%. Rientra per raccogliere ed evitare sprechi!",
            fullCapacityTime,
            OFFLINE_FULL_ID
        );

        Debug.Log($"[OfflineEvents] Notifica 'Deposito Pieno' programmata per: {fullCapacityTime}");
    }

    private void CancelOfflineNotification()
    {
        if (LocalNotificationController.Instance != null)
        {
            LocalNotificationController.Instance.CancelNotification(OFFLINE_FULL_ID);
        }
    }
}