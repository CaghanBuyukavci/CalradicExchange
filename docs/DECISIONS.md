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
