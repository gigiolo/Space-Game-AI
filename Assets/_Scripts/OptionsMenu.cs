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

    [Header("Nuovi Bottoni")]
    public Button artifactsMenuButton; 
    public ArtifactsMenuUI artifactsUIController; 

    [Header("Feedback Texts")]
    public TextMeshProUGUI statusText; 

    private bool _isWipeConfirmationActive = false;
    private bool _isOpenedByClick = false; 

    private void Start()
    {
        if (UIManager.Instance != null && panelVisuals != null)
            UIManager.Instance.RegisterMenu(panelVisuals);

        if(saveButton) saveButton.onClick.AddListener(OnSaveClicked);
        if(wipeSaveButton) wipeSaveButton.onClick.AddListener(OnWipeClicked);
        if(closeButton) closeButton.onClick.AddListener(CloseMenu);

        if (artifactsMenuButton != null && artifactsUIController != null)
        {
            artifactsMenuButton.onClick.AddListener(() => 
            {
                CloseMenu(); 
                // LA MODIFICA È QUI: Usiamo il nuovo metodo con "memoria"
                artifactsUIController.OpenFromOptions();
            });
        }

        if(panelVisuals && !_isOpenedByClick) panelVisuals.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (panelVisuals)
        {
            _isOpenedByClick = true; 
            bool opening = !panelVisuals.activeSelf;

            if (!opening)
            {
                UIPopupEffect effect = panelVisuals.GetComponent<UIPopupEffect>();
                if (effect != null) effect.Close();
                else panelVisuals.SetActive(false);
                
                PlanetOrbitCamera.IsInputBlocked = false;
            }
            else
            {
                if (UIManager.Instance != null) UIManager.Instance.CloseAllMenusExcept(panelVisuals);
                panelVisuals.SetActive(true);
                PlanetOrbitCamera.IsInputBlocked = true;

                _isWipeConfirmationActive = false;
                if(statusText) statusText.text = "OPTIONS";
                if (wipeSaveButton)
                {
                    var txt = wipeSaveButton.GetComponentInChildren<TextMeshProUGUI>();
                    if(txt) txt.text = "DELETE SAVE";
                }
            }
        }
    }

    public void CloseMenu()
    {
        if (panelVisuals != null && panelVisuals.activeSelf)
        {
             ToggleMenu();
        }
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