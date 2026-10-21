using System;
using UnityEngine;

public enum AtmosphereControlState
{
    Locked,
    Ready,
    Processing,
    Completed
}

// Kael'in tepkisi panelin tuş okuma ve süre yönetimini bilmeden bu olayı dinler.
public interface IAtmosphereControl
{
    AtmosphereControlState CurrentState { get; }
    string StatusMessage { get; }
    event Action AtmosphereControlCompleted;
    bool IsInInteractionArea(Transform actor);
    bool CanActivate(Transform actor);
    bool TryActivate(Transform actor);
}
