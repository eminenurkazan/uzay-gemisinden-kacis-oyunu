using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KaelInteraction : MonoBehaviour
{
    [SerializeField] private Transform kael;
    [SerializeField] private AX7Brain physicalDoorTask;
    [SerializeField] private KaelDialogueView dialogueView;
    [SerializeField] private BiometricDoorLink biometricDoor;
    [SerializeField] private AstronautController playerController;
    [SerializeField] private CompanionCommandInput companionInput;
    [SerializeField] private CompanionHUD companionHUD;
    [SerializeField] private AtmospherePanelInput atmospherePanel;
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;

    public bool IsDialogueOpen => dialogueView != null && dialogueView.IsOpen;
    public BiometricDoorLink BiometricDoor => biometricDoor;
    public bool CanInteract
    {
        get
        {
            if (!isActiveAndEnabled || restoring || kael == null || dialogueView == null
                || physicalDoorTask == null || physicalDoorTask.CurrentState != AX7State.Succeeded)
                return false;
            // Aynı E basışında hava kilidi paneli ve konuşma birlikte açılmaz.
            if (atmospherePanel != null && atmospherePanel.CanShowPrompt)
                return false;
            Vector3 offset = kael.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= interactionRange * interactionRange;
        }
    }

    private bool restoring;
    private bool controlsCaptured;
    private bool previousPlayerEnabled;
    private bool previousCommandEnabled;
    private bool previousHUDEnabled;

    private void OnEnable()
    {
        if (dialogueView != null)
            dialogueView.CloseRequested += CloseDialogue;
    }

    private void Update()
    {
        if (!Application.isFocused || restoring)
            return;
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        if (IsDialogueOpen)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
                CloseDialogue();
            return;
        }
        if (Cursor.lockState == CursorLockMode.Locked && keyboard.eKey.wasPressedThisFrame && CanInteract)
            OpenDialogue();
    }

    private void OpenDialogue()
    {
        if (playerController == null || dialogueView == null)
            return;
        previousPlayerEnabled = playerController.enabled;
        previousCommandEnabled = companionInput != null && companionInput.enabled;
        previousHUDEnabled = companionHUD != null && companionHUD.enabled;
        controlsCaptured = true;
        playerController.enabled = false;
        if (companionInput != null)
            companionInput.enabled = false;
        if (companionHUD != null)
            companionHUD.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (!dialogueView.Show())
        {
            RestoreControls(true);
            return;
        }
    }

    private void CloseDialogue()
    {
        if (!IsDialogueOpen || restoring)
            return;
        dialogueView.Hide();
        restoring = true;
        StartCoroutine(RestoreNextFrame());
    }

    private IEnumerator RestoreNextFrame()
    {
        // Aynı Esc basışının yeniden etkinleşen oyuncu kontrolünde işlenmesini önler.
        yield return null;
        RestoreControls(true);
        restoring = false;
    }

    private void RestoreControls(bool lockCursor)
    {
        if (!controlsCaptured)
            return;
        if (playerController != null)
            playerController.enabled = previousPlayerEnabled;
        if (companionInput != null)
            companionInput.enabled = previousCommandEnabled;
        if (companionHUD != null)
            companionHUD.enabled = previousHUDEnabled;
        controlsCaptured = false;
        Cursor.lockState = lockCursor && Application.isFocused ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        if (dialogueView != null)
        {
            dialogueView.CloseRequested -= CloseDialogue;
            dialogueView.Hide();
        }
        StopAllCoroutines();
        restoring = false;
        RestoreControls(false);
    }
}
