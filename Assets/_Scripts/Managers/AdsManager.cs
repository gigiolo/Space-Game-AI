using UnityEngine;
using System;
using System.Collections;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("Impostazioni")]
    [Tooltip("Se VERO, simulerà un video di 3 secondi senza collegarsi a internet.")]
    public bool testMode = true;

    // Variabile per ricordare cosa fare quando il video finisce
    private Action _onRewardEarnedCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (testMode)
        {
            Debug.Log("<color=cyan>[AdsManager] MOCK MODE ATTIVA. Nessuna connessione a Unity Ads. Verranno usati video simulati.</color>");
        }
    }

    // --- METODO PUBBLICO CHE CHIAMEREMO DAL GIOCO ---
    public void ShowRewardedAd(Action onSuccess)
    {
        _onRewardEarnedCallback = onSuccess;

        if (testMode)
        {
            Debug.Log("[AdsManager] Richiesta video ricevuta. Avvio simulazione...");
            StartCoroutine(MockVideoRoutine());
        }
        else
        {
            // Qui in futuro rimetteremo il vero codice di Unity Ads o AppLovin
            Debug.LogError("[AdsManager] Per usare le Ads reali, devi disattivare il Test Mode e configurare la Dashboard.");
        }
    }

    // --- LA NOSTRA FINTA PUBBLICITÀ DI 3 SECONDI ---
    private IEnumerator MockVideoRoutine()
    {
        Debug.Log("[AdsManager] <color=yellow>--- INIZIO VIDEO SIMULATO ---</color>");
        
        // 1. Mettiamo il gioco in pausa esattamente come farebbe una vera pubblicità a tutto schermo
        Time.timeScale = 0f;
        
        // Muta l'audio del gioco
        float originalVolume = 0.75f;
        if (AudioManager.Instance != null) 
        {
            originalVolume = AudioManager.Instance.GetVolumeSetting("MasterVol");
            AudioManager.Instance.SetMasterVolume(0.0001f); 
        }

        // 2. Aspettiamo 3 secondi (usiamo unscaledDeltaTime perché timeScale è a 0)
        float timer = 0f;
        float mockDuration = 3.0f;
        
        while (timer < mockDuration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3. Ripristiniamo il gioco
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(originalVolume);

        Debug.Log("[AdsManager] <color=green>--- FINE VIDEO. EROGAZIONE PREMIO ---</color>");
        
        // 4. Diamo la ricompensa al giocatore chiamando la funzione che ci ha passato il NotificationManager!
        _onRewardEarnedCallback?.Invoke();
        _onRewardEarnedCallback = null;
    }
}