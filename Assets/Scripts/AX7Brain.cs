using System;
using UnityEngine;

public enum AX7State
{
    Waiting,
    MovingToPanel,
    AtPanel,
    Failed,
    WaitingForObstacle
}

public sealed class AX7Brain : MonoBehaviour
{
    [SerializeField] private MonoBehaviour navigationMotor;
    [SerializeField] private Transform panelTarget;
    [SerializeField] private MonoBehaviour obstacleSensor;
    [SerializeField, Min(1f)] private float obstacleWaitTimeout = 20f;
    [SerializeField] private AX7State currentState = AX7State.Waiting;
    [SerializeField] private string failureReason = string.Empty;

    public AX7State CurrentState => currentState;
    public string FailureReason => failureReason;
    public event Action<AX7State> StateChanged;

    private INavigationMotor motor;
    private IObstacleSensor sensor;
    private float obstacleWaitStarted;

    private void Awake()
    {
        motor = navigationMotor as INavigationMotor;
        sensor = obstacleSensor as IObstacleSensor;
        if (motor == null || sensor == null || panelTarget == null)
        {
            SetFailure("Robotun hareket arayüzü, sensörü veya panel hedefi bağlanmamış.");
            enabled = false;
            return;
        }
        currentState = AX7State.Waiting;
        failureReason = string.Empty;
    }

    // Oyuncu komutu ve editör testi aynı görev girişini kullanır.
    [ContextMenu("Panele Git (Play Modunda)")]
    public void MoveToPanel()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("AX-7 hareketini denemek için önce Play'e bas.", this);
            return;
        }
        if (!isActiveAndEnabled || motor == null || (currentState == AX7State.MovingToPanel || currentState == AX7State.WaitingForObstacle))
            return;
        if (panelTarget == null)
        {
            SetFailure("Panel hedefi artık mevcut değil.");
            return;
        }
        failureReason = string.Empty;
        if (!motor.TryMoveTo(panelTarget.position))
        {
            SetFailure(motor.FailureReason);
            return;
        }
        ChangeState(AX7State.MovingToPanel);
        Debug.Log("AX-7: Panel rotasını izliyorum.", this);
    }

    private void Update()
    {
        if (currentState != AX7State.MovingToPanel && currentState != AX7State.WaitingForObstacle)
            return;
        motor.Tick();
        if (motor.Status == NavigationStatus.Failed)
        {
            SetFailure(motor.FailureReason);
            return;
        }

        if (motor.Status == NavigationStatus.Moving || motor.Status == NavigationStatus.Paused)
        {
            Vector3 direction = motor.SteeringTarget - transform.position;
            direction.y = 0f;
            bool blocked = sensor.Scan(direction, direction.magnitude);
            if (currentState == AX7State.WaitingForObstacle)
            {
                if (!blocked)
                {
                    if (!motor.Resume())
                        SetFailure(motor.FailureReason);
                    else
                    {
                        ChangeState(AX7State.MovingToPanel);
                        Debug.Log("AX-7: Engel kalktı, panel rotasına devam ediyorum.", this);
                    }
                }
                else if (Time.time - obstacleWaitStarted >= obstacleWaitTimeout)
                {
                    motor.Stop();
                    SetFailure("Önümdeki engel uzun süre kalkmadığı için görevi durdurdum.");
                }
                return;
            }
            if (blocked)
            {
                if (!motor.Pause())
                    SetFailure(motor.FailureReason);
                else
                {
                    obstacleWaitStarted = Time.time;
                    ChangeState(AX7State.WaitingForObstacle);
                    Debug.Log("AX-7: Raycast sensörüm engel algıladı; yolun açılmasını bekliyorum.", this);
                }
                return;
            }
        }
        switch (motor.Status)
        {
            case NavigationStatus.Arrived:
                ChangeState(AX7State.AtPanel);
                Debug.Log("AX-7: Panelin önüne ulaştım. Panel etkileşimi henüz eklenmedi.", this);
                break;
            case NavigationStatus.Failed:
                SetFailure(motor.FailureReason);
                break;
            case NavigationStatus.Idle:
                SetFailure("Robotun hareketi hedefe ulaşmadan durduruldu.");
                break;
        }
    }

    private void SetFailure(string reason)
    {
        failureReason = reason;
        ChangeState(AX7State.Failed);
        Debug.LogWarning("AX-7: " + reason, this);
    }

    private void ChangeState(AX7State next)
    {
        if (currentState == next)
            return;
        currentState = next;
        StateChanged?.Invoke(next);
    }

    private void OnDisable()
    {
        motor?.Stop();
        if (currentState == AX7State.MovingToPanel || currentState == AX7State.WaitingForObstacle)
            ChangeState(AX7State.Waiting);
    }
}
