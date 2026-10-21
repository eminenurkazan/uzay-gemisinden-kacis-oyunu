using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

// Sahnedeki EditorOnly köküne aittir; oyuncuya dağıtılan sürümde test girişi yoktur.
public sealed class EditorDoorTestButton : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private BiometricDoorLink doorLink;
    [SerializeField] private Transform playerView;
    [SerializeField] private AstronautController playerController;
    [SerializeField] private KaelInteraction kaelInteraction;
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;

    private Collider buttonCollider;
    private Material buttonMaterial;
    private TextMeshPro label;
    private bool aimed;
    private GUIStyle hintStyle;

    private void Start()
    {
        if (doorLink == null || playerView == null || playerController == null)
        {
            Debug.LogError("Geçici kapı test düğmesinin bağlantıları eksik.", this);
            enabled = false;
            return;
        }
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "GeciciTestDugmesiGorseli";
        body.transform.SetParent(transform, false);
        body.transform.localScale = new Vector3(0.55f, 0.35f, 0.18f);
        buttonCollider = body.GetComponent<Collider>();
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            buttonMaterial = new Material(shader);
            buttonMaterial.color = new Color(1f, 0.35f, 0.05f);
            body.GetComponent<Renderer>().sharedMaterial = buttonMaterial;
        }

        var textObject = new GameObject("TestDugmesiEtiketi", typeof(TextMeshPro));
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0f, 0.45f, -0.12f);
        label = textObject.GetComponent<TextMeshPro>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 2.5f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.rectTransform.sizeDelta = new Vector2(2.4f, 0.65f);
        label.text = "TEST: KAPIYI AÇ\nSol tık";
    }

    private void Update()
    {
        bool opened = doorLink != null && doorLink.CurrentState == BiometricDoorState.Open;
        bool opening = doorLink != null && doorLink.CurrentState == BiometricDoorState.Opening;
        if (buttonMaterial != null)
            buttonMaterial.color = opened ? Color.green : opening ? Color.yellow : new Color(1f, 0.35f, 0.05f);
        if (label != null)
            label.text = opened ? "TEST KAPISI AÇILDI" : opening ? "TEST KAPISI AÇILIYOR" : "TEST: KAPIYI AÇ\nSol tık";

        aimed = false;
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked
            || playerController == null || !playerController.isActiveAndEnabled
            || playerView == null || doorLink == null
            || (kaelInteraction != null && kaelInteraction.IsDialogueOpen))
            return;
        if (!Physics.Raycast(playerView.position, playerView.forward, out var hit,
                interactionRange, ~0, QueryTriggerInteraction.Ignore) || hit.collider != buttonCollider)
            return;
        aimed = true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            doorLink.OpenForEditorTest();
    }

    private void OnGUI()
    {
        if (!aimed)
            return;
        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.alignment = TextAnchor.MiddleCenter;
            hintStyle.fontSize = 20;
            hintStyle.normal.textColor = Color.white;
        }
        GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f + 35f, 360f, 42f), GUIContent.none);
        GUI.Label(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f + 35f, 360f, 42f),
            "Sol tık — Geçici test kapısını aç", hintStyle);
    }

    private void OnDisable() => aimed = false;

    private void OnDestroy()
    {
        if (buttonMaterial != null)
            Destroy(buttonMaterial);
    }
#endif
}
