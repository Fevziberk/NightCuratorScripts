using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI, mainPanel, settingsPanel, audioPanel;

   
    [Header("Audio Settings")]
    public Slider masterSlider;    // controls AudioListener.volume
    public Slider musicSlider;     // controls only the musicSource

    [Header("Pause Music")]
    public AudioClip pauseMusicClip;

    [Header("Camera Controller")]
    [Tooltip("Drag your camera-look script here (e.g. MouseLook, FirstPersonCamera)")]
    public Behaviour cameraController;

    AudioSource musicSource;
    bool        isPaused = false;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Escape;

    // PlayerPrefs keys
    const string PREF_FULLSCREEN    = "pref_fullscreen";
    const string PREF_RESOLUTION    = "pref_resolutionIndex";
    const string PREF_MASTER_VOLUME = "pref_masterVolume";
    const string PREF_MUSIC_VOLUME  = "pref_musicVolume";

    void Awake()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip        = pauseMusicClip;
        musicSource.playOnAwake = false;
        musicSource.loop        = true;
    }

    void Start()
    {
        // hide panels
        pauseMenuUI   .SetActive(false);
        mainPanel     .SetActive(false);
        settingsPanel .SetActive(false);
        
        audioPanel    .SetActive(false);

        // load & bind audio sliders
        masterSlider.value = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, AudioListener.volume);
        musicSlider.value  = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME,  musicSource.volume);
        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicSlider.onValueChanged .AddListener(OnMusicVolumeChanged);
        OnMasterVolumeChanged(masterSlider.value);
        OnMusicVolumeChanged(musicSlider.value);

        

    
        
        // lock cursor on start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            TogglePause();
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        GameState.IsPaused = isPaused;
        Time.timeScale    = isPaused ? 0f : 1f;

        // UI & time
        pauseMenuUI.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        // music
        if (isPaused)
        {
            musicSource.time = 0f;
            musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }

        // cursor
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = isPaused;

        // cameralock
        if (cameraController != null)
            cameraController.enabled = !isPaused;

        // main/settings panels
        if (isPaused)
        {
            mainPanel    .SetActive(true);
            settingsPanel.SetActive(false);
           
            audioPanel   .SetActive(false);
        }
    }

    // Main Panel
    public void OnResumeClicked()       => TogglePause();
    public void OnRestartClicked()
    {
        musicSource.Stop();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void OnSettingsClicked()    => Swap(mainPanel, settingsPanel);
    public void OnQuitClicked()
    {
        musicSource.Stop();
    #if UNITY_EDITOR
        SceneManager.LoadScene("MainMenu");
    #else
        SceneManager.LoadScene("MainMenu");
    #endif
    }

    // Navigation Helpers 
    public void OnSettingsBackClicked()   => Swap(settingsPanel, mainPanel);
    public void OnAudioClicked()          => Swap(settingsPanel, audioPanel);
    public void OnSubpanelBackClicked()   => Swap(settingsPanel, audioPanel);
    public void OnAudioCancelClicked()
    {
        audioPanel    .SetActive(false);
        settingsPanel .SetActive(true);
        // revert sliders to stored values
        masterSlider.value = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, AudioListener.volume);
        musicSlider .value = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME,  musicSource != null ? musicSource.volume : 1f);
        OnMasterVolumeChanged(masterSlider.value);
        OnMusicVolumeChanged(musicSlider.value);
    }
    void Swap(GameObject off, GameObject on, GameObject extraOff = null)
    {
        off.SetActive(false);
        on .SetActive(true);
        if (extraOff != null) extraOff.SetActive(false);
    }

    // Apply Buttons 
    public void OnApplyAudioClicked()
    {
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterSlider.value);
        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME,  musicSlider.value);
        PlayerPrefs.Save();
        Swap(audioPanel, settingsPanel);
    }


    void OnMasterVolumeChanged(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, AudioListener.volume);
        PlayerPrefs.Save();
    }
    void OnMusicVolumeChanged(float v)
    {
        musicSource.volume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicSource.volume);
        PlayerPrefs.Save();
    }
}
