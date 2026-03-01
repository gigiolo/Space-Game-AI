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

    // --- AGGIUNTO PER IL TEST RAPIDO DELLE ADS ---
    private void Update()
    {
        // Premi "A" sulla tastiera in Play Mode per generare la notifica del video
        if (Input.GetKeyDown(KeyCode.A))
        {
            SpawnRadioSignalAd();
        }
    }
    // --------------------------------------------

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

    // --- ESEMPI E TRIGGER SPECIFICI ---

    // 1. Test standard (Energia)
    public void TestSpawnGift()
    {
        SpawnNotification(new NotificationData(
            "Free Energy", 
            "A gift from the stars!", 
            moneyIcon, 
            () => { GameManager.Instance.AddInstantEmitters(5); }, 
            false
        ));
    }
    
    // 2. Regalo misterioso (Energia)
    public void SpawnRandomReward()
    {
        SpawnNotification(new NotificationData(
            "Mystery Box", 
            "Contains 100 Energy", 
            giftIcon, 
            () => { GameManager.Instance.AddEnergy(100); }, 
            false
        ));
    }

    // --- NUOVO: EVENTO ADS (Segnale Radio / Iridio Puro) ---
    public void SpawnRadioSignalAd()
    {
        // Quantità di premio
        int iridiumRewardAmount = 2;

        SpawnNotification(new NotificationData(
            "Trasmissione Sconosciuta", 
            "Segnale radio di origine terrestre intercettato.\nDecodificare la trasmissione visiva?\n\n<color=#FF00FF>Ricompensa: +" + iridiumRewardAmount + " Iridio Puro</color>", 
            adsIcon, // Usa l'icona video/TV
            () => {
                // AZIONE QUANDO IL GIOCATORE CLICCA "CLAIM" SUL POPUP
                if (AdsManager.Instance != null)
                {
                    AdsManager.Instance.ShowRewardedAd(() => 
                    {
                        // QUESTA PARTE VIENE ESEGUITA SOLO SE IL VIDEO VIENE VISTO FINO ALLA FINE
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.AddPureIridium(iridiumRewardAmount);
                        }
                        
                        if (ShipTerminalController.Instance != null)
                        {
                            ShipTerminalController.Instance.ShowSystemMessage("DECODIFICA COMPLETATA. DATI COMMERCIALI TERRESTRI CONVERTITI IN IRIDIO.");
                        }
                    });
                }
                else
                {
                    Debug.LogError("[NotificationManager] AdsManager non trovato! Assicurati di aver messo il prefab nella scena.");
                }
            }, 
            true // isAds = true (mostra il badge "Video" sul bottone della notifica)
        ));
    }
}