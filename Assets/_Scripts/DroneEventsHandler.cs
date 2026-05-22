// --- File: _Scripts\DroneEventsHandler.cs ---
using UnityEngine;
using System;

public class DroneEventsHandler : MonoBehaviour
{
    // ID base univoco per evitare conflitti con Viaggi (100), Regali (200) o Offline (300)
    private const int BASE_DRONE_NOTIF_ID = 4000;

    private void Start()
    {
        // Al rientro in gioco, annulliamo eventuali notifiche pendenti
        CancelAllDroneNotifications();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // L'app va in background: calcoliamo e programmiamo i rientri
            ScheduleDroneNotifications();
        }
        else
        {
            // L'app torna attiva: puliamo le notifiche di sistema
            CancelAllDroneNotifications();
        }
    }

    private void OnApplicationQuit()
    {
        ScheduleDroneNotifications();
    }

    private void ScheduleDroneNotifications()
    {
        if (DroneManager.Instance == null || LocalNotificationController.Instance == null) return;

        foreach (var drone in DroneManager.Instance.activeDrones)
        {
            // 1. Calcoliamo esattamente quanto manca usando il tempo universale (UTC)
            TimeSpan timeRemaining = drone.returnTime - DateTime.UtcNow;

            // 2. Se manca ancora del tempo (la sonda è in volo)
            if (!drone.isCompleted && timeRemaining.TotalSeconds > 0)
            {
                // 3. FIX: Convertiamo in ora LOCALE aggiungendo il tempo mancante al fuso orario del telefono
                DateTime localDeliveryTime = DateTime.Now.Add(timeRemaining);

                LocalNotificationController.Instance.ScheduleNotification(
                    "Sonda Rientrata! 🛰️",
                    $"La spedizione {drone.missionData.missionName} ha completato l'analisi. Raccogli i dati!",
                    localDeliveryTime,
                    BASE_DRONE_NOTIF_ID + drone.slotIndex
                );
                
                Debug.Log($"[DroneEvents] Notifica programmata per sonda {drone.slotIndex} alle (Ora Locale): {localDeliveryTime}");
            }
        }
    }

    private void CancelAllDroneNotifications()
    {
        if (LocalNotificationController.Instance == null || DroneManager.Instance == null) return;

        // Pulizia massiva basata sul numero massimo teorico di slot per sicurezza
        for (int i = 0; i < DroneManager.Instance.unlockedSlots + 5; i++)
        {
            LocalNotificationController.Instance.CancelNotification(BASE_DRONE_NOTIF_ID + i);
        }
    }
}