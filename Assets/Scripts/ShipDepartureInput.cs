using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ShipDepartureInput : MonoBehaviour
{
    [SerializeField] private MonoBehaviour departureController;
    [SerializeField] private AstronautController playerController;
    [SerializeField] private KaelInteraction kaelInteraction;

    private IShipDeparture departure;
    private bool SourceAvailable => isActiveAndEnabled && departureController != null
        && departureController.isActiveAndEnabled && departure != null;

    public bool HasObjective => SourceAvailable && departure.CurrentState != ShipDepartureState.Locked;
    public bool IsRunning => SourceAvailable && (departure.CurrentState == ShipDepartureState.Countdown
        || departure.CurrentState == ShipDepartureState.Fading);
    public string StatusMessage => departure != null ? departure.StatusMessage : string.Empty;
    public bool CanShowPrompt => SourceAvailable && playerController != null && playerController.isActiveAndEnabled
        && (kaelInteraction == null || !kaelInteraction.IsDialogueOpen)
        && departure.IsInInteractionArea(transform);
    public string Prompt => CanShowPrompt && departure.CanStart(transform)
        ? "E — Acil ayrılmayı başlat" : StatusMessage;

    private void Awake()
    {
        departure = departureController as IShipDeparture;
        if (departure == null || playerController == null)
        {
            Debug.LogError("Acil ayrılma girdisinin durum kaynağı veya oyuncu bağlantısı eksik.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked || !CanShowPrompt)
            return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            departure.TryStart(transform);
    }
}
