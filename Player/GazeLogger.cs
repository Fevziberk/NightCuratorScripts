using UnityEngine;

public class GazeLogger : MonoBehaviour
{
    public float gazeRange = 5f;
    public LayerMask detectableLayer;

    private GameObject lastLookedAt = null;

    void Update()
    {
        
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, gazeRange, detectableLayer))
        {
            GameObject currentObject = hit.collider.gameObject;

            if (currentObject != lastLookedAt)
            {
                Debug.Log($"Looking at object with tag: {currentObject.tag}");
                lastLookedAt = currentObject;
            }
        }
        else
        {
            lastLookedAt = null;
        }
    }

}
