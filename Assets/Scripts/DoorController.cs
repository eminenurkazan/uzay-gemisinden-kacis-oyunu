using UnityEngine;
using UnityEngine.AI;

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
    private NavMeshObstacle navigationObstacle;

    public bool IsOpen => currentState == DoorState.Open;
    public bool IsAvailable => isActiveAndEnabled && doorCollider != null
        && doorCollider.enabled && !doorCollider.isTrigger;

    private void Awake()
    {
        closedPosition = transform.localPosition;
        doorCollider = GetComponent<BoxCollider>();
        currentState = DoorState.Closed;
        navigationObstacle = GetComponent<NavMeshObstacle>();
        if (navigationObstacle != null)
            navigationObstacle.enabled = true;
        else
            Debug.LogWarning("Kapının NavMesh engeli eksik. Play dışında AX-7 NavMesh Hazırla menüsünü çalıştır.", this);
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
            // Collider yerinde kalır; yalnızca NavMesh üzerindeki kapalı geçiş kaldırılır.
            if (navigationObstacle != null)
                navigationObstacle.enabled = false;
            Debug.Log("Fiziksel kapı tamamen açıldı.", this);
        }
    }
}
