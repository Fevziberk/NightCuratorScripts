using UnityEngine;

public enum PickupType {Record, Crown}

public class PickupItem : MonoBehaviour
{
    public string itemName = "Unnamed";
    public PickupType type;
    public Transform holdPoint; 
}




