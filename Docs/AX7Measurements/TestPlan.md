# AX-7 sistematik ölçümleri

Durum: Araç hazırlandı; ölçülmüş deneme sonucu henüz yok.

Bu kayıtlar nihai README'den ayrı tutulur. Kael konuşması geliştirilmeye devam
ettiği için bu deneylerin tamamı ilk fiziksel kapı görevinde biter. Kael, Ollama
ve biyometrik kapı testi değildir. Sonuçlar kullanıcı denemelerinden sonra
hesaplanacak ve ortak final raporuna en son aktarılacaktır.

## Deney düzeni

- Başlangıç sürümü: `48a1a02`; buna editör ölçüm gözlemcisi eklendi.
- Sahne: `Assets/Scenes/EscapeScene.unity`.
- Hedef: AX-7'nin E ile kabul edilen komuttan fiziksel kapının tamamen açılmasına
  ve `Succeeded` durumuna geçmesine kadar görev süresi ve sonucu.
- Kayıt: Unity menüsünden **Uzay Oyunu → AX-7 Ölçüm Penceresi** açılır ve açık tutulur.
- Araç konum ayarlamaz, engel açıp kapatmaz, görev başlatmaz, rota çizmez.
  Komut ve tüm etkileşimler oyuncu tarafından normal şekilde yapılır.
- Her bağımsız denemede Play durdurulup tekrar başlatılır; ilk kapı kapalı ve
  AX-7 `Waiting` durumunda olmalıdır. Game alanına tıklanır, AX-7'ye yaklaşılır,
  gösterilen komut ipucuyla E'ye bir kez basılır. Robotun önünde beklenmez.
- Ölçüm sırasında kod/sahne kaydı veya düzenlemesi yapılmaz. Unity Pause ve
  zaman ölçeği değişikliği kullanılmaz. Sadece B testlerinde engel kapatılabilir.
- Renkli NavMesh gösterimi ve ölçüm penceresi oyun davranışını yönlendirmez.
- Görev sürerken reddedilen tekrar E basışları yeni kabul edilmiş görev değildir;
  ayrı bir deneme veya otomatik başarısızlık kaydı oluşturmaz.

## Önceden belirlenen testler

| Kod | Başlangıç | SensorTestEngeli | Uygulama | Beklenen | Tekrar |
| --- | --- | --- | --- | --- | --- |
| A1 | X=1,5; Z=-12; Y yaklaşık 0,025 | Kapalı | E ile görev; müdahale etme | Kapı tamamen açılır, Succeeded | 3 |
| A2 | X=0; Z=-10; Y yaklaşık 0,025 | Kapalı | E ile görev; müdahale etme | Kapı tamamen açılır, Succeeded | 3 |
| B1 | A1 ile aynı | Başlangıçta açık | Sensör engelde durunca, ölçüm penceresindeki engelde bekleme 5 saniyeye ulaşınca engeli kapat | Yeni komut olmadan devam, kapı açık, Succeeded | 3 |
| B2 | A1 ile aynı | Açık bırak | Engeli kaldırma | Yaklaşık 20 oyun saniyesi engelde beklemenin ardından Failed; kapı kapalı | 3 |

Toplam hedef 12 geçerli bağımsız denemedir. Geçersiz hazırlıkla yapılan denemeler
silinmez; gerekçeleriyle tutulur ve hedef tekrar sayısını doldurmaz.

A2 başlangıcı yalnızca Play sırasında hazırlanır: Hierarchy'de **AX-7 kökünü**
seç, NavMesh Agent bileşenini kapat, Transform Position'ı X=0, Y=0,025, Z=-10 yap,
ajanı yeniden aç. E'den önce X/Z'nin korunduğunu kontrol et. Y'nin NavMesh'e
otururken küçük farkla düzelmesi beklenebilir. Başarılı varış konumu başlangıç
olarak raporlanmaz. Test konumu sahneye kalıcı kaydedilmez.

## Ham kayıt ve süre tanımları

Ham sonuçlar `Docs/AX7Measurements/Raw/ax7-<oturum>.csv` dosyalarına eklenir.
Pencere oturum boyunca aynı dosyaya ekler. Yeni oturum yeni dosya oluşturabilir;
bu nedenle rapor hazırlanırken klasördeki bütün oturumlar okunur. Kayıtlar
oyun projesinin D sürücüsündedir, internet veya Kael sunucusu gerekmez.

- `duration_game_s`: kabul edilen komuttan görev bitimine oyun saatindeki süre.
  Sensörde bekleme, panel kullanımı ve fiziksel kapının açılması dahildir.
- `duration_wall_s`: aynı aralığın editörün monoton gerçek zaman saatindeki süresi.
  Yük altında oyun saati/gerçek zaman farkı ayrıca incelenebilir; birbirine karıştırılmaz.
- `obstacle_wait_game_s`: `WaitingForObstacle` durumunda geçen toplam oyun süresi.
- `arrival_game_s`, `panel_start_game_s`: komuttan varışa/etkileşime geçen oyun süresi;
  bu aşamalara ulaşılamadıysa boş kalır.
- Başlangıç koordinatları komut gerçekten kabul edildiği anda okunur; tahmini
  değerler veya görev sonundaki varış koordinatı kullanılmaz.
- Git commit, yerel tarih ve saat dilimi, hız, ajan boyutu, engel zaman aşımı,
  kapı/engel hazırlığı, durum geçişleri ve başarısızlık nedeni kaydedilir.
- CSV üç ondalıkla saklanır; bu, donanımın milisaniye hassasiyetinde doğrulandığı
  anlamına gelmez. Görev bitişi kare güncellemelerinde gözlenir.
- Play erken durdurulursa/ölçüm penceresi kapanırsa `Interrupted` kaydı geçersizdir.
  Pause, zaman ölçeği değişikliği, yanlış başlangıç, zaten açık kapı, taze olmayan
  görev ve yanlış engel düzeni de `valid_for_stats=0` ile işaretlenir.
- B1/B2'de sensör beklemesi hiç gözlenmezse engel senaryosu geçerli sayılmaz.
- Araç oyun açıldıktan sonra görevin ortasında açılırsa geriye dönük süre üretmez.
- Dosyaya yazma hatasında kayıt bellekte tutulur; penceredeki yeniden deneme
  düğmesiyle kaydedilir. Bekleyen kayıt çözülmeden yeni deneme başlatılmamalıdır.

## Değerlendirme

Deneme sonuçları henüz yoktur; bu belgede başarı oranı veya ortalama süre verilmez.

Her senaryo ayrı hesaplanır. Yalnızca `valid_for_stats=1` olan kayıtlar istatistiğe
girer; geçersiz ve yarım denemeler toplam sayıları ve gerekçeleriyle ayrıca gösterilir.

- A1/A2/B1 görev başarı oranı = başarılı görev / geçerli deneme sayısı × 100.
- Ortalama başarılı görev süresi, yalnızca başarıyla biten geçerli denemelerden
  hesaplanır. Oyun saati ve gerçek zaman ortalamaları ayrı sütunlardır.
- B2'de beklenen sonuç görev başarısızlığıdır. Engel zaman aşımı nedeniyle
  Failed olması ve kapının kapalı kalması, senaryonun doğru çalıştığı anlamına
  gelir; görev başarı oranına başarılı görev gibi eklenmez.
- `expected_result_met`, senaryo beklentisinin karşılanıp karşılanmadığını;
  `task_succeeded`, robotun gerçekten görevi başarıyla bitirip bitirmediğini belirtir.
- Başarısız görevlerin süreleri kaybolmaz; başarı ortalamasına karıştırılmaz.

Takip, tekrar komutu, kapı arızası veya ulaşılamayan hedef gibi ek işlevsel
denemeler daha sonra ayrı protokolle eklenebilir; bu 12 tekrarın yerine geçirilmez.
