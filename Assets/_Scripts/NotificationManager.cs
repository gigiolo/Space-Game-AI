using UnityEngine;
using System.Collections.Generic;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("Configurazione UI")]
    public Transform notificationContainer; // L'area verticale a destra dove finiscono i bottoni
    public NotificationButtonUI buttonPrefab; // Il prefab del bottone rotondo
    public NotificationPopup popupWindow;     // Il popup generico

    // Dizionario per tracciare le notifiche attive tramite un ID unico
    private Dictionary<string, GameObject> _activeNotifications = new Dictionary<string, GameObject>();

    [Header("Icone Standard")]
    public Sprite giftIcon;
    public Sprite moneyIcon;
    public Sprite adsIcon;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (popupWindow) popupWindow.Close();
    }

    // --- FUNZIONE PRINCIPALE DA CHIAMARE DA OVUNQUE ---
    public void SpawnNotification(NotificationData data)
    {
        // --- CONTROLLO ANTI-DUPLICATI ---
        // Se una notifica con lo stesso ID è già visualizzata, non fare nulla.
        if (_activeNotifications.ContainsKey(data.id))
        {
            return;
        }

        // 1. Crea il bottone dentro il container (Vertical Layout Group)
        GameObject newBtnObj = Instantiate(buttonPrefab.gameObject, notificationContainer);
        
        // 2. Configura il bottone con i dati
        NotificationButtonUI script = newBtnObj.GetComponent<NotificationButtonUI>();
        script.Setup(data);

        // 3. Aggiungi la notifica al dizionario per tracciarla
        _activeNotifications.Add(data.id, newBtnObj);

        // Opzionale: Aggiungi un suono o un'animazione di "pop" qui
    }

    // Usata dal bottone stesso
    public void OpenPopup(NotificationData data)
    {
        if (popupWindow)
        {
            popupWindow.Show(data);
        }
    }

    // NUOVO: Rimuove una notifica dalla UI e dal tracciamento
    public void DismissNotification(string id)
    {
        if (_activeNotifications.TryGetValue(id, out GameObject notificationObject))
        {
            Destroy(notificationObject);
            _activeNotifications.Remove(id);
        }
    }

    // --- ESEMPI DI UTILIZZO (DEBUG) ---
    // Puoi chiamare queste funzioni per testare se funziona
    public void TestSpawnGift()
    {
        SpawnNotification(new NotificationData(
            "test_gift", // ID Unico
            "Free Energy", 
            "A gift from the stars!", 
            moneyIcon, 
            () => { GameManager.Instance.AddInstantEmitters(5); },
            false
        ));
    }
    
    // Esempio avanzato: genera una ricompensa casuale
    public void SpawnRandomReward()
    {
        // Qui potrai mettere logica randomica in futuro
        SpawnNotification(new NotificationData(
            "random_reward", // ID Unico
            "Mystery Box", 
            "Contains 100 Energy", 
            giftIcon, 
            () => { GameManager.Instance.AddEnergy(100); }, 
            false
        ));
    }
}