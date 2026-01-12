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
    private string _currentNotificationId;

    public void Show(NotificationData data)
    {
        _currentData = data;
        _currentNotificationId = data.id;

        // Imposta testi e grafica
        if (titleText) titleText.text = data.title;
        if (descriptionText) descriptionText.text = data.description;
        if (rewardIcon && data.icon) rewardIcon.sprite = data.icon;

        // Setup bottone recluta
        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(OnClaim);

        panel.SetActive(true);
    }

    void OnClaim()
    {
        // Esegue il codice specifico (es. dare soldi, boost, etc.)
        _currentData.onClaimAction?.Invoke();

        // Ora, dopo aver riscosso, diciamo al manager di eliminare la notifica.
        if (!string.IsNullOrEmpty(_currentNotificationId))
        {
            NotificationManager.Instance.DismissNotification(_currentNotificationId);
        }

        Close();
    }

    public void Close()
    {
        panel.SetActive(false);
    }
}