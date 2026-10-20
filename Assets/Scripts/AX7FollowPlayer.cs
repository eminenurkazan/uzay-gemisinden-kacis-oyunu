using UnityEngine;

public enum AX7FollowState
{
    WaitingForTask,
    Following,
    WaitingNearPlayer,
    WaitingForObstacle,
    WaitingForRoute
}

// Panel görevi tamamlandıktan sonra hareket arayüzünün kontrolünü devralır.
public sealed class AX7FollowPlayer : MonoBehaviour
{
    [SerializeField] private AX7Brain taskBrain;
    [SerializeField] private MonoBehaviour navigationMotor;
    [SerializeField] private MonoBehaviour obstacleSensor;
    [SerializeField] private Transform player;
    [SerializeField, Min(0.5f)] private float stopDistance = 1.6f;
    [SerializeField, Min(0.6f)] private float resumeDistance = 2.3f;
    [SerializeField, Min(0.1f)] private float repathInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float targetMovementThreshold = 0.6f;
    [SerializeField, Min(0.2f)] private float retryInterval = 1f;
    [SerializeField] private AX7FollowState currentState;

    public AX7FollowState CurrentState => currentState;
    public string StatusMessage => currentState switch
    {
        AX7FollowState.Following => "AX-7 seni takip ediyor.",
        AX7FollowState.WaitingNearPlayer => "AX-7 yakınında bekliyor; uzaklaşınca seni takip edecek.",
        AX7FollowState.WaitingForObstacle => "AX-7 takip yolunda engel algıladı; yolun açılmasını bekliyor.",
        AX7FollowState.WaitingForRoute => "AX-7 sana ulaşan rotayı bekliyor; yeniden deneyecek.",
        _ => string.Empty
    };

    private INavigationMotor motor;
    private IObstacleSensor sensor;
    private CharacterController playerBody;
    private Vector3 lastDestination;
    private float nextPathTime;
    private bool ownsMotor;
    private int followReadyFrame;

    private void Awake()
    {
        motor = navigationMotor as INavigationMotor;
        sensor = obstacleSensor as IObstacleSensor;
        currentState = AX7FollowState.WaitingForTask;
        if (taskBrain == null || player == null || motor == null || sensor == null)
        {
            Debug.LogError("AX-7 takip görevi, oyuncu, hareket veya sensör bağlantısı eksik.", this);
            enabled = false;
            return;
        }
        playerBody = player.GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!taskBrain.isActiveAndEnabled || taskBrain.CurrentState != AX7State.Succeeded
            || player == null || navigationMotor == null || !navigationMotor.isActiveAndEnabled
            || obstacleSensor == null || !obstacleSensor.isActiveAndEnabled)
        {
            ReleaseMotor();
            return;
        }
        if (!ownsMotor)
        {
            ownsMotor = true;
            // Açılan kapının carving değişikliği işlenmeden eski rotayı seçme.
            followReadyFrame = Time.frameCount + 2;
        }
        if (Time.frameCount < followReadyFrame)
            return;
        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        float resumeAt = Mathf.Max(resumeDistance, stopDistance + 0.2f);
        if (distance <= stopDistance
            || (currentState == AX7FollowState.WaitingNearPlayer && distance < resumeAt))
        {
            if (currentState != AX7FollowState.WaitingNearPlayer)
                motor.Stop();
            ChangeState(AX7FollowState.WaitingNearPlayer);
            return;
        }

        Vector3 destination = player.position;
        // Oyuncu kökü kapsülün ortasındadır; hedef zemindeki ayak konumudur.
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
                ChangeState(AX7FollowState.WaitingForRoute);
                return;
            }
            nextPathTime = Time.time + repathInterval;
        }

        if (motor.Status == NavigationStatus.Failed || motor.Status == NavigationStatus.Idle
            || motor.Status == NavigationStatus.Arrived)
        {
            ChangeState(AX7FollowState.WaitingForRoute);
            return;
        }
        Vector3 direction = motor.SteeringTarget - transform.position;
        direction.y = 0f;
        if (sensor.Scan(direction, direction.magnitude))
        {
            if (motor.Status == NavigationStatus.Moving && !motor.Pause())
            {
                nextPathTime = Time.time + retryInterval;
                ChangeState(AX7FollowState.WaitingForRoute);
                return;
            }
            ChangeState(AX7FollowState.WaitingForObstacle);
            return;
        }
        if (motor.Status == NavigationStatus.Paused && !motor.Resume())
        {
            nextPathTime = Time.time + retryInterval;
            ChangeState(AX7FollowState.WaitingForRoute);
            return;
        }
        ChangeState(AX7FollowState.Following);
    }

    private void ChangeState(AX7FollowState next)
    {
        if (currentState == next)
            return;
        currentState = next;
        if (next != AX7FollowState.WaitingForTask)
            Debug.Log("AX-7: " + StatusMessage, this);
    }

    private void ReleaseMotor()
    {
        // Panel görevi sürerken motoru durdurmaz; yalnızca kendi takibini bırakır.
        if (ownsMotor)
            motor?.Stop();
        ownsMotor = false;
        currentState = AX7FollowState.WaitingForTask;
    }

    private void OnDisable() => ReleaseMotor();
}
