using System;
using UnityEngine;

public sealed class AtmosphereControlPanel : MonoBehaviour, IAtmosphereControl
{
    [SerializeField] private BiometricDoorLink biometricDoor;
    [SerializeField] private MonoBehaviour doorController;
    [SerializeField] private Transform authorizedPlayer;
    [SerializeField] private Transform airlockOrigin;
    [SerializeField] private Vector3 interactionAreaCenter = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 interactionAreaSize = new Vector3(5.5f, 3f, 4.5f);
    [SerializeField, Min(0.1f)] private float interactionRange = 1.8f;
    [SerializeField, Min(0.1f)] private float activationDuration = 3f;
    [SerializeField] private AtmosphereControlState currentState;
    [SerializeField] private string lastMessage = string.Empty;

    public AtmosphereControlState CurrentState => currentState;
    public string StatusMessage => currentState == AtmosphereControlState.Processing
        ? "Atmosfer kontrolü sürüyor… %" + Mathf.FloorToInt(100f * Mathf.Clamp01(elapsed / activationDuration))
        : lastMessage;
    public event Action AtmosphereControlCompleted;

    private IOpenableDoor gate;
    private float elapsed;
    private bool completionRaised;

    private bool AccessGranted => biometricDoor != null && biometricDoor.isActiveAndEnabled
        && biometricDoor.CurrentState == BiometricDoorState.Open
        && doorController != null && doorController.isActiveAndEnabled
        && gate != null && gate.IsAvailable && gate.IsOpen;

    private void Awake()
    {
        gate = doorController as IOpenableDoor;
        currentState = AtmosphereControlState.Locked;
        elapsed = 0f;
        completionRaised = false;
        lastMessage = "Önce biyometrik kapının açılması gerekiyor.";
        if (biometricDoor == null || gate == null || authorizedPlayer == null || airlockOrigin == null)
        {
            Debug.LogError("Atmosfer panelinin kapı, oyuncu veya hava kilidi bağlantısı eksik.", this);
            enabled = false;
        }
    }

    public bool IsInInteractionArea(Transform actor)
    {
        if (!isActiveAndEnabled || actor == null || actor != authorizedPlayer
            || !actor.gameObject.activeInHierarchy || airlockOrigin == null)
            return false;
        var area = new Bounds(interactionAreaCenter, interactionAreaSize);
        if (!area.Contains(airlockOrigin.InverseTransformPoint(actor.position)))
            return false;
        Vector3 offset = actor.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= interactionRange * interactionRange;
    }

    public bool CanActivate(Transform actor) => IsInInteractionArea(actor) && AccessGranted
        && !completionRaised && currentState != AtmosphereControlState.Processing;

    public bool TryActivate(Transform actor)
    {
        if (!CanActivate(actor))
            return false;
        elapsed = 0f;
        currentState = AtmosphereControlState.Processing;
        lastMessage = "Atmosfer kontrolü başlatıldı.";
        Debug.Log(lastMessage, this);
        return true;
    }

    private void Update()
    {
        if (completionRaised)
            return;
        if (currentState == AtmosphereControlState.Processing)
        {
            if (!AccessGranted || !IsInInteractionArea(authorizedPlayer))
            {
                CancelOperation();
                return;
            }
            elapsed += Time.deltaTime;
            if (elapsed >= activationDuration)
            {
                // Olay dinleyicileri hata verse veya yeniden etkinleştirse bile ikinci kez yayınlanmaz.
                completionRaised = true;
                currentState = AtmosphereControlState.Completed;
                lastMessage = "Atmosfer kontrolü tamamlandı.";
                Debug.Log("Atmosfer kontrolü tamamlandı; Kael'i etkisizleştirme olayı bir kez yayımlandı.", this);
                AtmosphereControlCompleted?.Invoke();
            }
            return;
        }
        var next = AccessGranted ? AtmosphereControlState.Ready : AtmosphereControlState.Locked;
        if (next == currentState)
            return;
        currentState = next;
        lastMessage = next == AtmosphereControlState.Ready
            ? "Atmosfer kontrol paneli hazır."
            : "Önce biyometrik kapının açılması gerekiyor.";
    }

    private void CancelOperation()
    {
        elapsed = 0f;
        currentState = AccessGranted ? AtmosphereControlState.Ready : AtmosphereControlState.Locked;
        lastMessage = "Panel işlemi iptal edildi; başlatmak için tekrar yaklaş.";
        Debug.Log(lastMessage, this);
    }

    private void OnDisable()
    {
        if (currentState == AtmosphereControlState.Processing)
            CancelOperation();
    }
}
