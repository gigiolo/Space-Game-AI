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
        // Ora il pulsante si limita a chiedere al Manager di aprire il popup.
        // Non si distrugge più da solo. Sarà il popup a gestire la sua rimozione
        // dopo che il premio è stato effettivamente riscosso.
        NotificationManager.Instance.OpenPopup(_data);
    }
}