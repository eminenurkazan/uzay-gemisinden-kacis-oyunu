using UnityEngine;

// Yerel LLM ile görüntü üretiminin aynı GPU'yu tüketmesini sınırlar.
internal static class GameFrameRateLimit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        Application.targetFrameRate = 60;
        Debug.Log("Oyun kare hızı sınırı: 60 FPS.");
    }
}
