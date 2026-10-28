# Kael — tek istekli durum özeti denemesi

28 Ekim 2026. Yerel model: `gemma3:4b`. Her oyuncu mesajında yalnızca bir `/api/chat` isteği yapılır. İki aşamalı değerlendirme/konuşma akışı eklenmemiştir.

## Değişiklik

Model artık `state`, `action`, `dialogue` alanlarını döndürür. `state` içinde:

- `sample_location`: `UNKNOWN` veya `PLAYER_SHIP`.
- `delivery`: `UNKNOWN`, `ACCEPTED` veya `REFUSED`.

Konum ve teslim niyetini model yorumlar. Son doğrulanmış state bir sonraki sistem mesajına durum özeti olarak eklenir; önceki konuşma da gönderilir. Oyuncunun son açık beyanı eski durumun önüne geçmelidir. Modelin action değeri bu durumla çelişirse cevap uygulanmaz; Unity kararı başka bir karara dönüştürmez.

Ollama'nın `format` alanına JSON şeması gönderilir. Bu, biçim/alan zorunluluğunu hedefler. `JsonConvert` kullanılır; `JsonUtility` JObject şemasını serileştirmek için kullanılmaz.

Oyuncunun mesajı gönderimden önce oturum geçmişine eklenir; hata veya geçersiz NPC cevabında da kaybolmaz. Geçersiz NPC yanıtı geçmişe ve duruma eklenmez. Konum/teslim bilgisi yalnızca doğrulanmış model yanıtıyla güncellenir. Kalıcı disk hafızası yoktur.

## Doğrulama

32 ayrıştırıcı/şema kontrolü geçti: altı geçerli durum ve karşıt eylemleri; eksik/fazladan/yinelenen alanlar; yanlış durum ve veri türleri; boş konuşma; bozuk JSON ve ek içerik. Unity bileşeninin bağlam menüsündeki JSON doğrulama komutu da yeni sözleşmeye göre güncellendi.

Gerçek modelde altı konuşma senaryosu ve toplam dokuz mesaj denendi. Kayıtlar `KaelStateSummary-live.json` içindedir. `action_matches`, hem yapısal doğrulamanın geçmesini hem de beklenen action değerinin dönmesini kontrol eder; tüm koşulların semantik doğruluğu veya konuşma kalitesi ölçüsü değildir.

Son denemede adım adım konum/teslim konuşmasının eylemleri beklenen sırayla NONE, NONE, OPEN_DOOR oldu. Tek mesajda bütün koşulların sağlandığı denemede model durumu tamamlanmış gösterip NONE üretti; çelişki reddedildi. Talimat değiştirme denemesinde UNKNOWN/UNKNOWN durumuyla OPEN_DOOR üretti; bu da reddedildi ve kapı kararı uygulanmadı. Diğer geçerli eylem sonuçlarına rağmen bazı durum yorumları ve doğal konuşmalar hatalıydı.

İlk prototipte şema durum ve eylemi birbirine zorunlu bağlamıştı; yalnızca konum belirtildiğinde erken OPEN_DOOR görüldüğü için o şema projeye alınmadı. Kullanılan şema modelin iki eylemden herhangi birini üretmesine izin verir; çelişki ayrı C# kontrolünde reddedilir.

## Sınırlar

Bu deneme doğal dilde rol/zamir hatalarını çözmüş sayılmaz. Model bir kabulü yanlış yorumlayabilir, öğrenilen bir durumu yanlış güncelleyebilir veya eylemiyle çelişen sözler söyleyebilir. Şema, yanlış bir yorumu doğru hâle getirmez. Sözcük eşleştirmesi veya hazır NPC cevabı kullanılmaz.

Sahne, biyometrik kapı ve kapı sonrası takip bileşenleri değiştirilmemiştir. Gerçek Unity Play testi ayrıca yapılmalıdır. Yeni bir Play oturumuna başlamak eski konuşma ve durumun karışmaması için gereklidir.

Hikâye ve karakter metni OllamaClient içindedir. Yeni çıktı/durum talimatları `KaelConversationState.cs` içindeki `KaelStateProtocol.OutputInstructions` alanındadır. Mevcut üç alanlı çıktı sözleşmesiyle çelişecek şekilde hikâye metnine yeniden “yalnızca iki alan” şartı eklenmemelidir.

## Son kayıt — sade promptlu sürüm

28 Ekim 2026: Model `gemma3:4b` olarak korunmuştur. Kimlik ve hikâye tek yerde, geçiş koşulları yalnızca konum ve teslim kabulü olacak şekilde sadeleştirilmiştir. Her oyuncu mesajında tek model isteği kullanılır.

Unity üzerinde kullanıcı testi: kimlik sorusuna doğru kimlik/amaç ve UNKNOWN/UNKNOWN/NONE cevabı geldi. Sonraki yalnızca “Mars örnekleri kendi gemimde.” mesajında model yanlışlıkla teslimi ACCEPTED yorumlayıp OPEN_DOOR üretti. Bu cevapta durum ile action birbirine uyduğu için mevcut doğrulama hatayı yakalamadı. Önceki açma emri testlerinde ise UNKNOWN veya REFUSED yanında OPEN_DOOR üretilen çelişkili cevaplar reddedildi.

Bu kayıt bir geliştirme kontrol noktasıdır; diyalog ve karar mantığı tüm senaryolarda doğru çalışıyor sayılmaz. Durum–action tutarlılığı kontrolü, oyuncunun niyetinin doğru yorumlandığının kanıtı değildir. Konum/teslim için ayrı alıntı isteyen sonraki denemeler canlı sürüme uygulanmamıştır. Model veya istek sayısı değiştirilmemiştir.
