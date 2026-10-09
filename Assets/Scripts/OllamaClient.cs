
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
    private const string Model = "llama3.2:3b";

    [Serializable]
    private class Message
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public Message[] messages;
        public bool stream = false;
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
                    content = "Sen bir uzay gemisindeki NPC'sin. " +
                              "Yalnızca Türkçe, kısa ve doğal konuş."
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
 