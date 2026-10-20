using UnityEngine;

public sealed class ControlPanel : MonoBehaviour, IInteractable
{
    [SerializeField] private MonoBehaviour doorController;
    [SerializeField] private Transform authorizedInteractor;
    [SerializeField, Min(0.1f)] private float interactionRange = 1.2f;
    [SerializeField, Min(0f)] private float activationDuration = 1f;
    [SerializeField, Min(1f)] private float doorWaitTimeout = 8f;
    [SerializeField] private InteractionStatus currentStatus = InteractionStatus.Idle;

    public InteractionStatus Status => currentStatus;
    public string FailureReason { get; private set; } = string.Empty;

    private IOpenableDoor door;
    private Transform activeActor;
    private float interactionStarted;
    private float openingRequestedAt;
    private bool openingRequested;

    private void Awake()
    {
        door = doorController as IOpenableDoor;
        currentStatus = InteractionStatus.Idle;
    }

    public bool TryInteract(Transform actor)
    {
        FailureReason = string.Empty;
        if (actor == null || authorizedInteractor == null || actor != authorizedInteractor)
            return Fail("Bu kontrol panelini yalnızca AX-7 kullanabilir.");
        if (!isActiveAndEnabled || !DoorIsAvailable())
            return Fail("Kontrol paneli veya bağlı kapı kullanılamıyor.");
        if (!IsInRange(actor))
            return Fail("AX-7 kontrol panelini kullanmak için yeterince yakın değil.");
        if (currentStatus == InteractionStatus.Succeeded && door.IsOpen)
            return true;

        activeActor = actor;
        interactionStarted = Time.time;
        openingRequested = false;
        currentStatus = InteractionStatus.InProgress;
        return true;
    }

    public void Tick(Transform actor)
    {
        if (currentStatus != InteractionStatus.InProgress)
            return;
        if (actor == null || actor != activeActor || actor != authorizedInteractor)
        {
            Fail("Paneli kullanan robot artık mevcut değil veya değişti.");
            return;
        }
        if (!isActiveAndEnabled || !DoorIsAvailable())
        {
            Fail("Panel veya kapı etkileşim sırasında devre dışı kaldı.");
            return;
        }
        if (!openingRequested)
        {
            if (!IsInRange(actor))
            {
                Fail("AX-7 panel etkinleştirilmeden etkileşim mesafesinden çıktı.");
                return;
            }
            if (Time.time - interactionStarted < activationDuration)
                return;
            if (!door.TryOpen(out var reason))
            {
                Fail(reason);
                return;
            }
            openingRequested = true;
            openingRequestedAt = Time.time;
            Debug.Log("AX-7 kontrol panelini etkinleştirdi; kapı açılıyor.", this);
        }

        if (door.IsOpen)
            currentStatus = InteractionStatus.Succeeded;
        else if (Time.time - openingRequestedAt >= doorWaitTimeout)
            Fail("Kapı ayrılan sürede tamamen açılamadı.");
    }

    private bool DoorIsAvailable()
    {
        if (doorController == null)
            return false;
        if (door == null)
            door = doorController as IOpenableDoor;
        return door != null && door.IsAvailable;
    }

    private bool IsInRange(Transform actor)
    {
        // Robotun kökü ayak hizasındadır; yatay etkileşim mesafesi ölçülür.
        Vector3 offset = actor.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= interactionRange * interactionRange;
    }

    private bool Fail(string reason)
    {
        FailureReason = reason;
        currentStatus = InteractionStatus.Failed;
        return false;
    }
}
