using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Yalnızca yönlendirmeleri gösterir; tuş okumaz ve robota emir vermez.
public sealed class CompanionHUD : MonoBehaviour
{
    [SerializeField] private CompanionCommandInput commandInput;
    [SerializeField] private KaelInteraction kaelInteraction;
    [SerializeField] private AX7FollowPlayer followPlayer;

    private GameObject canvasObject;
    private TMP_Text objectiveText;
    private TMP_Text promptText;

    private void Start()
    {
        if (commandInput == null || commandInput.Companion == null)
        {
            Debug.LogError("AX-7 yönlendirmesi için komut bileşeni bağlanmamış.", this);
            enabled = false;
            return;
        }
        CreateCanvas();
        RefreshMessages();
    }

    private void Update()
    {
        RefreshMessages();
    }

    private void RefreshMessages()
    {
        if (objectiveText == null || commandInput == null || commandInput.Companion == null)
            return;

        var companion = commandInput.Companion;
        string objective;
        switch (companion.CurrentState)
        {
            case AX7State.MovingToPanel:
                objective = "AX-7 kontrol paneline gidiyor…\nBakım geçidini kullanarak kapının arkasına ulaşacak.";
                break;
            case AX7State.WaitingForObstacle:
                objective = "AX-7 önünde engel algıladı; yolun açılmasını bekliyor.\nEngel kalkınca rotasına devam edecek.";
                break;
            case AX7State.AtPanel:
                objective = "AX-7 kontrol paneline ulaştı.";
                break;
            case AX7State.InteractingWithPanel:
                objective = "AX-7 kontrol panelini kullanıyor…\nKapının açılmasını bekle.";
                break;
            case AX7State.Succeeded:
                objective = "İlk kapı açıldı. Kael’in güvenlik alanına ilerle.\nKael’e yaklaşınca E ile konuşabilirsin.";
                if (kaelInteraction != null && kaelInteraction.BiometricDoor != null)
                {
                    var gate = kaelInteraction.BiometricDoor;
                    if (gate.CurrentState == BiometricDoorState.Open)
                        objective = "Biyometrik kapı açıldı.\nHava kilidine ilerleyebilirsin.";
                    else if (gate.CurrentState == BiometricDoorState.Opening)
                        objective = "Kael geçiş izni verdi.\nBiyometrik kapı açılıyor.";
                    else if (gate.CurrentState == BiometricDoorState.Failed)
                        objective = gate.LastMessage;
                }
                if (followPlayer != null && followPlayer.isActiveAndEnabled
                    && !string.IsNullOrEmpty(followPlayer.StatusMessage))
                    objective += "\n" + followPlayer.StatusMessage;
                break;
            case AX7State.Failed:
                objective = "AX-7 görevi tamamlayamadı.\n" + companion.FailureReason;
                break;
            default:
                objective = "Kapı kilitli. AX-7 bakım geçidinden kontrol paneline ulaşabilir.\nKomut vermek için AX-7’ye yaklaş.";
                break;
        }
        SetTextIfChanged(objectiveText, objective);

        string prompt;
        if (Cursor.lockState != CursorLockMode.Locked)
            prompt = "Devam etmek için oyun alanına tıkla.";
        else if (kaelInteraction != null && kaelInteraction.CanInteract)
            prompt = "E — Kael ile konuş";
        else if (commandInput.CanIssueCommand)
            prompt = companion.CurrentState == AX7State.Failed
                ? "E — AX-7’ye yeniden panele gitme komutu ver"
                : "E — AX-7’yi kontrol paneline gönder";
        else if (companion.CurrentState == AX7State.Waiting || companion.CurrentState == AX7State.Failed)
            prompt = "Komut vermek için AX-7’ye yaklaş.";
        else
            prompt = "WASD: Yürü   |   Fare: Etrafa bak   |   Esc: Fareyi bırak";
        SetTextIfChanged(promptText, prompt);
    }

    private void CreateCanvas()
    {
        // Canvas yalnızca bu sahnenin oyuncusuna aittir; sahne kapanınca yok olur.
        canvasObject = new GameObject("AX7Yonlendirmeleri", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        objectiveText = CreatePanel("GorevBilgisi", true, 150f, 28f);
        promptText = CreatePanel("KomutIpucu", false, 82f, 30f);
    }

    private TMP_Text CreatePanel(string name, bool atTop, float height, float fontSize)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        float anchorY = atTop ? 1f : 0f;
        rect.anchorMin = new Vector2(0.05f, anchorY);
        rect.anchorMax = new Vector2(0.95f, anchorY);
        rect.pivot = new Vector2(0.5f, anchorY);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = new Vector2(0f, atTop ? -25f : 25f);
        var background = panel.GetComponent<Image>();
        background.color = new Color(0.025f, 0.04f, 0.065f, 0.88f);
        background.raycastTarget = false;

        var textObject = new GameObject("Metin", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 10f);
        textRect.offsetMax = new Vector2(-24f, -10f);
        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.richText = false;
        label.raycastTarget = false;
        return label;
    }

    private static void SetTextIfChanged(TMP_Text label, string message)
    {
        if (label.text != message)
            label.text = message;
    }

    private void OnEnable()
    {
        if (canvasObject != null)
            canvasObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
            Destroy(canvasObject);
    }
}
