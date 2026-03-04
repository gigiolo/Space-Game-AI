// --- File: _Scripts\SettingsUI.cs ---
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Riferimenti Pannello")]
    [Tooltip("Il pannello principale da attivare/disattivare")]
    [SerializeField] private GameObject menuPanel;

    [Header("Riferimenti Interruttori (Toggles)")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle notificationsToggle;
    [SerializeField] private Toggle adsToggle;

    private bool _isOpenedByClick = false;

    private void Start()
    {
        // --- 1. SETUP PANNELLO MENU ---
        if (menuPanel != null)
        {
            // Spegniamo il pannello all'avvio, ma solo se non è stato appena aperto dal bottone
            if (!_isOpenedByClick) menuPanel.SetActive(false);
            
            // Registriamo il menu nel sistema globale per far funzionare le chiusure automatiche
            if (UIManager.Instance != null) UIManager.Instance.RegisterMenu(menuPanel);
        }

        // --- 2. CARICAMENTO DEGLI STATI SALVATI ---
        bool isMusicOn = PlayerPrefs.GetInt("Setting_Music", 1) == 1;
        bool isSfxOn = PlayerPrefs.GetInt("Setting_SFX", 1) == 1;
        bool isNotifOn = PlayerPrefs.GetInt("Setting_Notifications", 1) == 1;
        bool isAdsOn = PlayerPrefs.GetInt("Setting_Ads", 1) == 1;

        // --- 3. IMPOSTAZIONE GRAFICA INIZIALE ---
        if (musicToggle) musicToggle.SetIsOnWithoutNotify(isMusicOn);
        if (sfxToggle) sfxToggle.SetIsOnWithoutNotify(isSfxOn);
        if (notificationsToggle) notificationsToggle.SetIsOnWithoutNotify(isNotifOn);
        if (adsToggle) adsToggle.SetIsOnWithoutNotify(isAdsOn);

        // --- 4. ASSEGNAZIONE DEGLI EVENTI ---
        if (musicToggle) musicToggle.onValueChanged.AddListener(OnMusicToggled);
        if (sfxToggle) sfxToggle.onValueChanged.AddListener(OnSFXToggled);
        if (notificationsToggle) notificationsToggle.onValueChanged.AddListener(OnNotificationsToggled);
        if (adsToggle) adsToggle.onValueChanged.AddListener(OnAdsToggled);
    }

    // --- LOGICA APERTURA / CHIUSURA MENU ---
    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        
        _isOpenedByClick = true;
        bool opening = !menuPanel.activeSelf;

        if (opening)
        {
            // Chiude tutti gli altri menu aperti
            if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(menuPanel);
            
            menuPanel.SetActive(true);
            
            // Blocca la rotazione della telecamera mentre il menu è aperto
            PlanetOrbitCamera.IsInputBlocked = true; 
        }
        else
        {
            // Cerca l'animazione di chiusura, se presente
            UIPopupEffect effect = menuPanel.GetComponent<UIPopupEffect>();
            if (effect != null) effect.Close();
            else menuPanel.SetActive(false);
            
            // Sblocca la telecamera
            PlanetOrbitCamera.IsInputBlocked = false;
        }
    }

    // --- METODI CHIAMATI QUANDO SI CLICCANO I TOGGLE ---
    private void OnMusicToggled(bool isOn)
    {
        PlayerPrefs.SetInt("Setting_Music", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(isOn ? 1f : 0.0001f);
        }
    }

    private void OnSFXToggled(bool isOn)
    {
        PlayerPrefs.SetInt("Setting_SFX", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(isOn ? 1f : 0.0001f);
        }
    }

    private void OnNotificationsToggled(bool isOn)
    {
        PlayerPrefs.SetInt("Setting_Notifications", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (!isOn && LocalNotificationController.Instance != null)
        {
            LocalNotificationController.Instance.CancelAllNotifications();
            Debug.Log("[Settings] Tutte le notifiche in attesa sono state cancellate.");
        }
    }

    private void OnAdsToggled(bool isOn)
    {
        PlayerPrefs.SetInt("Setting_Ads", isOn ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log($"[Settings] Pubblicità {(isOn ? "Attivate" : "Disattivate")}");
    }
}