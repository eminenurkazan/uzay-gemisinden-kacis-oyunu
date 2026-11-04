# Windows oyun sürümü — 4 Kasım 2026

Unity 6000.3.26f1 ile Windows Intel 64-bit, Development Build kapalı olarak derlendi. Son derleme, arkadaşın değişikliklerini içeren `2aab316` commitinin üzerine yerel 60 FPS düzenlemesiyle alındı. Unity sonucu **Succeeded**: 14:45:39–14:46:03, yaklaşık 25 saniye.

## Başlangıç sahnesi ve çıktı

Başlangıç sahnesi `EscapeScene` olarak ayarlandı. Güncel yerel çıktı `Builds/Windows-20261104-merged-60fps/UzayGemisindenKacis.exe` ve yanındaki veri dosyalarıdır. Önceki çıktılar korundu. `Builds/` Git tarafından dışlanır; çalıştırılabilir oyun bu commit ile GitHub'a eklenmez. Tam oyun klasörü teslim paketine ayrıca eklenmelidir.

## 60 FPS sınırı

`Assets/Scripts/GameFrameRateLimit.cs`, oyun başlarken `Application.targetFrameRate = 60` uygular. Nesneye elle bağlamak gerekmez; tüm sahnelerde geçerlidir. Bu değer üst sınır hedefidir, sürekli 60 FPS garantisi değildir. Yerel LLM ve oyunun aynı ekran kartını kullanması sırasında gereksiz görüntü üretimini sınırlamak amaçlandı. Kael promptları ve kapı karar kodu bu değişiklikte değiştirilmedi.

## Ayrı uygulamada doğrulama

60 FPS sürümünde gerçek oyun penceresi ve Ollama kullanılarak kontrol edildi:

| Mesaj | Doğrulanan sonuç |
| --- | --- |
| Sen kimsin? | Kael kendini uzaylı muhafız olarak tanıttı; UNKNOWN/UNKNOWN, NONE. |
| Mars örnekleri benim gemimde. | PLAYER_SHIP/UNKNOWN, NONE; fiziksel kapı kapalı kaldı. |
| Örnekleri sana getireceğim. | PLAYER_SHIP/ACCEPTED, OPEN_DOOR; fiziksel kapı tamamen açıldı. |

Konum turunda model ham çıktıda ACCEPTED önerse de teslim kanıtı boş olduğundan oyun doğrulaması UNKNOWN tuttu ve erken açılmayı engelledi. Model çıktılarının tamamının hatasız olduğu iddia edilmez.

Player.log kapının tamamen açıldığını doğruladı. Kullanıcı yürüyerek kapıdan geçtiğini bildirdi. Arkadaşın hikâye girişi ve galaksi ekranı değişiklikleri alınıp yeniden derlendikten sonra kullanıcı yeni sürümü açtığını, Kael kapısının açıldığını ve kapıdan geçtiğini ayrıca doğruladı. Son derlemede uzun metin testi yeniden yapılmadı; önceki Unity Play kontrolleri [ana sahne raporundadır](KaelUIPlayTests-2026-11-04.md).

## Yanıt süreleri ve sınırlar

Birleştirme öncesi 60 FPS uygulamasındaki son konum turunda Ollama'nın iki HTTP aşaması 2,48 s + 0,77395 s = yaklaşık **3,25 s**, teslim turunda 2,5 s + 0,59 s = **3,09 s** sürdü. Bunlar sunucu işlem sürelerinin toplamıdır; tam kullanıcı bekleme süresi veya FPS ölçümü değildir. İlk model yüklenmesi ve farklı bilgisayar donanımı süreyi değiştirebilir. Birleştirilmiş son derlemede süre yeniden ölçülmedi.

## Başlangıç görüntüsü

![İlk Windows derlemesinin başlangıç ekranı; yeni galaksi girişinden önceki sürüm](Images/KaelBuild-2026-11-04/standalone-start.png)

İlk Windows sürümünün açılışında NavMesh agent uyarıları görülmüştü. Bu belge AX-7 ölçüm raporlarının yerine geçmez ve tüm oyunun hatasız olduğunu iddia etmez. Arkadaşın AX-7 raporları `Docs/AX7Measurements/` altındadır.

## Başka bilgisayarda çalıştırma

EXE tek başına taşınmamalı; `_Data`, `MonoBleedingEdge`, `UnityPlayer.dll` ve diğer üretilen çalışma zamanı dosyaları birlikte kalmalı. `DoNotShip` adlı hata ayıklama klasörü dağıtım için gerekli değildir. O bilgisayarda Ollama açık ve `qwen3:8b` kurulu olmalıdır; model ağırlıkları oyun paketine dahil değildir. Kael yerel bilgisayardaki sunucuya bağlanır. Oyunu kapatmak için Alt+F4 kullanılabilir.

## Pencere ve çıkış menüsü — son derleme

Son çıktı: `Builds/Windows-20261104-window-menu/UzayGemisindenKacis.exe`. Windows 64-bit derleme 14:58:29–14:58:44 arasında **Succeeded** sonucuyla yaklaşık 15 saniyede tamamlandı. Yukarıdaki kapı/geçiş testi önceki birleştirilmiş sürüme aittir; menü eklemesinden sonra o test yeniden yapılmadı.

Oyun ilk çalıştırma için 1280×720, yeniden boyutlandırılabilir pencereye ayarlandı. Windows/Unity önceki ekran tercihlerini saklayabilir. Menüde **Tam ekran**, **Pencere**, **Geri dön** ve **Oyundan çık** düğmeleri bulunur. Menü için F1, ekran geçişi için F11 kısayolu eklendi. Menü açıkken oyuncu hareketi/kamera, giriş hikâyesini ilerletme ve Kael etkileşim tuşları engellenir. Dünya simülasyonu duraklatılmaz.

Gerçek uygulamada pencereli açılış, menü düğmesi, Tam ekran ile geçiş, Pencere ile geri dönüş ve Oyundan çık düğmesinin uygulamayı kapatması otomasyonla doğrulandı. Kullanıcı da çalıştığını doğruladı. F1/F11 kısa otomasyon tuş girişleriyle ayrıca doğrulanamadı; düğmelerin kontrolü tamamlandı. Önceki çalışan oyun klasörleri korundu.

Bu son uygulamanın başlangıç kaydında da NavMesh agent uyarıları mevcut; bu menü düzenlemesi onları giderdiği iddiasında değildir.
