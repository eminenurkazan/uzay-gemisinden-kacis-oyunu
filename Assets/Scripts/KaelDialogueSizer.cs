using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class KaelDialogueSizer : MonoBehaviour
{
    public TMP_Text dialogueText;

    public float minimumHeight = 70f;
    public float maximumHeight = 180f;
    public float extraSpace = 20f;

    private ScrollRect scroll;
    private RectTransform area;
    private string previousText;
    private float previousWidth = -1f;

    private void Awake()
    {
        scroll = GetComponent<ScrollRect>();
        area = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (dialogueText == null ||
            scroll.content == null ||
            scroll.viewport == null)
        {
            return;
        }

        float width = scroll.viewport.rect.width;
        bool textChanged = previousText != dialogueText.text;

        if (!textChanged &&
            Mathf.Abs(previousWidth - width) < 0.5f)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        float contentHeight =
            LayoutUtility.GetPreferredHeight(scroll.content);

        float height = Mathf.Clamp(
            contentHeight + extraSpace,
            minimumHeight,
            maximumHeight);

        area.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height);

        if (textChanged)
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        previousText = dialogueText.text;
        previousWidth = width;
    }
}