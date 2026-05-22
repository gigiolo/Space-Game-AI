// --- File: _Scripts\TravelEventsHandler.cs ---
using UnityEngine;
using System;

public class TravelEventsHandler : MonoBehaviour
{
    // Usiamo l'ID 100 per differenziarlo dai regali giornalieri (ID 200) o droni (4000)
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
        // Pulizia dei riferimenti agli eventi quando l'oggetto viene distrutto
        if (PlanetManager.Instance != null)
        {
            PlanetManager.Instance.OnTravelStarted -= ScheduleTravelNotification;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // L'app va in background: programmiamo (o aggiorniamo) la notifica
            ScheduleTravelNotification();
        }
        else 
        {
            // L'app torna attiva: cancelliamo la notifica pendente
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

        // Se non è in corso alcun viaggio, si interrompe l'esecuzione
        if (!PlanetManager.Instance.isTraveling) return;

        DateTime startTimeUTC = PlanetManager.Instance.travelStartTime;
        double durationSeconds = PlanetManager.Instance.GetTotalTravelDuration();
        
        // Calcolo del momento dell'arrivo in UTC
        DateTime arrivalTimeUTC = startTimeUTC.AddSeconds(durationSeconds);

        // Calcolo del tempo rimanente effettivo
        TimeSpan timeRemaining = arrivalTimeUTC - DateTime.UtcNow;

        // Se il tempo rimanente è positivo, il viaggio è ancora in corso
        if (timeRemaining.TotalSeconds > 0)
        {
            // Conversione nell'orario locale del dispositivo
            DateTime localArrivalTime = DateTime.Now.Add(timeRemaining);

            string planetName = PlanetManager.Instance.GetNextPlanetData()?.planetName ?? "nuovo pianeta";

            LocalNotificationController.Instance.ScheduleNotification(
                "Arrivo a Destinazione! 🚀",
                $"La tua flotta è atterrata su {planetName}. Vieni a colonizzarlo!",
                localArrivalTime,
                TRAVEL_NOTIF_ID 
            );
            
            Debug.Log($"[Notifica Viaggio] Programmata per le (Ora Locale): {localArrivalTime}");
        }
    }
}