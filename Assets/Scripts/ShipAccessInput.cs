using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ShipAccessInput : MonoBehaviour
{
    [SerializeField] private MonoBehaviour shipAccess;
    [SerializeField] private AstronautController playerController;
    [SerializeField] private KaelInteraction kaelInteraction;

    private IShipAccess access;

    public ShipAccessState CurrentState => access != null ? access.CurrentState : ShipAccessState.Locked;
    public string StatusMessage => access != null ? access.StatusMessage : string.Empty;
    public bool HasObjective => isActiveAndEnabled && shipAccess != null && shipAccess.isActiveAndEnabled
        && access != null && access.CurrentState != ShipAccessState.Locked;
    public bool CanShowPrompt => isActiveAndEnabled && shipAccess != null && shipAccess.isActiveAndEnabled
        && access != null && playerController != null && playerController.isActiveAndEnabled
        && (kaelInteraction == null || !kaelInteraction.IsDialogueOpen)
        && access.IsInInteractionArea(transform);
    public string Prompt => CanShowPrompt && access.CanOpenHatch(transform)
        ? "E — Gemi kapağını aç" : StatusMessage;

    private void Awake()
    {
        access = shipAccess as IShipAccess;
        if (access == null || playerController == null)
        {
            Debug.LogError("Gemi geçiş girdisinin erişim veya oyuncu bağlantısı eksik.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked || !CanShowPrompt)
            return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            access.TryOpenHatch(transform);
    }
}
