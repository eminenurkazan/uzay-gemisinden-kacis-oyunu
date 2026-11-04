# Kael ekran sorumluluğunu ayırma — 3 Kasım 2026

> **4 Kasım 2026 güncellemesi:** Ana sahnede portreler, Enter ile gönderme, Esc/E ile kapatma/yeniden açma, uzun mesajda satır kaydırma ve gerçek kapıdan geçiş kullanıcı tarafından doğrulandı. Büyük portrelerin güncel ekranları ve kapsamı [yeni ana sahne raporunda](KaelUIPlayTests-2026-11-04.md) yer alır. Aşağıdaki tarihlenmiş kayıtlar kendi test sürümünü anlatır.

Cevap/hata yazısı, bekleme yazısı ve iki portreyi yönetme işi mevcut KaelDialogueView bileşenine taşındı. OllamaClient.ShowRequestError ve başarılı cevap sunumu ShowMessage metodunu, SetRequestState ise SetBusy metodunu çağırıyor. İstek bayrağı OllamaClient içinde kaldı. Mesaj girişi ve düğme bağlantısı korundu.

İki script ve iki sahne güncellendi. EscapeScene mevcut view bileşenine bağlandı. AITestScene için aynı view bileşeni eklendi; dialogueRoot boş bırakıldı, bu bağımsız test sahnesinde pencere kapatma/açma işlevi çağrılmaz. Dört UI alanının özgün nesne kimlikleri taşındı; yeni görüntü/metin nesnesi yaratılmadı. Sahnedeki mevcut kaydırma çubuğu değişikliği ve font önbelleği düzenlemesine dokunulmadı.

## Testler

- 120 istek örneğinde eski ve yeni JSON payloadları byte düzeyinde aynı. Qwen/Gemma, 6 durum ve 5 oyuncu cümlesi, iki aşama karşılaştırıldı.
- 54 protokol kontrolü ve 12 hata mesajı kontrolü geçti.
- 44 başarısız tur/yeniden deneme senaryosu ve 756 kontrol yeni kodla tekrar geçti.
- Unity kütüphaneleriyle derleme hatasız. Unity/JSON tarafından atanan alanlar için CS0649 ve harness yapılandırmasına göre CS0414 uyarıları var; tam uygulama build'i değildir.
- İki sahnede alan referansları, bileşen kimlikleri ve model/süre ayarları kontrol edildi. Diff boşluk kontrolü geçti.

## Gerçek Unity Play testi

AITestScene yeniden yüklendi ve Play çalıştırıldı. Sen kimsin sorusuna Ben Kael, uzaylı muhafızım cevabı geldi, karar NONE. Konum verilince söz istendi, karar NONE. Getirme sözü verilince Anlaştık cevabı ve OPEN_DOOR kararı görüldü. Düşünme yazısı ve portresi her istekte açılıp sonrasında normal görsele döndü.

Play sırasında geçici bulunmayan model seçilip mesaj gönderildi; yeni hata açıklaması ve normal portre görüldü. Play durdurularak model qwen3:8b ve süre 120 ayarlarına dönüldüğü Inspector'da doğrulandı.

EscapeScene bu turda oynanmadı; konuşma aç/kapat, Enter/Esc ve fiziksel kapı için ana sahne canlı kontrolü ayrıca yapılabilir. AITestScene'de fiziksel kapı animasyonu doğrulanmış sayılmaz.

Test bitiminde Unity organizasyon bilgisi alınamadı servis kaydı görüldü. Stack UnityEditor.EditorApplication tarafındaydı; Kael compile/runtime hatası gözlenmedi. Bu servis sorunu için hesap veya güvenlik ayarı değiştirilmedi.

## Korunan davranışlar

KaelConversationState kaynak hash'i aynı. Promptlar, JSON şemaları, model/think/sıcaklık/seed ayarları, iki istek sırası, bellek kaydı ve karar olayı kuralları aynı kaldı. UI ayrımı Kael'in mevcut semantik/rol hatalarını çözmüş sayılmaz. HTTP ve oturum yönetimi halen OllamaClient'ta; tüm SOLID ilkelerine tam uyum iddiası yok.

Uygulanan dosyalar test edilen adaylarla hash düzeyinde aynı. [Test özeti ve kaynak hash değerleri](KaelViewSeparationTests-2026-11-03.json).
