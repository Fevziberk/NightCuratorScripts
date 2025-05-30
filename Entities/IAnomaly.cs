// IAnomaly.cs
public interface IAnomaly
{
    /// <summary>Starts the anomaly’s behaviour.</summary>
    void ActivateAnomaly();

    /// <summary>Called by the GameController once the anomaly is “cleared.”</summary>
    event System.Action OnCleared;

    /// <summary>True from ActivateAnomaly() until it invokes OnCleared.</summary>
    bool IsActive { get; }
}
