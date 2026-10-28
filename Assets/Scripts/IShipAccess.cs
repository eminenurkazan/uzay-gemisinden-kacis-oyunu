using UnityEngine;

public enum ShipAccessState
{
    Locked,
    Ready,
    Opening,
    Open,
    Boarded,
    Failed
}

// Girdi ve HUD, gemi geçişinin fiziksel kapı ayrıntılarına bağımlı değildir.
public interface IShipAccess
{
    ShipAccessState CurrentState { get; }
    string StatusMessage { get; }
    bool IsPlayerInside { get; }
    bool IsInInteractionArea(Transform actor);
    bool CanOpenHatch(Transform actor);
    bool TryOpenHatch(Transform actor);
}
