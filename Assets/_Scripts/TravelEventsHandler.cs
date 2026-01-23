using UnityEngine;
using System;

public class TravelEventsHandler : MonoBehaviour
{
    private const int TRAVEL_NOTIF_ID = 100;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) ScheduleTravelNotification();
        else 
        {
            // Cancelliamo SOLO la notifica del viaggio quando rientriamo, lasciando quella del Daily attiva
            if(LocalNotificationController.Instance) 
                LocalNotificationController.Instance.CancelNotification(TRAVEL_NOTIF_ID);
        }
    }

    private void OnApplicationQuit()
    {
        ScheduleTravelNotification();
    }

    private void ScheduleTravelNotification()
    {
        if (PlanetManager.Instance == null || LocalNotificationController.Instance == null) return;

        if (PlanetManager.Instance.isTraveling)
        {
            DateTime startTime = PlanetManager.Instance.travelStartTime;
            double durationSeconds = PlanetManager.Instance.GetTotalTravelDuration();
            DateTime arrivalTime = startTime.AddSeconds(durationSeconds);

            if (arrivalTime > DateTime.Now)
            {
                // Usiamo l'ID 100
                LocalNotificationController.Instance.ScheduleNotification(
                    "Viaggio Completato! 🚀",
                    "La tua nave è arrivata a destinazione. Tocca per atterrare!",
                    arrivalTime,
                    TRAVEL_NOTIF_ID 
                );
            }
        }
    }
}