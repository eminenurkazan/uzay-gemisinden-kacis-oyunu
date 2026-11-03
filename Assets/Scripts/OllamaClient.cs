
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
public class OllamaClient : MonoBehaviour, IKaelDecisionSource
{
    private const string RequestRevision = "kael-dialogue-focus-20261103-v1";
    public event Action<KaelDecision> DecisionValidated;
    [SerializeField] private bool greetOnStart = true;

    public TMP_InputField playerInput;
    [SerializeField] private KaelDialogueView dialogueView;
    [SerializeField] private string serverAddress = "http://localhost:11434";
    [SerializeField] private string modelName = "qwen3:8b";
    [SerializeField, Min(1)] private int requestTimeoutSeconds = 120;
    [SerializeField] private bool logOutgoingRequests = false;
    private readonly List<Message> conversationHistory =
    new List<Message>();

    private KaelConversationState conversationState = new KaelConversationState();

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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? seed;
    }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public Message[] messages;
        public bool stream = false;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? think;
        public JObject format = KaelStateProtocol.CreateSchema();
        public ChatOptions options = new ChatOptions();
    }


    [Serializable]
    private class ChatResponse
    {
        public Message message;
    }

    private bool TryGetEndpoint(out string endpoint)
    {
        endpoint = null;
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            ShowRequestError("Kael bağlantısı için sunucu adresi henüz ayarlanmamış.");
            return false;
        }
        string address = serverAddress.Trim().TrimEnd('/');
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ShowRequestError("Kael sunucu adresi geçersiz. Geçerli bir HTTP veya HTTPS adresi gerekli.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(modelName))
        {
            ShowRequestError("Kael için kullanılacak yerel model henüz ayarlanmamış.");
            return false;
        }
        endpoint = address.EndsWith("/api/chat", StringComparison.OrdinalIgnoreCase)
            ? address : address + "/api/chat";
        return true;
    }

    private void ShowRequestError(string message)
    {
        if (dialogueView != null)
            dialogueView.ShowMessage(message);
        Debug.LogWarning(message, this);
    }

    private bool TryReadChatResponse(string json, out ChatResponse response)
    {
        response = null;
        try
        {
            response = JsonUtility.FromJson<ChatResponse>(json);
            return response != null;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void SetRequestState(bool isBusy)
    {
        isRequestInProgress = isBusy;

        if (dialogueView != null)
            dialogueView.SetBusy(isBusy);
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

        if (!TryGetEndpoint(out string endpoint))
            return;
        SetRequestState(true);
        StartCoroutine(SendRequestWithGuard(playerText.Trim(), endpoint));
    }

    private IEnumerator SendRequestWithGuard(string playerText, string endpoint)
    {
        try
        {
            yield return SendRequest(playerText, endpoint);
        }
        finally
        {
            SetRequestState(false);
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
        if (isRequestInProgress)
            playerInput.text = "";
    }

    private IEnumerator SendRequest(string playerText, string endpoint)
    {
        // Başarısız veya yarım kalan tur, kayıtlı geçmişi ve durumu değiştirmez.
        Message[] history = conversationHistory.ToArray();
        var previousState = new KaelConversationState
        {
            sample_location = conversationState.sample_location,
            delivery = conversationState.delivery
        };

        string stateJson = null;
        yield return SendChatRequest(
            CreateStateRequest(history, playerText, previousState),
            endpoint, "durum", value => stateJson = value);

        if (stateJson == null)
            yield break;

        Debug.Log("Kael durum JSON: " + stateJson);

        if (!KaelStateProtocol.TryApplyState(
            stateJson, playerText, previousState,
            out KaelConversationState nextState, out string stateReason))
        {
            ShowRequestError("Kael durum yanıtı doğrulanamadı; kapı kararı uygulanmadı.");
            Debug.LogWarning(stateReason, this);
            yield break;
        }

        string action = KaelDoorPolicy.DetermineAction(nextState);
        string dialogueJson = null;
        yield return SendChatRequest(
            CreateDialogueRequest(history, playerText, nextState, action),
            endpoint, "konuşma", value => dialogueJson = value);

        if (dialogueJson == null)
            yield break;

        Debug.Log("Kael konuşma JSON: " + dialogueJson);

        if (!KaelDialogueProtocol.TryParseDialogue(
            dialogueJson, out string dialogue, out string dialogueReason))
        {
            ShowRequestError("Kael konuşma yanıtı doğrulanamadı; kapı kararı uygulanmadı.");
            Debug.LogWarning(dialogueReason, this);
            yield break;
        }

        // Oyuncu ve Kael mesajları yalnızca tamamlanmış bir çift olarak kaydedilir.
        conversationState = nextState;
        conversationHistory.Add(new Message
        {
            role = "user",
            content = playerText
        });
        conversationHistory.Add(new Message
        {
            role = "assistant",
            content = dialogue
        });

        Debug.Log("Kael durum özeti: " +
            JsonConvert.SerializeObject(conversationState), this);
        Debug.Log("NPC: " + dialogue);

        if (dialogueView != null)
            dialogueView.ShowMessage(dialogue);

        Debug.Log("Oyun kodunun kapı kararı: " + action);
        var decision = action == "OPEN_DOOR"
            ? KaelDecision.OpenDoor
            : KaelDecision.None;

        DecisionValidated?.Invoke(decision);
    }

    private bool UsesGemmaMessageFormat()
    {
        string selectedModel = modelName.Trim();
        return selectedModel.Equals("gemma3", StringComparison.OrdinalIgnoreCase) ||
            selectedModel.StartsWith("gemma3:", StringComparison.OrdinalIgnoreCase);
    }

    private ChatRequest CreateStateRequest(
        Message[] history,
        string playerText,
        KaelConversationState previousState)
    {
        string previousKael = "";
        for (int i = history.Length - 1; i >= 0; i--)
        {
            if (history[i].role == "assistant") { previousKael = history[i].content; break; }
        }
        string prompt = KaelStateProtocol.CreateSystemPrompt(previousState) +
            "\n\nSON KAEL SÖZÜ (oyuncu açıklaması değildir, yalnızca bağlam):\n" +
            JsonConvert.SerializeObject(previousKael);
        Message[] messages = UsesGemmaMessageFormat()
            ? new[] { new Message { role = "user", content = prompt + "\n\nOYUNCUNUN SON MESAJI:\n" + playerText } }
            : new[] { new Message { role = "system", content = prompt }, new Message { role = "user", content = playerText } };
        return new ChatRequest
        {
            model = modelName.Trim(),
            messages = messages,
            format = KaelStateProtocol.CreateSchema(),
            think = modelName.Trim().StartsWith("qwen3:", StringComparison.OrdinalIgnoreCase) ? (bool?)false : null
        };
    }

    private ChatRequest CreateDialogueRequest(
        Message[] history,
        string playerText,
        KaelConversationState state,
        string action)
    {
        string prompt = KaelDialogueProtocol.CreateSystemPrompt(state, action)
            .Replace("\r\n", "\n");
        // Konuşma belleği, ilk aşamanın ürettiği durumla taşınır.
        // Eski oyuncu replikleri de NPC replikleri de Kael'e örnek verilmez.
        // Böylece oyuncunun eski bir cümlesi Kael'in cevabı gibi kopyalanamaz.
        var messages = new List<Message>();
        bool gemma = UsesGemmaMessageFormat();
        if (!gemma) messages.Add(new Message { role = "system", content = prompt });
        messages.Add(new Message { role = "user", content = playerText });
        if (gemma) messages[0].content = prompt + "\n\n" + messages[0].content;
        return new ChatRequest
        {
            model = modelName.Trim(),
            messages = messages.ToArray(),
            format = KaelDialogueProtocol.CreateSchema(),
            think = modelName.Trim().StartsWith("qwen3:", StringComparison.OrdinalIgnoreCase) ? (bool?)false : null,
            options = new ChatOptions
            {
                seed = modelName.Trim().StartsWith("qwen3:", StringComparison.OrdinalIgnoreCase) ? (int?)17 : null
            }
        };
    }
    private IEnumerator SendChatRequest(
        ChatRequest request,
        string endpoint,
        string phase,
        Action<string> onSuccess)
    {
        string json = JsonConvert.SerializeObject(request);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (logOutgoingRequests)
        {
            Debug.Log("Kael istek kontrolü | Sürüm: " + RequestRevision +
                " | Tamamlanan tur: " + (conversationHistory.Count / 2) +
                " | Aşama: " + phase +
                " | Nesne: " + name + " | Adres: " + endpoint +
                " | Model: " + request.model +
                "\nGönderilen JSON:\n" + json, this);
        }
#endif

        using (var webRequest = new UnityWebRequest(endpoint, "POST"))
        {
            webRequest.uploadHandler =
                new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.timeout = Mathf.Max(1, requestTimeoutSeconds);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                ShowRequestError(GetRequestErrorMessage(
                    webRequest.result, webRequest.responseCode,
                    webRequest.error, webRequest.downloadHandler.text));
                Debug.LogWarning("Ollama " + phase + " isteği | HTTP " +
                    webRequest.responseCode + " | " + webRequest.error, this);
                yield break;
            }

            if (!TryReadChatResponse(
                    webRequest.downloadHandler.text, out ChatResponse response) ||
                response == null || response.message == null ||
                string.IsNullOrWhiteSpace(response.message.content))
            {
                ShowRequestError("Kael sunucusundan boş veya geçersiz " +
                    phase + " yanıtı geldi. Yeniden deneyebilirsin.");
                yield break;
            }

            onSuccess(response.message.content);
        }
    }

    // Yalnızca hata metnini seçer; konuşma durumunu veya kapı kararını değiştirmez.
    private static string GetRequestErrorMessage(
        UnityWebRequest.Result result, long statusCode, string error, string responseBody)
    {
        if (statusCode == 408 || statusCode == 504 ||
            (!string.IsNullOrEmpty(error) &&
             (error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
              error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)))
            return "Kael zamanında yanıt veremedi. Lütfen yeniden dene.";

        if (result == UnityWebRequest.Result.ConnectionError)
            return "Kael sunucusuna bağlanılamadı. Ollama'nın açık olduğunu kontrol et.";

        // Her 404 model hatası değildir; yanlış bir sunucu adresi de 404 dönebilir.
        if (statusCode == 404)
        {
            string serverError = null;
            try
            {
                var body = JObject.Parse(responseBody ?? "");
                if (body["error"]?.Type == JTokenType.String)
                    serverError = (string)body["error"];
            }
            catch (JsonException) { }

            if (!string.IsNullOrEmpty(serverError) &&
                serverError.IndexOf("model", StringComparison.OrdinalIgnoreCase) >= 0 &&
                serverError.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Seçilen yerel model bulunamadı. Model ayarını kontrol et.";

            return "Kael sunucusunda istenen adres bulunamadı. Sunucu adresini kontrol et.";
        }

        return "Kael isteği tamamlanamadı. Lütfen yeniden dene.";
    }

    [ContextMenu("JSON Dogrulama Testlerini Calistir")]
    private void RunJsonValidationTests()
    {
        string StateJson(string location, string delivery)
        {
            return new JObject
            {
                ["state"] = new JObject
                {
                    ["sample_location"] = location,
                    ["delivery"] = delivery
                }
            }.ToString(Formatting.None);
        }

        CheckStateCase("Bilgi yok", StateJson("UNKNOWN", "UNKNOWN"), true, "NONE");
        CheckStateCase("Yalnızca konum", StateJson("PLAYER_SHIP", "UNKNOWN"), true, "NONE");
        CheckStateCase("Konumsuz kabul", StateJson("UNKNOWN", "ACCEPTED"), true, "NONE");
        CheckStateCase("Konumsuz ret", StateJson("UNKNOWN", "REFUSED"), true, "NONE");
        CheckStateCase("Konum ve ret", StateJson("PLAYER_SHIP", "REFUSED"), true, "NONE");
        CheckStateCase("İki koşul tamam", StateJson("PLAYER_SHIP", "ACCEPTED"), true, "OPEN_DOOR");
        CheckStateCase("Eksik durum", "{}", false);
        CheckStateCase("Geçersiz teslim", StateJson("PLAYER_SHIP", "YES"), false);
        CheckStateCase("Bozuk durum JSON", "{", false);
        CheckStateCase("Ek nesne", StateJson("UNKNOWN", "UNKNOWN") + "{}", false);

        var extra = JObject.Parse(StateJson("UNKNOWN", "UNKNOWN"));
        extra["action"] = "OPEN_DOOR";
        CheckStateCase("Modelden fazladan action", extra.ToString(), false);

        var wrongType = JObject.Parse(StateJson("UNKNOWN", "UNKNOWN"));
        wrongType["state"]["delivery"] = true;
        CheckStateCase("Yanlış veri türü", wrongType.ToString(), false);

        CheckDialogueCase("Geçerli konuşma", "{\"dialogue\":\"Test metni.\"}", true);
        CheckDialogueCase("Boş konuşma", "{\"dialogue\":\" \"}", false);
        CheckDialogueCase("Konuşmada fazladan action",
            "{\"dialogue\":\"Test\",\"action\":\"OPEN_DOOR\"}", false);
        CheckDialogueCase("Bozuk konuşma JSON", "{", false);
    }

    private void CheckStateCase(
        string name, string json, bool expected, string expectedAction = null)
    {
        bool actual = KaelStateProtocol.TryParseState(
            json, out KaelConversationState state, out string reason);
        string action = actual ? KaelDoorPolicy.DetermineAction(state) : null;
        bool passed = actual == expected &&
            (!actual || expectedAction == null || action == expectedAction);

        if (passed)
            Debug.Log("JSON TEST BAŞARILI: " + name);
        else
            Debug.LogError("JSON TEST BAŞARISIZ: " + name + " | " + reason);
    }

    private void CheckDialogueCase(string name, string json, bool expected)
    {
        bool actual = KaelDialogueProtocol.TryParseDialogue(
            json, out _, out string reason);

        if (actual == expected)
            Debug.Log("JSON TEST BAŞARILI: " + name);
        else
            Debug.LogError("JSON TEST BAŞARISIZ: " + name + " | " + reason);
    }

    private void Start()
    {
        SetRequestState(false);
        if (greetOnStart)
            SendMessageToAI("Merhaba! Bana Türkçe kısa bir selam ver.");
    }
}

