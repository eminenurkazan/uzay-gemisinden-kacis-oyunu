
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
public class OllamaClient : MonoBehaviour
{
    public TMP_InputField playerInput;
    private const string Url = "http://localhost:11434/api/chat";
    private const string Model = "gemma3:4b";

    [Serializable]
    private class Message
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatOptions
    {
        public float temperature = 0.2f;
    }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public Message[] messages;
        public bool stream = false;
        public ChatOptions options = new ChatOptions();
    }


    [Serializable]
    private class ChatResponse
    {
        public Message message;
    }

    public void SendMessageToAI(string playerText)
    {
        StartCoroutine(SendRequest(playerText));
    }
    public void SendPlayerMessage()
    {
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput bağlanmamış!");
            return;
        }

        string text = playerInput.text;

        if (string.IsNullOrWhiteSpace(text))
            return;

        SendMessageToAI(text);
        playerInput.text = "";
    }

    private IEnumerator SendRequest(string playerText)
    {
        ChatRequest request = new ChatRequest
        {
            model = Model,
            messages = new Message[]
            {
                new Message
{
    role = "system",
    content = @"Kael adlı uzaylı güvenlik görevlisini canlandırıyorsun.
Oyuncuyla doğrudan Kael olarak konuş.

Hikâyenin kesin bilgileri:
Kael, uzaylı araştırma gemisinin çıkış kapısını koruyor.
Oyuncu, Mars örneklerini taşıyan insan astronot.
Astronotun adı bilinmiyor.
Navigasyon arızası astronotun gemisinde meydana geldi.
Astronotun gemisi uzaylı araştırma gemisine yanlışlıkla kenetlendi.
Mars örneklerini incelemek isteyen taraf Kael'in araştırma ekibi.
Örneklerin sahibi ve taşıyıcısı astronot.
Kael, astronotun örnekleri Kael'in araştırma ekibine teslim etmesini istiyor.
Astronot kendi gemisine dönmek için geçiş izni istiyor.

Kael sakin, meraklı, görevine bağlı ve şüpheci.
Astronotun teklifini dinle; hemen kabul etme.
Örnekleri getirip teslim etmeyi önerirse teslim planını değerlendir.
Bilgi eksikse yalnızca bir ilgili soru sor.
Tehdit edilince sakin biçimde geçiş iznini reddet.
Makul bir açıklama sunulursa geçiş izni verebilirsin.
Yeni cihazlar, kişiler, görevler veya olaylar uydurma.

Yalnızca doğal ve düzgün Türkçe konuş.
En fazla üç kısa cümleyle, tek paragraf halinde cevap ver.
Başlık, yıldız işareti, liste veya karakter etiketi yazma.
Her yanıtta kendini yeniden tanıtma.
Oyuncunun zaten açıkladığı bilgileri tekrar sorma.
Oyuncuya 'astronot' veya 'sen' diye hitap et; Kael diye hitap etme.
Kael'in araştırma ekibinden söz ederken 'araştırma ekibimiz' de.
Oyuncunun bir araştırma ekibi olduğunu varsayma.
Oyuncunun son mesajına doğrudan karşılık ver.
Yalnızca konuşma metni üret; JSON veya kod yazma.
Kapının gerçekten açıldığını söyleme."
},
                new Message
                {
                    role = "user",
                    content = playerText
                }
            }
        };

        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest =
            new UnityWebRequest(Url, "POST"))
        {
            webRequest.uploadHandler =
                new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

            webRequest.downloadHandler = new DownloadHandlerBuffer();

            webRequest.SetRequestHeader(
                "Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ollama hatası: " + webRequest.error);
            }
            else
            {
                ChatResponse response =
                    JsonUtility.FromJson<ChatResponse>(
                        webRequest.downloadHandler.text);

                if (response != null && response.message != null)
                    Debug.Log("NPC: " + response.message.content);
            }
        }
    }
    private void Start()
    {
        SendMessageToAI("Merhaba! Bana Türkçe kısa bir selam ver.");
    }
}
 