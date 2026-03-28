// --- File: _Scripts\ThemedUIElement.cs ---
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Definisce "chi sono io" all'interno della UI
public enum UIStyleType
{
    BackgroundWindow,   // Finestra principale
    BackgroundSlot,     // Sfondo di un elemento lista
    ButtonPrimary,      // Bottone Compra/Azione
    ButtonSecondary,    // Bottone Info/Logistica
    ButtonClose,        // Bottone X
    TextTitle,          // Testo generico
    TextNumber          // Testo evidenziato
}

[ExecuteInEditMode] // <--- MAGIA: Funziona anche mentre editi la scena!
public class ThemedUIElement : MonoBehaviour
{
    public UIStyleType type;

    // Riferimento statico globale (così non dobbiamo trascinarlo ovunque)
    private static UITheme _globalTheme;

    void Start()
    {
        ApplyTheme();
    }

    // Chiamato automaticamente da GameManager
    public static void SetGlobalTheme(UITheme theme)
    {
        _globalTheme = theme;
        // Aggiorna tutti gli elementi attivi nella scena
        foreach (var element in FindObjectsByType<ThemedUIElement>(FindObjectsSortMode.None))
        {
            element.ApplyTheme();
        }
    }

    // Metodo per forzare l'aggiornamento (utile nell'editor)
    public void ApplyTheme()
    {
        if (_globalTheme == null) return;

        // Gestione Immagini (Sfondi, Bottoni)
        Image img = GetComponent<Image>();
        if (img != null)
        {
            switch (type)
            {
                case UIStyleType.BackgroundWindow: img.color = _globalTheme.backgroundDark; break;
                case UIStyleType.BackgroundSlot:   img.color = _globalTheme.backgroundLight; break;
                case UIStyleType.ButtonPrimary:    img.color = _globalTheme.primaryAction; break;
                case UIStyleType.ButtonSecondary:  img.color = _globalTheme.secondaryAction; break;
                case UIStyleType.ButtonClose:      img.color = _globalTheme.destructiveAction; break;
            }
        }

        // Gestione Testi
        TextMeshProUGUI txt = GetComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            // Applica il font se presente nel tema
            if (_globalTheme.mainFont != null) txt.font = _globalTheme.mainFont;

            switch (type)
            {
                case UIStyleType.TextTitle:  txt.color = _globalTheme.textMain; break;
                case UIStyleType.TextNumber: txt.color = _globalTheme.textHighlight; break;
                
                // Se metti un testo sopra un bottone, di solito vuoi che sia leggibile
                // Qui usiamo textMain per default, ma puoi personalizzare
                case UIStyleType.ButtonPrimary:
                case UIStyleType.ButtonSecondary:
                case UIStyleType.ButtonClose:
                     txt.color = _globalTheme.textMain; 
                     break;
            }
        }
    }

    // Questo fa aggiornare il colore appena cambi qualcosa nell'Inspector
    #if UNITY_EDITOR
    void OnValidate()
    {
        // Solo per test nell'editor, cerca di trovare un tema se non è settato
        if (_globalTheme == null)
        {
            // Trucco: Cerca il primo tema che trova nel progetto per mostrarti l'anteprima
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:UITheme");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                _globalTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<UITheme>(path);
            }
        }
        ApplyTheme();
    }
    #endif
}