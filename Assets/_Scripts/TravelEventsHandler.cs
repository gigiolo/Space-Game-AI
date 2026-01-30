using UnityEngine;
using System;

public class TravelEventsHandler : MonoBehaviour
{
    // Usiamo l'ID 100 per differenziarlo dai regali giornalieri (ID 200)
    private const int TRAVEL_NOTIF_ID = 100;

    private void Start()
    {
        // Ci iscriviamo all'evento: appena la nave parte, programmiamo la notifica
        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.OnTravelStarted += ScheduleTravelNotification;
        }
    }

    private void OnDestroy()
    {
        // Buona pratica: pulire i riferimenti agli eventi quando l'oggetto sparisce
        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.OnTravelStarted -= ScheduleTravelNotification;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Se mettiamo in pausa, programmiamo (o aggiorniamo) la notifica
            ScheduleTravelNotification();
        }
        else 
        {
            // Se torniamo in gioco, cancelliamo la notifica pendente perché siamo già qui!
            if(LocalNotificationController.Instance != null) 
                LocalNotificationController.Instance.CancelNotification(TRAVEL_NOTIF_ID);
        }
    }

    private void OnApplicationQuit()
    {
        ScheduleTravelNotification();
    }

    public void ScheduleTravelNotification()
    {
        // Controlli di sicurezza
        if (PlanetManager.Instance == null || LocalNotificationController.Instance == null) return;

        // Se non stiamo viaggiando, non c'è nulla da notificare
        if (!PlanetManager.Instance.isTraveling) return;

        DateTime startTime = PlanetManager.Instance.travelStartTime;
        double durationSeconds = PlanetManager.Instance.GetTotalTravelDuration();
        
        // Calcoliamo il momento esatto dell'arrivo
        DateTime arrivalTime = startTime.AddSeconds(durationSeconds);

        // Programmiamo la notifica solo se il tempo non è già passato
        if (arrivalTime > DateTime.Now)
        {
            string planetName = PlanetManager.Instance.GetNextPlanetData()?.planetName ?? "nuovo pianeta";

            LocalNotificationController.Instance.ScheduleNotification(
                "Arrivo a Destinazione! 🚀",
                $"La tua flotta è atterrata su {planetName}. Vieni a colonizzarlo!",
                arrivalTime,
                TRAVEL_NOTIF_ID 
            );
            
            Debug.Log($"[Notifica Viaggio] Programmata per le: {arrivalTime}");
        }
    }
}