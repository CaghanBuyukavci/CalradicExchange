# Calradic Exchange — Ekonomi Modeli

Sürüm: 0.2 (simülatörle ayarlandı, ADR-013/014) · Bu dokümandaki tüm sayılar başlangıç değerleridir. Simülatör ve oyun testleriyle ayarlanacaktır. Varsayılan parametrelerin tek kaynağı `ModuleData/settings.json` olacaktır; bu doküman onların açıklamasıdır.

---

## 1. Temel kavramlar

- **Zaman:** Fiyatlar her oyun gününde bir güncellenir (günlük tick). Bannerlord'da 1 yıl = 4 mevsim × 21 gün = **84 gün** (Faz 0'da doğrulanacak).
- **P(t):** t günündeki piyasa fiyatı (dinar).
- **F(t):** t günündeki temel değer, oyun verisinden hesaplanır.
- **S(t):** Olaylardan gelen duyarlılık terimi (logaritmik), zamanla söner.
- **Hedef değer:** T(t) = F(t) × e^S(t)
- Core içinde fiyatlar `decimal`. Oyuncuya ve işlemlere tam sayı dinar olarak yansır.

## 2. Temel değerler

### 2.1 Emtia payı

Bir pay = 10 birim mal.

```
F_c = 10 × Σ_towns ( w_town × price_town(c) ) / Σ_towns w_town
w_town = arz_town(c) + stok_town(c)     (pazar hacmi, ADR-017; hiçbir şehirde hacim yoksa refah ağırlığı)
```

- `price_town(c)`: Şehir pazarında malın satış fiyatı. Hangi API'nin bunu verdiği Faz 0'da doğrulanacak (API_NOTES).
- Kuşatma altındaki şehirler hesaba katılmaz.
- Bu değer (ve §2.2'deki sektör değeri) borsanın **gözlemi**dir; fiyatın çekildiği F doğrudan bu değil (ADR-022):

```
F = taban × e^E
E(t) = sön(E(t−1)) + 0,6 × ( Δln gözlem − sınıfın medyan Δln gözlemi )      |E| ≤ 0,3, sönme yarı ömrü 14 gün
taban(t): ln taban, ln gözleme doğru 120 gün yarı ömürle kayar
```

- Bütün ekonominin birlikte hareketi (yeni kampanyadaki stok patlaması ve çöküşü) E'ye girmez; sadece malın diğerlerinden farklı hareketi girer ve zamanla söner.
- Hacmi sıfır olan şehirler (stoğu ve arzı yok) katılmaz: oyun orada sadece teorik, tavana yakın bir fiyat gösterir (gerçek veri: kadifede şehirlerin %87'si).

### 2.2 Sektör endeksi

Başlangıç günü (veya modun eklendiği gün) her emtianın temel değeri F_i,0 olarak kaydedilir.

```
F_s = 100 × Σ_i ( w_i × F_i / F_i,0 )        Σ w_i = 1
```

Askeri Tedarik endeksi trade good değil item kategorilerinden hesaplanır ve ek olarak savaş yoğunluğuyla çarpılır:

```
F_mil = 100 × (kategori fiyat oranı) × (1 + 0,15 × aktif_savaş_sayısı_normalize)
aktif_savaş_sayısı_normalize = aktif savaş çifti sayısı / krallık sayısı
```

### 2.3 Lonca ortaklık payı (Faz 2, ADR-027)

Her lonca (şehir, atölye türü) 100 pay olarak tanımlanır. Oyuncuya görünen adı "ortaklık payı".

```
net_kâr_günlük   = Σ (kasa_bugün − kasa_dün + sahibin_çektiği)   (o şehirdeki o türdeki atölyeler; eksi olabilir)
kâr_ort          = son 28 günün net_kâr_günlük ortalaması
dağıtım_haftalık = son 7 günün net_kâr toplamı × (elde_tutulan_pay / 100)     (eksiyse oyuncu öder)
F_lonca          = taban × e^E      (ADR-022 gibi göreli sinyal; sınıf = tüm loncalar)
taban_başlangıç  = kâr_ort × 84 / r_guild / 100,   r_guild = 0,25 (yıllık hedef getiri)
```

- Zarar ödemesi: altın → aynı loncanın payları piyasa fiyatından (masrafsız) satılır → kalan borç sonraki herhangi bir lonca kâr dağıtımından düşülür (ADR-027).
- 2.1 verisi (58 gün): kâr_ort ortanca 174, %10–%90 aralığı 61–297 dinar/gün. Örnek pay fiyatı ≈ 585 dinar, haftalık dağıtım ≈ 12 dinar/pay.

### 2.4 Krallık endeksi (Faz 2)

```
F_k = 100 × ( Σ refah_k / Σ refah_k,0 ) × (fief_sayısı_k / fief_sayısı_k,0)^0,5 × savaş_çarpanı × kuşatma_çarpanı
savaş_çarpanı  = 1 − 0,05 × (krallığın savaşta olduğu düşman sayısı)       alt sınır 0,7
kuşatma_çarpanı = 1 − 0,08 × (kuşatma altındaki şehir sayısı)               alt sınır 0,6
```

Krallık yok olunca F_k = 0 ve araç listeden kaldırılır.

### 2.5 Tahvil (Faz 3)

Nominal 100, kupon c (yıllık), vade n hafta, haftalık temerrüt olasılığı h.

```
F_bond = Σ_{j=1..n} ( c/12 × 100 × (1−h)^j / (1+r/12)^j ) + 100 × (1−h)^n / (1+r/12)^n
```

h ihraççıya göre:

| İhraççı | Temel h (haftalık) | Artıranlar |
|---|---|---|
| Şehir | 0,002 | kuşatma +0,03; düşük güvenlik +0,005 |
| Lord | 0,004 | esir +0,05; klan seviyesi düşük +0,005; savaşta +0,005 |
| Krallık (savaş) | 0,003 | şehir kaybı başına +0,01; kral esir +0,04 |

Temerrüt gerçekleştiğinde kalan değerin %0–40'ı (rastgele) geri ödenir.

### 2.6 Hasat sözleşmesi — forward (Faz 3)

```
F_fwd(c, gün_sayısı) = spot_c × (1 + taşıma × gün_sayısı) × mevsim_beklentisi(c, vade_mevsimi)
taşıma = 0,001 / gün
```

`mevsim_beklentisi`: Tarım malları için kış 1,15, ilkbahar 1,05, yaz 0,95, sonbahar (hasat) 0,90. Diğer mallar 1,0. Simülatörle ayarlanacak.

### 2.7 Commenda (Faz 3)

```
getiri = (kervan_parası_sonda − kervan_parası_başta) / kervan_parası_başta
oyuncu_ödemesi = yatırım × (1 + pay_oranı × getiri)          pay_oranı = 0,5
kervan yok edildiyse: oyuncu_ödemesi = 0
```

Alt sınır: getiri en az −0,5 kabul edilir. Yani kervan yok edilmediyse, zarar etse bile oyuncuya yatırımının en az %75'i döner.

### 2.8 Sigorta (Faz 3)

```
prim_oranı (14 günlük) = baz_prim × (1 + 3 × bölge_tehlike) × (1 + 0,5 × savaş_var_mı)
baz_prim = 0,02
bölge_tehlike ∈ [0, 1]: bölgedeki haydut partisi sayısı / normalizasyon
tazminat = kervan değeri × 0,7
```

Sigortacı havuzu için beklenen getiri hedefi: yıllık %8–15, ama kötü sezonlarda zarar mümkün.

## 3. Günlük fiyat güncellemesi

Logaritmik form kullanılır (fiyat hiçbir zaman negatif olmaz):

```
r(t) = κ × ln( T(t) / P(t−1) ) + σ × ε(t) + oyuncu_etkisi(t)
P(t) = P(t−1) × e^r(t)
r(t) ∈ [−0,25, +0,25] aralığına kırpılır
ε(t) ~ N(0, 1)
```

- **κ:** temel değere dönüş hızı
- **σ:** günlük oynaklık

| Sınıf | κ | σ (günlük) |
|---|---|---|
| Emtia | 0,20 | 0,030 |
| Sektör endeksi | 0,15 | 0,018 |
| Lonca hissesi | 0,08 | 0,025 |
| Krallık endeksi | 0,08 | 0,015 |
| Tahvil | 0,20 | 0,004 |

Ayarlardaki oynaklık çarpanı σ'yı çarpar.

## 4. Olaylar ve şoklar

Her olay ilgili araçların S terimine bir dürtü ekler:

```
S_ham(t) = Σ_bileşenler [ S_i(t−1) × 0,5^(1 / yarı_ömür_i) ] + Σ yeni dürtüler
S(t) = S_ham(t) − B(t−1)          B: S_ham'ın 42 gün yarı ömürlü hareketli ortalaması (ADR-019)
```

Sık tekrarlanan olayların (her gün yağma, haftada birkaç kıtlık) beklenen etkisi B'ye girer; fiyatı sadece sürpriz kısmı oynatır.

Bir araca birden çok olaydan gelen dürtüler toplanır. |S| en fazla 1,0 (yani hedef en fazla e kat sapar).

### 4.1 Olay tablosu (başlangıç değerleri)

| Olay | Hedef araç | Dürtü | Yarı ömür (gün) |
|---|---|---|---|
| Köy yağmalandı | Köyün ana ürünü (emtia) | +0,06 | 10 |
| | Köyün bağlı olduğu şehrin krallığının tarım katkısı | −0,02 | 10 |
| | Bağlı şehrin tahvili | −0,01 | 7 |
| Kuşatma başladı | Şehrin tüm loncaları | −0,15 | kuşatma bitene kadar sabit, sonra 7 |
| | Şehrin tahvili | −0,10 | aynı |
| | Askeri Tedarik endeksi | +0,03 | 14 |
| Şehir el değiştirdi | Kaybeden krallık endeksi | −0,08 | 21 |
| | Kazanan krallık endeksi | +0,05 | 21 |
| | Şehrin loncaları | −0,05 ve 14 gün boyunca σ × 2 | 14 |
| Savaş ilanı | Askeri Tedarik | +0,05 | 21 |
| | Lüks | −0,03 | 21 |
| | İki krallığın endeksi | −0,02 ve σ × 1,5 (14 gün) | 14 |
| Barış | Lüks, Tekstil | +0,03 | 21 |
| | Askeri Tedarik | −0,04 | 21 |
| Mevsim değişimi | Tarım emtiaları | mevsim tablosuna göre (§2.6) | 21 |
| Kıtlık başladı | Tarım emtiaları | +0,12 × şehrin refah payı × 2 (ADR-018) | 14 |
| | Şehrin loncaları | −0,06 | 14 |
| Kervan yok edildi | Bölge sigorta primi | +0,1 tehlike | 14 |
| Lord öldü | Lord tahvili | temerrüt kontrolü (§2.5) | — |
| Krallık yok oldu | Krallık endeksi | sıfır, araç kapanır | — |

Olay ID'leri ve değerleri `ModuleData/settings.json` içinde `eventShocks` altında tutulur.

**Ayar notu (ADR-013, ADR-015, ADR-022):** ADR-022 ile bütün olay dürtüleri ×2 yapıldı (yağma +0,15, savaş → askeri tedarik +0,15, kıtlık +0,24); mevsim dürtüleri değişmedi. Yağma şoku bir araçta en fazla 4 yağma kadar birikir (`maxStack`). Eski not: Köy yağmasının emtia dürtüsü +0,075 (ADR-015). Tablodaki diğer tüm dürtüler ve mevsim dürtüleri simülatör ayarında ×1,5 yapıldı (örneğin savaş → askeri tedarik +0,075, kış → tarım +0,21). Güncel değerler `settings.json`'dadır.

### 4.2 Haber gecikmesi (Faz 4)

```
gecikme_gün(olay, şehir) = mesafe(olay_yeri, şehir) / haber_hızı
haber_hızı = 40 harita birimi / gün       (harita ölçeğine göre Faz 4'te ayarlanacak)
```

- Her olay dürtüsü, her şehir için kendi teslim gününe kadar bekletilir.
- Küresel fiyat P_global olaysız bileşen ve olay bileşenine ayrıştırılır:
  `P_yerel(şehir) = P_baz × e^(S_ulaşmış(şehir))`
- İşlemler yerel fiyattan yapılır.
- Bellek: olay kuyruğu, en uzak şehre ulaştıktan sonra silinir.

## 5. İşlem maliyetleri

```
işlem_fiyatı_alış = P × (1 + fark/2) × (1 + etki)
işlem_fiyatı_satış = P × (1 − fark/2) × (1 − etki)
etki = λ × (emir_değeri / derinlik)
komisyon = max(2, %2 × emir_değeri)      (herkese sabit, Trade yeteneğine bağlı değil; ADR-033)
kâr_vergisi = %10 × max(0, satış_geliri − komisyon − satılan_payların_maliyeti)   (sadece satışta, zararda yok)
```

| Sınıf | Fark | Derinlik (dinar) | λ |
|---|---|---|---|
| Emtia | %1 | 40.000 | 0,10 |
| Sektör endeksi | %0,6 | 80.000 | 0,06 |
| Lonca hissesi | %1,5 | 20.000 | 0,14 |
| Krallık endeksi | %0,8 | 60.000 | 0,08 |
| Tahvil | %0,5 | 30.000 | 0,06 |

- Komisyon ve kâr vergisi: yukarıdaki formül (ADR-014). Lonca, krallık ve tahvil değerleri Faz 2/3'te yeniden ayarlanacak.
- **Kalıcı fiyat etkisi:** Oyuncunun net alım satımı piyasa fiyatını da kaydırır: `P ← P × (1 ± 0,5 × etki)`. Bu etki κ ile doğal olarak temel değere geri döner. Böylece "al, fiyatı yükselt, sat" döngüsü zarar ettirir.

### 5.1 Sınırlı arz ve şehrin yatırımcıları (ADR-037)

Her aracın sınırlı sayıda payı var. Pay azaldıkça aynı emir fiyatı daha çok oynatır. Oyuncuya ayrıca bir tutma sınırı konmuyor; oyuncunun sınırı §6'daki limitlerden geliyor (ileride borsacı seviyesi).

```
çıkarılan_pay = derinlik × 6 / liste_fiyatı          (lonca: 100 pay)
yatırımcı_payı_başlangıçta = çıkarılan × 0,3           (lonca: 0)
serbest_pay = çıkarılan − yatırımcı_payı − oyuncu_payı
çarpan = clamp( serbest_oran_başlangıçta / serbest_oran_emir_ortasında , 0,5 , 4 )
etki = λ × (emir_değeri / derinlik) × çarpan
```

- "Emir ortası": alımda emrin yarısı kadar pay çekilmiş, satışta yarısı kadar pay geri gelmiş serbest oran.
- **Yatırımcılar (NPC):** Portföyleri yok; araç başına yalnızca toplam payları (`npcShares`) kaydediliyor. Emtia, sepet ve krallık endekslerinde her gün bir net emir veriyorlar. Lonca ortaklıklarını sadece oyuncu alıp satıyor.

```
günlük_net_emir = derinlik × ( 0,15 × N(0,1)  +  0 × 5_günlük_değişim  +  0,5 × (başlangıç_payı − pay) / çıkarılan )
```

- Trend takibi katsayısı (`flowMomentum`) 0. Simülatörde 0,5 değeri, kendi köyünü yağmalayıp fiyatı iten manipülatörün kazancını hedefin üstüne çıkardı.
- Alım serbest payla, satış yatırımcıların elindeki payla sınırlı. Fiyat etkisi oyuncunun emriyle aynı formül (kalıcı etki × 0,5, κ ile geri döner).
- **Balina:** Her gün %10 olasılıkla, açık araçlardan rastgele birinde bir balina derinliğin 1–3 katı büyüklükte işlem yapıyor. %60 alım; yatırımcılar zaten başlangıç payının 1,5 katından fazlasını tutuyorsa %60 satış. Haberlere ve mesaja düşüyor ("Bir Pravend tüccarı 800 adet tahıl hissesi topladı (+%13,1)").
- Bir balinanın topladığı paylar sonraki günlerde geri dönüş terimiyle satılıyor (günde fazlanın yaklaşık %8'i); fiyat bu sırada aşağı baskı görüyor.

## 6. Limitler ve teminat

```
toplam_limit = 5.000 + 15.000 × klan_seviyesi + 50 × trade_yeteneği      (ADR-014)
araç_başı_limit = derinlik × 1,5
```

Açığa satış (Faz 3):

```
ödünç_ücreti = %0,2 / gün (pozisyon değeri üzerinden)
başlangıç_teminatı = pozisyon değerinin %150'si (nakit kilitlenir)
teminat_oranı = kilitli_teminat / pozisyonun_güncel_değeri
zorla_kapatma: teminat_oranı < 1,10 olduğunda pozisyon güncel fiyattan kapatılır
```

## 7. Ödemeler

- Temettü ve kupon her 7 günde bir, günlük tick sırasında ödenir.
- Ödemeler doğrudan oyuncunun altınına eklenir ve mesaj gösterilir.
- Trade XP: gerçekleşmiş kârın %2'si kadar XP (vanilla ile kıyaslanıp ayarlanacak).

## 8. Simülatör hedefleri

Sim projesi 336 gün (4 oyun yılı) boyunca sentetik olaylarla çalışır. Sentetik olay üreteci: günde ortalama 0,8 köy yağması, 30 günde bir kuşatma, 60 günde bir savaş/barış, mevsim geçişleri, rastgele kıtlık.

Kabul hedefleri:

| Ölçüt | Hedef |
|---|---|
| Hiçbir fiyat başlangıcının | 0,2× altına inmez, 5× üstüne çıkmaz (krallık yok olması hariç) |
| Emtia yıllıklandırılmış oynaklık | %25–60 |
| Endeks yıllıklandırılmış oynaklık | %15–35 |
| Hedefle (F × e^S) korelasyon (30 günlük ortalamalar) | > 0,65 (ADR-022; önce temel değerle, ADR-015) |
| "Al ve tut" stratejisi 4 yıllık getirisi (maliyetler sonrası) | −%10 ile +%15 arası |
| Rastgele işlem stratejisi | ortalama negatif (maliyetler yüzünden) |
| "Olayı gören" strateji (olay sonrası ilk gün işlem), geç oyun profili (klan 5, Trade 250) | pozitif ama yıllık %40'ı geçmemeli |
| Erken oyun (klan 1, Trade 30): en iyi botun ilk yıl kârı | ortalama günde ≤ 50 dinar (ADR-014) |

Simülatör çıktısı: `sim/out.csv` (gün, araç, P, F, S) ve konsolda özet tablo.

## 9. Rastgele sayı üreteci

- Core kendi RNG'sini kullanır (xorshift128+), durum değişkenleri JSON kaydına yazılır.
- Kayıt yüklenince oyun katmanı RNG'yi zamana dayalı entropiyle yeniden tohumlar (bkz. DECISIONS ADR-004).
- Testler ve simülatör sabit tohumla çalışır.
