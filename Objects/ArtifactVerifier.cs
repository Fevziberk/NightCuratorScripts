using UnityEngine;
using UnityEngine.Events;

public class ArtifactVerifier : MonoBehaviour
{
    [Header("Expected Pickup")]
    public PickupType expectedType;

    [Header("Snap Point")]
    public Transform snapPoint;

    [Header("Correct Placement Event")]
    [Tooltip("Assign any methods (e.g. OnRecordPlaced) to this event.")]
    public UnityEvent onCorrectPlacement;

    /// <summary>
    /// Returns true if heldItem matches expectedType.
    /// Snaps it into place and invokes the event.
    /// </summary>
    public bool CheckObject(PickupItem heldItem)
    {
        if (heldItem == null || heldItem.type != expectedType)
            return false;

        // Snap the object
        heldItem.transform.SetParent(null);
        heldItem.transform.position = snapPoint.position;
        heldItem.transform.rotation = snapPoint.rotation;
        var rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // fires the invoke
        onCorrectPlacement?.Invoke();
        return true;
    }
}
