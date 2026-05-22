// --- File: _Scripts\UI\InfoButtonTrigger.cs ---
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InfoButtonTrigger : MonoBehaviour
{
    [Header("Testi del Popup")]
    public string infoTitle = "Informazioni";
    
    [TextArea(3, 8)]
    public string infoContent;

    private void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OpenInfo);

        // Aggiungiamo automaticamente il suono di click se non c'è già
        if (GetComponent<UISoundController>() == null)
        {
            gameObject.AddComponent<UISoundController>();
        }
    }

    private void OpenInfo()
    {
        if (InfoPopupManager.Instance != null)
        {
            InfoPopupManager.Instance.ShowInfo(infoTitle, infoContent);
        }
        else
        {
            Debug.LogError("[InfoButtonTrigger] InfoPopupManager non trovato nella scena. Assicurati che sia nel Core_Systems.");
        }
    }
}