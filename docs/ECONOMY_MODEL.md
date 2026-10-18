# Calradic Exchange — Ekonomi Modeli

Sürüm: 0.1 · Bu dokümandaki tüm sayılar başlangıç değerleridir. Simülatör ve oyun testleriyle ayarlanacaktır. Varsayılan parametrelerin tek kaynağı `ModuleData/settings.json` olacaktır; bu doküman onların açıklamasıdır.

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
w_town = prosperity_town            (alternatif: pazar hacmi, Faz 0'da veri durumuna göre seçilecek)
```

- `price_town(c)`: Şehir pazarında malın satış fiyatı. Hangi API'nin bunu verdiği Faz 0'da doğrulanacak (API_NOTES).
- Kuşatma altındaki şehirler hesaba katılmaz.

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

### 2.3 Lonca hissesi (Faz 2)

Her lonca 1.000 pay olarak tanımlanır.

```
kâr_günlük = son 28 günün atölye kârlarının ortalaması (o şehirdeki o türdeki tüm atölyeler)
temettü_haftalık = payout_ratio × kâr_günlük × 7 / 1000
F_guild = (temettü_haftalık × 12) / r_guild          (12 hafta = 1 oyun yılı)
```

- Varsayılan `payout_ratio = 0,6`, `r_guild = 0,20` (yıllık iskonto).
- Atölye kâr verisi doğrudan okunamazsa yedek tahmin: (çıktı fiyatı − girdi fiyatı) × üretim oranı × refah çarpanı. Faz 0'da karar verilecek.

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
| Emtia | 0,12 | 0,020 |
| Sektör endeksi | 0,10 | 0,012 |
| Lonca hissesi | 0,08 | 0,025 |
| Krallık endeksi | 0,08 | 0,015 |
| Tahvil | 0,20 | 0,004 |

Ayarlardaki oynaklık çarpanı σ'yı çarpar.

## 4. Olaylar ve şoklar

Her olay ilgili araçların S terimine bir dürtü ekler:

```
S(t) = S(t−1) × 0,5^(1 / yarı_ömür) + Σ dürtü
```

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
| Kıtlık başladı | Tarım emtiaları | +0,08 | 14 |
| | Şehrin loncaları | −0,06 | 14 |
| Kervan yok edildi | Bölge sigorta primi | +0,1 tehlike | 14 |
| Lord öldü | Lord tahvili | temerrüt kontrolü (§2.5) | — |
| Krallık yok oldu | Krallık endeksi | sıfır, araç kapanır | — |

Olay ID'leri ve değerleri `ModuleData/settings.json` içinde `eventShocks` altında tutulur.

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
komisyon = max(10, oran × emir_değeri) × (1 − 0,5 × trade_yeteneği / 300)
```

| Sınıf | Fark | Derinlik (dinar) | λ |
|---|---|---|---|
| Emtia | %2 | 40.000 | 0,5 |
| Sektör endeksi | %1 | 80.000 | 0,3 |
| Lonca hissesi | %3 | 20.000 | 0,7 |
| Krallık endeksi | %1,5 | 60.000 | 0,4 |
| Tahvil | %1 | 30.000 | 0,3 |

- Komisyon oranı: %1.
- **Kalıcı fiyat etkisi:** Oyuncunun net alım satımı piyasa fiyatını da kaydırır: `P ← P × (1 ± 0,5 × etki)`. Bu etki κ ile doğal olarak temel değere geri döner. Böylece "al, fiyatı yükselt, sat" döngüsü zarar ettirir.

## 6. Limitler ve teminat

```
toplam_limit = 20.000 + 20.000 × klan_seviyesi + 50 × trade_yeteneği
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
| Temel değerle korelasyon (30 günlük ortalamalar) | > 0,7 |
| "Al ve tut" stratejisi 4 yıllık getirisi (maliyetler sonrası) | −%10 ile +%15 arası |
| Rastgele işlem stratejisi | ortalama negatif (maliyetler yüzünden) |
| "Olayı gören" strateji (olay sonrası ilk gün işlem) | pozitif ama yıllık %40'ı geçmemeli |

Simülatör çıktısı: `sim/out.csv` (gün, araç, P, F, S) ve konsolda özet tablo.

## 9. Rastgele sayı üreteci

- Core kendi RNG'sini kullanır (xorshift128+), durum değişkenleri JSON kaydına yazılır.
- Kayıt yüklenince oyun katmanı RNG'yi zamana dayalı entropiyle yeniden tohumlar (bkz. DECISIONS ADR-004).
- Testler ve simülatör sabit tohumla çalışır.
