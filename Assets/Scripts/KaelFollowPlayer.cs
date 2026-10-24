using UnityEngine;

public enum KaelFollowState
{
    WaitingForPermission,
    Following,
    WaitingNearPlayer,
    WaitingForRoute
}

// Karar kapı durumundan gelir; yol bulmayı AX-7 ile ortak hareket arayüzü yürütür.
public sealed class KaelFollowPlayer : MonoBehaviour
{
    [SerializeField] private BiometricDoorLink biometricDoor;
    [SerializeField] private KaelDialogueView dialogueView;
    [SerializeField] private MonoBehaviour navigationMotor;
    [SerializeField] private Transform player;
    [SerializeField, Min(0.5f)] private float stopDistance = 1.7f;
    [SerializeField, Min(0.6f)] private float resumeDistance = 2.4f;
    [SerializeField, Min(0.1f)] private float repathInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float targetMovementThreshold = 0.6f;
    [SerializeField, Min(0.2f)] private float retryInterval = 1f;
    [SerializeField] private KaelFollowState currentState;

    public KaelFollowState CurrentState => currentState;
    public string StatusMessage => currentState switch
    {
        KaelFollowState.Following => "Kael seninle geliyor.",
        KaelFollowState.WaitingNearPlayer => "Kael yakınında bekliyor; ilerleyince seninle gelecek.",
        KaelFollowState.WaitingForRoute => "Kael geçilebilir rota bekliyor; yeniden deneyecek.",
        _ => string.Empty
    };

    private INavigationMotor motor;
    private CharacterController playerBody;
    private Vector3 lastDestination;
    private float nextPathTime;
    private int readyFrame;
    private bool ownsMotor;

    private void Awake()
    {
        motor = navigationMotor as INavigationMotor;
        currentState = KaelFollowState.WaitingForPermission;
        if (biometricDoor == null || dialogueView == null || motor == null || player == null)
        {
            Debug.LogError("Kael takibinin kapı, konuşma, hareket veya oyuncu bağlantısı eksik.", this);
            enabled = false;
            return;
        }
        playerBody = player.GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (biometricDoor == null || !biometricDoor.isActiveAndEnabled
            || biometricDoor.CurrentState != BiometricDoorState.Open
            || dialogueView == null || dialogueView.IsOpen || player == null
            || navigationMotor == null || !navigationMotor.isActiveAndEnabled)
        {
            ReleaseMotor();
            return;
        }
        if (!ownsMotor)
        {
            ownsMotor = true;
            readyFrame = Time.frameCount + 2;
            nextPathTime = 0f;
        }
        // Kapı carving güncellemesine fırsat ver; kapalı hâlin rotasına kilitlenme.
        if (Time.frameCount < readyFrame)
            return;
        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        if (offset.magnitude <= stopDistance
            || (currentState == KaelFollowState.WaitingNearPlayer
                && offset.magnitude < Mathf.Max(resumeDistance, stopDistance + 0.2f)))
        {
            if (currentState != KaelFollowState.WaitingNearPlayer)
                motor.Stop();
            ChangeState(KaelFollowState.WaitingNearPlayer);
            return;
        }

        Vector3 destination = player.position;
        destination.y = playerBody != null ? playerBody.bounds.min.y : transform.position.y;
        Vector3 targetChange = destination - lastDestination;
        targetChange.y = 0f;
        bool targetMoved = targetChange.sqrMagnitude >= targetMovementThreshold * targetMovementThreshold;
        motor.Tick();
        if (Time.time >= nextPathTime && (targetMoved
            || motor.Status == NavigationStatus.Idle || motor.Status == NavigationStatus.Arrived
            || motor.Status == NavigationStatus.Failed))
        {
            lastDestination = destination;
            if (!motor.TryMoveTo(destination))
            {
                nextPathTime = Time.time + retryInterval;
                ChangeState(KaelFollowState.WaitingForRoute);
                return;
            }
            nextPathTime = Time.time + repathInterval;
        }
        ChangeState(motor.Status == NavigationStatus.Moving
            ? KaelFollowState.Following : KaelFollowState.WaitingForRoute);
    }

    private void ChangeState(KaelFollowState next)
    {
        if (currentState == next)
            return;
        currentState = next;
        if (next != KaelFollowState.WaitingForPermission)
            Debug.Log(StatusMessage, this);
    }

    private void ReleaseMotor()
    {
        if (ownsMotor)
            motor?.Stop();
        ownsMotor = false;
        currentState = KaelFollowState.WaitingForPermission;
    }

    private void OnDisable() => ReleaseMotor();
}
