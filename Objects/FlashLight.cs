using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FlashLight : MonoBehaviour
{
    [Header("References")]
    public GameObject ON;
    public GameObject OFF;

    [Header("Toggle SFX")]
    [Tooltip("Sound to play when toggling the flashlight.")]
    public AudioClip toggleSfx;

    private AudioSource _sfxSource;
    private bool _isOn = false;

    void Awake()
    {
        // grab audioSource
        _sfxSource = GetComponent<AudioSource>();
        if (_sfxSource == null)
            _sfxSource = gameObject.AddComponent<AudioSource>();

        // don't play on awake
        _sfxSource.playOnAwake = false;

        // 3D spatial by default 
        _sfxSource.spatialBlend = 1f;
    }

    void Start()
    {
        ON.SetActive(false);
        OFF.SetActive(true);
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            // flip state
            _isOn = !_isOn;
            ON.SetActive(_isOn);
            OFF.SetActive(!_isOn);

            // click sound
            if (toggleSfx != null)
                _sfxSource.PlayOneShot(toggleSfx);
        }
    }

    public bool IsOn()
    {
        return _isOn;
    }
}
