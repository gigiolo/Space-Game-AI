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
        // Se esiste già un'istanza (quella persistente), e io non sono lei...
        if (Instance != null && Instance != this) 
        {
            // Non faccio nulla. Aspetto che il GameManager (o il padre) mi distrugga 
            // insieme a tutto il prefab duplicato.
            // Non devo toccare Instance, altrimenti rompo il riferimento globale!
            return;
        }

        // Se sono io il prescelto:
        Instance = this;
        
        // RIMOSSO: DontDestroyOnLoad(gameObject); 
        // MOTIVO: Sono figlio di Core_Systems, ci pensa lui a salvarmi.

        // FONDAMENTALE: Assicura che Unity metta in pausa il gioco quando premi Home.
        Application.runInBackground = false;
    }

    private void Start()
    {
        // Se non sono l'istanza ufficiale, mi fermo qui.
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
            Importance = Importance.High, // High = Suona e Vibra
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