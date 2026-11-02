using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

// Yalnızca sabit çevre etiketidir; etkileşim, sensör veya oyun kararı üretmez.
[DisallowMultipleComponent]
public sealed class WorldSign : MonoBehaviour
{
    [SerializeField, TextArea] private string text = string.Empty;
    [SerializeField] private Vector2 size = new Vector2(2f, 0.5f);
    [SerializeField, Min(0.1f)] private float fontSize = 3f;
    [SerializeField] private Color color = Color.white;

    private void Awake()
    {
        if (TMP_Settings.defaultFontAsset == null)
        {
            Debug.LogWarning("Çevre etiketi için varsayılan yazı tipi bulunamadı.", this);
            return;
        }
        var go = new GameObject("Etiket", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(transform, false);
        var label = go.GetComponent<TextMeshPro>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.rectTransform.sizeDelta = size;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize * 0.35f;
        label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.richText = false;
        label.color = color;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
