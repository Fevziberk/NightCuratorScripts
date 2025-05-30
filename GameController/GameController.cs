using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine;


[Serializable]
public class ScheduledAnomaly
{
    [Tooltip("At how many seconds from the shift start this anomaly will trigger.")]
    public float triggerTime;

    [Tooltip("Anomaly controller (e.g. StatueController, GramophoneAnomalyController, HorsemanController).")]
    public MonoBehaviour controller;

    [HideInInspector] public bool activated;
    [HideInInspector] public bool cleared;
}

public class GameController : MonoBehaviour
{
    [Header("GuardPost Zone")]
    [Tooltip("The trigger collider the player enters to go on break.")]
    public Collider guardPostTrigger;

    [Header("Shift Scheduling")]
    [Tooltip("Scheduling info: trigger time and controller for each anomaly.")]
    public ScheduledAnomaly[] schedule;

    private float shiftTimer;
    private bool shiftActive;
    private bool introDone;

    [Header("Intro")]
    [Tooltip("Canvas group that displays intro text on a black screen.")]
    public CanvasGroup introCanvas;

    [Header("Shift Length")]
    [Tooltip("Maximum duration of each shift in seconds.")]
    public float maxShiftDuration = 5f * 60f;

    [Header("Break Prompt")]
    [Tooltip("UI shown when all anomalies are cleared")]
    public CanvasGroup breakCanvas;


    public Transform cameraTransform;

    [Header("Look Offset")]
    [Tooltip("Height above the killer to aim the camera when showing Game Over.")]
    public float lookUpHeight = 10f;

    [Header("Jump-Scare UI")]
    [Tooltip("CanvasGroup containing a full-screen black Image and 'GAME OVER' text.")]
    public CanvasGroup gameOverCanvas;

    [Tooltip("CanvasGroup for timing display during fade to black.")]
    public CanvasGroup gameOverCanvasTime;
    public float fadeDuration = 1f;

    [Header("Audio")]
    [Tooltip("Scream SFX to play when player is killed.")]
    public AudioClip jumpScareSfx;
    private AudioSource sfxSource;

    [Header("Player Input")]
    [Tooltip("FPS controller or character script to disable on death.")]
    public Behaviour playerMovement;

    void Awake()
    {
        // Hide intro, break, and game over UIs
        if (introCanvas != null) introCanvas.alpha = 1f;
        if (breakCanvas != null) breakCanvas.alpha = 0f;
        if (gameOverCanvas != null) gameOverCanvas.alpha = 0f;
        if (gameOverCanvasTime != null) gameOverCanvasTime.alpha = 0f;

        // Prepare audio source
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    void Update()
    {
        // 1) Shift doesn't start before intro ends
        if (!introDone)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                StartCoroutine(FadeOutIntro());
            return;
        }

        // 2) After shift is activated 
        if (shiftActive)
        {
            shiftTimer += Time.deltaTime;

            // Has time run out?
            if (shiftTimer >= maxShiftDuration)
            {
                EndShift(false);
                return;
            }

            // Trigger scheduled anomalies
            foreach (var entry in schedule)
            {
                if (!entry.activated && shiftTimer >= entry.triggerTime)
                {
                    var anomaly = entry.controller as IAnomaly;
                    if (anomaly != null)
                        anomaly.ActivateAnomaly();

                    entry.activated = true;

                    //  anomaly's cleared event
                    if (anomaly != null)
                        anomaly.OnCleared += () => OnAnomalyCleared(entry);
                }
            }
        }
    }

    private IEnumerator FadeOutIntro()
    {
        float t = 0f, duration = 1f;
        while (t < duration)
        {
            t += Time.deltaTime;
            introCanvas.alpha = 1f - (t / duration);
            yield return null;
        }
        introCanvas.gameObject.SetActive(false);
        introDone = true;
        StartShift();
    }

    private void StartShift()
    {
        shiftActive = true;
        shiftTimer = 0f;

        // Reset flags for all scheduled entries
        foreach (var e in schedule)
        {
            e.activated = false;
            e.cleared = false;
        }
    }

    private void OnAnomalyCleared(ScheduledAnomaly entry)
    {
        entry.cleared = true;

        // If all anomalies have been triggered and cleared end shift successfully
        if (schedule.All(x => x.activated && x.cleared))
            EndShift(true);
    }

    private void EndShift(bool success)
    {
        shiftActive = false;

        if (success)
        {
            Debug.Log("All cleared – time for a break!");
            // Enable the break trigger now that everything is done
            breakCanvas.alpha = 1f;
            if (guardPostTrigger != null)
                guardPostTrigger.enabled = true;
        }
        else
        {
            Debug.Log("Shift over – you failed to handle everything.");

            // Fade in the timer canvas
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                gameOverCanvasTime.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            }

            ShiftTimeoutSequence();
            SceneManager.LoadScene("MainMenu");
        }
    }

    private IEnumerator FadeInCanvas(CanvasGroup cg, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
    }

    /// <summary>
    /// Called when an anomaly kills the player
    /// </summary>
    public void TriggerGameOver(Transform killer)
    {
        StartCoroutine(GameOverSequence(killer));
    }

    private IEnumerator GameOverSequence(Transform killer)
    {
        // Disable player controls
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Disable Cinemachine to manually control the camera
        var brain = cameraTransform.GetComponent<CinemachineBrain>();
        if (brain != null)
            brain.enabled = false;

        // rotate camera to look above the killer
        float rotDuration = 0.5f, elapsed = 0f;
        Transform camT = cameraTransform;
        Quaternion initialRotation = camT.rotation;
        Vector3 targetPoint = killer.position + Vector3.up * lookUpHeight;

        while (elapsed < rotDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotDuration);
            Vector3 direction = (targetPoint - camT.position).normalized;
            camT.rotation = Quaternion.Slerp(initialRotation, Quaternion.LookRotation(direction, Vector3.up), t);
            yield return null;
        }

        // Play the jump-scare scream
        if (jumpScareSfx != null)
            sfxSource.PlayOneShot(jumpScareSfx, 0.15f);

        // Fade to black and GAME OVER
        if (gameOverCanvas != null)
        {
            float fadeTime = 0f;
            while (fadeTime < fadeDuration)
            {
                fadeTime += Time.deltaTime;
                gameOverCanvas.alpha = Mathf.Clamp01(fadeTime / fadeDuration);
                yield return null;
            }
        }

        // Wait before returning to main menu
        yield return new WaitForSeconds(5f);

        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator ShiftTimeoutSequence()
    {
        // Wait before complete shutdown or transition
        yield return new WaitForSeconds(6f);
    }
}