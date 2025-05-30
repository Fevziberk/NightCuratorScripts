using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepController : MonoBehaviour
{
    [Header("Footstep Clips")]
    [Tooltip("Assign your 3 footstep AudioClips here")]
    public AudioClip[] footstepClips;

    [Header("Timing")]
    [Tooltip("Interval in seconds between steps while walking")]
    public float stepInterval = 0.6f;

    private AudioSource audioSource;
    private float stepTimer = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void Update()
    {
        
        // Only play footsteps while WASD pressed
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A))
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                PlayRandomStep();
                stepTimer = 0f;
            }
        }
        else
        {
            // reset timer when not moving
            stepTimer = stepInterval;
        }
    }

    private void PlayRandomStep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip);
    }
}
