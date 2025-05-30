// StatueController.cs
using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class StatueController : MonoBehaviour, IAnomaly
{
    [Header("Game Over")]
    public GameController gameController;

    [Header("References")]
    public Camera playerCamera;
    public Collider killCollider;
    public AudioSource footstepAudio;

    [Header("Visual States")]
    public GameObject angelIdle;
    public GameObject angelAttack;
    public GameObject angelPoint;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float viewDistance = 10f;

    [Header("Orientation")]
    [Tooltip("Yaw offset to correct model forward direction.")]
    public float yawOffset = 0f;

    [Header("Inspector Toggles")]
    [SerializeField] private bool isActive = false;
    [SerializeField] private bool isDisabled = false;

    private NavMeshAgent agent;

    public bool IsActive => throw new NotImplementedException();

    /// <summary>
    /// Raised when this anomaly has been successfully disabled.
    /// </summary>
    public event Action OnCleared;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Start()
    {
        // initialize audio & trigger
        killCollider.enabled = isActive && !isDisabled;
        footstepAudio.loop = true;
        if (!isActive || isDisabled)
            footstepAudio.Stop();

        // decide whether to move or stop
        if (isActive && !isDisabled)
        {
            bool inView = IsInCameraFrustum();
            agent.isStopped = inView;
            if (!inView) footstepAudio.Play();
        }

        UpdateVisualState();
    }

    void Update()
    {
        
        if (!isActive || isDisabled)
            return;

        bool inView = IsInCameraFrustum();
        if (inView)
        {
            // stop when statue becomes visible
            if (!agent.isStopped)
            {
                agent.ResetPath();
                agent.isStopped = true;
                footstepAudio.Stop();
                UpdateVisualState();
            }
        }
        else
        {
            // chase the player
            if (agent.isStopped)
            {
                agent.isStopped = false;
                footstepAudio.Play();
                UpdateVisualState();
            }
            NavMeshHit hit;
            if (NavMesh.SamplePosition(playerCamera.transform.position, out hit, viewDistance, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    void LateUpdate()
    {
        if (!isActive || isDisabled) return;
        if (IsInCameraFrustum()) return;

        // always face the player horizontally when out of view
        Vector3 dir = playerCamera.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            var target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = target * Quaternion.Euler(0, yawOffset, 0);
        }
    }

    private bool IsInCameraFrustum()
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        return GeometryUtility.TestPlanesAABB(planes, killCollider.bounds);
    }

    /// <summary>
    /// IAnomaly API: start the statue chase
    /// </summary>
    public void ActivateAnomaly()
    {
        isDisabled = false;
        isActive = true;
        killCollider.enabled = true;
        agent.isStopped = false;
        footstepAudio.Play();
        UpdateVisualState();
    }

    /// <summary>
    /// calling thist stop the statue and mark it cleared
    /// </summary>
    public void DisableStatue()
    {
        isDisabled = true;
        isActive = false;
        agent.ResetPath();
        agent.isStopped = true;
        killCollider.enabled = false;
        footstepAudio.Stop();
        UpdateVisualState();

        // signal to GameController that this anomaly is done
        OnCleared?.Invoke();
    }

    private void UpdateVisualState()
    {
        if (isDisabled)
        {
            // show only point
            angelIdle.SetActive(false);
            angelAttack.SetActive(false);
            angelPoint.SetActive(true);
        }
        else if (isActive)
        {
            // show attack pose
            angelIdle.SetActive(false);
            angelAttack.SetActive(true);
            angelPoint.SetActive(false);
        }
        else
        {
            // show idle
            angelIdle.SetActive(true);
            angelAttack.SetActive(false);
            angelPoint.SetActive(false);
        }
    }

    void OnValidate()
    {
        if (killCollider != null)
            killCollider.enabled = isActive && !isDisabled;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isDisabled && other.CompareTag("Player"))
        {
            // stop everything, switch to attack pose one last time
            killCollider.enabled = false;
            agent.isStopped = true;
            footstepAudio.Stop();
            UpdateVisualState();
            isDisabled = true;

            // kill the player
            gameController.TriggerGameOver(transform);
        }
    }
}
