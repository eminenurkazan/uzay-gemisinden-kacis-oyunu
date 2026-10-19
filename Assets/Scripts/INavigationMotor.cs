using UnityEngine;

public enum NavigationStatus
{
    Idle,
    Moving,
    Arrived,
    Failed,
    Paused
}

// Karar kodu bu sözleşmeyi kullanır; NavMeshAgent ayrıntılarını bilmez.
public interface INavigationMotor
{
    NavigationStatus Status { get; }
    string FailureReason { get; }
    Vector3 SteeringTarget { get; }
    bool TryMoveTo(Vector3 destination);
    void Tick();
    bool Pause();
    bool Resume();
    void Stop();
}
