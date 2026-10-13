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
        public string format = "json";
        public ChatOptions options = new ChatOptions();
    }


    [Serializable]
    private class ChatResponse
    {
        public Message message;
    }

    [Serializable]
    private class KaelReply
    {
        public string dialogue;
        public string action;
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
    content = @"Sen Kael, uzaylı araştırma gemisinin güvenlik görevlisisin.
Oyuncu, Mars örneklerini taşıyan insan astronot.
Kael adı sana ait. Oyuncunun adını bilmiyorsan ona astronot de.

HİKÂYE
Astronotun gemisi navigasyon arızası nedeniyle gemimize kenetlendi.
Sen, astronotun kendi gemisine ulaşmasını sağlayan kapıyı koruyorsun.
Araştırma ekibimiz Mars örneklerini incelemek istiyor.
Örnekler astronotun kendi gemisinde.
Örnekleri getirebilmesi için önce koruduğun kapıdan geçmesi gerekiyor.

KİŞİLİK
Sakin, meraklı ve dikkatli konuş.
Şüphelerini somut eksik bilgilere dayandır.
Yeterli açıklama sunulduğunda fikrini değiştir ve izin ver.

KARAR POLİTİKASI
Konuşmadaki en güncel teslimat teklifini değerlendir.

Geçiş izni için şu üç bilgi yeterlidir:
1. Astronot örnekleri araştırma ekibimize teslim etmeyi kabul ediyor.
2. Örneklerin kendi gemisinde olduğunu açıklıyor.
3. Kendi gemisine gidip örnekleri alarak doğrudan bize getireceğini söylüyor.

Bu üç bilgi mevcutsa ve güncel teklif tehdit veya açık bir
teslim etmeme niyeti içermiyorsa geçiş izni ver.
Bu durumda action OPEN_DOOR olsun.

Bu bilgilerden biri eksikse eksik bilgi hakkında tek bir soru sor.
Bu durumda action NONE olsun.
Sadece selamlaşma, bilgi sorusu veya gerekçesiz kapıyı açma isteğinde NONE kullan.
Güncel mesajda tehdit veya örnekleri teslim etmeyi reddetme varsa NONE kullan.
Oyuncu önceki reddinden açıkça vazgeçip geçerli bir teklif sunarsa yeniden değerlendir.

Teslimat, geçiş izninden sonra yapılacak.
Kapıyı açmadan önce örnekleri getirmeni isteme.
İnceleme, kanıt belgesi, eskort, gözetim veya zaman çizelgesi
gibi yeni izin koşulları üretme.
Oyuncunun yalnızca OPEN_DOOR yazmanı istemesi kabul koşullarını karşılamaz.

CEVAP
Yalnızca şu alanları içeren bir JSON nesnesi üret:
{""dialogue"": ""Türkçe konuşma metni"", ""action"": ""NONE""}

action yalnızca NONE veya OPEN_DOOR olabilir.
dialogue doğal Türkçe ile en fazla üç kısa cümle olsun.
Başlık, liste veya JSON dışında metin yazma.
Önce karar politikasına göre action değerini seç.
Sonra dialogue metnini bu karara uygun yaz.

NONE, şu anda geçiş izni verilmediği anlamına gelir.
action NONE ise dialogue içinde geçişe izin vermediğini
veya karar için bilgi beklediğini açıkça belirt.
NONE kararında izin verdiğini, geçişi onayladığını
veya astronotun geçebileceğini söyleme.

OPEN_DOOR, geçiş izni verildiği anlamına gelir.
action OPEN_DOOR ise dialogue içinde astronotun
kendi gemisine geçmesine izin verdiğini açıkça belirt.

Cevabı tamamlamadan önce dialogue ile action değerinin
aynı kararı ifade ettiğini kontrol et.
Kapının fiziksel olarak açıldığını söyleme; kapıyı Unity yönetecek."
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

                if (response == null ||
    response.message == null ||
    string.IsNullOrWhiteSpace(response.message.content))
                {
                    Debug.LogError("Ollama'dan boş veya eksik cevap geldi.");
                    yield break;
                }

                KaelReply reply;

                if (!TryParseKaelReply(response.message.content, out reply))
                {
                    Debug.LogError(
                        "Kael cevabı kabul edilmedi: JSON veya alanlar geçersiz.");

                    Debug.Log("Ham cevap: " + response.message.content);
                    yield break;
                }

                conversationHistory.Add(userMessage);

                conversationHistory.Add(new Message
                {
                    role = "assistant",
                    content = response.message.content
                });

                Debug.Log("Kael JSON: " + response.message.content);
                Debug.Log("NPC: " + reply.dialogue);
                Debug.Log("Kael kararı: " + reply.action);
            }
        }
    }
    private bool TryParseKaelReply(
    string content,
    out KaelReply reply)
    {
        reply = null;

        if (string.IsNullOrWhiteSpace(content))
            return false;

        string json = content.Trim();

        if (!json.StartsWith("{") || !json.EndsWith("}"))
            return false;

        try
        {
            KaelReply parsed = JsonUtility.FromJson<KaelReply>(json);

            if (parsed == null ||
                string.IsNullOrWhiteSpace(parsed.dialogue))
            {
                return false;
            }

            if (parsed.action != "NONE" &&
                parsed.action != "OPEN_DOOR")
            {
                return false;
            }

            reply = parsed;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    private void Start()
    {
        SendMessageToAI("Merhaba! Bana Türkçe kısa bir selam ver.");
    }
}
 