using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UVFlashlightDetector : MonoBehaviour
{
    [Header("Spotlight Settings")]
    [Tooltip("The Spotlight component you’re toggling UV on/off.")]
    public Light spotLight;
    [Tooltip("Regular (white) colour.")]
    public Color normalColor = Color.white;
    [Tooltip("UV mode colour (blueish).")]
    public Color uvColor = Color.blue;

    [Header("Detection")]
    [Tooltip("How far your UV beam reaches.")]
    public float uvRange = 5f;
    [Tooltip("What layers the statue(s) sit on.")]
    public LayerMask statueLayer;
    [Tooltip("How long you must hold on the statue to clear it.")]
    public float requiredHoldTime = 3f;

    [Header("Input")]
    [Tooltip("Key to toggle UV on/off.")]
    public KeyCode toggleUVKey = KeyCode.U;

    [Header("Toggle SFX")]
    [Tooltip("Sound to play when switching UV mode.")]
    public AudioClip toggleSfx;

    private AudioSource _sfxSource;
    private bool _isUV = false;
    private float _timer = 0f;

    void Awake()
    {
        // grab AudioSource for the click
        _sfxSource = GetComponent<AudioSource>();
        if (_sfxSource == null)
            _sfxSource = gameObject.AddComponent<AudioSource>();

        _sfxSource.playOnAwake = false;
        // make it 3D
        _sfxSource.spatialBlend = 1f;
    }

    void Update()
    {
       
        // toggle UV mode
        if (Input.GetKeyDown(toggleUVKey))
        {
            _isUV = !_isUV;
            spotLight.color = _isUV ? uvColor : normalColor;
            _timer = 0f;

            // play click SFX
            if (toggleSfx != null)
                _sfxSource.PlayOneShot(toggleSfx);
        }

        if (!_isUV)
            return;

        // if UV is on, do the whispering statue entity raycast
        Ray ray = new Ray(spotLight.transform.position, spotLight.transform.forward);
        if (Physics.Raycast(ray, out var hit, uvRange, statueLayer))
        {
            var statue = hit.collider.GetComponent<WhisperingStatue>();
            if (statue != null && statue.IsActive)
            {
                _timer += Time.deltaTime;
                if (_timer >= requiredHoldTime)
                {
                    statue.StopWhispers();
                    _timer = 0f;
                }
                return;
            }
        }

        // reset if no active statue
        _timer = 0f;
    }
}
