using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer Reference")]
    [Tooltip("Assegna qui il MainMixer creato nel progetto.")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Audio Sources")]
    [Tooltip("Sorgente dedicata alla musica (Loop).")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB; // Per il cross-fade
    
    [Tooltip("Sorgente per effetti sonori 2D (UI, Click).")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Defaults")]
    [SerializeField] private float defaultCrossfadeDuration = 2.0f;

    // Internal State
    private bool _isUsingSourceA = true;
    private const string MIXER_MASTER = "MasterVol";
    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Non serve DontDestroyOnLoad perché fa parte del prefab persistente Core_Systems
    }

    private void Start()
    {
        LoadVolumeSettings();
    }

    // --- MUSIC SYSTEM (Con Crossfade) ---
    public void PlayMusic(AudioClip newClip, float fadeDuration = -1f)
    {
        if (newClip == null) return;

        float duration = fadeDuration < 0 ? defaultCrossfadeDuration : fadeDuration;
        
        // Determina quale sorgente è attiva e quale è libera
        AudioSource activeSource = _isUsingSourceA ? musicSourceA : musicSourceB;
        AudioSource newSource = _isUsingSourceA ? musicSourceB : musicSourceA;

        // Se la stessa canzone sta già suonando, non fare nulla
        if (activeSource.clip == newClip && activeSource.isPlaying) return;

        StartCoroutine(CrossfadeRoutine(activeSource, newSource, newClip, duration));
        _isUsingSourceA = !_isUsingSourceA;
    }

    private IEnumerator CrossfadeRoutine(AudioSource fadingOut, AudioSource fadingIn, AudioClip newClip, float duration)
    {
        fadingIn.clip = newClip;
        fadingIn.volume = 0f;
        fadingIn.Play();

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Funziona anche in pausa
            float t = timer / duration;

            fadingIn.volume = Mathf.Lerp(0f, 1f, t);
            fadingOut.volume = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        fadingIn.volume = 1f;
        fadingOut.volume = 0f;
        fadingOut.Stop();
    }

    // --- SFX SYSTEM ---
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f)
    {
        if (clip == null) return;

        // Piccola variazione di pitch per evitare l'effetto "mitragliatrice" robotica
        if (pitchVariance > 0)
        {
            sfxSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        }
        else
        {
            sfxSource.pitch = 1f;
        }

        sfxSource.PlayOneShot(clip, volumeScale);
    }

    // --- VOLUME CONTROL (Logaritmico per AudioMixer) ---
    public void SetMasterVolume(float sliderValue) => SetMixerVolume(MIXER_MASTER, sliderValue);
    public void SetMusicVolume(float sliderValue) => SetMixerVolume(MIXER_MUSIC, sliderValue);
    public void SetSFXVolume(float sliderValue) => SetMixerVolume(MIXER_SFX, sliderValue);

    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        // Convertiamo slider (0.0001 a 1) in Decibel (-80 a 0)
        // Log10(0.0001) * 20 = -80db (Mute)
        // Log10(1) * 20 = 0db (Max)
        float value = Mathf.Max(sliderValue, 0.0001f);
        float db = Mathf.Log10(value) * 20;
        
        mainMixer.SetFloat(parameterName, db);
        
        // Salviamo subito le preferenze
        PlayerPrefs.SetFloat(parameterName, sliderValue);
        PlayerPrefs.Save();
    }

    public float GetVolumeSetting(string parameterName)
    {
        return PlayerPrefs.GetFloat(parameterName, 0.75f); // Default 75%
    }

    private void LoadVolumeSettings()
    {
        SetMasterVolume(GetVolumeSetting(MIXER_MASTER));
        SetMusicVolume(GetVolumeSetting(MIXER_MUSIC));
        SetSFXVolume(GetVolumeSetting(MIXER_SFX));
    }
}