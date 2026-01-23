using System;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
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
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Se siamo nell'Editor, non inizializziamo nulla per evitare errori
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
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
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
        // 1. Sicurezza: Se siamo nell'Editor, simuliamo e basta.
        if (Application.isEditor)
        {
            Debug.Log($"[EDITOR MOCK] Notifica '{title}' programmata per: {deliveryTime} (ID: {id})");
            return;
        }

        if (deliveryTime <= DateTime.Now) return;

        try
        {
            // Pulisci vecchie notifiche con lo stesso ID
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

            // Metodo Universale: Salva l'ID generato da Android
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
            // QUESTO È FONDAMENTALE: Se fallisce, logga l'errore ma NON bloccare il gioco!
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
        catch { /* Ignora errori in cancellazione */ }
    }

    private void SaveNotificationId(int logicId, int androidId)
    {
        PlayerPrefs.SetInt($"LocalNotif_Map_{logicId}", androidId);
        PlayerPrefs.Save();
    }

    private int GetSavedNotificationId(int logicId)
    {
        string key = $"LocalNotif_Map_{logicId}";
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : -1;
    }
}