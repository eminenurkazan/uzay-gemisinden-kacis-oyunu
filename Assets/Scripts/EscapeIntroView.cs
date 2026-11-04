using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Presents the opening story; never starts a robot task or changes an NPC decision.
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class EscapeIntroView : MonoBehaviour
{
    [SerializeField] private Behaviour[] gameplayControls;
    [SerializeField] private CompanionHUD companionHUD;
    [SerializeField] private Texture2D backgroundTexture;
    [SerializeField, TextArea(2, 4)] private string[] sentences;
    [SerializeField, Min(0.05f)] private float sentenceFadeIn = 0.65f;
    [SerializeField, Min(0.05f)] private float sentenceFadeOut = 0.4f;
    [SerializeField, Min(0.05f)] private float gameRevealDuration = 0.9f;

    private GameObject canvasObject;
    private CanvasGroup screenGroup;
    private CanvasGroup sentenceGroup;
    private RectTransform sentenceRect;
    private TMP_Text storyText;
    private TMP_Text hintText;
    private TMP_Text pageText;
    private Image progressImage;
    private bool[] previousEnabled;
    private bool previousHUDEnabled;
    private bool controlsCaptured;
    private bool waitingForInput;
    private bool advanceRequested;
    private bool finished;
    private int sentenceIndex;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    private void Awake()
    {
        if (companionHUD == null || gameplayControls == null || gameplayControls.Length == 0
            || sentences == null || sentences.Length == 0 || TMP_Settings.defaultFontAsset == null)
        {
            Debug.LogError("Giriş hikâyesinin metin, yazı tipi veya kontrol bağlantıları eksik.", this);
            enabled = false;
            return;
        }
        foreach (var control in gameplayControls)
        {
            if (control == null || control == this)
            {
                Debug.LogError("Giriş hikâyesinin kontrol listesi geçersiz.", this);
                enabled = false;
                return;
            }
        }
        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                Debug.LogError("Giriş hikâyesinde boş cümle var.", this);
                enabled = false;
                return;
            }
        }
        CaptureControls();
        CreateCanvas();
    }

    private void Start()
    {
        if (!finished && canvasObject != null)
            StartCoroutine(ShowStory());
    }

    private void Update()
    {
        if (GameWindowMenu.IsOpen || finished || !Application.isFocused || !waitingForInput)
            return;
        if (hintText != null)
            hintText.color = new Color(0.8f, 0.84f, 0.88f, 0.72f + Mathf.Sin(Time.unscaledTime * 2f) * 0.1f);
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            advanceRequested = true;
    }

    private IEnumerator ShowStory()
    {
        for (sentenceIndex = 0; sentenceIndex < sentences.Length; sentenceIndex++)
        {
            storyText.text = sentences[sentenceIndex];
            pageText.text = (sentenceIndex + 1).ToString("D2") + " / " + sentences.Length.ToString("D2");
            progressImage.rectTransform.localScale = new Vector3((sentenceIndex + 1f) / sentences.Length, 1f, 1f);
            hintText.text = sentenceIndex == sentences.Length - 1
                ? "SPACE / ENTER  —  OYUNA BAŞLA" : "SPACE / ENTER  —  DEVAM ET";
            hintText.gameObject.SetActive(false);
            yield return FadeSentence(true);
            advanceRequested = false;
            waitingForInput = true;
            hintText.gameObject.SetActive(true);
            while (!advanceRequested)
                yield return null;
            waitingForInput = false;
            hintText.gameObject.SetActive(false);
            yield return FadeSentence(false);
        }
        float elapsed = 0f;
        while (elapsed < gameRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            screenGroup.alpha = 1f - Ease(elapsed / gameRevealDuration);
            yield return null;
        }
        screenGroup.alpha = 0f;
        screenGroup.blocksRaycasts = false;
        finished = true;
        RestoreControls();
        Destroy(canvasObject);
        canvasObject = null;
    }

    private IEnumerator FadeSentence(bool appearing)
    {
        float elapsed = 0f;
        float duration = appearing ? sentenceFadeIn : sentenceFadeOut;
        sentenceGroup.alpha = appearing ? 0f : 1f;
        sentenceRect.anchoredPosition = new Vector2(0f, appearing ? 12f : 0f);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Ease(elapsed / duration);
            sentenceGroup.alpha = appearing ? t : 1f - t;
            sentenceRect.anchoredPosition = new Vector2(0f, appearing ? Mathf.Lerp(12f, 0f, t) : Mathf.Lerp(0f, -12f, t));
            yield return null;
        }
        sentenceGroup.alpha = appearing ? 1f : 0f;
    }

    private static float Ease(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

    private void CaptureControls()
    {
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        previousHUDEnabled = companionHUD.enabled;
        previousEnabled = new bool[gameplayControls.Length];
        controlsCaptured = true;
        for (int i = 0; i < gameplayControls.Length; i++)
        {
            previousEnabled[i] = gameplayControls[i].enabled;
            gameplayControls[i].enabled = false;
        }
        companionHUD.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    private void RestoreControls()
    {
        if (!controlsCaptured)
            return;
        controlsCaptured = false;
        for (int i = 0; i < gameplayControls.Length; i++)
            if (gameplayControls[i] != null)
                gameplayControls[i].enabled = previousEnabled[i];
        if (companionHUD != null)
            companionHUD.enabled = previousHUDEnabled;
        if (finished && Application.isPlaying && Application.isFocused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = Application.isPlaying && Application.isFocused ? previousCursorLock : CursorLockMode.None;
            Cursor.visible = Cursor.lockState == CursorLockMode.None || previousCursorVisible;
        }
    }

    private void CreateCanvas()
    {
        canvasObject = new GameObject("GirisHikayeEkrani", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        screenGroup = canvasObject.GetComponent<CanvasGroup>();

        GalaxyScreenBackground.Create(canvasObject.transform, backgroundTexture);

        var chapter = Text("Bolum", "BAŞLANGIÇ", canvasObject.transform, new Vector2(.12f, .80f), new Vector2(.88f, .87f), 23f);
        chapter.characterSpacing = 8f;
        chapter.color = new Color(.36f, .77f, .85f);
        Text("Baslik", "UZAY GEMİSİNDEN KAÇIŞ", canvasObject.transform, new Vector2(.10f, .68f), new Vector2(.90f, .80f), 46f);

        var sentenceObject = new GameObject("HikayeCumlesi", typeof(RectTransform), typeof(CanvasGroup));
        sentenceObject.transform.SetParent(canvasObject.transform, false);
        sentenceRect = sentenceObject.GetComponent<RectTransform>();
        Place(sentenceRect, new Vector2(.13f, .34f), new Vector2(.87f, .63f));
        sentenceGroup = sentenceObject.GetComponent<CanvasGroup>();
        sentenceGroup.alpha = 0f;
        storyText = Text("Cumle", "", sentenceObject.transform, Vector2.zero, Vector2.one, 41f);
        storyText.lineSpacing = 12f;

        var progress = new GameObject("Ilerleme", typeof(RectTransform), typeof(Image));
        progress.transform.SetParent(canvasObject.transform, false);
        Place(progress.GetComponent<RectTransform>(), new Vector2(.43f, .30f), new Vector2(.57f, .303f));
        progressImage = progress.GetComponent<Image>();
        progressImage.color = new Color(.36f, .77f, .85f);
        progressImage.raycastTarget = false;
        pageText = Text("Sayfa", "", canvasObject.transform, new Vector2(.30f, .24f), new Vector2(.70f, .29f), 21f);
        pageText.color = new Color(.55f, .61f, .65f);
        hintText = Text("DevamIpucu", "", canvasObject.transform, new Vector2(.12f, .10f), new Vector2(.88f, .19f), 24f);
        hintText.gameObject.SetActive(false);
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text Text(string name, string content, Transform parent, Vector2 min, Vector2 max, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), min, max);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = content;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * .62f;
        label.fontSizeMax = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.richText = false;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        waitingForInput = false;
        RestoreControls();
        finished = true;
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
