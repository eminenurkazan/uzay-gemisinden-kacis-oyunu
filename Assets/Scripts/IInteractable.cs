using UnityEngine;

public enum InteractionStatus
{
    Idle,
    InProgress,
    Succeeded,
    Failed
}

// Robot, panelin veya kapının somut sınıfını bilmek zorunda değildir.
public interface IInteractable
{
    InteractionStatus Status { get; }
    string FailureReason { get; }
    bool TryInteract(Transform actor);
    void Tick(Transform actor);
}

// Panel, kapının nasıl hareket ettiğine değil açılma sözleşmesine bağımlıdır.
public interface IOpenableDoor
{
    bool IsOpen { get; }
    bool IsAvailable { get; }
    bool TryOpen(out string failureReason);
}
