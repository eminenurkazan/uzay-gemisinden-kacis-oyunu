using UnityEngine;

public enum NavigationStatus
{
    Idle,
    Moving,
    Arrived,
    Failed
}

// Karar kodu bu sözleşmeyi kullanır; NavMeshAgent ayrıntılarını bilmez.
public interface INavigationMotor
{
    NavigationStatus Status { get; }
    string FailureReason { get; }
    bool TryMoveTo(Vector3 destination);
    void Tick();
    void Stop();
}
