# İlk oturum promptu

Aşağıdaki metni Claude Code'a olduğu gibi yapıştır.

---

Merhaba. Bu klasörde Mount & Blade II: Bannerlord için "Calradic Exchange" adlı bir borsa modu geliştireceğiz. Önce `CLAUDE.md`'yi, sonra `docs/` altındaki tüm dokümanları oku: GDD, ECONOMY_MODEL, PLAN, API_NOTES, DECISIONS.

Okuduktan sonra `docs/PLAN.md` içindeki **Faz 0**'ı uygula:

1. Bana Bannerlord kurulum yolunu, oyun sürümünü ve açık DLC'leri sor. Cevapları `CLAUDE.md` içindeki Environment bölümüne yaz.
2. Gerekli araçları kontrol et (.NET SDK, ilspycmd, git, BUTR şablonları). Eksik olanı kur.
3. Oyunun DLL'lerini decompile edip `docs/API_NOTES.md` içindeki her API'yi doğrula. Durumlarını ve gerçek imzalarını güncelle. Item, item category ve workshop ID'lerini oyunun XML dosyalarından kontrol et ve `ModuleData/instruments.json` taslağını gerçek ID'lerle oluştur.
4. Solution iskeletini kur: Core (oyundan bağımsız), Core.Tests, Sim ve oyun modülü. Git deposunu başlat.
5. Oyun modülünde şehir menüsüne "Borsaya git" seçeneği ekle. Seçenek tıklandığında "Calradic Exchange yakında açılıyor" mesajı göstersin. Debug konsol komutlarının iskeletini ve log dosyasını kur.
6. Derle, oyunun Modules klasörüne kur ve bana kısa bir test listesi ver.

Doğrulama sırasında tasarımla çelişen bir şey bulursan (bir API yoksa, bir veri erişilemiyorsa) uygulamaya geçmeden önce bana sor ve kararı `docs/DECISIONS.md`'ye yaz. Benimle Türkçe konuş.
