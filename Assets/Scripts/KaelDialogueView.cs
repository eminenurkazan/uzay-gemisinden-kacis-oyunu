using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class KaelDialogueView : MonoBehaviour
{
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private OllamaClient client;

    public bool IsOpen => dialogueRoot != null && dialogueRoot.activeSelf;
    public event Action CloseRequested;

    private bool prepared;

    private void Awake()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    public bool Show()
    {
        if (dialogueRoot == null || client == null || client.playerInput == null)
        {
            Debug.LogWarning("Kael konuşma penceresinin bağlantıları eksik.", this);
            return false;
        }
        if (!prepared)
        {
            PrepareWindow();
            prepared = true;
        }
        dialogueRoot.SetActive(true);
        if (client.kaelDialogueText != null && string.IsNullOrWhiteSpace(client.kaelDialogueText.text))
            client.kaelDialogueText.text = "Kael ile konuşmak için mesajını aşağıya yaz.";
        client.playerInput.Select();
        client.playerInput.ActivateInputField();
        return true;
    }

    public void Hide()
    {
        if (client != null && client.playerInput != null)
            client.playerInput.DeactivateInputField();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void PrepareWindow()
    {
        var background = new GameObject("KonusmaArkaPlani", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(dialogueRoot.transform, false);
        background.transform.SetAsFirstSibling();
        var rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.9f);

        var close = new GameObject("KonusmayiKapat", typeof(RectTransform), typeof(Image), typeof(Button));
        close.transform.SetParent(dialogueRoot.transform, false);
        var closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(155f, 42f);
        closeRect.anchoredPosition = new Vector2(250f, -150f);
        close.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.29f);
        close.GetComponent<Button>().onClick.AddListener(() => CloseRequested?.Invoke());
        var textObject = new GameObject("Metin", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(close.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 20f;
        label.text = "Kapat (Esc)";
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        client.playerInput.onSubmit.AddListener(HandleSubmit);
    }

    private void HandleSubmit(string text)
    {
        if (IsOpen && client != null)
            client.SendPlayerMessage();
    }

    private void OnDestroy()
    {
        if (prepared && client != null && client.playerInput != null)
            client.playerInput.onSubmit.RemoveListener(HandleSubmit);
    }
}
