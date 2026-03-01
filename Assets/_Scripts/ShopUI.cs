// --- File: _Scripts\ShopUI.cs ---
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Riferimenti Pannello")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button closeButton;

    [Header("Bottoni Acquisto")]
    [SerializeField] private Button buy50Button;
    [SerializeField] private Button buy200Button;
    [SerializeField] private Button buy500Button;

    private bool _isOpenedByClick = false;

    private void Start()
    {
        if (menuPanel != null)
        {
            if (!_isOpenedByClick) menuPanel.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.RegisterMenu(menuPanel);
        }

        // Assegna la chiusura
        if (closeButton != null) closeButton.onClick.AddListener(ToggleMenu);

        // Collega i bottoni visivi alle funzioni del nostro IAPManager
        // RIMOSSI TUTTI I VECCHI LISTENER PER SICUREZZA
        if (buy50Button != null) 
        {
            buy50Button.onClick.RemoveAllListeners();
            buy50Button.onClick.AddListener(() => IAPManager.Instance.BuyIridium50());
        }
        
        if (buy200Button != null) 
        {
            buy200Button.onClick.RemoveAllListeners();
            buy200Button.onClick.AddListener(() => IAPManager.Instance.BuyIridium200());
        }

        // CORREZIONE: Ora punta correttamente a buy500Button
        if (buy500Button != null) 
        {
            buy500Button.onClick.RemoveAllListeners();
            buy500Button.onClick.AddListener(() => IAPManager.Instance.BuyIridium500());
        }
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        
        _isOpenedByClick = true;
        bool opening = !menuPanel.activeSelf;

        if (opening)
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(menuPanel);
            menuPanel.SetActive(true);
            PlanetOrbitCamera.IsInputBlocked = true; // Blocca la visuale mentre lo shop è aperto
        }
        else
        {
            UIPopupEffect effect = menuPanel.GetComponent<UIPopupEffect>();
            if (effect != null) effect.Close();
            else menuPanel.SetActive(false);
            
            PlanetOrbitCamera.IsInputBlocked = false;
        }
    }
}