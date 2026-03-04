// --- File: _Scripts\LocalNotificationController.cs ---
using System;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android; 
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class LocalNotificationController : MonoBehaviour
{
    public static LocalNotificationController Instance;

    private const string AndroidChannelId = "space_events_channel"; 

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            return;
        }

        Instance = this;
        Application.runInBackground = false;
    }

    private void Start()
    {
        if (Instance != this) return;
        if (Application.isEditor) return;

        try 
        {
            SetupAndroidChannel();
            RequestPermissions();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Notification System] Errore inizializzazione: {e.Message}");
        }
    }

    private void SetupAndroidChannel()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = AndroidChannelId,
            Name = "Eventi di Gioco",
            Importance = Importance.High, 
            Description = "Notifiche per viaggi e ricompense",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

    public void RequestPermissions()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }

        string exactAlarmPerm = "android.permission.SCHEDULE_EXACT_ALARM";
        if (!Permission.HasUserAuthorizedPermission(exactAlarmPerm))
        {
            Permission.RequestUserPermission(exactAlarmPerm);
        }
#elif UNITY_IOS
        StartCoroutine(RequestAuthorizationIOS());
#endif
    }

#if UNITY_IOS
    private System.Collections.IEnumerator RequestAuthorizationIOS()
    {
        using (var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, true))
        {
            while (!req.IsFinished) yield return null;
        }
    }
#endif

    public void ScheduleNotification(string title, string body, DateTime deliveryTime, int id)
    {
        // --- NUOVO: CONTROLLO IMPOSTAZIONI ---
        // Se il giocatore ha spento le notifiche (valore 0), blocchiamo tutto subito!
        if (PlayerPrefs.GetInt("Setting_Notifications", 1) == 0)
        {
            return; 
        }
        // ------------------------------------

        if (Application.isEditor)
        {
            Debug.Log($"[EDITOR MOCK] Notifica '{title}' programmata per: {deliveryTime} (ID: {id})");
            return;
        }

        if (deliveryTime <= DateTime.Now) return;

        try
        {
            CancelNotification(id);

#if UNITY_ANDROID
            var notification = new AndroidNotification
            {
                Title = title,
                Text = body,
                FireTime = deliveryTime,
                SmallIcon = "small_icon", 
                LargeIcon = "large_icon" 
            };

            int generatedId = AndroidNotificationCenter.SendNotification(notification, AndroidChannelId);
            SaveNotificationId(id, generatedId);

#elif UNITY_IOS
            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = deliveryTime - DateTime.Now,
                Repeats = false
            };

            var notification = new iOSNotification()
            {
                Identifier = id.ToString(),
                Title = title,
                Body = body,
                ShowInForeground = true,
                ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
                CategoryIdentifier = "game_event",
                ThreadIdentifier = "main_thread",
                Trigger = timeTrigger
            };

            iOSNotificationCenter.ScheduleNotification(notification);
#endif
            Debug.Log($"[Notification] Schedulata OK: {title} (ID: {id})");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Notification Error] Impossibile schedulare: {e.Message}");
        }
    }

    public void CancelNotification(int id)
    {
        if (Application.isEditor) return;

        try
        {
#if UNITY_ANDROID
            int androidId = GetSavedNotificationId(id);
            if (androidId != -1)
            {
                AndroidNotificationCenter.CancelNotification(androidId);
                PlayerPrefs.DeleteKey($"LocalNotif_Map_{id}");
            }
#elif UNITY_IOS
            iOSNotificationCenter.RemoveScheduledNotification(id.ToString());
            iOSNotificationCenter.RemoveDeliveredNotification(id.ToString());
#endif
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Notification Error] Errore cancellazione: {e.Message}");
        }
    }

    public void CancelAllNotifications()
    {
        if (Application.isEditor) return;
        
        try
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
        }
        catch { }
    }

    private void SaveNotificationId(int logicId, int androidId)
    {
        PlayerPrefs.SetInt($"LocalNotif_Map_{logicId}", androidId);
        PlayerPrefs.Save();
    }

    private int GetSavedNotificationId(int logicId)
    {
        string key = $"LocalNotif_Map_{logicId}";
        if (PlayerPrefs.HasKey(key))
        {
            return PlayerPrefs.GetInt(key);
        }
        return -1; 
    }
}