using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractionManager : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Sound effect played when picking up an item.")]
    public AudioClip pickupSfx;
    private AudioSource sfxSource;

    [Header("Pickup Settings")]
    [Tooltip("Maximum distance from which items can be picked up.")]
    public float pickupRange = 3f;
    [Tooltip("Layer mask specifying which objects can be picked up or interacted with.")]
    public LayerMask pickupLayer;
    [Tooltip("Transform representing the player's hand for holding items.")]
    public Transform handTransform;

    [Header("UI Prompts")]
    [Tooltip("UI prompt indicating the player can pick up an item.")]
    public GameObject pickupUIPrompt;
    [Tooltip("UI prompt indicating the player can interact (e.g. press F).")]
    public GameObject interactUIPrompt;
    
    [Header("Timing Settings")]
    [Tooltip("Cooldown time after picking up before the item can be dropped.")]
    public float pickupCooldown = 1f;
    private GameObject heldObject;
    private PickupItem currentItem;

    private float pickupTimer = 0f;
    private bool isHoldingItem = false;
    private bool justPickedUp = false;
    private bool canDrop = false;

    void Start()
    {
        // Hide UI prompts
        pickupUIPrompt?.SetActive(false);
        interactUIPrompt?.SetActive(false);

        // make sure an AudioSource exists
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    void Update()
    {
        // Handle lens equip first, skip other logic if done
        if (TryHandleLensEquip()) return;
        
        if (isHoldingItem)
        {
            HandleHoldingItem();
        }
        else
        {
            HandlePickupDetection();
        }
    }

    /// <summary>
    /// If the player is looking at an unused LensStation and presses F,
    /// equips the lens and returns true to skip further processing.
    /// </summary>
    private bool TryHandleLensEquip()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out var hit, pickupRange, pickupLayer))
        {
            var lens = hit.collider.GetComponent<LensStation>();
            if (lens != null && !lens.Used)
            {
                interactUIPrompt?.SetActive(true);
                if (Input.GetKeyDown(KeyCode.F))
                {
                    lens.ActivateLens();
                    interactUIPrompt?.SetActive(false);
                }
                return true;
            }
        }
        return false;
    }

    // Controls when an item is already held
    void HandleHoldingItem()
    {
        pickupUIPrompt?.SetActive(false);

        // Wait for cooldown after pickup
        if (justPickedUp)
        {
            pickupTimer += Time.deltaTime;
            if (pickupTimer >= pickupCooldown)
            {
                justPickedUp = false;
                canDrop = true;
            }
            return;
        }

        // Drop item immediately on G
        if (Input.GetKeyDown(KeyCode.G) && canDrop)
        {  
            DropItem();
            return;
        }

        // Show interact prompt when looking at an ArtifactVerifier
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer))
        {
            ArtifactVerifier artifact = hit.collider.GetComponent<ArtifactVerifier>();

            if (artifact != null)
            {
                interactUIPrompt?.SetActive(true);

                if (Input.GetKeyDown(KeyCode.F))
                {
                    TrySubmitToArtifact();
                }
                return;
            }
        }

        // Hide interact prompt if nothing to interact with
        interactUIPrompt?.SetActive(false);
    }

    
    // Detect and pick up items
    void HandlePickupDetection()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer))
        {
            PickupItem item = hit.collider.GetComponent<PickupItem>();

            if (item != null)
            {
                pickupUIPrompt?.SetActive(true);
                interactUIPrompt?.SetActive(false);

                currentItem = item;

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUp(item);
                }
                return;
            }
        }

        // Hide all prompts if nothing detected
        pickupUIPrompt?.SetActive(false);
        interactUIPrompt?.SetActive(false);
    }

    
    // Pick up the specified item
    void PickUp(PickupItem item)
    {
        heldObject    = item.gameObject;
        isHoldingItem = true;
        justPickedUp  = true;
        canDrop       = false;
        pickupTimer   = 0f;

        // Play pickup sound effect
        if (pickupSfx != null)
            sfxSource.PlayOneShot(pickupSfx, 4f);

        // Parent the object to the hand transform
        heldObject.transform.SetParent(handTransform, worldPositionStays: false);
        heldObject.transform.localPosition = item.holdPoint.localPosition;
        heldObject.transform.localRotation = item.holdPoint.localRotation;

        // Disable physics
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        // Disable collider while holding to prevent self-raycast
        var col = heldObject.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        pickupUIPrompt?.SetActive(false);
    }

     // Drop the held item
    void DropItem()
    {
        if (heldObject != null)
        {
            // Re-enable collider
            var col = heldObject.GetComponent<Collider>();
            if (col != null)
                col.enabled = true;

            // Unparent from hand
            heldObject.transform.SetParent(null);

            // Restore physics and apply a small force
            var rb = heldObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(Camera.main.transform.forward * 2f, ForceMode.Impulse);
            }
        }

        // Reset state
        heldObject      = null;
        isHoldingItem   = false;
        justPickedUp    = false;
        canDrop         = false;
    }

    
    // Attempt to submit the held item to an artifact verifier 
    void TrySubmitToArtifact()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer))
        {
            ArtifactVerifier verifier = hit.collider.GetComponent<ArtifactVerifier>();

            if (verifier != null && heldObject != null)
            {
                bool placedSuccessfully = verifier.CheckObject(heldObject.GetComponent<PickupItem>());

                if (placedSuccessfully)
                {
                    // Play confirmation sound and clear held item
                    sfxSource.PlayOneShot(pickupSfx, 4f);
                    heldObject = null;
                    isHoldingItem = false;
                    justPickedUp = false;
                    canDrop = false;
                }
                else
                {
                    Debug.Log("Object remained in hand.");
                }
            }
        }
    }
}
