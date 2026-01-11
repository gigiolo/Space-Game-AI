using UnityEngine;
using System.Collections.Generic;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("Configurazione UI")]
    public Transform notificationContainer; // L'area verticale a destra dove finiscono i bottoni
    public NotificationButtonUI buttonPrefab; // Il prefab del bottone rotondo
    public NotificationPopup popupWindow;     // Il popup generico

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
        // 1. Crea il bottone dentro il container (Vertical Layout Group)
        GameObject newBtnObj = Instantiate(buttonPrefab.gameObject, notificationContainer);
        
        // 2. Configura il bottone con i dati
        NotificationButtonUI script = newBtnObj.GetComponent<NotificationButtonUI>();
        script.Setup(data);

        // Opzionale: Aggiungi un suono o un'animazione di "pop" qui
    }

    // Usata dal bottone stesso
    public void OpenPopup(NotificationData data)
    {
        if (popupWindow) popupWindow.Show(data);
    }

    // --- ESEMPI DI UTILIZZO (DEBUG) ---
    // Puoi chiamare queste funzioni per testare se funziona
    public void TestSpawnGift()
    {
        SpawnNotification(new NotificationData(
            "Free Energy", 
            "A gift from the stars!", 
            moneyIcon, 
            () => { GameManager.Instance.AddInstantEmitters(5); }, // Ora funziona perché il metodo esiste!
            false
        ));
    }
    
    // Esempio avanzato: genera una ricompensa casuale
    public void SpawnRandomReward()
    {
        // Qui potrai mettere logica randomica in futuro
        SpawnNotification(new NotificationData(
            "Mystery Box", 
            "Contains 100 Energy", 
            giftIcon, 
            // CORRETTO: Ora usa il metodo pubblico AddEnergy
            () => { GameManager.Instance.AddEnergy(100); }, 
            false
        ));
    }
}