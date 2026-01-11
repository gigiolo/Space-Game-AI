using UnityEngine;
using UnityEngine.UI;

public class NotificationButtonUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Button myButton;
    public GameObject adsBadge; // Un piccolo bollino "Play" se è una pubblicità

    private NotificationData _data;

    // Chiamato dal Manager quando crea il bottone
    public void Setup(NotificationData data)
    {
        _data = data;

        if (iconImage != null && data.icon != null) 
            iconImage.sprite = data.icon;

        if (adsBadge != null)
            adsBadge.SetActive(data.isAds);

        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        // 1. Diamo i dati al Manager per aprire il popup
        NotificationManager.Instance.OpenPopup(_data);

        // 2. Distruggiamo questo bottone (la notifica è stata letta)
        Destroy(gameObject);
    }
}