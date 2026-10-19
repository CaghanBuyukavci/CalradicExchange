# Mimari Kararlar (ADR)

Her karar: bağlam, karar, gerekçe. Yeni kararlar sona eklenir. Bir karar değişirse eskisi silinmez, "Yerini aldı: ADR-XXX" notu düşülür.

---

## ADR-001 — Ekonomi mantığı oyundan bağımsız bir Core kütüphanesinde

- **Bağlam:** Oyunu her test için açmak yavaş. Claude Code oyunu oynayamaz.
- **Karar:** Fiyatlama, emir motoru, portföy ve kalıcılık `CalradicExchange.Core` (netstandard2.0) içinde. Oyun modülü sadece veri okur, olayları çevirir ve Core'u çağırır.
- **Gerekçe:** Formüller birim testleriyle ve simülatörle oyun açılmadan test edilir. Oyun güncellemeleri sadece ince adaptör katmanını etkiler.

## ADR-002 — Kayıt verisi tek bir JSON metni olarak saklanır

- **Bağlam:** Bannerlord'un kayıt sistemine özel tip kaydetmek (`SaveableTypeDefiner`) güçlü ama kırılgan. Mod kaldırıldığında veya sınıf değiştiğinde kayıt dosyası açılmayabilir.
- **Karar:** Tüm mod durumu `SyncData` ile tek bir `string` olarak (`"cx_state_v1"`) saklanır. JSON kökünde `schemaVersion` bulunur. Şema değişikliklerinde migration yazılır.
- **Gerekçe:** Mod kaldırılınca oyun sadece bilmediği bir metni yok sayar. Şema değişiklikleri bizim kontrolümüzde.

## ADR-003 — Harmony kullanılmaz (aksi kararlaştırılmadıkça)

- **Bağlam:** Harmony yamaları oyun güncellemelerinde en sık kırılan parça ve diğer modlarla çakışma kaynağı.
- **Karar:** Sadece `CampaignEvents`, menü API'leri ve okuma amaçlı public özellikler kullanılır. Harmony gerekirse önce buraya yeni bir ADR yazılır.
- **Gerekçe:** Uyumluluk ve bakım kolaylığı.

## ADR-004 — Core deterministik, oyun katmanı yüklemede yeniden tohumlar

- **Bağlam:** Tamamen deterministik bir RNG'de oyuncu kaydedip yarının fiyatını görebilir, sonra yükleyip ona göre işlem yapabilir. Tamamen rastgele bir RNG'de ise testler tekrarlanamaz.
- **Karar:** Core, enjekte edilen RNG ile deterministiktir (testler ve simülatör sabit tohum kullanır). Oyun katmanı her kayıt yüklemesinde RNG'yi zamana dayalı entropiyle yeniden tohumlar.
- **Gerekçe:** "Geleceği görüp geri yükleme" istismarı engellenir. Yükleyip fiyatı yeniden zarlamak hâlâ mümkün ama bunun kazancı sınırlı ve her yüklemede gürültü farklı.

## ADR-005 — Fiyatlar günlük güncellenir

- **Bağlam:** Saatlik güncelleme daha canlı görünür ama performans maliyeti ve gürültüsü yüksek.
- **Karar:** Fiyatlar `DailyTickEvent` ile günde bir kez güncellenir. Haber gecikmesi (Faz 4) de gün çözünürlüğünde.
- **Gerekçe:** Bannerlord'da bir gün kısa bir gerçek süre. Günlük çözünürlük oyuncuya yeterince canlı gelir.

## ADR-006 — MVP arayüzü menü ve inquiry pencereleriyle, Gauntlet Faz 2'de

- **Bağlam:** Gauntlet (XML prefab + ViewModel) öğrenmesi zaman alıyor ve grafik çizimi belirsiz.
- **Karar:** Faz 1 oyunun kendi menü sistemi ve inquiry pencereleriyle yapılır. Gauntlet ekranı Faz 2'de eklenir, menü arayüzü yedek olarak kalır.
- **Gerekçe:** Oynanabilir sürüme hızlı ulaşmak; ekonomi dengesini UI işinden önce test etmek.

## ADR-007 — Karşı taraf "kasa", para basımı maliyetler ve limitlerle sınırlanır

- **Bağlam:** Borsada NPC karşı taraf yok; oyuncu kârları oyuna yeni para sokar.
- **Karar:** Fark, komisyon, kayma, kalıcı fiyat etkisi, pozisyon limitleri ve temel değere dönüş birlikte kullanılır. Faz 5'te NPC yatırımcılar para çıkışı olarak eklenir.
- **Gerekçe:** Simülatör hedefleri (ECONOMY_MODEL §8) bu önlemlerin yeterli olup olmadığını ölçer.

## ADR-008 — Takvim uzunlukları oyundan okunur

- **Bağlam:** v1.4.8'de `DefaultCampaignTimeModel` normal oyunda 7 gün/hafta, 3 hafta/mevsim, 4 mevsim/yıl (84 gün) veriyor; `FastMode` modülü açıkken (`GameAccelerationMode.Fast`) 3 gün/hafta, 2 hafta/mevsim (yıl 24 gün).
- **Karar:** Hafta, mevsim ve yıl uzunlukları `CampaignTime.DaysInWeek / DaysInSeason / DaysInYear` ile okunup `WorldSnapshot` üzerinden Core'a verilir. Temettü/kupon "her oyun haftası", tahvil vadesi "1 oyun yılı" gibi takvim birimleriyle tanımlanır. Olay yarı ömürleri gün cinsinden kalır. (Kullanıcı onayı: 2026-10-18)
- **Gerekçe:** FastMode ile oynayanlarda mevsim şokları ve ödeme dönemleri takvimle uyumlu kalır; Core hâlâ oyundan bağımsız.

## ADR-009 — Emtia temel değeri kategori fiyat çarpanından hesaplanır (PLAN 0.6)

- **Bağlam:** Bannerlord'da pazar fiyatı item başına değil `ItemCategory` başına tutulan arz/talep/stok verisinden hesaplanıyor (`TownMarketData`, `DefaultTradeItemPriceFactorModel`). `TownMarketData.GetPrice(ItemObject, null, false, null)` güvenle çağrılabiliyor ama %6 sabit ticaret cezası içeriyor.
- **Karar:** `price_town(c) = item.Value × town.MarketData.GetPriceFactor(item.ItemCategory)`. Saf alış/satış ortası fiyatı, oyuncunun perk'lerinden ve partisinden bağımsız. Hesap ucuz (sözlük okuması + bir `Pow`), günlük tick için uygun.
- **Not:** Aynı kategorideki item'lar aynı çarpanı paylaşır (ör. `iron` kategorisinde demir cevheri ve külçeler). Emtia payları ana item'a (`iron` = Iron Ore) bağlanır.

## ADR-010 — Lonca kârı gerçek veriden okunur (PLAN 0.6)

- **Bağlam:** `Workshop.ProfitMade = max(Capital − InitialCapital, 0)` birikmiş kâr; sahip her gün `ClanFinanceModel.CalculateOwnerIncomeFromWorkshop(workshop)` kadarını çeker. Doğrudan "günlük kâr" alanı yok ama sahibin günlük geliri public bir model metoduyla hesaplanabiliyor.
- **Karar:** Lonca günlük kârı = o şehirdeki o türdeki tüm atölyeler için `Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromWorkshop(w)` toplamı. Mod bunu her gün kendi durumunda saklar ve 28 günlük ortalamayı alır (ECONOMY_MODEL §2.3). Yedek tahmin formülüne gerek yok, ama Faz 2'de değerler sıfıra yakın çıkarsa yedek formül devreye alınabilir.
- **Gerekçe:** Gerçek oyun verisi, Harmony gerektirmez.

## ADR-011 — Tekstil sepetinde `cloth` yerine `felt`

- **Bağlam:** `cloth` kategorisi oyunda ticari mal değil (`isTradeGood: false`) ve karşılık gelen item yok. `cotton` item'ı oyunda "Raw Silk" adıyla görünüyor.
- **Karar:** Tekstil = `flax`, `linen`, `wool`, `felt`, `velvet`, `cotton`. `cotton` arayüzde "Ham İpek / Raw Silk" olarak adlandırılır. (Kullanıcı onayı: 2026-10-18)

## ADR-012 — JSON için oyunla gelen Newtonsoft.Json 13 kullanılır

- **Bağlam:** Oyun `bin\Win64_Shipping_Client\Newtonsoft.Json.dll` (assembly 13.0.0.0, dosya 13.0.1) ile geliyor ve `TaleWorlds.Library` onu kullanıyor.
- **Karar:** Core `Newtonsoft.Json 13.0.1` NuGet paketine referans verir. Oyun modülü bu DLL'i kopyalamaz; çalışma zamanında oyunun kendi kopyası yüklenir.
- **Gerekçe:** Ek DLL dağıtmadan olgun bir serileştirici. Assembly sürümü 13.0.0.0 olduğu için 13.x paketleri bağlanır.

## ADR-013 — Simülatör ayarı: ucuz işlem, güçlü olaylar (PLAN 1.14)

- **Bağlam:** ECONOMY_MODEL 0.1 değerleriyle 8.000 dinarlık bir emir tek yönde %10 kayma ödüyordu (λ=0,5). Olayı takip eden bot yılda -%61 kaybediyor, oynaklık hedeflerin altında kalıyordu (emtia %19, sektör %11). Bir olayın fiyata kattığı ek getiri ≈ dürtü × 0,5; çoğu dürtü (0,03–0,08) gidiş-dönüş maliyetinin altındaydı.
- **Karar (kullanıcı onayı 2026-10-18, "olaylar güçlü" seçeneği):** κ emtia 0,12→0,20, sektör 0,10→0,15; σ emtia 0,020→0,030, sektör 0,012→0,018; kayma λ ÷5; alış-satış farkı yarıya (emtia %1, sektör %0,6). Köy yağmasının emtia dürtüsü (0,06) hariç tüm olay ve mevsim dürtüleri ×1,5.
- **Gerekçe:** Büyük olaylar (savaş, mevsim, kıtlık) bilgili oyuncuya fırsat verir; tek bir köy yağması vermez. Oyuncu yağmaları kendisi yapabildiği için bu aynı zamanda manipülasyonu zayıflatır.
- **Simülatör:** Sentetik dünyada küresel emtia oynaklığı 0,012→0,02. Olay botu emir büyüklüğünü kaymayı ≤%0,5 tutacak şekilde ayarlar (bilgili oyuncu emri böler).

## ADR-014 — Erken oyunda hızlı zenginleşmeye karşı kısıtlar

- **Bağlam:** Kullanıcı: borsa, özellikle oyun başında, oyunu kıracak kadar hızlı para kazandırmamalı.
- **Karar (kullanıcı, 2026-10-18):**
  - Toplam limit = 5.000 + 15.000 × klan seviyesi + 50 × Trade (seviye 0: 5.000, seviye 1: 20.000).
  - Komisyon her alım ve satımda %5; Trade becerisi arttıkça doğrusal azalır, Trade 300'de en az %1. Asgari komisyon 5 dinar.
  - Satışta gerçekleşen kârın %10'u kâr vergisi olarak kesilir; zararda kesinti yok.
  - Yeni simülatör hedefi: klan seviyesi 1, Trade 30 profilinde en iyi botun ilk yıl kârı ortalama günde ≤ 50 dinar.
  - "Olayı gören strateji pozitif" hedefi geç oyun profilinde (klan 5, Trade 250) ölçülür.
- **Sonuç (20 seed, 4 yıl):** Erken oyun en iyi bot ortalama 1,4 dinar/gün (en kötü seed 15,5). Olay botu: erken -%8,2/yıl, orta -%3,8, geç +%0,3. Bilgiyle kâr ancak yüksek Trade becerisiyle mümkün.

## ADR-015 — Dolaylı manipülasyona daha fazla alan; komisyon tabanı %0,5

- **Bağlam:** Kullanıcı: oyuncu dolaylı yollardan (kendi yağmaları gibi) piyasayı etkileyebilmeli; manipülasyon yollarını keşfetmek eğlenceli. Geç oyunda bilgiyle kâr çok düşüktü (+%0,3/yıl).
- **Karar (kullanıcı, 2026-10-18):** Trade 300'deki komisyon tabanı %1 → %0,5. Köy yağmasının emtia dürtüsü 0,06 → 0,075. Korelasyon hedefi 0,7 → 0,65 (sık yağmalanan mallar temel değerin biraz üstünde durur; bu manipülasyon alanının doğal sonucu).
- **Ölçüm:** Simülatöre manipülatör bot eklendi: emtia alır, 4 gün boyunca o malın köylerini kendisi yağmalar, 8. gün satar, 21 günde bir tekrarlar; pozisyon ≤ 12.000 dinar (daha büyüğünde kayma kazancı yer). Döngü başına kâr: erken +49, orta +226, geç +747 dinar. Erken oyun en iyi bot ortalama 31 dinar/gün (sınır 50).
- **Not:** Piyasa derinliği (kayma) büyük pozisyonlarla manipülasyonu kendiliğinden frenler. Faz 4 şüphe puanı bu dengenin üstüne gelecek.

## ADR-016 — Türkçe terimler: "Hisse" ve "Komisyon"

- **Karar (kullanıcı, 2026-10-18):** Arayüzde "pay" yerine "hisse", "sarraf hakkı" yerine "komisyon" kullanılır (GDD §6.10 güncellendi). "Sarraf" kelimesi yalnızca betimleyici metinlerde (borsadaki para bozucular) kalır.

## ADR-017 — Temel değer pazar hacmiyle ağırlıklandırılır; eski kayıtlar yeniden kurulur

- **Bağlam:** Gerçek oyun verisi (`market_raw.csv`, 10 gün, 53 şehir): bir malın stoğu ve arzı olmayan şehirlerde oyun fiyat çarpanını çok yüksek gösteriyor (kadife: şehirlerin %87'si stoksuz, çarpan ~5; stoklu şehirlerde 1,45). Refah ağırlıklı ortalama işlenmiş malları taban değerin 5–7 katında gösteriyordu. Sadece stoklu şehirleri almak ise en oynak yöntemdi (günlük %7).
- **Karar:** Ağırlık = günlük arz + stok (ECONOMY_MODEL §2.1'deki "pazar hacmi" alternatifi). Hacmi olmayan şehir katılmaz; hiçbir şehirde hacim yoksa refah ağırlığına düşülür. Kadife 4,2× → 1,9×, zeytinyağı 5,0× → 2,1×; günlük temel değer oynaklığı %4,0 (en düşüklerden).
- **Geçiş:** `MarketState.FundamentalMethod` alanı eklendi (şema 3). Kayıtlı yöntem farklıysa piyasa yeniden kurulur; açık pozisyonlar o anki piyasa fiyatından masrafsız ve vergisiz geri alınır, oyuncuya mesaj gösterilir.

## ADR-018 — Kıtlık şoku şehrin refah payıyla ölçeklenir, 21 gün bekleme

- **Bağlam:** Gerçek oyunda yiyecek stoğu sıfırlanan şehir çok yaygın (10 günde 1/53 → 12/53). Her biri tarımı +0,12 ittiği için tarım sepeti 5 günde %60 şişti.
- **Karar (kullanıcı, 2026-10-18, "şehir payıyla ölçekle"):** Tarım dürtüsü = 0,12 × şehrin toplam refahtaki payı × 2. Şehirlerin yarısı (refaha göre) aç kalınca toplam etki tam şoka ulaşır. Aynı şehir 21 gün içinde yeni kıtlık başlatamaz. Şehrin loncalarına etki (Faz 2) ölçeklenmez.

## ADR-019 — Beklenen şok tabanı: sadece sürprizler fiyatı oynatır

- **Bağlam:** Sık gelen olaylar S'yi kalıcı pozitif bir seviyeye taşıyor. Borsa S=0 ile açıldığı için ilk aylarda tarım fiyatları bu seviyeye tırmanıyor; erken alan herkes kazanıyor (simülatörde al-tut ilk yıl günde +15, manipülatör +51 dinar). Mod oyuna sonradan eklendiğinde de aynısı olur.
- **Karar:** Her araç için S_ham'ın 42 gün yarı ömürlü hareketli ortalaması B tutulur, fiyat S = S_ham − B'ye göre hareket eder. İlk olay tam etkili; aynı olay sürekli tekrarlanırsa etkisi söner; uzun sessizlikten sonra yeniden haber değeri kazanır. `settings.json`: `shockBaselineHalfLifeDays` (0 = kapalı).
- **Sonuç (20 seed, 4 yıl, gerçek veriye yaklaştırılmış sentetik dünya):** 8/8 hedef geçiyor. Erken oyun en iyi bot günde 36,7 dinar (en kötü seed 69,8). Al-tut -%0,1. Olay takipçisi geç oyunda +%0,6/yıl. Manipülatör döngü başına: erken +190, orta +384, geç +907 dinar.
- **Simülatör kalibrasyonu:** Şehir başına günlük kıtlık olasılığı 1/40 (gerçek veri), hacim ağırlığı (üretici şehirler ağır), daha yüksek yerel/küresel fiyat gürültüsü. Temel değer günlük oynaklığı simülatörde %2,7, gerçek oyunda %4,0 (10 günlük, yaz ve kıtlık ağırlıklı örnek); uzun veriyle tekrar kontrol edilecek.

## ADR-020 — Piyasa ve portföy tabloları için Gauntlet penceresi (Faz 2.6 öne çekildi)

- **Bağlam:** Kullanıcı tabloların okunaklı olmasını, artışların yeşil, azalışların kırmızı yazılmasını ve sütunların hizalı olmasını istedi. Vanilla inquiry metni `Popup.Description.Text` fırçasında yalnızca `Default` stili var (renk yok), yazı tipi orantılı ve sekme durağı yok (hizalama mümkün değil).
- **Karar:** Kendi Gauntlet prefablarımız (`GUI/Prefabs/CxMarketTable.xml`, `CxPortfolioTable.xml`) ve `ExchangeTableVM`. Sütunlar sabit genişlikli, sayılar sağa hizalı, renk `Brush.FontColor="@..."` bağlamasıyla. Vanilla fırçaları ve sprite'ları kullanılır; kendi fırçamız yok. Katman açılamazsa eski metin penceresine düşülür (ADR-006).
- **Ek (2026-10-19, kullanıcı isteği):** Al/Sat adet penceresi de aynı yolla yapıldı: `CxQuantityPopup.xml` + `QuantityVM`. Slider (`SliderWidget`) ve sayı kutusu (`IntegerInputTextWidget`) aynı `Quantity` özelliğine bağlı; ikisi de v1.4.8 vanilla prefablarında (pazarlık ekranı) kullanılıyor. Sprite'lar `ui_group1` kategorisinde (vanilla metin penceresiyle aynı), haritada yüklü.

## ADR-021 — Kampanya başında temel değer yumuşatılır (ADR-022 ile kaldırıldı)

- **Bağlam:** Yeni kampanyada şehir depoları boş başlıyor, işlenmiş mallar normalin 5–6 katına satılıyor. Gerçek oyun verisi (`market_raw.csv`, borsa günü 6–40): depolar doldukça temel değer düşüyor; kadife −%63, keten kumaş −%62, şarap −%61, alet −%57, tahıl −%48; 12 emtianın ortalaması −%25. Borsa bunu doğru yansıtıyordu ama oyuncu ilk 40 günde neredeyse her gün kırmızı gördü ve para kaybetti (−599 dinar, ~220'si komisyon).
- **Karar (kullanıcı, 2026-10-19, "temel değeri yumuşat"; borsa ilk günden açık kalacak):** Fiyatın çekildiği F, gözlenen temel değerin log-üstel ortalamasıdır. Yarı ömür piyasa açıldığında 45 gün, 90 günde doğrusal olarak 0'a iner (yumuşatma kapanır). Gözlenen değer `PriceState.RawFundamental` alanında tutulur. `settings.json`: `fundamentalStartHalfLifeDays` 45, `fundamentalRampDays` 90, `fundamentalHalfLifeDays` 0 (rampadan sonraki yarı ömür).
- **Neden oturmuş oyunda yumuşatma yok:** Simülatörde 3 günlük yarı ömür bile manipülatörün erken oyun döngü kârını +190'dan −8 dinara düşürdü (yağmanın arz etkisi gecikiyor, komisyon kazancı yiyor). 14 gün korelasyon hedefini bozdu (0,63). Kullanıcı manipülasyon yollarının açık kalmasını istemişti (ADR-015).
- **Sonuç:** Gerçek veride ilk 35 gün: ortalama −%25 → −%7, en kötü emtia −%62 → −%28, en kötü gün −%9,3 → −%2,2. Simülatör (20 seed, 4 yıl): 8/8 hedef; erken oyun en iyi bot günde 17,7 dinar; manipülatör döngü başına erken +88, orta +270, geç +790 dinar (rampanın olduğu ilk 90 gün kârı düşürüyor).
- **Bilinen sınır:** Düşüş kaybolmuyor, rampa boyunca (yaklaşık 2–4. ay) yayılıyor. Kampanya ortasında kurulan modda da rampa çalışır; piyasa zaten oturmuşsa etkisi küçüktür.
- **Açığa satışla etkileşim (Faz 3.8):** Yumuşatılmış F gerçek fiyatın gerisinden gelir. Açığa satış varken kampanya başında "aç, bekle, kapat" neredeyse risksiz kazanç olur. Açığa satış eklenirken ödünç ücreti, ayrı limit ve rampa süresince kapalı tutma değerlendirilecek.
- **Kayıt:** Şema 4: `market.startDay`, `prices[].rawFundamental`. Göç: başlangıç günü en eski geçmiş noktasından alınır, ham değer son F'den kopyalanır. Yeniden kurulum gerekmez.

## ADR-022 — Taban + göreli ekonomi sinyali; haberler 2 kat, komisyon en fazla %2,5

- **Bağlam (2026-10-19 testi, yeni kampanya, 35 gün):** Yeni kampanyada şehirlerin başlangıç stoğu ilk 10 günde tükeniyor. İşlenmiş mallar 2–3 katına çıkıp sonra düşüyor (kadife 3.256 → 8.215 → 6.620, alet 1.693 → 4.105 → 1.756). Borsa bunu birebir izledi: günlerin %96'sı yeşil (gün 1–11), sonra %38 ve %25. Kullanıcı şunu istedi: hem oyun içi olaylardan etkilenen hem dengeli yeşil/kırmızı gösteren, bilen oyuncunun az da olsa kazandığı, şans payı olan bir borsa. "Gerekirse gerçek ekonominin ağırlığını azaltalım, ama haberleri takip etmek anlamlı olmalı." Hurma yağma haberinden sonra alınan hisse zarar etti. Yağma şokunun etkisi %7,5, erken oyunda bir alıp satmanın komisyonu ise ~%9'du. ADR-021 yumuşatması bu testte oyuna ulaşmamıştı (deploy hatası: eski `bin\x64` çıktısı kopyalanıyordu; düzeltildi, deploy artık DLL özetini doğruluyor).
- **Karar (kullanıcı, 2026-10-19: "yeni model + yavaş gerçek bağ", "ikisi birden"):**
  - **Fiyat modeli:** F = taban × e^E. `PriceState.Anchor` ve `EconomySignal` alanları eklendi.
  - **Ekonomi sinyali E:** Her gün malın şehir fiyatı değişimi, kendi sınıfının (emtia veya sektör) medyan değişimi çıkarılarak alınır, 0,6 ile çarpılır ve E'ye eklenir. E 14 günlük yarı ömürle söner ve ±0,3 ile sınırlıdır. Tek günlük gözlem ±0,15 ile kırpılır. Bütün ekonominin birlikte inip çıkması (başlangıç dalgası gibi) E'ye girmez.
  - **Yavaş gerçek bağ:** Taban, şehir fiyatına 120 günlük yarı ömürle kayar.
  - **ADR-021 yumuşatması kaldırıldı.** `settings.json`: `economyWeight` (0 = eski doğrudan mod, testler kullanır), `economyHalfLifeDays`, `economyMaxAbs`, `economyMaxDailyMove`, `anchorHalfLifeDays`.
  - **Haber şokları ×2** (yağma 0,075 → 0,15, savaş → askeri +0,15, kıtlık 0,24 …). Mevsim dürtüleri değişmedi, çünkü önceden bilinebiliyorlar.
  - **Komisyon:** En yüksek oran %5 → %2,5. Taban oran %0,5 (Tüccar 300) ve asgari 5 → 2 dinar. Oran her işlemde karakterin o anki Tüccar becerisinden hesaplanır; yüksek Tüccar ile başlayan karakter ilk işlemden itibaren düşük öder (örneğin Tüccar 60: %2,1). Log her işlemde beceriyi ve oranı yazar.
  - **Yağma birikme sınırı:** Aynı kuraldan bir araca gelen canlı şoklar dürtü × `maxStack` değerini geçemez. Yağmada `maxStack` 4. `ShockComponent.Source` alanı eklendi. Sınır olmadan kendi köylerini yağmalayan manipülatör erken oyunda günde 123 dinar kazanıyordu.
  - **Simülatör hedefi değişti:** "Temel değerle korelasyon" yerine "hedef F × e^S ile korelasyon" (> 0,65). Yeni modelde yönü haberler belirlediği için F tek başına fiyatın izlemesi gereken değer değil.
- **Sonuç (20 seed, 4 yıl):** 8/8 hedef geçiyor.
  - Haber takipçisi yıllık erken +%0,5, orta +%0,5, geç +%1,1 (eskiden erken −%4,5, orta −%2,3). Olay başına geç oyunda: savaş +146, mevsim +138 dinar.
  - Manipülatör döngü başına: erken +123, orta +175, geç +404 dinar.
  - Erken oyun en iyi bot günde 40,8 dinar (sınır 50). Al-tut +%0,5, rastgele −%12,9.
  - Gerçek veri (35 gün, gürültü ve şoklar hariç): yeşil gün oranı dönemlere göre %53 / %62 / %62. 35 günlük değişim ortalama +%9, aralık −%14 … +%46 (eskiden +%40, −%29 … +%175).
- **Bedeli:** Hisse fiyatı artık "10 birim malın şehirdeki fiyatı" değil; ona sadece yavaşça yaklaşan bir endeks.
- **Kayıt:** Şema 5: `prices[].anchor`, `prices[].economySignal`, `shocks[].source`. Göç: taban mevcut F'den başlar, sinyal 0.
