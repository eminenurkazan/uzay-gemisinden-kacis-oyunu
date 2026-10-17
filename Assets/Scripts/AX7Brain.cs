using System;
using UnityEngine;

public enum AX7State
{
    Waiting,
    MovingToPanel,
    AtPanel,
    Failed
}

public sealed class AX7Brain : MonoBehaviour
{
    [SerializeField] private MonoBehaviour navigationMotor;
    [SerializeField] private Transform panelTarget;
    [SerializeField] private AX7State currentState = AX7State.Waiting;
    [SerializeField] private string failureReason = string.Empty;

    public AX7State CurrentState => currentState;
    public string FailureReason => failureReason;
    public event Action<AX7State> StateChanged;

    private INavigationMotor motor;

    private void Awake()
    {
        motor = navigationMotor as INavigationMotor;
        if (motor == null || panelTarget == null)
        {
            SetFailure("Robotun hareket arayüzü veya panel hedefi bağlanmamış.");
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
        if (!isActiveAndEnabled || motor == null || currentState == AX7State.MovingToPanel)
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
        if (currentState != AX7State.MovingToPanel)
            return;
        motor.Tick();
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
        if (currentState == AX7State.MovingToPanel)
            ChangeState(AX7State.Waiting);
    }
}
