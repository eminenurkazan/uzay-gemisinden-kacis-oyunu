using UnityEngine;

public enum DoorState
{
    Closed,
    Opening,
    Open
}

[RequireComponent(typeof(BoxCollider))]
public sealed class DoorController : MonoBehaviour, IOpenableDoor
{
    [SerializeField, Min(0.1f)] private float openingHeight = 3.2f;
    [SerializeField, Min(0.1f)] private float openingSpeed = 2.5f;
    [SerializeField] private DoorState currentState = DoorState.Closed;

    private Vector3 closedPosition;
    private BoxCollider doorCollider;

    public bool IsOpen => currentState == DoorState.Open;
    public bool IsAvailable => isActiveAndEnabled && doorCollider != null
        && doorCollider.enabled && !doorCollider.isTrigger;

    private void Awake()
    {
        closedPosition = transform.localPosition;
        doorCollider = GetComponent<BoxCollider>();
        currentState = DoorState.Closed;
    }

    public bool TryOpen(out string failureReason)
    {
        failureReason = string.Empty;
        if (!IsAvailable)
        {
            failureReason = "Kapı veya fiziksel çarpışma bileşeni etkin değil.";
            return false;
        }
        if (openingHeight <= 0f || openingSpeed <= 0f)
        {
            failureReason = "Kapının açılma mesafesi veya hızı geçersiz.";
            return false;
        }
        if (currentState == DoorState.Closed)
            currentState = DoorState.Opening;
        return true;
    }

    private void Update()
    {
        if (currentState != DoorState.Opening)
            return;
        Vector3 target = closedPosition + Vector3.up * openingHeight;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, openingSpeed * Time.deltaTime);
        if ((transform.localPosition - target).sqrMagnitude <= 0.000001f)
        {
            transform.localPosition = target;
            currentState = DoorState.Open;
            Debug.Log("Fiziksel kapı tamamen açıldı.", this);
        }
    }
}
