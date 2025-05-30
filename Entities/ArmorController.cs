using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody), typeof(AudioSource))]
public class HorsemanController : MonoBehaviour, IAnomaly
{
    [Header("Game Over")]
    [Tooltip("Reference to the GameController to trigger Game Over sequence.")]
    public GameController gameController;

    [Header("References")]
    [Tooltip("Transform of the player to chase.")]
    public Transform player;
    [Tooltip("Animator component for controlling movement/attack animations.")]
    public Animator anim;
    [Tooltip("AudioSource for playing footstep sounds.")]
    public AudioSource footstepAudio;
    [Tooltip("AudioSource for playing slash sound.")]
    public AudioSource slashAudio;
    [Tooltip("Sound effect clip for the slash attack.")]
    public AudioClip slashSfx;
    [Tooltip("Collider used to detect and kill the player when in range.")]
    public Collider killCollider;

    [Header("Crown Pickup")]
    [Tooltip("The crown object that the Horseman can pick up.")]
    public GameObject crownPickupObject;
    [Tooltip("Location where the crown should be placed to clear the anomaly.")]
    public Transform crownGuardPoint;

    [Header("Chase Settings")]
    [Tooltip("Movement speed of the Horseman when chasing.")]
    public float chaseSpeed = 3f;

    [Header("Manual Trigger")]
    [Tooltip("Key to manually activate the anomaly (for testing).")]
    public KeyCode manualTriggerKey = KeyCode.H;

    [Header("Countermeasure")]
    [Tooltip("ArtifactVerifier responsible for checking crown placement.")]
    public ArtifactVerifier crownVerifier;

    private NavMeshAgent agent;
    private bool anomalyActive;
    private bool crownPlaced;

    public bool AnomalyActive => anomalyActive;

    public bool IsActive => throw new System.NotImplementedException();

    // Event invoked when anomaly is cleared
    public event System.Action OnCleared;

    void Awake()
    {
        // cache NavMeshAgent and configure movement
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // Disable physics on the rigidbody
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Configure slash audio source
        slashAudio = GetComponent<AudioSource>();
        slashAudio.playOnAwake = false;

        // Disable kill collider until anomaly is active
        killCollider.isTrigger = true;
        killCollider.enabled = false;

        // Loop footstep sound when walking
        footstepAudio.loop = true;

        // Subscribe to crown placement event
        crownVerifier.onCorrectPlacement.AddListener(OnCrownPlaced);
    }

    void Start()
    {
        // Ensure crown starts on default layer
        if (crownPickupObject != null)
            crownPickupObject.layer = LayerMask.NameToLayer("Default");
    }

    void Update()
    {
        // Manual activation (for debug)
        if (!anomalyActive && Input.GetKeyDown(manualTriggerKey))
            ActivateAnomaly();

        // Chase when active and crown not placed
        if (anomalyActive && !crownPlaced)
        {
            agent.SetDestination(player.position);
            bool walking = agent.desiredVelocity.sqrMagnitude > 0.1f;
            anim.SetBool("isWalking", walking);

            // Play/stop footstep audio based on movement
            if (walking && !footstepAudio.isPlaying) footstepAudio.Play();
            if (!walking && footstepAudio.isPlaying) footstepAudio.Stop();

            // Smoothly rotate towards movement direction
            if (walking)
            {
                Quaternion look = Quaternion.LookRotation(agent.desiredVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
            }
        }
    }

    /// <summary>
    /// Activates the Horseman anomaly: enables chasing, audio, and kill collider.
    /// </summary>
    public void ActivateAnomaly()
    {
        anomalyActive = true;
        crownPlaced = false;
        agent.isStopped = false;
        footstepAudio.Play();
        killCollider.enabled = true;

        // Allow crown pickup
        if (crownPickupObject != null)
            crownPickupObject.layer = LayerMask.NameToLayer("Pickup");
    }

   
    // Called when the crown is correctly placed at the verifier.
    // Stops the chase and walks Horseman to guard point.
   
    private void OnCrownPlaced()
    {
        if (!anomalyActive) return;

        crownPlaced = true;
        anomalyActive = false;
        agent.isStopped = true;
        anim.SetBool("isWalking", false);
        footstepAudio.Stop();
        killCollider.enabled = false;

        // Return crown to default layer
        if (crownPickupObject != null)
            crownPickupObject.layer = LayerMask.NameToLayer("Default");

        // Walk to guard point then idle
        StartCoroutine(WalkToGuardPointAndIdle());

        // Notify shift controller of anomaly clearance
        OnCleared?.Invoke();
    }

    private IEnumerator WalkToGuardPointAndIdle()
    {
        if (crownGuardPoint == null) yield break;

        agent.isStopped = false;
        anim.SetBool("isWalking", true);
        agent.SetDestination(crownGuardPoint.position);

        // Wait until arrival or timeout
        float timeout = 20f;
        while ((agent.pathPending || agent.remainingDistance > agent.stoppingDistance) && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Stop moving and playing footstep audio
        agent.isStopped = true;
        anim.SetBool("isWalking", false);
        footstepAudio.Stop();
    }

    void OnTriggerEnter(Collider other)
    {
        // If anomaly active and collides with player, trigger game over
        if (anomalyActive && other.CompareTag("Player"))
        {
            agent.isStopped = true;
            footstepAudio.Stop();
            anim.SetTrigger("Attack");
            slashAudio.PlayOneShot(slashSfx, 1f);
            killCollider.enabled = false;
            gameController?.TriggerGameOver(transform);
        }
    }
}
