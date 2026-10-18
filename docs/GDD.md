# Calradic Exchange — Oyun Tasarım Dokümanı (GDD)

Sürüm: 0.1 · Tarih: 2026-10-18 · Durum: Taslak

Fiyat formülleri ve sayısal parametreler `ECONOMY_MODEL.md` dosyasında. Bu doküman neyin, neden ve oyuncuya nasıl görüneceğini anlatır.

---

## 1. Özet

Calradic Exchange, Bannerlord kampanyasına bir finans katmanı ekler. Şehirlere girildiğinde menüde "Borsaya git" seçeneği çıkar. Borsada oyuncu fiziksel mal taşımadan, mallara ve kurumlara bağlı kâğıt araçlar alıp satar.

Fiyatlar rastgele hareket etmez. Her aracın oyun dünyasından hesaplanan bir **temel değeri** vardır: pazar fiyatları, köy üretimi, atölye kârları, şehir refahı, krallıkların durumu. Piyasa fiyatı bu temel değeri gürültülü ve gecikmeli şekilde takip eder. Dünyayı iyi okuyan oyuncu kazanır.

**Tasarım cümlesi:** Savaşı ve dünyayı izleyen bir tüccar olarak kâr et. İstersen dünyayı kendin değiştirip o değişimden kâr et.

## 2. Tasarım hedefleri

1. **Dünyaya bağlılık.** Her fiyat hareketinin oyuncunun anlayabileceği bir sebebi olmalı. "Kuzeyde köyler yağmalandı, tahıl yükseldi" gibi.
2. **Ortaçağ hissi.** Araçların çoğunun tarihî bir karşılığı var: commenda (kervan ortaklığı), deniz sigortası, hasat forward'ı, lonca payları, kral borçları. Modern terimler arayüzde ortaçağ adlarıyla sunulur.
3. **Oyuncu etkisi.** Oyuncu dünyayı değiştirebildiği için piyasayı da etkileyebilir. Bu bir hata değil, özelliktir. Sınırları ve riskleri vardır (bkz. §8).
4. **Ekonomiyi bozmamak.** Bannerlord'da oyun sonunda zaten çok para birikir. Borsa sınırsız para basan bir makineye dönüşmemeli (bkz. §9).
5. **Kayıt güvenliği.** Mod kaldırılınca veya güncellenince kayıt dosyası bozulmamalı.

## 3. Hedef oyuncu

- Tüccar oyunu oynayan, ticaret ve kervan sistemini sığ bulan oyuncular.
- Savaş oyuncuları için ikincil bir gelir ve risk yönetimi aracı: savaşa girmeden önce düşmanın endeksini açığa satmak gibi.
- Strateji ve ekonomi simülasyonu sevenler.

## 4. Oyuncu akışı

1. Oyuncu bir şehre girer. Şehir menüsünde **"Borsaya git"** seçeneği vardır.
2. Borsa ekranında şu sekmeler bulunur:
   - **Piyasa:** tüm araçlar, fiyatları, günlük değişim, 7 günlük mini grafik
   - **Portföy:** sahip olunan pozisyonlar, ortalama maliyet, gerçekleşmemiş ve gerçekleşmiş kâr/zarar, temettü geçmişi
   - **Haberler:** borsaya ulaşmış son olaylar ve fiyatlara etkileri
   - **Araç detayı:** fiyat grafiği, temel değerin bileşenleri, sözleşme koşulları
3. Alım satım: araç seçilir, adet girilir, onay ekranında fiyat, alış-satış farkı, komisyon, kayma ve toplam gösterilir.
4. Oyuncu borsadan çıkar. Pozisyonlar sahada iken de değer kazanır veya kaybeder. Önemli olaylar (temettü, büyük fiyat hareketi, tahvil temerrüdü, vadenin dolması) ekrana mesaj olarak düşer.

**MVP arayüzü:** İlk sürümde Gauntlet ekranı yok. Borsa, oyunun kendi menü sistemi (alt menüler) ve inquiry pencereleriyle çalışır. Gauntlet ekranı Faz 2'de gelir (bkz. PLAN).

## 5. Borsalar

- **Varsayılan:** Her şehirde borsaya erişim var. Ayarlardan "sadece büyük şehirler" veya "sadece krallık başkentleri" seçilebilir.
- **Faz 1–3:** Tek bir küresel fiyat vardır, hangi şehirden bakılırsa bakılsın aynıdır.
- **Faz 4 (haber gecikmesi):** Her şehir kendi yerel fiyatını görür. Olaylar her şehre uzaklığa göre gecikmeli ulaşır (bkz. §7.3).
- Lezzet notu: "Borsa" kelimesi Brugge'deki Van der Beurse ailesinin hanından geldiği kabul edilir. Borsa binası menüde bir hanın arka odası, sarraf masası gibi tasvir edilebilir.

## 6. Yatırım araçları

Her araç `ModuleData/instruments.json` içinde tanımlanır. Araç sınıfları:

### 6.1 Emtia payları (Faz 1)

- **Ne:** Bir ticari malın fiyatına bağlı senet. Bir pay yaklaşık 10 birim malın değerini temsil eder. Fiziksel teslimat yok.
- **Temel değer:** Malın tüm şehir pazarlarındaki ağırlıklı ortalama fiyatı × 10.
- **MVP listesi (12):** tahıl, zeytin, zeytinyağı, hurma, tuz, demir cevheri, alet, keten, keten kumaş, yün, kadife, şarap. Gerçek item ID'leri Faz 0'da doğrulanır.
- **Oyuncu için anlamı:** Mal taşımadan fiyat hareketinden kâr etmek. Kervanla aynı malı taşıyan oyuncu için riskten korunma (hedge) aracı.

### 6.2 Sektör endeksleri (Faz 1)

Birden çok malın normalize edilmiş temel değerlerinin ağırlıklı ortalaması. Başlangıç değeri 100.

| Endeks | İçerik (taslak) | Karakter |
|---|---|---|
| Tarım | tahıl, zeytin, hurma, balık, üzüm | Mevsimsel, yağmalara duyarlı |
| Tekstil | keten, keten kumaş, yün, kumaş, kadife, pamuk | Barışta güçlü |
| Maden ve Metal | demir cevheri, alet, gümüş | Savaşta güçlü |
| Lüks | şarap, kadife, mücevher, zeytinyağı | Barış ve refahla yükselir, savaşta düşer |
| Askeri Tedarik | silah, zırh, at kategorileri | Savaş yoğunluğuyla yükselir |

### 6.3 Lonca hisseleri (Faz 2)

- **Ne:** Bir şehirdeki belirli bir atölye türünün toplamına ortaklık. Örnek: "Pravend Dokumacılar Loncası".
- **Kapsam:** Her (şehir, atölye türü) çifti için şehirde en az bir atölye varsa lonca oluşur. Listede kalabalık olmaması için varsayılan olarak her şehirde en kârlı 2 lonca listelenir.
- **Temettü:** Her 7 günde bir, loncanın son dönem kârının bir kısmı pay başına dağıtılır.
- **Temel değer:** Beklenen temettülerin bugünkü değeri.
- **Riskler:** Şehir kuşatılırsa sert düşüş. Şehir el değiştirirse belirsizlik. Kıtlık ve düşük güvenlik kârı düşürür.
- **Oyuncu için anlamı:** Atölye satın almanın küçük parçalara bölünmüş hâli. Birden fazla şehre yayılarak risk dağıtılabilir.

### 6.4 Krallık endeksleri (Faz 2)

- **Ne:** Bir krallığın ekonomik gücünü izleyen endeks. Örnek: "Vlandia Endeksi".
- **Temel değer:** Krallığın şehirlerinin toplam refahı, fief sayısı, savaş durumu ve kuşatma altındaki şehir sayısından hesaplanır.
- **Oyuncu için anlamı:** Savaş sonucuna yatırım. Kazanacağını düşündüğün krallığı al, kaybedeceğini düşündüğünü açığa sat.
- Yok olan bir krallığın endeksi sıfıra iner ve listeden kalkar. Pozisyonlar sıfırdan kapanır.

### 6.5 Kervan ortaklığı — Commenda (Faz 3)

- **Ne:** Gerçek bir NPC kervanının seferine sermaye koymak. Tarihî commenda sözleşmesinin oyun karşılığı.
- **İşleyiş:** Borsada o şehirde bulunan veya yakında geçecek kervanlar listelenir. Oyuncu belirli bir tutar yatırır. Sefer süresi sabittir (varsayılan 14 gün).
- **Sonuç:** Süre sonunda kervan yaşıyorsa getiri, kervanın ticaret parasındaki değişime göre hesaplanır. Oyuncu kârdan payını (varsayılan %50) alır, zarar varsa zarara ortak olur. Kervan yok edilirse yatırım kaybolur.
- **Risk göstergesi:** Kervanın güzergâhındaki haydut yoğunluğundan hesaplanan bir "tehlike" seviyesi gösterilir.

### 6.6 Tahviller (Faz 3)

| Tür | İhraççı | Kupon | Temerrüt riski |
|---|---|---|---|
| Lord tahvili | Lord (klan lideri) | Lordun serveti ve klan seviyesine göre | Lord ölür, uzun süre esir kalır veya klanı yok olursa |
| Savaş tahvili | Krallık (kral) | Savaş sırasında yüksek | Krallık yıkılırsa; savaş kaybedildikçe fiyat düşer |
| Şehir tahvili | Şehir | Düşük, düzenli | Şehir el değiştirirse yeni sahip borcu tanımayabilir |

- Kupon her 7 günde ödenir, ana para vade sonunda (varsayılan 84 gün, yani 1 oyun yılı).
- Tahvil fiyatları ikinci el piyasada temerrüt olasılığına göre değişir.
- Lord tahvili ilişki etkisi yapar: tahvili vadesinde tam ödenen lordla ilişki hafif artar. Lord temerrüde düşerse oyuncu "alacak" olarak not edilir. Bu, ileride bir görev veya diyalog seçeneğine bağlanabilir (Faz 5 fikri).

### 6.7 Hasat vadeli sözleşmeleri — Forward (Faz 3)

- **Ne:** Belirli bir şehirde, belirli bir tarihte, belirli miktarda malı bugünden belirlenen fiyatla almak veya satmak.
- **Fiyat:** Spot fiyat + taşıma maliyeti + mevsim beklentisi.
- **Teslim:** Vade gününden itibaren 7 gün içinde o şehre gidilirse mal envantere eklenir (alış) veya envanterden düşülür (satış). Gidilmezse nakit farkla kapanır ve küçük bir ceza kesilir.
- **Oyuncu için anlamı:** Kışa girerken tahıl fiyatını kilitlemek, ordunun yiyeceğini garantiye almak, kervan planlaması.

### 6.8 Sigorta (Faz 3)

İki yön:

- **Sigorta almak:** Oyuncunun kendi kervanları için. Prim ödenir. Kervan haydutlar veya düşman tarafından yok edilirse kervan değerinin bir kısmı ödenir.
- **Sigortacı olmak:** Oyuncu bir bölgedeki NPC kervanlarını sigortalayan bir havuza sermaye koyar. Prim geliri alır, bölgede kervan yok edildikçe havuz ödeme yapar.
- Prim, bölgedeki haydut yoğunluğu ve savaş durumuna göre değişir.

### 6.9 Açığa satış (Faz 3)

- Emtia, sektör, lonca ve krallık araçlarında açığa satış mümkün.
- Günlük ödünç alma ücreti alınır. Teminat (marj) gerekir. Zarar teminatı aşarsa pozisyon zorla kapatılır.
- Ayarlardan kapatılabilir.

### 6.10 Araç adlandırma

Arayüzde ortaçağ dili kullanılır:

| Modern | Oyundaki adı |
|---|---|
| Hisse, pay | Pay senedi |
| Endeks | Sepet / Sektör sepeti |
| Tahvil | Borç senedi |
| Forward | Hasat sözleşmesi |
| Temettü | Kâr payı |
| Açığa satış | Ödünç satış |
| Komisyon | Sarraf hakkı |

İngilizce karşılıkları: share, basket, bond / debt note, harvest contract, dividend, borrowed sale, broker's fee.

## 7. Fiyatları ne hareket ettirir?

### 7.1 Temel değer

Her günün başında her aracın temel değeri oyun verisinden yeniden hesaplanır (formüller `ECONOMY_MODEL.md` §2). Piyasa fiyatı her gün temel değere doğru bir miktar çekilir.

### 7.2 Olaylar

Oyundaki olaylar fiyatlara anlık bir "duyarlılık şoku" ekler. Şok zamanla söner (yarı ömrü olaydan olaya değişir).

| Olay | Etkisi |
|---|---|
| Köy yağmalandı | Köyün ürününe ait emtia ↑ (arz şoku). Bölgenin tarım endeksi ve şehir tahvilleri ↓ |
| Kuşatma başladı | Şehrin lonca hisseleri ↓↓, şehir tahvili ↓, askeri tedarik ↑ |
| Şehir el değiştirdi | Kaybeden krallık endeksi ↓, kazanan ↑. Şehrin loncaları ve tahvilleri belirsizlik (oynaklık ↑) |
| Savaş ilanı | Askeri tedarik ↑, lüks ↓, iki krallığın endeksinde oynaklık ↑ |
| Barış | Lüks ve tekstil ↑, askeri tedarik ↓ |
| Mevsim değişimi | Kışa girerken tarım ↑, hasatta ↓ |
| Kıtlık (şehir yiyecek stoğu bitti) | Tarım emtiaları ↑↑, şehrin loncaları ↓ |
| Kervan yok edildi | Commenda zararı, sigorta ödemesi, bölge primleri ↑ |
| Lord öldü / esir düştü | Lord tahvili temerrüt veya donma |
| Krallık yok oldu | Krallık endeksi sıfır, savaş tahvilleri temerrüt |
| Oyuncu büyük miktarda mal sattı veya aldı | Malın emtia fiyatına kısa süreli etki (bkz. §8) |

### 7.3 Haber gecikmesi (Faz 4)

Modu sıradan bir borsa simülasyonundan ayıran özellik.

- Ortaçağda haber atla taşınır. Bir olayın şoku her şehre, olay yerine uzaklığına göre gecikmeli ulaşır.
- Yerel fiyat = olaysız temel fiyat + o şehre ulaşmış olayların şokları.
- Sonuç: Borsalar arasında geçici fiyat farkları oluşur. Haberi önce öğrenen oyuncu uzak bir borsaya gidip arbitraj yapabilir.
- Oyuncu bir olayı kendi gözüyle görürse (yağmaya tanık olursa, kuşatmada bulunursa) haberi anında bilir. Ama borsa ancak haber şehre ulaşınca fiyatlar.
- **Haberler sekmesi** şehre ulaşmış olayları gösterir: "Üç gün önce Ocs Hall yakınlarında iki köy yağmalandı."
- Trade yeteneği yüksek oyuncu, yolda olan haberler hakkında belirsiz ipuçları alır: "Kuzeyden kötü haberler geldiğine dair söylentiler var."

### 7.4 Gürültü

Her araca sınıfına göre günlük rastgele hareket eklenir. Böylece fiyatlar canlı görünür ama uzun vadede temel değerden kopmaz.

## 8. Oyuncu etkisi ve piyasa manipülasyonu (Faz 4)

Oyuncu dünyayı değiştirebilir, bu yüzden fiyatları da değiştirebilir:

- Önce bir malı açığa satıp sonra o malı bir şehre yığarak fiyatı düşürmek.
- Bir şehrin loncasını açığa satıp şehri kuşatmak.
- Düşman köylerini yağmalamadan önce o köyün ürününün emtia payını almak.

Bunlar oynanışın parçası. Ama riskleri var:

- **Şüphe puanı:** Oyuncu bir araçta pozisyon açtıktan sonraki N gün içinde o aracı etkileyen bir olaya kendisi sebep olursa şüphe puanı artar.
- **Sonuçlar (kademeli):** uyarı → para cezası → şehir ileri gelenleriyle ilişki kaybı → belirli süre o borsadan men.
- Ayarlardan kapatılabilir veya şiddeti değiştirilebilir.
- Oyuncunun büyük alım satımları (envanter işlemleri) ilgili emtia fiyatına kısa süreli bir baskı uygular. Bu, pazar fiyatı zaten değiştiği için temel değere de doğal olarak yansır.

## 9. Ekonomik denge

**Sorun:** Borsada karşı taraf "kasa"dır. Oyuncunun her kârı oyuna yoktan para girmesi demek.

Önlemler:

1. **Alış-satış farkı ve komisyon.** Doğal para çıkışı. Trade yeteneği komisyonu en fazla yarıya indirir.
2. **Piyasa derinliği ve kayma.** Büyük emirler fiyatı aleyhine oynatır. Oyuncu büyük pozisyonları parça parça ve zamana yayarak açmak zorunda kalır.
3. **Pozisyon limitleri.** Toplam yatırım limiti klan seviyesine bağlıdır.
4. **Uzun vadeli getiri beklentisi sıfıra yakın.** Fiyatlar temel değere döndüğü için pasif "al ve unut" stratejisi yüksek getiri sağlamaz. Para, dünyayı okuyan ve doğru zamanda işlem yapan oyuncuya gider.
5. **NPC yatırımcılar (Faz 5).** Lordlar da portföy tutar. Bu hem para çıkışı yaratır hem dünyayı canlandırır.
6. **Simülatörle test.** Her denge değişikliği Sim projesiyle 4 oyun yılı (336 gün) boyunca farklı stratejilerle test edilir. Hedef değerler `ECONOMY_MODEL.md` §8'de.

## 10. İlerleme ve yetenek bağlantıları

- Borsa kârları vanilla ticaretteki gibi Trade yeteneğine XP verir (kârla orantılı, düşük oranda).
- Trade yeteneği: komisyon indirimi, daha yüksek pozisyon limiti, Faz 4'te haber ipuçları.
- Klan seviyesi: pozisyon limiti, tahvil ihracına katılım (Faz 3), sigorta havuzu açma (Faz 3).
- Mevcut perk'leri değiştirmek yerine bu bonuslar modun kendi kurallarıyla hesaplanır. Bu, Harmony gerektirmez ve vanilla dengeyi bozmaz.

## 11. Arayüz

### 11.1 MVP (Faz 1): menü tabanlı

- Şehir menüsünde "Borsaya git" → Borsa alt menüsü:
  - Piyasaya bak → araç listesini fiyat ve değişimle gösteren bir pencere
  - Al → araç seçimi (çoktan seçmeli pencere) → adet girişi (metin girişi) → onay
  - Sat → portföyden araç seçimi → adet → onay
  - Portföy → pozisyonlar ve kâr/zarar
  - Haberler → son olaylar
  - Ayrıl
- Günlük özet: büyük hareketler ve temettüler ekranın sol altına mesaj olarak düşer.

### 11.2 Faz 2: Gauntlet ekranı

- Tam ekran borsa ekranı. Sol tarafta araç listesi (filtre: sınıf, sektör, krallık), sağda araç detayı ve grafik, altta emir paneli.
- Grafik: Gauntlet'te hazır grafik bileşeni yok. Fiyat geçmişi, ince dikey widget'larla (çubuk grafik) veya önceden üretilmiş bir doku ile çizilir. Bu teknik bir risk (bkz. PLAN Faz 2).
- Görsel stil: parşömen, mürekkep, mühür. Rakamlar için ortaçağ tarzı ama okunaklı bir font.

### 11.3 Yerelleştirme

- Tüm metinler `TextObject` ve `{=cx_...}` ID'leriyle. Türkçe ve İngilizce dil dosyaları `ModuleData/Languages` altında.

## 12. Ayarlar

MCM (Mod Configuration Menu) varsa onunla, yoksa `settings.json` ile. MCM isteğe bağlı bağımlılık olmalı.

- Borsa erişimi: tüm şehirler / büyük şehirler / başkentler
- Oynaklık çarpanı (0,5× – 2×)
- Komisyon ve fark çarpanı
- Pozisyon limiti çarpanı
- Açığa satış açık/kapalı
- Haber gecikmesi açık/kapalı ve haber hızı
- Manipülasyon tespiti: kapalı / hafif / sert
- Debug modu

## 13. Kayıt ve uyumluluk

- Tüm mod durumu tek bir JSON metni olarak kampanya kaydına yazılır (bkz. DECISIONS ADR-002).
- Mod yeni başlatılmış veya ortasından eklenmiş bir kampanyada çalışmalı. Ortasından eklenince fiyat geçmişi o günden başlar.
- Mod kaldırılırsa kayıt bozulmamalı. Sadece borsa verisi kaybolur.
- Diğer modlarla uyum: ekonomi modları (Settlement Finances, bankalar) ile çakışma beklenmez çünkü Harmony kullanmıyoruz. Yine de Faz 5'te popüler ekonomi modlarıyla test edilir.
- DLC'ye bağımlılık yok.

## 14. Kapsam dışı (şimdilik)

- Çok oyunculu destek
- Oyuncunun kendi şirketini halka arz etmesi
- Opsiyonlar ve karmaşık türev araçlar
- Gerçek zamanlı (saatlik) fiyat akışı. Fiyatlar günlük güncellenir; saatlik güncelleme sadece Faz 4'te haber yayılımı için değerlendirilebilir.

## 15. Açık sorular

1. Borsa erişimi tüm şehirlerde mi olsun, yoksa sadece büyük ticaret merkezlerinde mi? (Varsayılan: tüm şehirler, ayarlanabilir.)
2. Açığa satış varsayılan olarak açık mı olsun?
3. Manipülasyon tespiti varsayılan seviyesi ne olsun?
4. Mod adı kesinleşsin mi? (Çalışma adı: Calradic Exchange.)
5. Lord tahvili temerrüdünün bir görev veya diyalog sonucu olması istenir mi?
