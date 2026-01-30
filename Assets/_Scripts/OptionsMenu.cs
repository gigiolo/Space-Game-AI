using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 
using TMPro;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelVisuals; 
    
    [Header("Buttons")]
    public Button saveButton;
    public Button wipeSaveButton; 
    public Button closeButton;

    [Header("Feedback Texts")]
    public TextMeshProUGUI statusText; 

    private bool _isWipeConfirmationActive = false;

    private void Start()
    {
        // --- REGISTRAZIONE MENU ---
        if (UIManager.Instance != null && panelVisuals != null)
            UIManager.Instance.RegisterMenu(panelVisuals);

        if(saveButton) saveButton.onClick.AddListener(OnSaveClicked);
        if(wipeSaveButton) wipeSaveButton.onClick.AddListener(OnWipeClicked);
        if(closeButton) closeButton.onClick.AddListener(CloseMenu);

        if(panelVisuals) panelVisuals.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (panelVisuals == null) return;

        bool isActive = !panelVisuals.activeSelf;

        if (isActive)
        {
            // APERTURA - Chiudi gli altri!
            if (UIManager.Instance != null)
                UIManager.Instance.CloseAllMenusExcept(panelVisuals);
        }
        else
        {
            // CHIUSURA - Usa l'effetto se c'è
            UIPopupEffect effect = panelVisuals.GetComponent<UIPopupEffect>();
            if (effect != null) 
            {
                effect.Close();
                PlanetOrbitCamera.IsInputBlocked = false;
                return; // Esce qui perché la chiusura è gestita dall'effetto
            }
        }

        panelVisuals.SetActive(isActive);
        PlanetOrbitCamera.IsInputBlocked = isActive;

        if (isActive)
        {
            _isWipeConfirmationActive = false;
            if(statusText) statusText.text = "OPTIONS";
            if (wipeSaveButton) 
            {
                var txt = wipeSaveButton.GetComponentInChildren<TextMeshProUGUI>();
                if(txt) txt.text = "DELETE SAVE";
            }
        }
    }

    public void CloseMenu()
    {
        if (panelVisuals)
        {
             UIPopupEffect effect = panelVisuals.GetComponent<UIPopupEffect>();
             if (effect != null) effect.Close();
             else panelVisuals.SetActive(false);
        }
        PlanetOrbitCamera.IsInputBlocked = false;
    }

    private void OnSaveClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            if(statusText) statusText.text = "<color=green>GAME SAVED!</color>";
            
            CancelInvoke("ResetStatusText");
            Invoke("ResetStatusText", 1.5f);
        }
    }

    private void OnWipeClicked()
    {
        if (!_isWipeConfirmationActive)
        {
            _isWipeConfirmationActive = true;
            if(statusText) statusText.text = "<color=red>ARE YOU SURE?</color>";
            
            var btnText = wipeSaveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = "CONFIRM DELETE";
        }
        else
        {
            SaveManager.DeleteSaveFile();
            Debug.Log("Save File Deleted. Restarting...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void ResetStatusText()
    {
        if(statusText) statusText.text = "OPTIONS";
    }
}