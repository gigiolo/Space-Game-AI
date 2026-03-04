// --- File: _Scripts\NotificationManager.cs ---
using UnityEngine;
using System.Collections.Generic;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("Configurazione UI")]
    public Transform notificationContainer; 
    public NotificationButtonUI buttonPrefab; 
    public NotificationPopup popupWindow;     

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SpawnRadioSignalAd();
        }
    }

    public void SpawnNotification(NotificationData data)
    {
        GameObject newBtnObj = Instantiate(buttonPrefab.gameObject, notificationContainer);
        NotificationButtonUI script = newBtnObj.GetComponent<NotificationButtonUI>();
        script.Setup(data);
    }

    public void OpenPopup(NotificationData data)
    {
        if (popupWindow) popupWindow.Show(data);
    }

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

    public void SpawnRadioSignalAd()
    {
        // --- NUOVO: CONTROLLO IMPOSTAZIONI ---
        // Se le pubblicità sono spente, impediamo la generazione del segnale radio
        if (PlayerPrefs.GetInt("Setting_Ads", 1) == 0)
        {
            return;
        }
        // ------------------------------------

        int iridiumRewardAmount = 2;

        SpawnNotification(new NotificationData(
            "Trasmissione Sconosciuta", 
            "Segnale radio di origine terrestre intercettato.\nDecodificare la trasmissione visiva?\n\n<color=#FF00FF>Ricompensa: +" + iridiumRewardAmount + " Iridio Puro</color>", 
            adsIcon, 
            () => {
                if (AdsManager.Instance != null)
                {
                    AdsManager.Instance.ShowRewardedAd(() => 
                    {
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
            true
        ));
    }
}