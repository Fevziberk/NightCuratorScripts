using UnityEngine;

public class LaptopTerminal : MonoBehaviour
{
    [Header("Statue Reference")]
    [Tooltip("Reference to the StatueController to disable when interacting.")]
    public StatueController statue;

    [Header("Screens")]
    [Tooltip("Child GameObject with the laptop screen OFF (closed)")]
    public GameObject closedScreen;
    [Tooltip("Child GameObject with the laptop screen ON (open)")]
    public GameObject openScreen;

    [Header("Interaction Settings")]
    [Tooltip("Key to press for interaction.")]
    public KeyCode interactKey = KeyCode.F;
    [Tooltip("Maximum distance the player can interact from.")]
    public float interactDistance = 3f;
    [Tooltip("Layer mask for the laptop interaction raycast.")]
    public LayerMask interactLayer;

    [Header("Player Interaction Manager")]
    [Tooltip("Reference to the player's interaction manager (handles UI prompts).")]
    public PlayerInteractionManager playerInteractionManager;

    [Header("Audio")]
    [Tooltip("Sound to play on each keypress")]
    public AudioClip keyPressSfx;
    private AudioSource audioSource;

    private Camera playerCamera;
    private bool isOpen = false;

    void Start()
    {
        playerCamera = Camera.main;
        if (statue == null)
            Debug.LogWarning("LaptopTerminal: No StatueController assigned.");
        if (playerInteractionManager == null)
            Debug.LogWarning("LaptopTerminal: No PlayerInteractionManager assigned.");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // ensure initial state
        SetScreen(false);
    }

    void LateUpdate()
    {
        
        // hide prompt if statue already disabled
        if (statue == null || !statue.killCollider.enabled)
        {
            playerInteractionManager.interactUIPrompt.SetActive(false);
            return;
        }

        // raycast to see if we are looking at this laptop
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer) 
            && hit.collider.gameObject == gameObject)
        {
            playerInteractionManager.interactUIPrompt.SetActive(true);

            if (Input.GetKeyDown(interactKey))
            {
                // toggle screen open
                isOpen = !isOpen;
                SetScreen(isOpen);

                // play keypress sound
                if (keyPressSfx != null)
                    audioSource.PlayOneShot(keyPressSfx);

                // perform countermeasure when first opened
                if (isOpen)
                {
                    statue.DisableStatue();
                    Debug.Log("Statue countermeasured via Laptop Terminal.");
                    playerInteractionManager.interactUIPrompt.SetActive(false);
                }
            }
        }
        else
        {
            playerInteractionManager.interactUIPrompt.SetActive(false);
        }
    }

    private void SetScreen(bool open)
    {
        if (closedScreen != null) closedScreen.SetActive(!open);
        if (openScreen   != null) openScreen  .SetActive(open);
    }
}
