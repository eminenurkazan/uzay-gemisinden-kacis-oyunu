using UnityEngine;

public enum ShipAccessState
{
    Locked,
    Ready,
    Opening,
    Open,
    Boarded,
    Failed,
    Closing,
    Sealed,
    Reopening
}

// Girdi ve HUD, gemi geçişinin fiziksel kapı ayrıntılarına bağımlı değildir.
public interface IShipAccess
{
    ShipAccessState CurrentState { get; }
    string StatusMessage { get; }
    bool IsPlayerInside { get; }
    bool IsSealed { get; }
    bool IsInInteractionArea(Transform actor);
    bool CanOpenHatch(Transform actor);
    bool TryOpenHatch(Transform actor);
    bool CanCloseHatch(Transform actor);
    bool TryCloseHatch(Transform actor);
}
