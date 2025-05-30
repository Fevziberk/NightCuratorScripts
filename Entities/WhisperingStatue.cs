using System;
using UnityEngine;

/// <summary>
/// A “whispering statue” anomaly:
/// - When activated, it plays a looping whisper SFX whose volume ramps up as the player approaches.
/// - Player must shine UV light (or otherwise call StopWhispers) for a sustained hold to clear it.
/// - Fires OnCleared once the whispers have been stopped.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WhisperingStatue : MonoBehaviour, IAnomaly
{
    [Header("Whisper SFX")]
    [Tooltip("Drag in the AudioClip that contains the looping whisper sound.")]
    public AudioClip whisperClip;

    [Header("Detection Settings")]
    [Tooltip("Player transform used for distance checks.")]
    public Transform player;
    [Tooltip("Within this radius the whispers are audible.")]
    public float detectionRadius = 30f;

    private AudioSource audioSource;
    private bool _active;

   
    // raised by this anomaly when the player successfully stops the whispers.
    
    public event Action OnCleared;

    /// <summary>
    /// IAnomaly API: has this anomaly been activated
    /// </summary>
    public bool IsActive => _active;

    void Awake()
    {
        // set up AudioSource
    audioSource = GetComponent<AudioSource>();
    audioSource.clip = whisperClip;
    audioSource.loop = true;
    audioSource.playOnAwake = false;
    audioSource.volume = 0f;

    
    audioSource.spatialBlend = 0f;  
    }

    void Update()
    {
         if (GameState.IsPaused) return;
        if (!_active || player == null)
        {
            // if not active, make sure it's not playing
            if (audioSource.isPlaying)
                audioSource.Stop();
            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);
        if (distance <= detectionRadius)
        {
            // start whispering
            if (!audioSource.isPlaying)
                audioSource.Play();

            // ramp volume from 0 (far) to 1 (right next to statue, it acts like spatial)
            audioSource.volume = Mathf.Clamp01(1f - (distance / detectionRadius));
        }
        else
        {
            // if out of range stop whispering
            if (audioSource.isPlaying)
                audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    /// <summary>
    /// IAnomaly API: call this to start anomaly
    /// </summary>
    public void ActivateAnomaly()
    {
        _active = true;
    }

    /// <summary>
    /// Call this to stop the whispering
    /// </summary>
    public void StopWhispers()
    {
        if (!_active)
            return;

        _active = false;
        if (audioSource.isPlaying)
            audioSource.Stop();
        audioSource.volume = 0f;

        Debug.Log("WhisperingStatue: whispers have been silenced.");

        // notify the GameController that this anomaly is cleared
        OnCleared?.Invoke();
    }
}
