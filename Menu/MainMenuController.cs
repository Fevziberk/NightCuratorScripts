using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject audioPanel;
    public GameObject creditsPanel;

    [Header("Audio Settings (same as PauseMenu)")]
    public Slider masterSlider;    // controls AudioListener.volume
    public Slider musicSlider;     // controls musicSource.volume
    public AudioSource musicSource; // looping menu music (optional)

    [Header("Scene Names")]
    public string gameSceneName = "SampleScene";

    // same PlayerPrefs keys as PauseMenuController
    const string PREF_MASTER_VOLUME = "pref_masterVolume";
    const string PREF_MUSIC_VOLUME  = "pref_musicVolume";

    void Awake()
    {
        // start menu music if you have one
        if (musicSource != null)
        {
            musicSource.loop        = true;
            musicSource.playOnAwake = false;
            musicSource.Play();
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        // set up panels
        mainMenuPanel.SetActive(true);
        settingsPanel .SetActive(false);
        audioPanel    .SetActive(false);
        creditsPanel  .SetActive(false);

        // initialize sliders from PlayerPrefs
        masterSlider.value = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, AudioListener.volume);
        musicSlider .value = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME,  musicSource != null ? musicSource.volume : 1f);

        // bind callbacks
        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicSlider .onValueChanged.AddListener(OnMusicVolumeChanged);

        // apply immediately
        OnMasterVolumeChanged(masterSlider.value);
        OnMusicVolumeChanged(musicSlider.value);
    }

    // Main Menu Buttons 

    public void OnStartClicked()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
    }

    public void OnSettingsClicked()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel .SetActive(true);
        audioPanel    .SetActive(false);
    }

    public void OnExitClicked()
    {
    #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    // Settings Panel Buttons 

    public void OnSettingsBackClicked()
    {
        settingsPanel .SetActive(false);
        mainMenuPanel .SetActive(true);
    }

    public void OnAudioClicked()
    {
        settingsPanel .SetActive(false);
        audioPanel    .SetActive(true);
    }

    // **Cancel** on Audio panel: just go back, no saving
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

    // **Apply** on Audio panel: save prefs then go back
    public void OnAudioApplyClicked()
    {
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterSlider.value);
        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME,  musicSource != null ? musicSource.volume : musicSlider.value);
        PlayerPrefs.Save();

        audioPanel    .SetActive(false);
        settingsPanel .SetActive(true);
    }

    // Credits 

    public void OnCreditsClicked()
    {
        mainMenuPanel.SetActive(false);
        creditsPanel  .SetActive(true);
    }
    public void OnCreditsBackClicked()
    {
        creditsPanel  .SetActive(false);
        mainMenuPanel .SetActive(true);
    }

    // Audio callbacks 

    void OnMasterVolumeChanged(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v);
    }

    void OnMusicVolumeChanged(float v)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(v);
    }
}
