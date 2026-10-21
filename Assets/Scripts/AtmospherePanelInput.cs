using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AtmospherePanelInput : MonoBehaviour
{
    [SerializeField] private MonoBehaviour atmosphereControl;
    [SerializeField] private KaelInteraction kaelInteraction;
    [SerializeField] private AstronautController playerController;

    private IAtmosphereControl panel;

    public AtmosphereControlState CurrentState => panel != null
        ? panel.CurrentState : AtmosphereControlState.Locked;
    public string StatusMessage => panel != null ? panel.StatusMessage : string.Empty;
    public bool CanShowPrompt => isActiveAndEnabled && atmosphereControl != null
        && atmosphereControl.isActiveAndEnabled && panel != null
        && playerController != null && playerController.isActiveAndEnabled
        && (kaelInteraction == null || !kaelInteraction.IsDialogueOpen)
        && panel.IsInInteractionArea(transform);
    public string Prompt => CanShowPrompt && panel.CanActivate(transform)
        ? "E — Atmosfer kontrolünü başlat" : StatusMessage;

    private void Awake()
    {
        panel = atmosphereControl as IAtmosphereControl;
        if (panel == null || playerController == null)
        {
            Debug.LogError("Atmosfer panelinin etkileşim veya oyuncu kontrolü bağlantısı eksik.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked || !CanShowPrompt)
            return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            panel.TryActivate(transform);
    }
}
