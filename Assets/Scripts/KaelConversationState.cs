using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Serializable]
public sealed class KaelConversationState
{
    public string sample_location = "UNKNOWN";
    public string delivery = "UNKNOWN";
}

// Doğal dili model yorumlar. Burada yalnızca çıktı yapısı ve karar tutarlılığı denetlenir.
public static class KaelStateProtocol
{
    public const string OutputInstructions = @"ÇIKTI BİÇİMİ
Yalnızca state, action ve dialogue alanlarından oluşan geçerli bir JSON nesnesi döndür.
- state.sample_location: Örneklerin oyuncunun gemisinde olduğu öğrenildiyse PLAYER_SHIP, bilinmiyorsa UNKNOWN.
- state.delivery: Oyuncunun son teslim kararı kabul ise ACCEPTED, ret ise REFUSED, henüz karar yoksa UNKNOWN.
- action: Yukarıdaki geçiş koşullarına göre NONE veya OPEN_DOOR.
- dialogue: Kael'in oyuncuya söylediği, boş olmayan Türkçe cevap.

Önceki durum özeti ve konuşma geçmişini birlikte kullan. Yeni bilgi yoksa öğrenilmiş bilgiyi koru; oyuncu açıkça düzelttiyse güncelle. Üç ana alanı da yaz. Ek alan veya JSON dışında metin üretme.

";

    public static JObject CreateSchema()
    {
        return new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["state"] = new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["sample_location"] = StringChoice("UNKNOWN", "PLAYER_SHIP"),
                        ["delivery"] = StringChoice("UNKNOWN", "ACCEPTED", "REFUSED")
                    },
                    ["required"] = new JArray("sample_location", "delivery"),
                    ["additionalProperties"] = false
                },
                ["action"] = StringChoice("NONE", "OPEN_DOOR"),
                ["dialogue"] = new JObject { ["type"] = "string", ["minLength"] = 1 }
            },
            ["required"] = new JArray("state", "action", "dialogue"),
            ["additionalProperties"] = false
        };
    }

    private static JObject StringChoice(params string[] values) => new JObject
    {
        ["type"] = "string", ["enum"] = new JArray(values)
    };

    public static bool TryParse(string content, out string dialogue, out string action,
        out KaelConversationState state, out string reason)
    {
        dialogue = null;
        action = null;
        state = null;
        reason = "JSON veya alanlar geçersiz.";
        if (string.IsNullOrWhiteSpace(content)) return false;
        try
        {
            using (var text = new StringReader(content))
            using (var reader = new JsonTextReader(text))
            {
                reader.DateParseHandling = DateParseHandling.None;
                reader.MaxDepth = 8;
                var root = JToken.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                }) as JObject;
                if (reader.Read() || root == null || root.Count != 3 ||
                    !ReadString(root["dialogue"], out string parsedDialogue) || string.IsNullOrWhiteSpace(parsedDialogue) ||
                    !ReadString(root["action"], out string parsedAction)) return false;

                var memory = root["state"] as JObject;
                if (memory == null || memory.Count != 2 ||
                    !ReadString(memory["sample_location"], out string location) ||
                    !ReadString(memory["delivery"], out string delivery) ||
                    (location != "UNKNOWN" && location != "PLAYER_SHIP") ||
                    (delivery != "UNKNOWN" && delivery != "ACCEPTED" && delivery != "REFUSED")) return false;

                bool conditionsMet = location == "PLAYER_SHIP" && delivery == "ACCEPTED";
                if ((parsedAction != "NONE" && parsedAction != "OPEN_DOOR") ||
                    (parsedAction == "OPEN_DOOR") != conditionsMet)
                {
                    reason = "Kapı kararı, modelin döndürdüğü durum özetiyle çelişiyor.";
                    return false;
                }

                dialogue = parsedDialogue.Trim();
                action = parsedAction;
                state = new KaelConversationState { sample_location = location, delivery = delivery };
                reason = "";
                return true;
            }
        }
        catch (JsonException) { return false; }
    }

    private static bool ReadString(JToken token, out string value)
    {
        value = null;
        if (token?.Type != JTokenType.String) return false;
        value = token.Value<string>();
        return true;
    }
}
