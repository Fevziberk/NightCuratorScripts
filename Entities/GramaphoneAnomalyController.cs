using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Anomaly controller for the ancient gramophone.
/// Implements IAnomaly so it can be scheduled & cleared by GameController.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GramophoneAnomalyController : MonoBehaviour, IAnomaly
{
    [Header("Audio Sources")]
    [Tooltip("Looping normal ambience")]
    public AudioSource normalSource;
    [Tooltip("Looping rising anomaly effect")]
    public AudioSource anomalySource;

    [Header("Anomaly Settings")]
    [Tooltip("How long until the anomaly 'fails' if the player doesn't swap the record")]
    public float anomalyRiseDuration = 60f;
    [Range(0f,1f)]
    [Tooltip("Max volume of anomalySource at the end of rise")]
    public float maxAnomalyVolume = 1f;

    [Header("Spinning Pivot")]
    [Tooltip("Empty transform at the center of the turntable under which record meshes spin")]
    public Transform spinPivot;

    [Header("Original Record Mesh")]
    [Tooltip("The static mesh under spinPivot when no swap has happened")]
    public GameObject originalRecordMesh;

    [Header("Record Pickup")]
    [Tooltip("The world-space record GameObject that the player can pick up")]
    public GameObject recordPickupObject;

    [Header("Record Mount Point")]
    [Tooltip("Where the ArtifactVerifier will snap the record")]
    public Transform recordMountPoint;

    [Header("Debug Trigger")]
    [Tooltip("For testing in Editor only")]
    public bool manualTrigger;

    [Header("Runtime")]
    [Tooltip("Rotation speed of the turntable in degrees/sec")]
    public float rotationSpeed = 180f;

    // IAnomaly API
    public bool AnomalyActive => anomalyActive;

    public bool IsActive => throw new NotImplementedException();

    public event Action OnCleared;

    private ArtifactVerifier verifier;
    private bool anomalyActive;
    private float anomalyTimer;
    private Coroutine failTimer;

    void Awake()
    {
        // hook up the verifier
        verifier = GetComponent<ArtifactVerifier>();
        if (verifier == null)
            Debug.LogError("GramophoneAnomalyController requires ArtifactVerifier!");
        else
            verifier.onCorrectPlacement.AddListener(OnRecordPlaced);

        // ensure anomalySource is ready
        anomalySource.loop = true;
        anomalySource.volume = 0f;
    }

    void Start()
    {
        // disable pickup until anomaly begins
        if (recordPickupObject != null)
            recordPickupObject.layer = LayerMask.NameToLayer("Default");

        // start normal audio
        normalSource.loop = true;
        normalSource.Play();
    }

    void Update()
    {
        
#if UNITY_EDITOR
        if (manualTrigger)
        {
            manualTrigger = false;
            ActivateAnomaly();
        }
#endif
        // spin the turntable
        if (spinPivot != null)
            spinPivot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

        // if anomaly is up, ramp volume
        if (anomalyActive)
        {
            anomalyTimer += Time.deltaTime;
            float t = Mathf.Clamp01(anomalyTimer / anomalyRiseDuration);
            anomalySource.volume = Mathf.Lerp(0f, maxAnomalyVolume, t);
        }
    }

    /// <summary>
    /// IAnomaly API: start the anomaly.
    /// Enables the world-record for pickup and kicks off the rise timer.
    /// </summary>
    public void ActivateAnomaly()
    {
        if (anomalyActive) return;

        anomalyActive = true;
        anomalyTimer  = 0f;

        // swap audio
        normalSource.Stop();
        anomalySource.Play();

        // enable pickup layer
        if (recordPickupObject != null)
            recordPickupObject.layer = LayerMask.NameToLayer("Pickup");

        // schedule a failure if not placed in time
        if (failTimer != null) StopCoroutine(failTimer);
        failTimer = StartCoroutine(FailAfterDelay(anomalyRiseDuration));
    }

    /// <summary>
    /// Called by ArtifactVerifier when the correct record is placed.
    /// Snaps the record into place, stops anomaly, restores normal audio,
    /// disables further pickup, and raises OnCleared.
    /// </summary>
    private void OnRecordPlaced()
    {
        // cancel the failure timer
        if (failTimer != null)
        {
            StopCoroutine(failTimer);
            failTimer = null;
        }

        // hide the original mesh
        if (originalRecordMesh != null)
            originalRecordMesh.SetActive(false);

        // find the snapped-in record by tag and parent under spinPivot
        var hits = Physics.OverlapSphere(recordMountPoint.position, 0.1f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Record"))
            {
                // parent and align
                hit.transform.SetParent(spinPivot, worldPositionStays: true);
                hit.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                break;
            }
        }

        // reset anomaly
        anomalyActive = false;
        anomalySource.Stop();
        normalSource.Play();

        // disable pickup again
        if (recordPickupObject != null)
        {
            recordPickupObject.layer = LayerMask.NameToLayer("Default");
        }

        // tell GameController we're done
        OnCleared?.Invoke();
    }

    /// <summary>
    /// If the player fails to place within the allotted time,
    /// reset the anomaly but do NOT invoke OnCleared.
    /// </summary>
    private IEnumerator FailAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        anomalyActive = false;
        anomalySource.Stop();
        normalSource.Play();
        Debug.LogWarning("Gramophone anomaly timed out!");
    }

    void OnDestroy()
    {
        if (verifier != null)
            verifier.onCorrectPlacement.RemoveListener(OnRecordPlaced);
    }
}
