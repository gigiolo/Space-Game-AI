using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class UISoundController : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip clickSound;
    public AudioClip hoverSound; // Opzionale

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.2f)] public float pitchRandom = 0.05f;

    private void Start()
    {
        // Setup automatico degli eventi se manca
        SetupEventTrigger();
        
        // Se c'è un bottone, aggiungiamo il listener per il click
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlayClick);
        }
    }

    private void SetupEventTrigger()
    {
        // Solo se vogliamo il suono hover
        if (hoverSound == null) return;

        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) => { PlayHover(); });
        trigger.triggers.Add(entry);
    }

    public void PlayClick()
    {
        if (AudioManager.Instance != null && clickSound != null)
            AudioManager.Instance.PlaySFX(clickSound, volume, pitchRandom);
    }

    public void PlayHover()
    {
        if (AudioManager.Instance != null && hoverSound != null)
            AudioManager.Instance.PlaySFX(hoverSound, volume * 0.5f, pitchRandom);
    }
}