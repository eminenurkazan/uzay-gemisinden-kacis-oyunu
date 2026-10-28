using UnityEngine;
using UnityEngine.AI;

public enum DoorState
{
    Closed,
    Opening,
    Open,
    Closing
}

[RequireComponent(typeof(BoxCollider))]
public sealed class DoorController : MonoBehaviour, IClosableDoor
{
    [SerializeField, Min(0.1f)] private float openingHeight = 3.2f;
    [SerializeField, Min(0.1f)] private float openingSpeed = 2.5f;
    [SerializeField] private DoorState currentState = DoorState.Closed;
    [SerializeField] private MonoBehaviour closingSafetySensor;

    private Vector3 closedPosition;
    private BoxCollider doorCollider;
    private NavMeshObstacle navigationObstacle;
    private IDoorwaySensor safety;

    public bool IsOpen => currentState == DoorState.Open;
    public bool IsClosed => currentState == DoorState.Closed;
    public bool IsClosing => currentState == DoorState.Closing;
    public bool IsAvailable => isActiveAndEnabled && doorCollider != null
        && doorCollider.enabled && !doorCollider.isTrigger;
    public bool CanCloseSafely => IsAvailable && closingSafetySensor != null
        && closingSafetySensor.isActiveAndEnabled && safety != null && safety.IsClear();

    private void Awake()
    {
        closedPosition = transform.localPosition;
        doorCollider = GetComponent<BoxCollider>();
        currentState = DoorState.Closed;
        safety = closingSafetySensor as IDoorwaySensor;
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
        if (currentState == DoorState.Closed || currentState == DoorState.Closing)
            currentState = DoorState.Opening;
        return true;
    }

    public bool TryClose(out string failureReason)
    {
        failureReason = string.Empty;
        if (!IsAvailable)
        {
            failureReason = "Kapı veya fiziksel çarpışma bileşeni etkin değil.";
            return false;
        }
        if (IsClosed || IsClosing)
            return true;
        if (!IsOpen)
        {
            failureReason = "Kapanmadan önce kapının tamamen açılması gerekiyor.";
            return false;
        }
        if (!CanCloseSafely)
        {
            failureReason = "Kapı açıklığı boş değil veya güvenlik sensörü kullanılamıyor.";
            return false;
        }
        currentState = DoorState.Closing;
        if (navigationObstacle != null)
            navigationObstacle.enabled = true;
        return true;
    }

    private void Update()
    {
        if (currentState != DoorState.Opening && currentState != DoorState.Closing)
            return;
        if (!IsAvailable)
            return;
        if (IsClosing && !CanCloseSafely)
        {
            currentState = DoorState.Opening;
            Debug.LogWarning("Kapak kapanışı güvenlik nedeniyle durdu; yeniden açılıyor. Engel: "
                + (safety != null ? safety.BlockingObjectName : "Sensör kullanılamıyor."), this);
            return;
        }
        bool opening = currentState == DoorState.Opening;
        Vector3 target = opening ? closedPosition + Vector3.up * openingHeight : closedPosition;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, openingSpeed * Time.deltaTime);
        if ((transform.localPosition - target).sqrMagnitude > 0.000001f)
            return;
        transform.localPosition = target;
        currentState = opening ? DoorState.Open : DoorState.Closed;
        if (navigationObstacle != null)
            navigationObstacle.enabled = !opening;
        Debug.Log(opening ? "Fiziksel kapı tamamen açıldı." : "Fiziksel kapı tamamen kapandı.", this);
    }
}
