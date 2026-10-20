using UnityEngine;

public enum BiometricDoorState
{
    Locked,
    Opening,
    Open,
    Failed
}

public sealed class BiometricDoorLink : MonoBehaviour
{
    [SerializeField] private MonoBehaviour decisionSource;
    [SerializeField] private MonoBehaviour doorController;
    [SerializeField] private AX7Brain physicalDoorTask;
    [SerializeField, Min(1f)] private float openingTimeout = 8f;
    [SerializeField] private BiometricDoorState currentState = BiometricDoorState.Locked;
    [SerializeField] private string lastMessage = "Kael'in geçiş izni bekleniyor.";

    public BiometricDoorState CurrentState => currentState;
    public string LastMessage => lastMessage;

    private IKaelDecisionSource boundSource;
    private IOpenableDoor door;
    private float openingStarted;

    private void OnEnable()
    {
        boundSource = decisionSource as IKaelDecisionSource;
        door = doorController as IOpenableDoor;
        if (decisionSource == null || boundSource == null || doorController == null
            || door == null || physicalDoorTask == null)
        {
            Fail("Biyometrik kapının karar kaynağı, kapı veya görev bağlantısı eksik.");
            boundSource = null;
            return;
        }
        boundSource.DecisionValidated += HandleDecision;
    }

    private void OnDisable()
    {
        if (boundSource != null)
            boundSource.DecisionValidated -= HandleDecision;
        boundSource = null;
    }

    private void HandleDecision(KaelDecision decision)
    {
        if (!isActiveAndEnabled || decisionSource == null || !decisionSource.isActiveAndEnabled)
            return;
        // NONE açık kapıyı kapatmaz; yeni açılma isteği oluşturmaz.
        if (decision != KaelDecision.OpenDoor)
        {
            if (currentState == BiometricDoorState.Locked)
            {
                lastMessage = "Kael henüz geçiş izni vermedi; biyometrik kapı kapalı.";
                Debug.Log(lastMessage, this);
            }
            return;
        }
        if (currentState == BiometricDoorState.Opening || currentState == BiometricDoorState.Open)
            return;
        if (physicalDoorTask == null || physicalDoorTask.CurrentState != AX7State.Succeeded)
        {
            lastMessage = "Geçiş izni uygulanmadı: önce AX-7 fiziksel kapı görevini tamamlamalı.";
            Debug.LogWarning(lastMessage, this);
            return;
        }
        if (doorController == null || door == null || !door.IsAvailable)
        {
            Fail("Biyometrik kapı şu anda kullanılamıyor.");
            return;
        }
        if (!door.TryOpen(out string reason))
        {
            Fail("Biyometrik kapı açılamadı: " + reason);
            return;
        }

        openingStarted = Time.time;
        currentState = door.IsOpen ? BiometricDoorState.Open : BiometricDoorState.Opening;
        lastMessage = door.IsOpen ? "Biyometrik kapı açık." : "Kael'in geçiş izni doğrulandı; biyometrik kapı açılıyor.";
        Debug.Log(lastMessage, this);
    }

    private void Update()
    {
        if (currentState != BiometricDoorState.Opening)
            return;
        if (doorController == null || door == null || !door.IsAvailable)
        {
            Fail("Biyometrik kapı açılırken devre dışı kaldı.");
            return;
        }
        if (door.IsOpen)
        {
            currentState = BiometricDoorState.Open;
            lastMessage = "Biyometrik kapı tamamen açıldı; hava kilidine geçilebilir.";
            Debug.Log(lastMessage, this);
        }
        else if (Time.time - openingStarted >= openingTimeout)
            Fail("Biyometrik kapı ayrılan sürede tamamen açılamadı.");
    }

    private void Fail(string message)
    {
        currentState = BiometricDoorState.Failed;
        lastMessage = message;
        Debug.LogWarning(message, this);
    }
}
