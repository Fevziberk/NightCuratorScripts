using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class LensStation : MonoBehaviour
{
    [Header("UV Flashlight Script")]
    [Tooltip("Drag your UVFlashlightDetector component here")]
    public MonoBehaviour UVFlashlightDetector;

    [Header("Lens Model (World)")]
    [Tooltip("The physical lens object in the scene")]
    public GameObject lensModel;
    public UnityEvent onLensPlaced;

    bool used = false;
    public bool Used => used;

    void Awake()
    {
        // collider is a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    
    // calling this to “equip” the lens—enables UV mode script, hides the world model
    
    public void ActivateLens()
    {
        if (used) return;
        used = true;

        // hide world model
        if (lensModel != null)
            lensModel.SetActive(false);

        // enable the UV-flashlight
        if (UVFlashlightDetector != null)
            UVFlashlightDetector.enabled = true;
        Debug.Log("aktif oldu uv");
        onLensPlaced?.Invoke();
    }
}
