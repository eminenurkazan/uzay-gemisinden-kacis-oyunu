using UnityEngine;
using UnityEngine.UI;

// Both screens share one imported texture; no animated scene or extra camera.
public static class GalaxyScreenBackground
{
    public static void Create(Transform parent, Texture2D texture)
    {
        var root = new GameObject("GalaksiArkaPlan", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = Color.black;
        if (texture == null)
            return;

        var picture = new GameObject("MarsGalaksisi", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        picture.transform.SetParent(root.transform, false);
        Stretch(picture.GetComponent<RectTransform>());
        var image = picture.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        var fit = picture.GetComponent<AspectRatioFitter>();
        fit.aspectRatio = (float)texture.width / texture.height;
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        var shade = new GameObject("YaziKontrasti", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(root.transform, false);
        Stretch(shade.GetComponent<RectTransform>());
        shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        shade.GetComponent<Image>().raycastTarget = false;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
