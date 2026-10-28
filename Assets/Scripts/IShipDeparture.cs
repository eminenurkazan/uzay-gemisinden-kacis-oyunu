using System;
using UnityEngine;

public enum ShipDepartureState
{
    Locked,
    Ready,
    Countdown,
    Fading,
    Completed
}

public interface IShipDeparture
{
    ShipDepartureState CurrentState { get; }
    string StatusMessage { get; }
    float FadeProgress { get; }
    event Action<ShipDepartureState> StateChanged;
    bool IsInInteractionArea(Transform actor);
    bool CanStart(Transform actor);
    bool TryStart(Transform actor);
}
