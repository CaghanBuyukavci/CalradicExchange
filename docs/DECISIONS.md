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
- **Oyun testi (2026-10-19, yeni kampanya, 43 gün, Tüccar 30–34):**
  - Yeşil gün oranı dönemlere göre %58 / %54 / %55 / %55; her gün 17 hissenin 5–15'i yeşil kapandı.
  - Oyuncu satılan işlemlerden +375 dinar gerçekleşmiş kâr etti (8 işlemin 6'sı kârlı; komisyon 535, vergi 47). Haberle yapılan 4 işlemin 3'ü kazandı.
  - Komisyon her işlemde Tüccar becerisinden doğru hesaplandı (30 → %2,30, 33 → %2,28, 34 → %2,27). Oturum açılışında log Tüccar 0 yazar, çünkü yeni kampanyada karakter henüz oluşturulmamıştır.
  - Slider'lı miktar penceresi görünüyor ve çalışıyor (kullanıcı onayı).
  - Hurma yağma haberi yine zarar etti. Sebep: yaz şokunun sona ermesiyle fiyat hedefin üstünde kalmıştı; yağma sadece +%5 getirdi, bu da alıp satma maliyetinin (~%5,6) altında kaldı. Bu bir hata değil, şans payı sayıldı.
  - **İzlenecek:** Hisseler 43 günde ortalama +%15 yükseldi. Yaklaşık yarısı geçici şoklardan (kış başlangıcı, savaşlar), kalanı tabanın kampanya başındaki fiyat yükselişini izlemesinden geliyor. Gerçek fiyatlar çoğu malda tabanın altında, bu yüzden taban geri inmeli. Uzun oyunda al-tut stratejisi hâlâ bu kadar kazandırıyorsa `anchorHalfLifeDays` veya mevsim dürtüleri ayarlanacak.
  - Kayıttan borsa durumu okunabiliyor: `.sav` dosyası 4 baytlık başlık uzunluğu + JSON başlık + ham deflate gövdeden oluşuyor. Durum JSON'u `"schemaVersion"` aranarak bulunur.

## ADR-023 — Borsa menüsü arka planı çalışma anında yüklenen doku ile

- **Bağlam:** Kullanıcı "Borsaya gir"den sonra açılan menünün arka planı olarak kendi görselini (`cx_townBG.png`, 932×1688, vanilla 445×805 arka planlarla aynı oran) istedi.
- **Karar:** Görsel `GUI/Textures/cx_exchange_bg.png` olarak dağıtılır. Menü ilk açıldığında `Texture.LoadTextureFromPath` (olmazsa `CreateFromMemory`) ile motor dokusu yüklenir, tüm dokuyu gösteren bir `Sprite` alt sınıfı (`TextureSprite`) `UIResourceManager.SpriteData.Sprites`'a `cx_exchange_bg` adıyla eklenir ve menü init'i `SetBackgroundMeshName("cx_exchange_bg")` çağırır.
- **Neden:** Sprite sheet/TPAC üretmek Modding Kit gerektirir. Harmony gerekmez. Yükleme başarısız olursa vanilla arka plan kalır ve log'a yazılır.
- **Test (2026-10-19):** Doku yüklendi (log: `932x1688, loaded from path`), ama görsel her saatte mavi göründü. `LoadTextureFromPath` PNG'nin kırmızı ve mavi kanallarını takas ediyor (sıcak turuncu → mavi). Orijinal görsel `Assets/Textures/` altında duruyor; `deploy.ps1` bunu kırmızı/mavi önceden takas edilmiş olarak oyunun `GUI/Textures/` klasörüne yazıyor.
- **İkinci test (2026-10-19):** Önceden takas edilmiş kopya da mavi göründü. Menü widget'ı gündüz beyaz renkte (ton eklemiyor) ve oyun klasöründe önbellek kopyası yok. Fark dosya formatındaydı: ilk dosya 24-bit RGB, takas edilmiş kopya 32-bit RGBA PNG'ydi. Yeni varsayım: motor sadece 24-bit PNG'de kanalları takas ediyor. `deploy.ps1` artık görseli takassız 32-bit RGBA olarak yazıyor (`$swapRedBlue = $false`).
- **Üçüncü test (2026-10-19):** Takassız 32-bit RGBA görsel doğru renklerde göründü (kullanıcı onayı). Kural: çalışma anında yüklenen her PNG 32-bit RGBA olmalı; deploy bunu otomatik yapıyor.
- **Buton görselleri (2026-10-19):** Piyasa tablolarındaki AL/SAT butonları için `Assets/Textures/cx_btn_{buy,sell}[_hover].png` (53×25) eklendi. Yükleyici genelleştirildi (`UI/RuntimeSprites`). Brush'lar sprite'larını oyun açılışında ada göre çözdüğü için, `UI/ButtonSkins` görselleri ilk piyasa tablosu açılırken `Cx.RowButton.Buy/Sell` brush'larının Default katmanına ve Default/Hovered/Pressed/Disabled stillerine atıyor. Görsel yüklenemezse düz renkli kare kalır. Butonlar 64×30 (görselle aynı oran).

## ADR-024 — İşlem geçmişi kayda yazılır; seçim tablosunda "Devam" yok

- **Bağlam:** Kullanıcı borsa menüsünün sonuna bütün işlemlerini listeleyen bir "Takas Geçmişim" penceresi istedi. Ayrıca alım/satımda hisse seçince "Devam"a basmadan miktar adımına geçilmesini istedi. O zamana kadar tek tek işlemler sadece `exchange.log`'daydı; kayıtta sadece toplamlar (gerçekleşen kâr, komisyon, vergi, işlem sayısı) vardı.
- **Karar:**
  - `Portfolio.History` (`TradeRecord` listesi) şema 6 ile kayda eklendi. Her kayıtta şunlar tutulur: oyun günü, hisse, yön, adet, gerçekleşen fiyat, tutar, komisyon, vergi, gerçekleşen kâr. Liste `trading.tradeHistoryLimit` (varsayılan 500) ile sınırlı; en eskiler düşer.
  - Şema 5 kayıtları boş geçmişle açılır. Tablonun altında "Daha eski N işlem bu listede yok" yazar (N = `TradeCount` − listedeki işlem sayısı).
  - Vanilla çoklu seçim penceresinin seçim anında çağrılan bir geri çağrısı yok. Bu yüzden alım/satım seçimi kendi Gauntlet tablomuzla (`CxPickTable`) yapılıyor; satıra tıklamak doğrudan miktar penceresini açar. Pencere açılamazsa eski seçim penceresine düşülür (ADR-006).
  - Takas Geçmişim: `CxHistoryTable`, en yeni işlem üstte. Sütunlar: Tarih, İşlem, Hisse, Adet, Fiyat, Tutar (altın akışı, işaretli), Masraf (komisyon + vergi), Kâr/zarar (sadece satışlarda).
- **Bedel:** Seçim tablosunda vanilla penceredeki eşya ikonları yok. Karşılığında bugün/7 gün değişimi (alımda) ve kâr/zarar (satımda) sütunları var.

## ADR-025 — Emtia ve Sepet piyasası; her satırda AL / SAT

- **Bağlam (2026-10-19):** Kullanıcı borsa menüsünden "Hisse al" ve "Hisse sat"ın kaldırılmasını istedi. Yerine "Emtia Piyasası" (sadece emtia hisseleri) ve "Sepet Piyasası" (sadece sepetler) gelecek. Her satırda kendi AL ve SAT butonu olacak; SAT sadece elde o hisse varsa görünecek. Butonlar doğrudan miktar penceresini açacak. Kullanıcı bunu bir taslak görselle (`piyasa.png`, depoya alınmadı) gösterdi.
- **Karar:** Yeni tablo `CxTradeTable` (sütunlar: Hisse, Fiyat, Bugün, 7 gün, Elde, AL, SAT). Butonlar `GUI/Brushes/CxBrushes.xml`'deki `Cx.RowButton.Buy/Sell` brush'larını kullanır (yeşil/kırmızı, hover ve basılı durumları var). İşlem bitince veya miktar/onay adımında vazgeçilince aynı piyasa tablosu yeniden açılır; böylece art arda işlem yapılabilir. Fiyatı olmayan veya kapanmış araçta AL gizlenir. ADR-024'teki seçim tablosu (`CxPickTable`) kaldırıldı. Tablo açılamazsa yedek akış devreye girer: vanilla seçim penceresi, ardından elde varsa Al/Sat sorusu.
- **Kullanıcıya sorulmadan eklenen:** "Elde" sütunu (eldeki adet). Bu sütun SAT butonunun neden görünüp görünmediğini açıklıyor. "Piyasaya bak" menüde genel bakış olarak kaldı.

## ADR-026 — Faz 2 kapsamı: loncalar yerel alınır, her yerde satılır; tam ekran yerine grafik penceresi

- **Bağlam (2026-10-19):** Faz 2 planlanırken iki soru açıktı. Birincisi: yaklaşık 106 lonca (53 şehir × en kârlı 2 lonca) tek listede çok kalabalık. İkincisi: tablo pencereleri iyi çalışıyor; tam ekran borsa ekranına gerek var mı?
- **Karar (kullanıcı):**
  - **Yerel al, her yerde sat.** Lonca Piyasası bulunulan şehrin loncalarını listeler ve sadece onlar alınabilir. Oyuncunun başka şehirlerdeki lonca hisseleri de aynı tabloda ayrı bir bölümde, sadece SAT butonuyla görünür.
  - **Tablolar + grafik penceresi.** Tam ekran borsa ekranı yapılmaz. Tablolarda hisse adına tıklanınca fiyat grafiği ve temel değer bileşenlerini gösteren bir detay penceresi açılır. 2.8–2.11 bu yöne göre yeniden yazıldı.
- **Gerekçe:** Kısa liste ve seyahate anlam katması; çalışan ve kullanıcının beğendiği tablo arayüzünü korumak.

## ADR-027 — Lonca "ortaklık payı": kâra ve zarara ortaklık, %25/yıl, göreli fiyat; kâr kasa değişiminden ölçülür

- **Bağlam (2026-10-19, 2.1 verisi, 58 gün):**
  - Haritada 152 lonca var: 46 şehirde 3'er, 7 şehirde 2'şer. Bunların 145'inde tek atölye var.
  - Sahibin günlük geliri (ADR-010 ölçüsü) ortanca 174 dinar/gün, %10–%90 aralığı 61–297. Günlük oynaklık yaklaşık %39.
  - Kampanya başında ortanca 290'dan 175'e düştü.
  - Bu ölçü zararı gösteremiyor: sahip sadece birikmiş kârdan pay çekiyor, zarar eden atölye 0 görünüyor (58 günde 290 sıfır gün).
- **Karar (kullanıcı):**
  - Lonca araçlarına "hisse" değil **"ortaklık payı"** denir.
  - **Kâra ve zarara ortaklık:** Her 7 günde bir, loncanın o haftaki net kârının ya da zararının tamamı, elde tutulan pay oranında (pay / toplam pay) oyuncuya yansır. Zarar haftasında oyuncu öder.
  - **Getiri hedefi:** Payın fiyatı, beklenen dağıtımın yılda yaklaşık %25'i olacak şekilde kurulur (yıl = 84 gün).
  - **Fiyat:** Göreli model (ADR-022 gibi). Taban, lonca açıldığındaki kârdan kurulur. Fiyat, loncanın kârının bütün loncaların ortanca hareketine göre ne yaptığını izler. Kampanya başındaki genel düşüş fiyatları kırmızıya boyamaz, ama dağıtıma gerçek kâr olarak yansır.
- **Teknik karar:**
  - Net kâr = günlük kasa değişimi + sahibin o gün çektiği para. Çekilen tutar yerine bir önceki okumadaki `CalculateOwnerIncomeFromWorkshop` kullanılır. Eksi olabilir.
  - İlk okumada (yüklemeden sonra) ve sahip ya da tür değişince kasa sıfırlandığı için o atölye o gün ölçülmez.
  - Ölçüm doğrulaması için günlük log'a bütün atölyelerin net kâr toplamı ile sahip geliri toplamı birlikte yazılır. Uzun vadede ikisi yakın olmalı.
  - Şema 8: eski ölçüyle toplanan seriler silinir, 28 günde yeniden dolar. ADR-010'un "sahip geliri = lonca kârı" kısmının yerine bu geçer.
- **Zarar ödemesi, altın yetmezse (kullanıcı, 2026-10-19):**
  1. Önce oyuncunun altınından alınır.
  2. Eksik kalırsa, zararı yazan loncanın payları piyasa fiyatından, komisyonsuz ve vergisiz satılır; borç bu satıştan ödenir. Satılan paylar Takas Geçmişim'e "zorunlu satış" olarak yazılır.
  3. O da yetmezse kalan tutar borç olarak kayda yazılır. Borç, oyuncunun bir sonraki herhangi bir lonca kâr dağıtımından düşülür.
  - Başka loncaların payları ve emtia/sepet hisseleri bu borç için satılmaz.
  - Borç portföy penceresinde ve borsa menüsü metninde görünür.
  - Borç lonca bazında tutulur; hangi loncanın zararından doğduğu bilinir.
- **Borç cezası (kullanıcı, 2026-10-19):** Bir loncaya bekleyen borç varken her hafta (dağıtım günü), o loncanın şehrinin bağlı olduğu krallığın hükümdarıyla oyuncunun ilişkisi 2 azalır.
  - Uygulama: `ChangeRelationAction.ApplyPlayerRelation(ruler, -2, showQuickNotification: false)`.
  - Uyarı: ekranın üstünden çıkan panel, `MBInformationManager.AddQuickInformation(metin, 0, ruler.CharacterObject)`. Metinde kişi, neden (hangi loncaya borç) ve miktar yazar.
  - Şehir bir krallığa bağlı değilse, şehrin sahibi klanın lideri kullanılır.
- **Varsayılan ölçek (2.2'de simülatörle kontrol edilecek):**
  - Her lonca 100 pay.
  - Örnek: ortanca 174 dinar/gün kârda pay fiyatı ≈ 174 × 84 / 0,25 / 100 ≈ 585 dinar. Haftalık dağıtım pay başına ≈ 12 dinar (%2/hafta).

## ADR-028 — Kayıttaki durum sıkıştırılıp 32 KB altı parçalara bölünür

- **Bağlam (2026-10-19):** Kullanıcının kaydı "Kayıtlı oyunu yüklerken bir sorun oluştu" hatasıyla açılmadı. Oyun log'unda: "Load Strings" adımından sonra "Dizi boyutları desteklenen aralığı aştı" ve "Could not load the game!". Sebep: Bannerlord kayıttaki her metnin boyutunu 16 bitlik bir sayıya yazıyor (`SaveContext.SaveStringTo`: `WriteShort((short)(4 + utf8Bytes))`). Modun durum JSON'u lonca serileriyle 121.000 karaktere çıkmıştı; boyut taştı ve metin bölümü bozuldu. Dünkü `save008` da aynı sebeple bozuktu: 51.837 karakter, uzun fiyat geçmişi.
- **Karar:**
  - `Core/Persistence/SaveChunks`: JSON deflate ile sıkıştırılır, base64'e çevrilir ve en fazla 24.000 karakterlik parçalara bölünür. Parça biçimi `CXZ1|sıra|toplam|veri`. Parçalar `cx_state_v1`, `cx_state_v1_1`, ... anahtarlarıyla kaydedilir.
  - `{` ile başlayan ilk parça eski düz JSON kabul edilir, bu yüzden eski kayıtlar yine okunur.
  - Eksik ya da bozuk parçada kampanya bozulmaz; borsa sıfırdan başlar ve log'a hata yazılır.
- **Kurtarma:** `tools/repair_save.py` bozuk kaydı açıyor; durumu (lonca serileri çıkarılarak) tek sıkıştırılmış parça hâlinde yazıyor, boyut alanlarını düzeltip `*_onarildi.sav` olarak kaydediyor. Orijinaller `Game Saves/cx_backup/` klasöründe. `save009`, `saveauto2` ve `save008` onarıldı; durumlarının mod kodu (net472) ile okunduğu doğrulandı.

## ADR-029 — Lonca ortaklık payının uygulanışı (PLAN 2.2–2.3a)

- **Dinamik araçlar:** Lonca araçları `instruments.json`'da değil; `MarketState.Guilds` serilerinden her gün yeniden kurulur. Kimlik: `cx_guild_<şehir>_<tür>`.
  - Fiyat motoru önce katalogu (dosya sırasıyla), sonra loncaları (kimlik sırasıyla) işler.
  - Bir lonca listelendiğinde sonraki günlerin rastgele sayı sırası değişir. Bu sadece gürültünün hangi sayıyı aldığını etkiler; determinizm korunur.
  - Atölyesiz sentetik dünyada (simülatör) çıktı önceki sürümle bayt bayt aynı.
- **Listeleme:** En az 7 günlük kâr verisi (`minListingDays`) gerekir. Yeni kampanyada loncalar 7. günde açılır.
- **Fiyat:** Gözlenen değer = max(28 günlük ortalama, 10 dinar/gün) × 84 / 0,25 / 100. Alt sınır, 28 günlük ortalaması eksi olan 14 loncaya yine de küçük ama pozitif bir fiyat verir. Üstüne ADR-022 göreli modeli uygulanır, sınıf olarak bütün loncalarla. Olay kuralları (kuşatma −0,45 ve kuşatma bitene kadar tutulur, el değiştirme −0,15, kıtlık −0,18) `guilds:eventTown` ile zaten tanımlıydı.
- **Hak ediş pay-gün üzerinden:** Günlük kâr × eldeki pay / 100, her gün `Portfolio.GuildAccruals`'a eklenir. Kapanış 7 günde bir yapılır (borsa günü % 7 = 0). Böylece dağıtımdan bir gün önce alıp haftanın kârını almak ya da zarar haftasından önce satıp kaçmak işe yaramaz.
- **İşlem kuralları:** Alım sadece loncanın şehrinde yapılır (`NotInGuildTown`). Bir loncadan en fazla 100 pay tutulabilir (`GuildSharesExceeded`). Satış her yerde yapılabilir. Portföy penceresinde AL butonu sadece o şehrin loncalarında görünür.
- **Kapanma ve yeniden açılma (2.3):** Şehirde o türden atölye kalmazsa araç kapanır. Paylar son fiyattan, masrafsız geri alınır ve Takas Geçmişim'e "Zorunlu satış" olarak yazılır. Birikmiş hak ediş bir sonraki kapanışta ödenir. Atölye geri gelirse lonca yeni bir fiyatla yeniden açılır.
- **Lonca Piyasası (2.3a):**
  - Sütunlar: Lonca, Fiyat, 7 gün, Hafta kârı/pay, Elde.
  - Önce "{şehir} loncaları" bölümü (AL/SAT), sonra "Başka şehirlerdeki payların" bölümü (sadece SAT).
  - Elde pay varken lonca büyük hareket ederse ekrana bildirim düşer. Elde olmayan loncalar için bildirim yok, çünkü yaklaşık 150 lonca var.
- **Şema 9:** `portfolio.guildAccruals`, `guildDebts`, `guildIncomeTotal`, `history[].forced` ve `guilds[].lastProfitDay` eklendi. Hepsinin varsayılanı var, bu yüzden geçiş adımı boş.


## ADR-030 — Krallık endeksleri (PLAN 2.4)

- **Kullanıcı kararları (2026-10-19):**
  - Endeksler her şehirde alınıp satılır.
  - Açığa satış şimdilik yok. Teminat, ödünç ücreti ve zorla kapatma ile birlikte sonra, bütün sınıflara birden eklenecek.
  - Yıkılan krallığın endeksi sıfırlanır; elde tutulan paylar değersiz kalır.
  - Bütün krallıklar listelenir, oyuncunun kurduğu krallık da.
- **Dinamik araçlar:** Loncalar gibi `instruments.json`'da değil. `MarketState.Kingdoms` kayıtlarından kurulur (şema 10). Kimlik: `cx_kingdom_<krallık>`. Fiyat motoru katalog → loncalar → krallıklar sırasıyla işler.
- **Listeleme:** Takip başladığında yaşayan krallıklar hemen listelenir. Sonradan kurulanlar (isyan, oyuncu) 7 gün (`minListingDays`) bekler.
- **Temel değer:** ECONOMY_MODEL 2.4 formülü. Refah ve fief sayısı şehirler ve kaleler (`Kingdom.Fiefs`) üzerinden. Taban: krallığın ilk görüldüğü günün değerleri.
  - Fiefsiz krallık için alt sınırlar var (refah en az 100, fief en az 0,25), böylece endeks pozitif kalır.
  - Üstüne ADR-022 göreli modeli uygulanır, sınıf olarak bütün krallıklarla. Bütün Calradia'nın birlikte zenginleşmesi endeksleri yükseltmez; krallıklar birbirine göre hareket eder.
- **Olaylar:** settings.json'daki kurallar zaten vardı: yağma −0,06, el değiştirme kaybeden −0,24 / kazanan +0,15, savaş ilanı iki taraf −0,06 ve oynaklık ×1,5.
- **Yıkılış:** `KingdomDestroyed` olayı ya da günlük okumada krallığın yaşayanlar arasında olmaması endeksi kapatır (fiyat 0, bir daha açılmaz). Oyun katmanı elde tutulan payları sıfır fiyattan "Zorunlu satış" olarak kapatır; kayıp gerçekleşmiş zarara ve Takas Geçmişim'e yazılır.
- **Okuyucu hatası koruması:** Hiç krallık içermeyen anlık görüntü (okuma hatası, simülatör) hiçbir şeyi kapatmaz. Simülatör çıktısı bu yüzden değişmedi; 8 hedefin hepsi geçiyor.
- **Arayüz:** Menüde "Krallık Piyasası" (Emtia/Sepet tablosuyla aynı düzen), portföyde "Krallıklar" sekmesi. Günlük log'a her krallık için fiyat, temel değer, fief, refah, düşman ve kuşatma sayısı yazılır (ayar turu için).

## ADR-031 — Arayüz turu: arama, Portföy kısayolu, onay listesi, tutsak haberleri, performans grafiği (2026-10-20)

- **Kullanıcı istekleri (2026-10-20, save10 testinden sonra):**
  - Emtia Piyasası'nda sağ üstte arama kutusu; satırlarda "hissesi" eki yok.
  - Altın ve kalan limit bütün pencerelerde sol altta.
  - Sepet Piyasası'nın ilk sütunu "Sepet".
  - Portföy dışındaki pencerelerde Kapat'ın sağında Portföy butonu. Portföy kapanınca önceki pencere geri gelir.
  - "Elde" sütunu ortalı.
  - Haberlere tutsak alınma olayları kendi renginde eklenir. Yer adlarının yanında parantez içinde krallık yazılır. En fazla 20 haber tutulur.
  - Alım/satım onayı Gauntlet liste penceresi olur; satışta kâr/zarar görünür.
  - Portföy'de Performans butonu; 7/14/30 gün seçilebilen çizgi grafik.
- **Kullanıcı kararları:**
  - Grafikte **toplam kâr/zarar** çizilir: açık K/Z + gerçekleşen K/Z + lonca ödemeleri + henüz ödenmemiş lonca hak edişi. Alım ve satım çizgiyi oynatmaz; sadece kazanç ve kayıp oynatır.
  - Geçmiş günler **yeniden hesaplanmaz**. Her gün bir nokta kaydedilir (`portfolio.performance`, son 60 gün, şema 11). Grafik ikinci kayıtlı günden itibaren görünür.
- **Tutsak haberi:** `HeroPrisonerTaken`. Sadece krallık lordları (`IsLord` ve krallık fraksiyonu) haber olur; yoldaşlar, gezginler ve küçük klanlar dışarıda kalır.
  - Yeni olay türü `HeroCaptured`, `MarketEvent.HeroId` / `OtherHeroId` alanlarıyla. Şok kuralı yok (`"HeroCaptured": []`).
  - Lord tahvilleri (Faz 3) bu olayı kullanacak.
  - Esir alan partinin lideri yoksa (garnizon) metin sadece fraksiyonu yazar.
- **Yer adlarında krallık:** Haberin gösterildiği andaki sahibi yazılır (`Settlement.MapFaction`). Fetih haberinde yazılmaz, çünkü metin iki krallığı zaten söylüyor.
- **20 haber:** `settings.json` `newsKeep` 30'dan 20'ye indi. Kayıttaki eski haberler bir sonraki olayda kırpılır.
- **Portföy kısayolu:** `ExchangeTableVM.WithSecondary` ile Kapat'ın sağına ikinci buton eklenir. `ShowPortfolio(back)`: Portföy kapanınca `back` çalışır. Portföyden yapılan bir işlem portföye döner ve `back`'i korur. Emtia aramasındaki metin de işlemden sonra geri gelir.
- **Onay penceresi:** `CxConfirmPopup.xml`, etiket | değer satırları. Satış şunları gösterir:
  - Ortalama maliyet
  - Kâr vergisi
  - Eline geçecek tutar
  - Kâr/zarar: `TradeQuote.RealizedPnl = Total − CostBasis`, işlem sonrası `TradeResult.RealizedPnl` ile aynı.
  - Gauntlet açılamazsa eski metin onayı yedek olarak kalır.
- **Grafik tekniği (PLAN 2.7):** Gauntlet'te widget döndürme ve çizgi çizme yok. Çözüm: Core'daki `Charts/LineChart` grafiği 4 piksellik dilimlere böler.
  - Her dilim için çizgi parçasının üst kenarı ve yüksekliği hesaplanır, ayrıca sıfır çizgisine kadar dolgu (kârda yeşil, zararda kırmızı).
  - Dilim içinde kalan tepe noktaları da hesaba katılır, böylece sivri tepeler kaybolmaz.
  - Dikey eksen her zaman sıfırı içerir. Konumlar `float` VM özellikleriyle `MarginTop` / `SuggestedHeight`'a bağlanır (bkz. API_NOTES).
  - 820 piksellik grafik 205 dilim. Dinamik doku gerekmedi.
- **Krallık endeksi ayarı (save10 log'undan):**
  - Vlandia 28 günde 15 yağma yedi. Her biri −0,06 ve yığılma sınırı yoktu, bu yüzden fiyat temel değerin %25 altına indi (P 67,8, F 90,5).
  - Kural −0,03'e indi, `maxStack` 3 (canlı en fazla −0,09).
  - Diğer krallıklarda fiyat/temel değer farkı %3–5.

## ADR-032 — Krallık endeksine bütün krallık verileri (2026-10-20)

- **Kullanıcı isteği:** Krallıkla ilgili her şey endekse girsin: şehirler, kaleler, köyler, yağma, fethedilme ve fethetme, klan liderlerinin ve kralın ölümü, savaş kazanma/kaybetme, savaş ve barış ilanları. Hepsi farklı ağırlıkta olsun. Ağırlıkları ben seçtim; hepsi `settings.json`'da.
- **Temel değer (günlük okuma):**
  - F = 100 × refah^0,6 × fief puanı^0,5 × köy hane^0,3 × askeri güç^0,2 × klan^0,3 × savaş × kuşatma × yağma.
  - Her oran bugünkü değer / krallığın taban değeri.
  - Fief puanı = şehir × 2 + kale × 1. Bir şehir kaybı iki kale kaybı kadar ağır.
  - Köy hanesi `Village.Hearth` toplamı. Köyler fiefleriyle birlikte el değiştirdiği için ayrıca olay gerekmez.
  - Askeri güç `Kingdom.CurrentTotalStrength`. Oranı 0,25–3 arasında tutuluyor; ordusu dağılan krallık sıfıra inmiyor.
  - Klan sayısına paralı askerler girmiyor.
  - Yağma çarpanı: o gün yağmalanmış ya da yağmalanan köy başına −%1,5, en az 0,85.
  - Savaş (düşman başına −%5, en az 0,7) ve kuşatma (−%8, en az 0,6) çarpanları değişmedi.
  - Refah üssü 1'den 0,6'ya indi, çünkü artık yükü başka parçalar da taşıyor.
- **Olay şokları (fiyatı hemen iter, sonra söner):**

| Olay | Krallık | Etki | Yarı ömür |
|---|---|---|---|
| Köy yağması | sahibi | −0,03 (en fazla 3 birikir) | 10 gün |
| Şehir fethi | kaybeden / kazanan | −0,24 / +0,15 | 21 gün |
| Kale fethi | kaybeden / kazanan | −0,10 / +0,06 | 21 gün |
| Hükümdar ölümü | kendi | −0,15, oynaklık ×1,5 (21 gün) | 21 gün |
| Klan lideri ölümü | kendi | −0,04 (en fazla 3 birikir) | 14 gün |
| Hükümdar esir düştü | kendi | −0,08 | 14 gün |
| Klan lideri esir düştü | kendi | −0,015 (en fazla 3 birikir) | 10 gün |
| Muharebe | kazanan / kaybeden | +0,04 / −0,05 × büyüklük (en fazla 4 birikir) | 10 gün |
| Savaş ilanı | iki taraf | −0,06, oynaklık ×1,5 | 14 gün |
| Barış | iki taraf | +0,03 | 14 gün |
| Savaşın sonu (haraçlı barış) | kazanan / kaybeden | +0,08 / −0,10 | 28 gün |
| Klan ayrıldı / katıldı | eski / yeni | −0,05 / +0,03 (isyanda ×1,6) | 21 gün |
| Yıkılış | kendi | endeks 0 | — |

- **Muharebe:** Savaştaki iki krallık arasında, kazananı olan, yağma olmayan çarpışmalar. İki tarafın toplam kaybı 20'den azsa sayılmaz. Büyüklük = kayıp / 300 (en fazla 1). 100 kayıptan küçük muharebeler fiyatı oynatır ama habere yazılmaz (`MarketEvent.Quiet`), yoksa 20 haberlik liste dolardı.
  - Birikme sınırı ölçeksiz şoka göre sayılır (`maxStack` × 0,04). Küçük muharebeler sınırı genişletmez.
- **Savaşın kazananı:** Barışta haraç ödeyen kaybetmiştir. Haraçsız barışın kazananı yok; sadece barış şoku uygulanır.
- **Ölüm ve esaret rütbesi:** `BeforeHeroKilledEvent` kullanılıyor, çünkü `HeroKilledEvent` geldiğinde klan liderliği ve taht varise geçmiş olur. Sadece hükümdar ve klan lideri ölümleri olay olur; sıradan lordların ölümü değil. Kurallarda yeni `heroRank` filtresi ve `scaleByMagnitude` alanı var.
- **Klan değişikliği:** Paralı asker sözleşmeleri ve yıkılışla gelen ayrılmalar sayılmaz. İsyan (`LeaveWithRebellion`, `CreateKingdom`) ×1,6.
- **Şema 12:** `kingdoms[].baseFiefScore / baseHearth / baseStrength / baseClans` eklendi ve ilk okumada doldurulur. Eski kayıtlarda fief puanı tabanı eski fief sayısı tabanından ölçeklenir, böylece o güne kadar kaybedilen fiefler sayılmaya devam eder. Yeni parçalar (köy, güç, klan) eski kayıtlarda bugünden itibaren ölçülür.
- **Henüz kullanılmayan:** StanceLink'teki savaş istatistikleri (kayıp, başarılı kuşatma ve yağma sayıları). Haraçsız barışta kazananı bunlardan çıkarmak sonraki ayar turuna kalabilir.

## ADR-033 — Sabit komisyon, Trade yeteneği etkisi kaldırıldı (2026-10-20)

- **Kullanıcı isteği:** Komisyon en fazla %2 olsun ve Tüccar (Trade) yeteneğiyle değişmesin. İleride modun kendi Simsar (Broker) seviyesi, perkleri ve yetenek ağacı olacak; indirimler oradan gelecek.
- **Karar:** `trading.commissionRate` = 0,02, her işlemde sabit. `minCommission` (2 dinar) duruyor. `commissionRateMax`, `commissionRateMin` ve `tradeSkillCap` ayarları kaldırıldı. `OrderEngine.CommissionRate` artık parametresiz.
- **Değişmeyen:** Trade yeteneği pozisyon limitini artırmaya devam ediyor (`limits.perTradeSkill`). Kullanıcı sadece komisyonu sordu.
- **Etkisi:** Trade 10 olan oyuncu için komisyon %2,43'ten %2'ye indi. ADR-022'nin "haberle işlem komisyonu yener" hesabı daha da rahatladı.

## ADR-034 — Kendi atölyesinin loncasından pay alınamaz (2026-10-20)

- **Kullanıcı isteği:** Oyuncu kendi dükkanından ortaklık payı alamaz. Ortaklık payı olan bir dükkanı satın alırsa payları o anki geçerli fiyattan satılır.
- **Eşleme:** Lonca = bir şehirdeki aynı türden atölyelerin tamamı (ADR-027). Oyuncunun o şehirde o türden bir atölyesi varsa o loncanın payını alamaz (`TradeError.OwnWorkshop`). Satış serbest. Lonca tablosunda satırın adına "(atölyen)" eklenir ve Al düğmesi gizlenir.
- **Zorunlu satış:** Oyuncu atölye satın aldığında (`WorkshopOwnerChangedEvent`) ya da atölyesinin türünü değiştirdiğinde (`WorkshopTypeChangedEvent`), elindeki o loncanın payları hemen satılır. Günlük tick'te de aynı kontrol yapılır, kaçan bir durum kalmasın diye.
- **Fiyat:** Piyasa fiyatı; alış-satış farkı, kayma, komisyon ve kâr vergisi yok. Lonca kapanışındaki zorunlu satışla aynı kural (`Portfolio.ForceSell`). İşlem geçmişinde zorunlu satış olarak görünür. Satış ani ve oyuncunun seçimi değil, bu yüzden ceza kesilmiyor.

## ADR-035 — Arayüz turu 2: tek Piyasalar penceresi, tablo düzeni, performans penceresi (2026-10-20)

- **Şehir menüsü:** "Borsaya git" vanilla "Ticaret" seçeneğinin hemen üstünde. Sıra numarası sabit değil, `trade` seçeneğinin yerine bakılarak bulunuyor (`GameMenu.MenuOptions`). Bulunamazsa 5. sıra. Seçenek `OnAfterSessionLaunchedEvent`'te ekleniyor: `OnSessionLaunchedEvent`'te vanilla seçenekler henüz yoktu, liste boş olduğu için "Borsaya git" en üste düşüyordu (ilk test).
- **Piyasalar:** Borsa menüsündeki dört piyasa seçeneği tek "Piyasalar" seçeneği oldu. Pencerede dört sekme var: Hisseler (emtia), Sepetler, Ortaklıklar (lonca), Endeksler (krallık). Başlık, açıklama ve sütun başlıkları sekmeye göre değişiyor. Arama kutusu sadece Hisseler sekmesinde. Seçili sekme oyun boyunca hatırlanıyor; işlemden ya da Portföy'den dönünce aynı sekme açılıyor.
- **Adlar:** "Krallık Piyasası" → "Endeks Piyasası". Krallık tablolarının ilk sütunu "Krallık Endeksleri". Portföy sekmeleri de piyasa sekmeleriyle aynı adları aldı: Ortaklıklar, Endeksler.
- **Tablo düzeni:** İsim sütunundan sonraki sayı sütunları her tabloda eşit genişlikte ve ortalı: Piyasalar 4 × 115 px, Portföy 5 × 110 px, Takas Geçmişi 5 × 104 px. Takas Geçmişi'nde tarih, işlem türü ve isim metin sütunu olduğu için kendi genişliklerinde kaldı. Haberler tablosu serbest metin olduğu için değişmedi. Kâr/zarar başlıkları "K/Z" ve "K/Z %".
- **Ortaklıklar sekmesi:** Başka şehirlerdeki paylar ayrı bölümde gösteriliyor. Bölümün kendi başlığı, ayırıcı çizgisi ve kaydırma çubuğu var; yaklaşık 5 satır görünüyor (236 px). Bu şehrin loncaları üstteki ana listede kaldı.
- **Portföyüm:** Pencere başlığı "Portföyüm". Alttaki toplam metni (değer, kâr/zarar, komisyon, vergi, lonca geliri ve borcu) Portföy'den ve Takas Geçmişim'den kaldırıldı; performans penceresine tablo olarak taşındı.
- **Performans penceresi:** Yukarıdan aşağıya: dönem sekmeleri (7/14/30 gün), grafik, başlık ve toplam satırı, portföy toplamları tablosu. Grafik pencerede ortalı: 700 × 260 px çizim alanının iki yanında 110 px boşluk var, y etiketleri soldaki boşlukta. Yatay eksende her gün için bir nokta var (`LineChart.Ticks`, grafikle aynı ölçek). Toplamlar tablosu (portföy değeri, açık ve gerçekleşmiş K/Z, loncalardan gelen ve biriken kâr, lonca borcu, komisyon, vergi, işlem sayısı, toplam K/Z) etiket | değer listesi. Kaydırma yok, 10 satırın hepsi görünüyor (30 px satır, 520 px genişlik). Başlık 22 punto. Pencere 1040 × 900.
- **Takas Geçmişim:** Sol altta "Bu oturumda yapılan toplam işlem: X". Sayaç behavior örneğinde tutuluyor, kaydedilmiyor; oyun yüklenince sıfırdan başlıyor. Sadece oyuncunun kendi alım/satımları sayılıyor; zorunlu satışlar (atölye, krallık yıkımı) sayılmıyor.

## ADR-036 — Emtia hisseleri 12'den 31'e (2026-10-20)

- **Eklenen 19 hisse:** balık, et, peynir, tereyağı, üzüm, bira, koyun, domuz, kil, çömlek, sert kereste, tahta, gümüş cevheri, mücevher, ham ipek, keçe, post, deri, kürk. Kaynak, kullanıcının oyundaki ticaret ekranı görüntüleri (Hisseler_1..3). Item ID'leri v1.4.8 oyun verisinde doğrulandı (API_NOTES §8). Koyun ve domuz `horses_and_others.xml`'de, kategori ID'leri `sheep` ve `hog`, `isTradeGood: true`.
- **Etiket:** Sadece üzüm `agriculture` aldı: tarım sepetindeki diğer ürünlerle aynı mevsim ve kıtlık şoklarını yesin. Hayvansal ürünler ve işlenmiş mallar etiketsiz. Mevsimsel etkiyi genişletmek ayrı bir ayar işi.
- **Mevcut kayıtlar:** Şema değişmedi. Yeni hisseler kayıtlı oyunda ilk günlük tikte, dünya fiyatı görüldüğünde dünya fiyatından açılıyor (`MarketEngine.Tick`, `StartPrice`). Geçmişleri o günden başlıyor.
- **Sepetler:** Bu adımda değişmedi; yeni sepetler ayrı iş.

## ADR-037 — Sınırlı arz, şehrin yatırımcıları ve balinalar (2026-10-20)

**Bağlam:** Kullanıcı her hissenin ve sepetin sınırlı sayıda olmasını, NPC'lerin rastgele al/sat yapmasını ve büyük işlemlerin fiyatı oynatmasını (balina etkisi) istedi. NPC'ler için portföy tutulmayacak.

**Karar** (formüller ECONOMY_MODEL §5.1):
- **Arz:** Her araç ilk fiyatlandığı gün `derinlik × 6 / fiyat` pay çıkarıyor. Lonca ortaklıklarının arzı mevcut 100 pay. Yatırımcılar başlangıçta arzın %30'unu tutuyor; lonca ortaklıklarında 0.
- **Fiyat etkisi:** Kayma, serbest payın başlangıç seviyesine oranıyla çarpılıyor (0,5–4 arası). Oyuncu ve yatırımcılar aynı formülü kullanıyor.
- **Oyuncuya tutma sınırı yok** (kullanıcı kararı). Oyuncunun sınırı mevcut toplam ve araç başı limitler; ileride borsacı seviyesi.
- **Yatırımcılar:** Her gün emtia, sepet ve krallık endekslerinde bir net emir: gürültü ve tutulan payın başlangıç seviyesine geri dönmesi. Trend takibi ayarda var ama 0: simülatörde manipülatör botun erken oyun kazancını hedefin (günde 50 dinar) üstüne çıkardı. Lonca ortaklıklarında yatırımcı akışı yok.
- **Kayıt:** Araç başına `totalShares` ve `npcShares` kaydediliyor (kullanıcı kararı: NPC'lerin net payı kaydedilsin). Şema 13. Eski kayıtlarda arz, bir sonraki günlük tikte o günkü fiyattan çıkarılıyor; yatırımcılar %30 ile başlıyor.
- **Balina:** Günde %10 olasılık, derinliğin 1–3 katı. Haberlerde "Balina" kategorisi (mavi) ve ekranda yeşil/kırmızı mesaj. Tablolara hacim sütunu şimdilik eklenmedi (kullanıcı kararı).
- **Test için konsol:** `exchange.whale <id> <buy|sell> [derinlik]`. `exchange.dump_prices` artık yatırımcı payı / arz sütununu da gösteriyor.

**Gerekçe:** Tek sayı kaydetmek portföy tutmadan "balina topladı, sonra boşaltıyor" davranışını veriyor. Fiyat etkisini oyuncunun emriyle aynı formülden geçirmek, oyuncuya "büyük emir fiyatı oynatır, sığ piyasada daha çok" kuralını tutarlı biçimde öğretiyor. Simülatörün 8 hedefi geçiyor. Emtia yıllık oynaklığı medyanı %29,9'dan %31,7'ye çıktı. Erken oyunda en iyi bot günde 48,1 dinar kazanıyor.

## ADR-038 — Arayüz turu 3: oturum sayacı, kısa adlar, lonca bölümü, geniş grafik, muharebe haberleri (2026-10-20)

**Karar** (kullanıcı istekleri):
- **Takas Geçmişim sayacı:** "Bu oturumda yapılan toplam işlem" artık zorunlu satışları da sayıyor (lonca kapanışı, lonca zararı için satış, kendi atölyenin payları, yıkılan krallık). Sayaç geçmişe yazılan her kaydı sayıyor (`Portfolio.RecordedSinceLoad`, kaydedilmiyor); yükleme ile sıfırlanıyor. Zorunlu satışlar zaten tabloda "Zorunlu satış" olarak listeleniyordu.
- **Portföyüm:** Tablodaki adlar kısa ad: emtialarda sonda "hissesi" yok.
- **Lonca Piyasası:** İkinci bölümün başlığı "Başka Şehirlerdeki Ortaklıkların". Bölümün yüksekliği bu şehrin satır sayısına göre hesaplanıyor (`OtherHeight`, 236–400 px); bölüm üstteki listenin hemen altında başlıyor, boşluk altta kalıyor.
- **Performans:** Grafik 700'den 820 px'e genişledi; pencere 1140 px. Alttaki tablo aynı genişlikte kaldı.
- **Haberler:** Muharebe haberi yer adı vermiyor ("Vlandia, Battania ordusunu yendi (118 kayıp)").

**ADR-037 ek (2026-10-20):** Balina alımından sonraki satış artık garanti değil (kullanıcı isteği). Yatırımcıların paylarını başlangıç seviyesine çekmesi her araç için her gün `holdingReversionChance` (0,4) olasılıkla oluyor; o gün güç `holdingReversion` 1 (önceden her gün 0,5). Ortalama geri dönüş biraz yavaşladı (günde fazlanın ~%8'inden ~%7'sine). Simülatör 8/8 geçiyor; emtia oynaklığı medyanı %31,9, erken oyunda en iyi bot günde 46,0 dinar.

## ADR-039 — Kar Al / Zarar Durdur emirleri (2026-10-20)

**Bağlam:** Kullanıcı, eldeki hisseler için hedef kâr ve hedef zarar yüzdesiyle, süreli satış emri istedi. Lonca ortaklıkları hariç.

**Karar** (formüller ECONOMY_MODEL §5.2):
- **Erişim:** Portföyüm penceresinde her satırda SAT'ın sağında bronz renkli EMİR butonu (lonca sekmesinde yok). Alt çubukta Performans'ın solunda Emirlerim butonu.
- **Emir penceresi:** Dört slider: adet, Hedef Kar %, Hedef Zarar %, Geçerlilik Süresi. Yüzdelerin sağında karşılık gelen fiyat yazar. Altında emir ücreti ve emrin verilememe sebebi. Alt ortada Emri Ver ve İptal.
- **Yüzde tabanı:** Ortalama maliyet. Kullanıcı yüzdeyi fiyata çevirip yanına yazmamızı istedi; "kâr" maliyete göre olduğu için taban maliyet. Güncel fiyat pencerenin üstünde gösteriliyor.
- **Bir hedef kapatılabilir** (slider 0 = Kapalı); ikisi birden kapalıysa emir verilemez (kullanıcı onayı).
- **Satış fiyatı:** O günün piyasa fiyatı, normal kayma ile (kullanıcı onayı). Komisyon yok, sadece kâr vergisi (kullanıcı kuralı).
- **Ücret:** Değerin %0,2'si + gün başına %0,02'si, en az 10 dinar (kullanıcı onayı). Geri ödenmez: süre dolsa da, oyuncu iptal etse de.
- **Kilit:** Emirdeki paylar pozisyonda kalır ama kilitlidir (`Portfolio.LockedQuantity`, bekleyen emirlerden hesaplanır, ayrıca kaydedilmez). Portföy tablosu ve piyasa tablolarındaki "Elde" sütunu serbest adedi gösterir; değer, limit ve serbest pay toplam adetle hesaplanır.
- **Kontrol:** Günlük fiyat güncellemesinden sonra, oyuncu nerede olursa olsun. Kampanya günüyle (`CampaignTime.Now.ToDays`) sayılır; Takas Geçmişim tarihleriyle aynı.
- **Emirlerim penceresi:** Yeniden eskiye: Verildi, Ürün, Adet, Kar Al, Zarar Durdur, Kalan süre, Durum (Bekleyen / Gerçekleşen / İptal), Sonuç. Bekleyenlerde kırmızı İPTAL butonu (SAT ile aynı görsel). Son 20 emir tutulur; bekleyen emir listeden düşmez.
- **Geçmiş ve bildirim:** Sadece gerçekleşen emirler Takas Geçmişim'de görünür ("Kar Al" / "Zarar Durdur" türüyle) ve oturum sayacına girer. Gerçekleşme, süre dolması, iptal ve piyasa kapanması sol alttaki mesajlara yazılır.
- **Kayıt:** Şema 14: `portfolio.orders`, `portfolio.nextOrderId`, `history[].order`. Eski kayıtlarda emir yok.
- **Konsol:** `exchange.dump_portfolio` kilitli adetleri ve emirleri de listeliyor.

## ADR-040 — Emtia 2.0: olayların etkisi görünür, sürprizler, haberi izleyen kazanır (2026-10-21)

**Bağlam:** Kullanıcı emtia alım satımını sıkıcı buldu: fiyatlar oyuncudan ve dünyadan etkileniyor gibi görünmüyordu, sürpriz yoktu, haberi izlemek kazandırmıyordu. Ölçüm (oyun log'u, gün 277–388):
- 31 emtianın günlük oynaklığı %3,0–4,0 arasında ve neredeyse aynıydı, yani bizim %3'lük gürültümüzdü.
- En büyük günlük hareket çoğu üründe %8–10'du; şok terimi çoğunda hiç oluşmamıştı.
- Sık yağmalanan ürünlerde beklenen şok tabanı yağmaları siliyordu.
- Oyunun pazarı bir akış ekonomisi. Köyler her gün üretiyor, yağmalanan köy üretmiyor, şehirler refahlarına göre tüketiyor, kervanlar şehirler arası farkı kapatıyor. Kaynaklar tükenmiyor.
- Şehirler arası fiyat farkı 10 kata kadar çıkıyor. Bazı malların dünya stoğu çok küçük: mücevher 84, gümüş 105, demir 153 birim.

**Karar (1. aşama, bu sürüm; formüller ECONOMY_MODEL §4.3):**
- Yerel bir olay fiyatı ancak dünya arzından götürdüğü kadar oynatır. Yağma şoku, köyün dünya üretim kapasitesindeki payıyla ölçeklenir ve haber bu payı yazar.
- Emtialarda beklenen şok tabanı kapalı.
- Gürültü %3'ten %1,8'e indi, yerine nadir habersiz sıçramalar geldi (kullanıcı onayı).
- Oyun dışı piyasa haberleri eklendi (kullanıcı onayı): 16 şablon, olumlu ve olumsuz dengeli. Etki ilk gün yaklaşık üçte bir, kalanı birkaç günde gelir; haberi erken okuyan kazanır.
- Söylentiler eklendi. Bir kısmı yalan (kullanıcı onayı: olsun). Yalan oranı ilerde borsacı seviyesine bağlanacak (kullanıcı kararı).
- Ölçüm: Günlük dünya stoğu CSV'si ve oyuncunun mal alım satım log'u.
- Kayıt: Şema 15 (`market.scheduledNews`, `news[].event.itemShare`).

**Karar (2. aşama, stok verisi gelince):**
- Emtia fiyatı dünya stoğu ve talep oranına bağlanacak. Dünya stoğuna kervanların taşıdığı mal da girer (kullanıcı kararı, 2026-10-21; oyun içi ölçümde malın çoğu kervanlarda). Oyuncunun malı piyasadan toplaması (köşeleme) ya da boşaltması fiyatı doğrudan oynatacak.
- Manipülasyon soruşturması (kullanıcı onayı): hem hisse tutup hem malı toplayan oyuncu belli bir olasılıkla ceza ve itibar kaybı yer. Olasılık borsacı seviyesi arttıkça düşecek (kullanıcı kararı).

**Simülatör (336 gün, 20 tohum):**

| | Önce | Sonra |
|---|---|---|
| Günlük oynaklık (medyan) | %3,5 | %3,1 |
| %5'ten büyük gün sayısı (ürün başına yılda) | 49 | 26 |
| En büyük 5 günlük hareket (medyan) | %24 | %31 |
| Al ve tut | +%1,1 | +%13,2 |
| Rastgele | −%8,0 | −%0,6 |
| Yağma kovalayan bot (erken oyun) | −%0,5 | +%6,1 |

Bütün hedefler geçiyor; sadece işlenmiş mallarda yıllık oynaklık %23–25'te kalıyor (hedef ≥ %25).

## ADR-041 — Tutsak alma haberlerden çıktı (2026-10-21)

**Bağlam:** Kullanıcı: tutsak alma haberleri listede çok yer kaplıyor.

**Karar:** Lord tutsak alma olayı (`HeroCaptured`) haber listesine girmiyor (`Quiet`). Kural tablosundaki etkisi (hükümdar tutsak → krallık endeksi) sürüyor. Eski kayıtlardaki tutsak haberleri listede gösterilmiyor.
