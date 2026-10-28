using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Durumu gözlemler; kaçış kararı üretmez. Ekranı ve kontrol kilidini yönetir.
public sealed class EscapeEndingView : MonoBehaviour
{
    [SerializeField] private MonoBehaviour departureController;
    [SerializeField] private Behaviour[] gameplayControls;
    [SerializeField] private CompanionHUD companionHUD;

    private IShipDeparture departure;
    private GameObject canvasObject;
    private Image blackImage;
    private GameObject endingRoot;
    private bool subscribed;
    private bool captured;
    private bool[] previousEnabled;
    private bool previousHUDEnabled;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    private void Awake()
    {
        departure = departureController as IShipDeparture;
        if (departure == null || companionHUD == null || gameplayControls == null || gameplayControls.Length == 0)
        {
            Debug.LogError("Kaçış ekranının durum kaynağı, HUD veya kontrol bağlantıları eksik.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (departure == null)
            departure = departureController as IShipDeparture;
        if (departure != null && !subscribed)
        {
            departure.StateChanged += HandleStateChanged;
            subscribed = true;
        }
        if (canvasObject != null)
            canvasObject.SetActive(true);
    }

    private void Start()
    {
        CreateCanvas();
        Refresh();
    }

    private void Update() => Refresh();
    private void HandleStateChanged(ShipDepartureState state) => Refresh();

    private void Refresh()
    {
        if (departure == null || canvasObject == null)
            return;
        var state = departure.CurrentState;
        bool running = state == ShipDepartureState.Countdown || state == ShipDepartureState.Fading
            || state == ShipDepartureState.Completed;
        if (running)
            CaptureControls();
        else
            RestoreControls();

        bool blackVisible = state == ShipDepartureState.Fading || state == ShipDepartureState.Completed;
        blackImage.gameObject.SetActive(blackVisible);
        blackImage.color = new Color(0f, 0f, 0f, departure.FadeProgress);
        endingRoot.SetActive(state == ShipDepartureState.Completed);
        if (state == ShipDepartureState.Completed && companionHUD != null && companionHUD.enabled)
            companionHUD.enabled = false;
    }

    private void CaptureControls()
    {
        if (captured)
            return;
        captured = true;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        previousHUDEnabled = companionHUD != null && companionHUD.enabled;
        previousEnabled = new bool[gameplayControls.Length];
        for (int i = 0; i < gameplayControls.Length; i++)
        {
            var control = gameplayControls[i];
            if (control == null)
                continue;
            previousEnabled[i] = control.enabled;
            control.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreControls()
    {
        if (!captured)
            return;
        for (int i = 0; i < gameplayControls.Length; i++)
            if (gameplayControls[i] != null)
                gameplayControls[i].enabled = previousEnabled[i];
        if (companionHUD != null)
            companionHUD.enabled = previousHUDEnabled;
        bool restoreCursor = Application.isPlaying && Application.isFocused;
        Cursor.lockState = restoreCursor ? previousCursorLock : CursorLockMode.None;
        Cursor.visible = restoreCursor ? previousCursorVisible : true;
        captured = false;
    }

    private void CreateCanvas()
    {
        canvasObject = new GameObject("KacisSonuEkrani", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var blackObject = new GameObject("Kararma", typeof(RectTransform), typeof(Image));
        blackObject.transform.SetParent(canvasObject.transform, false);
        Stretch(blackObject.GetComponent<RectTransform>());
        blackImage = blackObject.GetComponent<Image>();
        blackImage.color = new Color(0f, 0f, 0f, 0f);
        blackImage.raycastTarget = true;
        blackObject.SetActive(false);

        endingRoot = new GameObject("BasariliKacis", typeof(RectTransform));
        endingRoot.transform.SetParent(canvasObject.transform, false);
        Stretch(endingRoot.GetComponent<RectTransform>());
        CreateText("Baslik", "BAŞARIYLA KAÇTIN", new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.76f), 64f);
        CreateText("Sonuc", "Acil ayrılma tamamlandı.\nKendi geminle güvenle uzaklaştın.",
            new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.56f), 34f);
        endingRoot.SetActive(false);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void CreateText(string objectName, string content, Vector2 min, Vector2 max, float fontSize)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(endingRoot.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = content;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize * 0.65f;
        label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.richText = false;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
    }

    private void OnDisable()
    {
        if (departure != null && subscribed)
            departure.StateChanged -= HandleStateChanged;
        subscribed = false;
        RestoreControls();
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    private void OnDestroy()
    {
        RestoreControls();
        if (canvasObject != null)
            Destroy(canvasObject);
    }
}
