using UnityEngine;
using UnityEngine.UI;

public class VolumeSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        if (AudioManager.Instance == null) return;

        // Inizializza gli slider con i valori salvati
        if (masterSlider)
        {
            masterSlider.value = AudioManager.Instance.GetVolumeSetting("MasterVol");
            masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        }

        if (musicSlider)
        {
            musicSlider.value = AudioManager.Instance.GetVolumeSetting("MusicVol");
            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        }

        if (sfxSlider)
        {
            sfxSlider.value = AudioManager.Instance.GetVolumeSetting("SFXVol");
            sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        }
    }
}