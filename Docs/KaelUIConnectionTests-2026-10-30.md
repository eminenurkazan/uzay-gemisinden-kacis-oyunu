# Kael — Arayüz ve bağlantı testleri

**Tarih:** 30 Ekim 2026
**Ortam:** Unity Editor, AITestScene, yerel Ollama bağlantısı
**Model:** gemma3:4b
**Yöntem:** Kullanıcının uyguladığı manuel testler, paylaşılan ekran görüntüleri ve Console çıktıları.

## Test sonuçları

| No | Test | Beklenen sonuç | Gözlenen sonuç | Durum |
|---|---|---|---|---|
| UI-01 | Mesaj alanına uzun metin yazılması | Metin kutu genişliğine ulaşınca alt satıra geçmeli. | Multi Line Newline ayarından sonra kullanıcı alt satıra geçişi denedi ve çalıştığını doğruladı. | Geçti |
| UI-02 | Çok satırlı mesajın gönderilmesi | Gönder butonu mesajı iletmeli; cevap sonrasında yeniden yazılabilmeli. | Kullanıcı gönderimi ve ardından tekrar yazmayı denediğini doğruladı. | Geçti |
| UI-03 | Yanıt beklenirken tekrar gönderme | İkinci bir eşzamanlı istek engellenmeli. | Kullanıcı bu akışın çalıştığını doğruladı. | Geçti |
| UI-04 | Bekleme durumunun sona ermesi | Cevap sonrası “Kael düşünüyor…” yazısı kaybolmalı; yeni mesaj gönderilebilmeli. | Kullanıcı bu adımların çalıştığını doğruladı. | Geçti |
| UI-05 | Uzun cevapların gösterimi | Metinler üst üste gelmemeli; uzun cevap okunabilmeli. | Kullanıcı metinlerin üst üste gelmediğini ve sorun bulunmadığını bildirdi. | Geçti — kullanıcı bildirimi |
| NET-01 | Ollama kapalıyken mesaj gönderilmesi | Bağlantı hatası kullanıcıya anlaşılır biçimde gösterilmeli. | Ekranda “Kael yanıt veremedi. Sunucu bağlantısını ve yerel modelin çalıştığını kontrol et.” uyarısı görüldü. Normal Kael görseli de görünüyordu. | Geçti |
| NET-02 | Ollama yeniden açıldıktan sonra mesaj gönderilmesi | Aynı Play oturumunda yeni istek cevap alabilmeli. | Sonraki istekte geçerli Kael JSON cevabı geldi; state UNKNOWN/UNKNOWN ve action NONE idi. | Geçti |
| UI-06 | 16:9 görünümünde yerleşim | Konuşma yazısı, görsel ve giriş kontrolleri ekrana sığmalı. | İlk kontrolde üst yazı kesiliyordu. Canvas ölçekleme ve konum düzenlemesinden sonra bütün öğelerin sığdığı görüldü. | Düzenleme sonrası geçti |
| UI-07 | Paylaşılan Free Aspect ölçüsünde yerleşim | Öğeler birbirine taşmadan görünmeli. | Son ekran görüntüsünde konuşma kutusu, görsel, mesaj alanı ve Gönder butonu görünüyordu. | Geçti — gösterilen ölçü için |

## Uygulanan arayüz düzenlemeleri

- PlayerInput için Line Type, Multi Line Newline olarak ayarlandı.
- Canvas ölçekleme yöntemi Scale With Screen Size olarak değiştirildi.
- 16:9 yerleşimi için 800 × 450 referans çözünürlüğü ve Match 1 ayarları yönlendirildi; panel ve giriş kontrollerinin dikey konumları düzenlendi.
- Son ekran görüntülerinde üstteki metnin kesilmediği ve kontrollerin ekrana sığdığı görüldü.

## Açık sorunlar ve kalan kontroller

- **Kael'in anlamsal karar hatası:** Önceki diyalog testinde yalnızca “Mars örnekleri kendi gemimde.” mesajına karşılık model, teslim kararını yanlışlıkla ACCEPTED yorumlayıp OPEN_DOOR üretti. Oyuncu teslim sözü vermemişti.
- **Doğrulamanın sınırı:** Kod, modelin durum özetiyle çelişen action değerini reddediyor. Yanlış durum yorumu ve action birbirine uyduğunda bu kontrol tek başına hatayı yakalayamıyor.
- **Diyalog tutarlılığı:** Önceki testlerde veren–alan rollerini karıştırma ve olmayan bilgi ekleme görüldü. Bağlantı testinin geçmesi bu sorunların düzeldiği anlamına gelmez.
- **Görseller:** Kael görselinin son hâli, arkadaşın görselleri tamamladıktan sonra yerleştirilecek.
- **Ekran kapsamı:** Kesin piksel çözünürlükleri kaydedilmedi. 4:3 ve diğer dar ekranlar için kontrollü ölçü testi yapılmadı. Bütün ekran boyutları için uygunluk doğrulanmış değildir.
- **Ortak test:** Ana sahnede kapı, Kael takibi, AX7 ve baştan sona oyun akışı arkadaşla birlikte kontrol edilecek.
- **Çalıştırılabilir sürüm:** Bu kayıt Unity Editor testlerine aittir; bağımsız oyun sürümü ve ikinci bilgisayar testi ayrıca yapılmalıdır.

## Teslim raporuna ekleme

Bu dosya arayüz ve bağlantı testlerini belgeler. En az beş farklı gerçek LLM diyalog senaryosu ayrıca oyuncu mesajları, model cevapları, beklenen sonuçlar ve gözlenen hatalarla kaydedilmelidir. Ayrıştırıcı kontrolleri ile manuel arayüz testleri bu diyalog kayıtlarından ayrı gösterilmelidir.

## Kayıt öncesi dosya kontrolü

Sahne dosyasında kaydırma çubuğu tutamacının hesaplanamayan `NaN` konum değeri `0` olarak düzeltildi. Sahnedeki sayısal değerler ve birleştirme işaretleri kontrol edildi. Bu küçük düzeltmeden sonra Unity Play kontrolü ayrıca yapılmalıdır.
