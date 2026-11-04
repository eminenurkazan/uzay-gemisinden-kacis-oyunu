# NavMesh başlangıç sırası — 4 Kasım 2026

## Neden

EscapeScene içinde AX-7 ve Kael için iki farklı ajan türü ve iki pişirilmiş NavMesh verisi bulunuyor. Ajanlar sahnede etkin kaydedildiğinden, Unity yerel ajanı oluşturmaya yüzey verileri NavMeshSurface.OnEnable üzerinden kaydedilmeden girişebiliyor. Daha sonra veriler yüklenince bağlanmaları, ilk uyarının oluşmadığı anlamına gelmiyor.

## Kontrollü yeniden üretim

Unity 6000.3.26f1 ile, asıl projenin NavMeshAreas ayarları, iki pişirilmiş NavMesh dosyası ve AI Navigation çalışma zamanı kaynakları kullanılarak ayrı bir tanı projesi derlendi. Ajan türleri ve başlangıç konumları gerçek sahneden alındı. Windows uygulaması batchmode/nographics ile çalıştırıldı; bu test tam oyunun oynanış testi değildir ve sahnenin kapı carving/LLM bileşenlerini içermez.

| Açılış | Yerel ajan oluşturma uyarısı | Sonraki NavMesh bağlantısı |
| --- | ---: | --- |
| Önceki yöntem, iki ajan | 3 | İkisi de bağlı |
| Önceki yöntem, yalnız AX-7 | 2 | Bağlı |
| Önceki yöntem, yalnız Kael | 1 | Bağlı |
| Düzeltilmiş yöntem, iki ajan | 0 | İkisi de bağlı |

Seçilen günlük satırları: [tanı çıktısı](NavMeshStartup-Audit-2026-11-04.txt).

## Düzeltme

İki NavMeshAgent bileşeni sahnede kapalı kaydedildi. Her karaktere eklenen NavMeshAgentStartup, Start aşamasında tüm yüzeylerin OnEnable çağrılarından sonra kendi ajan türü ve alan maskesiyle en fazla 0,15 birim yakındaki başlangıç zeminini doğrular. Yalnız başlangıçtaki küçük yüzey farkını düzeltip ajanı etkinleştirir ve isOnNavMesh durumunu denetler. Zemin bulunmazsa ajan kapalı kalır; hata karakter adı, ajan türü ve konumuyla raporlanır. Çalışma sırasında rota ve hareket NavMeshMovement tarafından hesaplanmaya devam eder.

Görev süreleri, sensörler, hızlar, başlangıç X/Z konumları ve NPC kararları değiştirilmedi. Eski Windows uygulamaları değişmez; son oyun build'i yeniden alınmalıdır. EscapeScene Play ve nihai Windows build başlangıcı, AX-7 panel görevi ve Kael takibi ayrıca doğrulanmalıdır. A1/A2 ölçümleri 413c827 sürümüne ait olarak korunur; bu değişiklik sonrası sürüm bilgisiyle ayrı değerlendirilmelidir.
