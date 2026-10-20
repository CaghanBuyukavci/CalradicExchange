# Calradic Exchange — Geliştirme Planı

Bu dosya projenin yol haritasıdır. Claude Code her görevi bitirdiğinde kutucuğu işaretler ve plandan sapma olduysa altına kısa bir not ekler.

Süre tahminleri haftada 10–15 saat çalışma ve Claude Code kullanımı varsayımıyla verilmiştir.

**Mevcut faz: Faz 1** (Faz 0 tamamlandı 2026-10-18)

---

## Faz 0 — Kurulum ve doğrulama (≈1 hafta)

Amaç: Tasarımın dayandığı her şeyin oyunda gerçekten var olduğunu doğrulamak ve boş ama çalışan bir mod elde etmek.

- [x] 0.1 Ortam bilgilerini al (kurulum yolu, oyun sürümü, DLC'ler) ve `CLAUDE.md`'ye yaz
  - v1.4.8, Steam, G: sürücüsü, DLC yok.
- [x] 0.2 Araçları kontrol et ve kur: .NET SDK, ilspycmd, git, Bannerlord.Templates
  - ilspycmd 11.1 ve Bannerlord.Templates 1.3.1 kuruldu. .NET SDK 10, git 2.55 mevcuttu.
- [x] 0.3 Oyun DLL'lerini `.decompiled/` altına decompile et
  - CampaignSystem, Core, Library, Localization, MountAndBlade, ObjectSystem, ModuleManager, SandBox.
- [x] 0.4 `API_NOTES.md` içindeki her API'yi doğrula, durumunu ve imzasını güncelle
  - Faz 1 API'lerinin hepsi VERIFIED. Sapmalar: takvim uzunluğu FastMode'da değişiyor (ADR-008), `MapDistanceModel.GetDistance` imzası değişti (Faz 4), mevsim ve kıtlık için event yok (tick'te tespit, planlandığı gibi).
- [x] 0.5 Item, item category ve workshop type ID'lerini oyun XML'lerinden çıkar; `ModuleData/instruments.json` taslağını gerçek ID'lerle yaz
  - `cloth` ticari mal değil, yerine `felt` (ADR-011). `stable` atölyesi oyunda kapalı.
- [x] 0.6 Şehir pazar fiyatı ve atölye kârı verisine nasıl erişileceğine karar ver (ECONOMY_MODEL §2.1, §2.3). Karar `DECISIONS.md`'ye
  - Emtia: `item.Value × MarketData.GetPriceFactor(category)` (ADR-009). Lonca kârı: `ClanFinanceModel.CalculateOwnerIncomeFromWorkshop` gerçek veri (ADR-010).
- [x] 0.7 Solution iskeleti: `Core`, `Core.Tests`, `Sim`, oyun modülü. `.gitignore`, git init, ilk commit
  - Test ve Sim net8.0. Oyun modülü net472, LangVersion 10 (BUTR şablon varsayılanı).
- [x] 0.8 `SubModule.xml` ve deploy betiği (build sonrası `Modules\CalradicExchange`'e kopyalama)
  - `deploy.ps1`: testleri çalıştırır, derler, sadece kendi DLL'lerimizi + SubModule.xml + ModuleData'yı kopyalar.
- [x] 0.9 Şehir menüsüne "Borsaya git" seçeneği; tıklanınca yer tutucu mesaj
  - Türkçe metin `ModuleData/Languages/TR` altında. Oyun içinde doğrulandı (2026-10-18).
- [x] 0.10 Mod log dosyası ve debug konsol komutlarının iskeleti (`exchange.ping`)
  - `exchange.ping` çalışır; diğer komutlar Faz 1.22'ye kadar 'not implemented' döner. Oyun içinde doğrulandı (2026-10-18).
- [x] 0.11 JSON kayıt iskeleti: `SyncData` ile boş bir durum nesnesini kaydet ve yükle
  - `cx_state_v1` anahtarı, schemaVersion 1, round-trip testleri. Oyun içinde doğrulandı (2026-10-18).

**Kabul kriterleri**
- Mod launcher'da görünüyor, kampanya hatasız açılıyor.
- Şehir menüsünde "Borsaya git" seçeneği çalışıyor.
- `exchange.ping` konsolda cevap veriyor, log dosyası oluşuyor.
- Kaydet → yükle sonrası log'da durum nesnesinin yüklendiği görülüyor.
- `API_NOTES.md` içinde Faz 1'in kullandığı tüm API'ler `VERIFIED`.

---

## Faz 1 — MVP: Emtia ve sektör borsası (≈4–6 hafta)

Amaç: Menü tabanlı arayüzle oynanabilir, dünyaya tepki veren ilk borsa.

### 1A. Core (oyundan bağımsız)
- [x] 1.1 Veri modeli: `Instrument`, `InstrumentClass`, `PriceState (P, F, S, history)`, `Position`, `Portfolio`, `MarketEvent`, `WorldSnapshot`
  - Model/ (Core). `MarketState`, `PriceState` (P, F, S, geçmiş), `Position`, `Portfolio`. S, her olay için ayrı yarı ömürle sönen bileşenlerin toplamı.
- [x] 1.2 `instruments.json` ve `settings.json` okuyucu, doğrulama ve anlamlı hata mesajları
  - Tüm hatalar tek seferde listelenir. instruments.json'a `tags` eklendi (olay hedefleri için).
- [x] 1.3 RNG (xorshift128+), durumu serileştirilebilir
- [x] 1.4 Temel değer hesaplayıcıları: emtia (§2.1), sektör (§2.2)
  - Emtia fiyatı ADR-009'a göre. Kategori sepeti (askeri tedarik) savaş çarpanıyla.
- [x] 1.5 Günlük fiyat güncellemesi (§3) ve S terimi sönümü (§4)
- [x] 1.6 Olay → şok eşlemesi (§4.1), tablo `settings.json`'dan
  - Mevsim değişimi ve kıtlık Core içinde snapshot'tan tespit edilir.
- [x] 1.7 Emir motoru: fark, komisyon, kayma, kalıcı fiyat etkisi (§5), limitler (§6)
  - Faz 1A'ya çekildi (simülatör botları maliyet istiyordu). Komisyon Trade'e göre %5→%1, %10 kâr vergisi (ADR-014).
- [x] 1.8 Portföy: ortalama maliyet, gerçekleşmiş/gerçekleşmemiş kâr-zarar
  - Faz 1A'ya çekildi.
- [x] 1.9 Kalıcılık: tüm durumun JSON'a ve geri dönüşümü, `schemaVersion`, round-trip testleri
  - Şema 2. Faz 0 kayıtları için migration ve testi var.
- [x] 1.10 Birim testleri: her formül için en az bir test, emir motoru için kenar durumları (yetersiz para, limit aşımı, sıfır adet)
  - 80 test.

### 1B. Simülatör
- [x] 1.11 Sentetik olay üreteci ve sentetik dünya (ECONOMY_MODEL §8)
  - 24 şehir, 6 krallık, 72 köy. Gerçek oyunla kalibrasyonu Faz 1C'de `export_csv` verisiyle kontrol edilmeli.
- [x] 1.12 Strateji botları: al-tut, rastgele, olayı gören
  - Ek olarak: yağma kovalayan bot (bilgi amaçlı) ve erken/orta/geç oyuncu profilleri.
- [x] 1.13 CSV çıktısı ve özet tablo; §8 hedeflerinin otomatik kontrolü (geçti/kaldı)
  - Erken oyun gelir sınırı hedefi eklendi (ADR-014).
- [x] 1.14 Parametreleri hedefler tutana kadar ayarla; sonuçları kullanıcıya sayılarla raporla
  - ADR-013, ADR-014, ADR-015. 20 seed'de 8/8 hedef geçti. Manipülatör bot eklendi (oyuncunun kendi yağmaları).

### 1C. Oyun katmanı
- [x] 1.15 `WorldReader`: şehir fiyatları, refah, kuşatma durumu → `WorldSnapshot`
  - Fiyat = item değeri × kategori çarpanı; verisi olmayan şehirler atlanır. İlk oyun testi (2026-10-18): işlenmiş mallar taban değerin 5–7 katı, temel değer günde %10–17 oynuyor (simülatörde %1–2). Stoksuz şehirlerin tavan çarpanı şüpheli.
  - [x] 1.15a Ortalama yöntemini gerçek veriyle kalibre et (`Logs/market_raw.csv`), sentetik dünyayı da buna göre ayarla.
  - ADR-017 (hacim ağırlığı), ADR-018 (kıtlık), ADR-019 (beklenen şok tabanı). Otomatik döküm 120. borsa gününe kadar sürüyor (2026-10-19'da 40'tan uzatıldı); uzun veriyle oynaklık ve piyasanın oturma günü kontrol edilince kaldırılacak.
  - ADR-021: kampanya başındaki fiyat çöküşü için temel değer ilk 90 gün yumuşatılıyor (yarı ömür 45 → 0).
  - ADR-022 (2026-10-19) ADR-021'in yerine geçti: F = taban × e^E (göreli, sönen ekonomi sinyali), haber şokları ×2, komisyon en fazla %2,5, yağma birikme sınırı.
  - 2026-10-19 oyun testi (43 gün): yeşil gün oranı %54–58 ve sabit, oyuncu +375 dinar kâr etti, Tüccar becerisine göre komisyon doğru, slider çalışıyor. Açık konu: ortalama +%15 yükseliş (bkz. ADR-022 test notu). Kalibrasyon bitince otomatik piyasa dökümü (120. gün) kaldırılacak.
- [x] 1.16 `EventBridge`: köy yağması, kuşatma başlangıcı/bitişi, şehir el değiştirme, savaş, barış, mevsim → `MarketEvent`
  - Kale kuşatmaları ve küçük fraksiyon savaşları sayılmaz. Mevsim ve kıtlık Core'da.
- [x] 1.17 Günlük tick: snapshot al → Core'u güncelle → büyük hareketleri mesajla bildir
  - Gün sayacı borsanın kendi sayacı (API_NOTES, 09:00 tick notu). Elde tutulan araçta ≥%3, herhangi bir araçta ≥%8 harekette mesaj.
- [x] 1.18 Borsa alt menüsü: Piyasaya bak, Al, Sat, Portföy, Haberler, Ayrıl
- [x] 1.19 Al/Sat akışı: araç seçimi (çoktan seçmeli inquiry) → adet (metin inquiry) → onay penceresi (fiyat, fark, komisyon, toplam)
  - 2026-10-19: adet penceresi Gauntlet'e taşındı (`CxQuantityPopup`): slider + yazılabilir kutu + −/+ ve canlı maliyet satırı. Açılamazsa metin inquiry'ye düşer (ADR-020).
- [x] 1.20 Altın işlemleri oyuncunun parasıyla bağlantılı; yetersiz parada anlaşılır uyarı
  - Altın `GiveGoldAction` ile; işlemi Core onaylamadan altın değişmez.
- [x] 1.21 Kampanya ortasında mod eklenirse başlangıç değerlerini o günden kur
  - İlk session'da açılış fiyatları hemen kurulur; Faz 0 kayıtları şema 2'ye taşınır.
- [x] 1.22 Debug komutları: `dump_prices`, `dump_portfolio`, `fire_event`, `set_price`, `export_csv`
  - Ek: `exchange.tick [n]` (oyun zamanını ilerletmeden borsa günü çalıştırır).
- [x] 1.23 Tüm handler'larda try/catch ve log
- [x] 1.24 Metinler `TextObject` ile; şimdilik İngilizce + Türkçe taslak
  - 97 metin, Türkçe tam. İngilizce kodda varsayılan.

**Kabul kriterleri**
- Simülatör §8 hedeflerinin hepsini geçiyor.
- Oyunda 100 gün boyunca çökme yok; log'da yakalanmış hata yok.
- Bir köy yağmalanınca ilgili emtia ertesi günlerde belirgin şekilde yükseliyor (konsolla tetiklenen olayla da test edilir).
- Al → kaydet → yükle → sat akışı doğru para ve kâr/zararla çalışıyor.
- Mod kaldırılıp kayıt yüklenince oyun açılıyor.

**Faz sonu:** Kullanıcıyla birlikte 1–2 saatlik oyun testi. Denge notları `DECISIONS.md`'ye.

---

## Faz 2 — Lonca hisseleri, krallık endeksleri, Gauntlet arayüzü (≈5–7 hafta)

### 2A. Yeni araçlar
- [x] 2.1 Atölye kârı okuyucu (Faz 0.6 kararına göre gerçek veri veya tahmin)
  - 2026-10-19: kod hazır (ADR-010 yolu). `WorldReader` atölyeleri türe göre gruplar, `MarketState.Guilds` 28 günlük kâr serisini tutar (şema 7), günlük log'a lonca özeti düşer, `exchange.dump_guilds` listeler ve CSV yazar. İlk test (58 gün) değerleri verdi. Zararı da göstermek için ölçü kasa değişimine çevrildi (ADR-027, şema 8).
- [x] 2.2 Lonca ortaklık payı: temel değer ve haftalık kâr/zarar dağıtımı (§2.3, ADR-027)
  - ADR-029: pay-gün hak edişi, alt sınırlı fiyat, borç ve ilişki cezası. Core testleri var; oyun içi test bekleniyor.
- [x] 2.3 Lonca listesini dinamik yönet: atölye kaybolursa lonca kapanır, pozisyonlar son fiyattan nakde döner
  - 2026-10-20 (ADR-034): Oyuncu kendi atölyesinin loncasından pay alamaz. Atölye alınca o loncadaki payları piyasa fiyatından, masrafsız satılır.
- [x] 2.3a Lonca Piyasası tablosu: bulunulan şehrin loncaları (AL/SAT) + başka şehirlerde elde tutulan loncalar (sadece SAT) (ADR-026)
- [x] 2.4 Krallık endeksi (§2.4); krallık yok olunca kapanış; Krallık Piyasası tablosu
  - ADR-030: her şehirde alınır, açığa satış yok, yıkılınca sıfır, oyuncunun krallığı da listelenir. Şema 10. Oyun içi test bekleniyor.
  - 2026-10-20 (ADR-032): Endekse köyler, kaleler, askeri güç ve klan sayısı eklendi; olaylara kale fethi, hükümdar ve klan lideri ölümü ve esareti, muharebeler, savaşın kazananı/kaybedeni (haraç), barış ve klan iltihak/ayrılmaları eklendi.
- [ ] 2.5 Simülatöre yeni araçları ekle, hedefleri yeniden kontrol et (özellikle temettülü al-tut getirisi)
  - 2026-10-20 (ADR-036): Emtia hisseleri 12'den 31'e çıktı. Simülatörün sentetik dünyasına yeni ürünlerin oyun fiyatları eklendi; 8 hedefin hepsi geçiyor.

### 2B. Gauntlet ekranı
- [ ] 2.6 Teknik deneme (spike): Gauntlet'te basit bir liste ekranı ve ViewModel. Örnek olarak vanilla ekranlardan birinin prefab'ını incele
  - Kısmen öne çekildi (ADR-020): piyasa ve portföy tablo pencereleri. Oyun içi test bekleniyor.
  - 2026-10-19: borsa menüsü arka plan görseli (ADR-023); tıklanınca doğrudan miktar adımına geçen alım/satım seçim tablosu ve "Takas Geçmişim" tablosu (ADR-024).
  - 2026-10-19: "Hisse al/sat" yerine Emtia Piyasası ve Sepet Piyasası tabloları; her satırda AL/SAT, işlemden sonra tabloya dönüş (ADR-025).
- [x] 2.7 Grafik çözümü denemesi: ince widget'larla çubuk grafik veya dinamik doku. Sonuç `DECISIONS.md`'ye
  - 2026-10-20: 4 piksellik ince widget dilimleriyle çizgi grafik (dönme ve doku yok), ilk kullanım Portföy Performansı penceresi (ADR-031). 2.9'daki fiyat grafiği aynı `LineChart` yerleşimini kullanacak.
  - 2026-10-20 (ADR-035): Performans penceresi düzenlendi: grafik ortalı ve sekmelerin altında, eksende gün noktaları, portföy toplamları altta kayan tablo.
  - 2026-10-20: Toplamlar tablosu kaymıyor, 10 satırın hepsi görünüyor; tablo daraldı, başlık küçüldü.
- [ ] 2.8 ~~Piyasa sekmesi~~ Tam ekran ekran yapılmıyor (ADR-026); piyasa tabloları (Emtia, Sepet, Lonca, Krallık) bu işi görüyor
  - 2026-10-20 (ADR-035): Dört piyasa tek "Piyasalar" penceresinde sekme oldu: Hisseler, Sepetler, Ortaklıklar, Endeksler. Borsa seçeneği şehir menüsünde Ticaret'in hemen üstünde.
  - 2026-10-20: Menü sırası düzeltildi: seçenek artık `OnAfterSessionLaunchedEvent`'te ekleniyor (ilk testte en üstte görünüyordu).
- [ ] 2.9 Araç detay penceresi: tablolarda hisse adına tıklanınca fiyat grafiği ve temel değer bileşenleri
- [x] 2.10 Emir paneli: adet girişi, maliyet önizlemesi, onay
  - Slider'lı miktar penceresi + onay penceresi; satırdaki AL/SAT butonlarından açılıyor (ADR-025).
- [x] 2.11 Portföy ve Haberler pencereleri: portföy tablo (Hisseler / Sepetler / Loncalar sekmeleri), Haberler renkli tablo (olay türüne göre renk)
  - 2026-10-20 (ADR-031): Emtia araması, alt solda altın/limit, Portföy butonu ve geri dönüş, Gauntlet alım/satım onayı (satışta K/Z), haberlerde tutsak alınan lordlar ve yer adlarının yanında krallık, en fazla 20 haber, Portföy Performansı grafiği.
  - 2026-10-20: Takas Geçmişim'in sol altında bu oturumdaki işlem sayısı (kaydedilmiyor).
  - 2026-10-20 (ADR-038): sayaç zorunlu satışları da sayıyor; portföyde kısa adlar; lonca ikinci bölümü yukarıda; geniş performans grafiği; muharebe haberinde yer adı yok.
- [x] 2.12 Menü tabanlı arayüzü yedek olarak tut (Gauntlet hata verirse ona düş)
  - Her tablo penceresi açılamazsa vanilla inquiry'ye düşüyor.

### 2B+. Piyasa derinliği
- [x] 2.15 Sınırlı arz, şehrin yatırımcıları (NPC al/sat) ve balina işlemleri (ADR-037)
  - Şema 13. Oyuncuya tutma sınırı yok; hacim sütunu yok. Oyun içi test bekleniyor.
  - Balina sonrası satış rastgele günlerde (günde %40 olasılık).
- [x] 2.16 Kar Al / Zarar Durdur emirleri: portföyde EMİR butonu, Emirlerim penceresi, günlük kontrol (ADR-039)
  - Şema 14. Oyunda test edildi (kar al, zarar durdur, süre dolması). Otomatik Al/Sat sonraki adım.
- [x] 2.17 Emtia 2.0, 1. aşama: yağma payla ölçeklenir, sakin gürültü ve sıçramalar, piyasa haberleri ve söylentiler, dünya stoğu ölçümü (ADR-040)
  - Şema 15. Oyun içi test bekleniyor. Tutsak alma haberleri listeden çıktı (ADR-041).
- [ ] 2.18 Emtia 2.0, 2. aşama: dünya stoğuna dayalı emtia fiyatı ve manipülasyon soruşturması (ADR-040, world_supply.csv verisiyle)
  - Piyasa haberleri fiziksel dünyaya da yansır: üretim haberleri haber etkisi sürdükçe köy ve atölye üretimini değiştirir, talep haberleri şehir pazarlarından mal çeker. Gıdada etki yarım, kuşatma altındaki şehirlere dokunulmaz (kullanıcı onayı, 2026-10-21).
- [ ] 2.19 Otomatik Al/Sat emirleri (kullanıcı detayları verecek)

### 2C. Ayarlar ve yerelleştirme
- [ ] 2.13 MCM entegrasyonu (isteğe bağlı bağımlılık); MCM yoksa `settings.json`
- [ ] 2.14 Tam Türkçe ve İngilizce dil dosyaları

**Kabul kriterleri**
- Lonca temettüleri doğru zamanda ve doğru tutarda ödeniyor.
- Kuşatılan şehrin loncaları sert düşüyor, kuşatma bitince toparlanıyor.
- Gauntlet ekranı tüm işlemleri yapabiliyor, 1080p ve 1440p'de düzgün görünüyor.
- MCM ile ve MCM olmadan çalışıyor.

---

## Faz 3 — Kredi ve risk araçları (≈5–7 hafta)

- [ ] 3.1 Tahvil altyapısı: nominal, kupon, vade, temerrüt olasılığı (§2.5)
- [ ] 3.2 Şehir tahvilleri
- [ ] 3.3 Lord tahvilleri; lord ölümü/esareti/klan yok oluşu ile temerrüt
- [ ] 3.4 Savaş tahvilleri; savaş ilanında ihraç, krallık yok oluşunda temerrüt
- [ ] 3.5 Commenda: kervan listesi, yatırım, 14 günlük sefer, kervan yok oluşu takibi (§2.7)
- [ ] 3.6 Hasat sözleşmeleri: fiyat, vade, şehirde teslim veya nakit kapanış (§2.6)
- [ ] 3.7 Sigorta: kendi kervanını sigortalama ve sigortacı havuzu (§2.8)
- [ ] 3.8 Açığa satış: ödünç ücreti, teminat, zorla kapatma (§6)
  - Kullanıcı istiyor (2026-10-19). ADR-021'deki yumuşatma rampasıyla birlikte risksiz kazanç açmasın: rampa süresince kapalı veya ödünç ücretiyle.
- [ ] 3.9 Simülatöre yeni araçları ve stratejileri ekle
- [ ] 3.10 UI: yeni araç türleri için detay panelleri

**Kabul kriterleri**
- Her araç türü için en az bir uçtan uca oyun testi (örneğin: commenda yatır → kervanı haydutlara kaybet → yatırım sıfır).
- Simülatörde tahvil temerrüt oranları beklenen aralıkta.
- Açığa satışta zorla kapatma doğru çalışıyor ve oyuncu eksi paraya düşmüyor.

---

## Faz 4 — Haber gecikmesi ve oyuncu etkisi (≈4–5 hafta)

- [ ] 4.1 Olay kuyruğu ve şehir bazlı teslim günü (§4.2)
- [ ] 4.2 Yerel fiyat hesabı; işlemler yerel fiyattan
- [ ] 4.3 Haberler sekmesinde olayın şehre ulaşma zamanı
- [ ] 4.4 Oyuncunun tanık olduğu olayları işaretle
- [ ] 4.5 Trade yeteneğine bağlı söylenti ipuçları
- [ ] 4.6 Arbitraj testi: iki uzak şehir arasında fiyat farkı oluşuyor mu, ne kadar sürüyor?
- [ ] 4.7 Manipülasyon tespiti: şüphe puanı, kademeli sonuçlar, ayarlar (GDD §8)
- [ ] 4.8 Borsa kârından Trade XP; limit bonusları (GDD §10). Komisyon indirimi ayrı Simsar seviyesi ve perk ağacına kaldı (ADR-033).
- [ ] 4.9 Performans: 50+ şehir × tüm araçlar × olay kuyruğu için günlük tick süresi ölçümü. Hedef < 5 ms

**Kabul kriterleri**
- Uzak bir yağma, yakın şehirde hemen, uzak şehirde günler sonra fiyatlanıyor.
- Arbitraj kârlı ama sınırsız değil (maliyetler ve süreyle sınırlanıyor).
- Manipülasyon tespiti açıkken bilinçli manipülasyon cezalandırılıyor, normal oyunda yanlış alarm yok.

---

## Faz 5 — Canlı dünya ve yayın (≈4–6 hafta)

- [ ] 5.1 NPC yatırımcılar: lordların basit portföyleri, para çıkışı
- [ ] 5.2 Denge geçişi: 3 farklı oyun stili ile uzun oyun testleri
- [ ] 5.3 Popüler ekonomi modlarıyla uyumluluk testi
- [ ] 5.4 Görsel cilalama: borsa ikonları, menü arka planı, ses efektleri
- [ ] 5.5 Workshop ve Nexus sayfası: açıklama, ekran görüntüleri, kısa video
- [ ] 5.6 Sürüm notları ve sürüm numaralandırma
- [ ] 5.7 Oyun güncellemesi sonrası kontrol listesi (API_NOTES yeniden doğrulama)

**Kabul kriterleri**
- Steam Workshop'ta yayında.
- İlk hafta gelen hata raporları için düzeltme sürümü çıkmış.

---

## Toplam tahmin

| Faz | Süre |
|---|---|
| Faz 0 | ≈1 hafta |
| Faz 1 (MVP) | ≈4–6 hafta |
| Faz 2 | ≈5–7 hafta |
| Faz 3 | ≈5–7 hafta |
| Faz 4 | ≈4–5 hafta |
| Faz 5 | ≈4–6 hafta |
| **Toplam** | **≈6–8 ay** |

Faz 1 sonunda oynanabilir bir sürüm çıkar. İstenirse Faz 1 veya Faz 2 sonrası "erken erişim" olarak Workshop'a konabilir.

## Riskler

| Risk | Olasılık | Etki | Önlem |
|---|---|---|---|
| Şehir fiyatı veya atölye kârı verisine doğrudan erişilemiyor | Orta | Yüksek | Faz 0'da doğrula; yedek tahmin formülleri hazır |
| Gauntlet'te grafik çizmek zor | Yüksek | Orta | Faz 2 başında spike; menü arayüzü yedek kalır |
| Oyun güncellemesi API'yi kırıyor | Orta | Orta | Harmony yok; API_NOTES kontrol listesi |
| Ekonomi parayı şişiriyor | Orta | Yüksek | Simülatör hedefleri, limitler, maliyetler |
| Kayıt uyumsuzluğu | Düşük | Yüksek | Tek JSON, schemaVersion, migration ve round-trip testleri |
