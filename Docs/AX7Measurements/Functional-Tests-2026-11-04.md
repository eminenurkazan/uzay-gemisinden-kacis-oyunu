# AX-7 otomatik işlev kontrolleri — 4 Kasım 2026

## Sonuç

`b6d1ab7` oyun kodu ve EscapeScene kopyası üzerinde tamamlanan son koşuda **29/29 kontrol geçti**. [Kontrol listesi](Functional-Audit-2026-11-04.txt), [seçilmiş günlük satırları](Functional-Evidence-2026-11-04.txt), [test yürütücüsü](Automation/AX7FunctionalAuditRunner.cs).

| Davranış | Doğrulanan sonuç |
| --- | --- |
| NavMesh başlangıcı | AX-7 ve Kael doğru türde NavMesh'e bağlı; ajan oluşturma uyarısı yok |
| Komut bekleme | Görev oyuncu komutu öncesinde Waiting |
| Tekrar komut | Çalışan göreve 20 ek MoveToPanel çağrısı görevi yeniden başlatmadı veya Failed yapmadı; CanIssueCommand false |
| Panel ve kapı | Gerçek NavMesh rotası, panel etkileşimi ve kapı açılışı başarılı; tek başarı olayı ve tek kapı açılışı |
| Tamamlanan göreve tekrar komut | 10 ek çağrı yeni görev oluşturmadı |
| Açık kapıdan takip | Robot güvenlik odasından ilk odaya dönerken z=-4 kapı düzlemini mutlak X < 2 aralığından geçti; bakım geçidi seçilmedi |
| Yakında bekleme | Duruş geçişinden sonra 0,6 oyun saniyesi boyunca konum farkı 0; WaitingNearPlayer |
| Uzaklaşınca devam | Oyuncu 2,3 birimlik devam eşiğinin dışına taşınınca Following; yakına gelince yeniden durdu |
| Takipte engel | Gerçek Raycast ile test kutusu algılandı; duruş sonrası konum farkı 0; engel kaldırılınca takip devam etti |
| Hava kilidi | Editör kapı test girişi sonrası Kael gerçek takiple tamamen hava kilidine girdi |
| Atmosfer ve vedalaşma | Gerçek panel olayı Kael'i düşürdü; AX-7 takip bileşenini kapatıp gerçek NavMesh rotasıyla bekleme noktasına ulaştı |
| Geminin dışında kalma | Gerçek ön koşullarla transfer kapağı açıldı; oyuncu gemideyken AX-7 WaitingAtExit kaldı, konum farkı 0; gemi içi AX-7 NavMesh'inde yürünebilir değil |

## Test yöntemi ve sınırlar

- Asıl açık Unity projesi değiştirilmeden Assets ve ProjectSettings ayrı tanı kopyasına alındı. Unity 6000.3.26f1 batchmode/nographics Editor Play kullanıldı. AI Navigation, Input System ve URP dahil paketler aynı yerel kaynaklardan alındı. Son koşudan sonra oyun C# dosyalarının, EscapeScene'in, iki pişirilmiş NavMesh dosyasının ve NavMeshAreas ayarlarının asıl projeyle byte düzeyinde eşit olduğu doğrulandı.
- Oyuncu CharacterController konumu test düzeneği tarafından belirlenen noktalara taşındı. AX-7 konumu veya görev/durum alanları test tarafından elle atanmadı; robot kendi NavMeshMovement, ObstacleSensor, AX7Brain, AX7FollowPlayer ve AX7Farewell bileşenleriyle hareket etti.
- Klavye E tuşu fiziksel olarak simüle edilmedi. E'nin kullandığı MoveToPanel görev girişi ve CanIssueCommand koşulu kontrol edildi; bu bir gerçek kullanıcı arayüzü veya görsel kalite testi değildir. Giriş hikâyesi/HUD ve manuel oyuncu kontrolü yalnız kopyadaki test kurulumunda devre dışı bırakıldı.
- Kael için Ollama/Qwen çağrısı yapılmadı, sahte model yanıtı üretilmedi. Biyometrik kapı, mevcut UNITY_EDITOR OpenForEditorTest girişiyle açıkça test amacıyla açıldı. Ardından gerçek Kael takibi, AtmosphereControlPanel.TryActivate, düşme olayı ve gemi kapağı ön koşulları çalıştı. Bu sonuçlar LLM entegrasyonunu veya nihai Windows build'ini doğrulamaz.
- Takipte kullanılan geçici fizik kutusu yalnız tanı kopyasına eklendi ve kaldırıldı. Üretim sahnesine test düğmesi veya kutusu eklenmedi.
- Bu koşu performans ölçümü değildir; 29 kontrol ayrı 29 bağımsız tekrar anlamına gelmez. Kullanıcının 12 ana süre denemesi ayrı CSV'de korunur.

## Tanı sırasında yapılan test düzeneği düzeltmeleri

İlk koşularda duruma geçilen kareyle fizik güncellemesinin tamamlandığı anın farklı olması nedeniyle duruş konumu erken örneklendi. Son düzende duruşa 0,3–0,4 oyun saniyesi yerleşme payı verilip ardından konum sabitliği ölçüldü. Uzaklaşma kontrolünde oyuncu başlangıçta yanlışlıkla robotun yakınlık alanına taşınmıştı; hedef gerçek 2,3 birimlik devam eşiğinin dışına düzeltildi. Oyun kodu değiştirilmedi.

Tanı projesinin UnityEditor.Search.SearchDatabase arama dizini ArgumentOutOfRange istisnası, oyun istisnalarından ayrı tutuldu; test gözlemcisi yalnız bu belirli editör yığınını sayımdan çıkardı. Diğer Error/Exception ve Failed to create agent mesajları başarısızlık nedeni olarak izlendi. Son koşuda oyun çalışma zamanı hatası ve yerel ajan oluşturma uyarısı bulunmadı. Editör dizin istisnası, oyunun hatası olarak raporlanmaz; genel Unity editörünün bütünüyle hatasız olduğu da iddia edilmez.

## Tekrarlama

Yürütücüyü yalnız ayrı bir proje kopyasında Assets altına koyup Unity'yi `-batchmode -nographics -executeMethod AX7FunctionalAuditEntry.Run` ile açın. Oyun kopyası EscapeScene'i yükleyip Editor Play'e girer, AX7-functional-audit.txt dosyasına yazar ve tamamlanınca çıkar. Asıl kullanıcı projesinde çalıştırmayın; test oyuncu konumlarını ve editör oturumunu yönetir.

Nihai ortak sunumdan önce gerçek E etkileşimi, görsel görünüm ve yeniden alınmış Windows build başlangıcı/kaçış akışı ayrıca denenmelidir.
