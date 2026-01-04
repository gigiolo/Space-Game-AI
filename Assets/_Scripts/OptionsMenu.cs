using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Serve per ricaricare la scena dopo il Wipe
using TMPro;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelVisuals; // Il GameObject "Window" o "Panel" dentro il Canvas
    
    [Header("Buttons")]
    public Button saveButton;
    public Button wipeSaveButton; // Tasto rosso per cancellare tutto
    public Button closeButton;

    [Header("Feedback Texts")]
    public TextMeshProUGUI statusText; // Per messaggi tipo "Game Saved!"

    // Variabile per gestire la conferma della cancellazione
    private bool _isWipeConfirmationActive = false;

    private void Start()
    {
        // Setup Button Listeners
        if(saveButton) saveButton.onClick.AddListener(OnSaveClicked);
        if(wipeSaveButton) wipeSaveButton.onClick.AddListener(OnWipeClicked);
        if(closeButton) closeButton.onClick.AddListener(CloseMenu);

        // Assicuriamoci che il pannello sia spento all'avvio
        if(panelVisuals) panelVisuals.SetActive(false);
    }

    public void ToggleMenu()
    {
        bool isActive = !panelVisuals.activeSelf;
        panelVisuals.SetActive(isActive);

        // Blocca/Sblocca la camera del pianeta
        PlanetOrbitCamera.IsInputBlocked = isActive;

        // Reset dello stato UI quando apri
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
        if(panelVisuals) panelVisuals.SetActive(false);
        PlanetOrbitCamera.IsInputBlocked = false;
    }

    private void OnSaveClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            if(statusText) statusText.text = "<color=green>GAME SAVED!</color>";
            
            // Ripristina il testo dopo 1.5 secondi
            CancelInvoke("ResetStatusText");
            Invoke("ResetStatusText", 1.5f);
        }
    }

    private void OnWipeClicked()
    {
        // Logica a due step per evitare click accidentali
        if (!_isWipeConfirmationActive)
        {
            _isWipeConfirmationActive = true;
            if(statusText) statusText.text = "<color=red>ARE YOU SURE?</color>";
            
            // Cambia testo bottone se possibile
            var btnText = wipeSaveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = "CONFIRM DELETE";
        }
        else
        {
            // ESEGUI CANCELLAZIONE
            SaveManager.DeleteSaveFile();
            Debug.Log("Save File Deleted. Restarting...");
            
            // Ricarica la scena corrente per resettare tutto
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void ResetStatusText()
    {
        if(statusText) statusText.text = "OPTIONS";
    }
}