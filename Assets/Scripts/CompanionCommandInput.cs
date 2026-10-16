using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CompanionCommandInput : MonoBehaviour
{
    [SerializeField] private AX7Brain companion;
    [SerializeField, Min(0.1f)] private float commandRange = 3f;

    public AX7Brain Companion => companion;
    public bool IsInRange
    {
        get
        {
            if (companion == null)
                return false;
            Vector3 offset = companion.transform.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= commandRange * commandRange;
        }
    }

    public bool CanIssueCommand => companion != null
        && companion.isActiveAndEnabled
        && IsInRange
        && (companion.CurrentState == AX7State.Waiting || companion.CurrentState == AX7State.Failed);

    private void Update()
    {
        // Menü veya başka pencere açıkken oyun komutu gönderilmez.
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked)
            return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame && CanIssueCommand)
            companion.MoveToPanel();
    }
}
