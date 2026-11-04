using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Görüntü ve uygulama seçenekleri; görev veya NPC durumunu değiştirmez.
[DefaultExecutionOrder(-2000)]
public sealed class GameWindowMenu : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    private static GameWindowMenu instance;
    private GameObject panel;
    private CursorLockMode previousLock;
    private bool previousVisible;
    private int windowWidth = 1280;
    private int windowHeight = 720;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; IsOpen = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        var go = new GameObject("OyunSecenekleri");
        instance = go.AddComponent<GameWindowMenu>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        CreateUI();
    }

    private void Update()
    {
        if (!Application.isFocused || Keyboard.current == null) return;
        if (Keyboard.current.f1Key.wasPressedThisFrame) SetOpen(!IsOpen);
        if (Keyboard.current.f11Key.wasPressedThisFrame) SetFullscreen(!Screen.fullScreen);
    }

    private void LateUpdate()
    {
        if (!IsOpen) return;
        // Giriş/son ekranı da görünse seçeneklerin fare erişimi korunur.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetOpen(bool open)
    {
        if (panel == null || IsOpen == open) return;
        if (open)
        {
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            IsOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            panel.SetActive(true);
        }
        else
        {
            panel.SetActive(false);
            IsOpen = false;
            Cursor.lockState = Application.isFocused ? previousLock : CursorLockMode.None;
            Cursor.visible = !Application.isFocused || previousVisible;
        }
    }

    private void SetFullscreen(bool fullscreen)
    {
        if (fullscreen)
        {
            if (!Screen.fullScreen)
            {
                windowWidth = Screen.width;
                windowHeight = Screen.height;
            }
            var display = Screen.currentResolution;
            Screen.SetResolution(display.width, display.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            var display = Screen.currentResolution;
            int width = Mathf.Min(windowWidth, Mathf.Max(640, display.width - 80));
            int height = Mathf.Min(windowHeight, Mathf.Max(360, display.height - 100));
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CreateUI()
    {
        var canvasObject = new GameObject("SeceneklerCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;

        ButtonAt(canvasObject.transform, "Menü (F1)", new Vector2(1, 1),
            new Vector2(-88, -24), new Vector2(156, 36), () => SetOpen(!IsOpen));
        panel = new GameObject("SeceneklerPaneli", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.015f, .025f, .045f, .97f);
        Label(panel.transform, "OYUN SEÇENEKLERİ", new Vector2(0, 150), new Vector2(480, 54), 30);
        Label(panel.transform, "Ekran geçişi: F11  •  Menü: F1", new Vector2(0, 103), new Vector2(500, 36), 18);
        ButtonAt(panel.transform, "Tam ekran", new Vector2(.5f, .5f), new Vector2(-108, 35),
            new Vector2(200, 48), () => SetFullscreen(true));
        ButtonAt(panel.transform, "Pencere", new Vector2(.5f, .5f), new Vector2(108, 35),
            new Vector2(200, 48), () => SetFullscreen(false));
        ButtonAt(panel.transform, "Geri dön", new Vector2(.5f, .5f), new Vector2(0, -35),
            new Vector2(416, 48), () => SetOpen(false));
        ButtonAt(panel.transform, "Oyundan çık", new Vector2(.5f, .5f), new Vector2(0, -105),
            new Vector2(416, 48), QuitGame);
        panel.SetActive(false);
    }

    private static void ButtonAt(Transform parent, string text, Vector2 anchor, Vector2 position,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = new Color(.08f, .36f, .45f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.onClick.AddListener(action);
        Label(go.transform, text, Vector2.zero, size, 22);
    }

    private static void Label(Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject("Metin", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text; label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white; label.raycastTarget = false;
        label.richText = false;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        if (IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        IsOpen = false;
        instance = null;
    }
}
