using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "New Theme", menuName = "UI/Theme")]
public class UITheme : ScriptableObject
{
    [Header("--- FONDI & PANNELLI ---")]
    public Color backgroundDark;   // Sfondo scuro (es. finestre)
    public Color backgroundLight;  // Sfondo chiaro (es. slot oggetti)

    [Header("--- BOTTONI ---")]
    public Color primaryAction;    // Azioni principali (es. Buy, Upgrade)
    public Color secondaryAction;  // Azioni secondarie / Info
    public Color destructiveAction;// Chiudi, Vendi, Resetta

    [Header("--- TESTI ---")]
    public Color textMain;         // Titoli e testo normale
    public Color textHighlight;    // Valori numerici, bonus
    
    [Header("--- FONT GLOBAL ---")]
    // Se lasciato vuoto, userà il font di default dell'oggetto
    public TMP_FontAsset mainFont; 
}