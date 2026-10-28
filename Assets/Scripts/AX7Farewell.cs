using UnityEngine;

public enum AX7FarewellState
{
    WaitingForEscape,
    MovingToWaitingPoint,
    WaitingForObstacle,
    WaitingForRoute,
    WaitingAtExit
}

// Kael etkisizleşince takipten devralır; hareket ve gözlem mevcut arayüzlerden gelir.
public sealed class AX7Farewell : MonoBehaviour
{
    [SerializeField] private AX7Brain taskBrain;
    [SerializeField] private MonoBehaviour atmosphereTarget;
    [SerializeField] private AX7FollowPlayer followPlayer;
    [SerializeField] private MonoBehaviour navigationMotor;
    [SerializeField] private MonoBehaviour obstacleSensor;
    [SerializeField] private Transform waitingPoint;
    [SerializeField, Min(0.2f)] private float retryInterval = 1f;
    [SerializeField] private AX7FarewellState currentState;
    [SerializeField] private string failureReason = string.Empty;

    public AX7FarewellState CurrentState => currentState;
    public bool HasStarted { get; private set; }
    public bool IsWaitingAtExit => currentState == AX7FarewellState.WaitingAtExit;
    public string StatusMessage => currentState switch
    {
        AX7FarewellState.MovingToWaitingPoint => "AX-7 geminin dışında bekleyeceği yere gidiyor.",
        AX7FarewellState.WaitingForObstacle => "AX-7'nin yolunda engel var; yol açılınca devam edecek.",
        AX7FarewellState.WaitingForRoute => "AX-7 bekleme noktasına rota bulamıyor; yeniden deniyor.",
        AX7FarewellState.WaitingAtExit => "AX-7: Ben burada kalacağım. Sen gemine geç.",
        _ => string.Empty
    };

    private IAtmosphereTarget target;
    private INavigationMotor motor;
    private IObstacleSensor sensor;
    private float nextRouteAttempt;
    private float nextObstacleRepath;
    private int navigationReadyFrame;
    [SerializeField] private string blockingObstacleName = string.Empty;

    private void Awake()
    {
        target = atmosphereTarget as IAtmosphereTarget;
        motor = navigationMotor as INavigationMotor;
        sensor = obstacleSensor as IObstacleSensor;
        currentState = AX7FarewellState.WaitingForEscape;
        HasStarted = false;
        if (taskBrain == null || target == null || followPlayer == null || motor == null
            || sensor == null || waitingPoint == null)
        {
            Debug.LogError("AX-7 vedalaşmasının görev, Kael, takip, hareket, sensör veya bekleme noktası bağlantısı eksik.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!HasStarted)
        {
            if (!taskBrain.isActiveAndEnabled || taskBrain.CurrentState != AX7State.Succeeded
                || atmosphereTarget == null || !atmosphereTarget.isActiveAndEnabled
                || !target.IsIncapacitated)
                return;

            // Önceki motor sahibini devre dışı bırak; iki karar bileşeni aynı anda rota yazmaz.
            followPlayer.enabled = false;
            motor.Stop();
            HasStarted = true;
            nextRouteAttempt = 0f;
            // Düşen gövdenin carving güncellemesi bir sonraki NavMesh karesinde görünür.
            navigationReadyFrame = Time.frameCount + 2;
        }
        if (IsWaitingAtExit || Time.frameCount < navigationReadyFrame)
            return;
        if (waitingPoint == null || navigationMotor == null || !navigationMotor.isActiveAndEnabled
            || obstacleSensor == null || !obstacleSensor.isActiveAndEnabled)
        {
            WaitForRoute("Bekleme noktası, hareket veya sensör bileşeni kullanılamıyor.");
            return;
        }

        motor.Tick();
        if (motor.Status == NavigationStatus.Arrived)
        {
            motor.Stop();
            failureReason = string.Empty;
            ChangeState(AX7FarewellState.WaitingAtExit);
            return;
        }
        if (motor.Status != NavigationStatus.Moving && motor.Status != NavigationStatus.Paused)
        {
            if (Time.time < nextRouteAttempt)
                return;
            if (!motor.TryMoveTo(waitingPoint.position))
            {
                WaitForRoute(motor.FailureReason);
                return;
            }
            failureReason = string.Empty;
        }

        bool blocked = ScanObstacle();
        if (blocked && Time.time >= nextObstacleRepath)
        {
            nextObstacleRepath = Time.time + retryInterval;
            // Son carving verisiyle gerçek bir alternatif rota iste. Hareketi elle kaydırma.
            if (!motor.TryMoveTo(waitingPoint.position))
            {
                WaitForRoute(motor.FailureReason);
                return;
            }
            // Yeni rota hâlâ kapalıysa aynı karede yeniden durdur; engelin içine yürüme.
            blocked = ScanObstacle();
        }
        if (blocked)
        {
            if (motor.Status == NavigationStatus.Moving && !motor.Pause())
            {
                WaitForRoute(motor.FailureReason);
                return;
            }
            if (blockingObstacleName != sensor.ObstacleName)
            {
                blockingObstacleName = sensor.ObstacleName;
                Debug.Log("AX-7 vedalaşma sensörü: algılanan engel = " + blockingObstacleName, this);
            }
            ChangeState(AX7FarewellState.WaitingForObstacle);
            return;
        }
        blockingObstacleName = string.Empty;
        if (motor.Status == NavigationStatus.Paused && !motor.Resume())
        {
            WaitForRoute(motor.FailureReason);
            return;
        }
        ChangeState(AX7FarewellState.MovingToWaitingPoint);
    }

    private bool ScanObstacle()
    {
        Vector3 direction = motor.SteeringTarget - transform.position;
        direction.y = 0f;
        return sensor.Scan(direction, direction.magnitude);
    }

    private void WaitForRoute(string reason)
    {
        bool changed = currentState != AX7FarewellState.WaitingForRoute || failureReason != reason;
        motor.Stop();
        failureReason = reason;
        nextRouteAttempt = Time.time + retryInterval;
        currentState = AX7FarewellState.WaitingForRoute;
        if (changed)
            Debug.LogWarning("AX-7: " + StatusMessage + " " + reason, this);
    }

    private void ChangeState(AX7FarewellState next)
    {
        if (currentState == next)
            return;
        currentState = next;
        Debug.Log(StatusMessage, this);
    }

    private void OnDisable()
    {
        if (HasStarted)
            motor?.Stop();
    }
}
