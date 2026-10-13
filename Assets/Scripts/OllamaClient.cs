using System.Collections.Generic;
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
    private readonly List<Message> conversationHistory =
    new List<Message>();

    private bool isRequestInProgress;

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
        if (string.IsNullOrWhiteSpace(playerText))
            return;

        if (isRequestInProgress)
        {
            Debug.Log("Kael düşünüyor. Cevabını bekle.");
            return;
        }

        isRequestInProgress = true;
        StartCoroutine(SendRequestWithGuard(playerText.Trim()));
    }

    private IEnumerator SendRequestWithGuard(string playerText)
    {
        try
        {
            yield return SendRequest(playerText);
        }
        finally
        {
            isRequestInProgress = false;
        }
    }
    public void SendPlayerMessage()
    {
        if (isRequestInProgress)
        {
            Debug.Log("Kael düşünüyor. Cevabını bekle.");
            return;
        }
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
        // Mevcut istekteki system ve user mesajlarını al.
        Message systemMessage = request.messages[0];
        Message userMessage = request.messages[1];

        // System prompt, geçmiş konuşmalar ve yeni oyuncu mesajı.
        List<Message> messagesToSend = new List<Message>();
        messagesToSend.Add(systemMessage);
        messagesToSend.AddRange(conversationHistory);
        messagesToSend.Add(userMessage);

        request.messages = messagesToSend.ToArray();
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

                if (response != null &&
                response.message != null &&
                !string.IsNullOrWhiteSpace(response.message.content))
                {
                    conversationHistory.Add(userMessage);

                    conversationHistory.Add(new Message
                    {
                        role = "assistant",
                        content = response.message.content
                    });

                    Debug.Log("NPC: " + response.message.content);
                }
                else
                {
                    Debug.LogError("Ollama'dan boş veya eksik cevap geldi.");
                }
            }
        }
    }
    private void Start()
    {
        SendMessageToAI("Merhaba! Bana Türkçe kısa bir selam ver.");
    }
}
 