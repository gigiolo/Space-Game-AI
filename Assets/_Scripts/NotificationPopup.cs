using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image rewardIcon;
    public Button claimButton;

    private NotificationData _currentData;

    public void Show(NotificationData data)
    {
        _currentData = data;

        // Imposta testi e grafica
        if (titleText) titleText.text = data.title;
        if (descriptionText) descriptionText.text = data.description;
        if (rewardIcon && data.icon) rewardIcon.sprite = data.icon;

        // Setup bottone recluta
        if (claimButton)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaim);
        }

        // Apertura semplice e istantanea
        panel.SetActive(true);
    }

    void OnClaim()
    {
        _currentData.onClaimAction?.Invoke();
        Close();
    }

    public void Close()
    {
        // Chiusura semplice e istantanea
        panel.SetActive(false);
    }
}