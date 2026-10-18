# Calradic Exchange (çalışma adı)

Mount & Blade II: Bannerlord için bir finans modu. Şehirlerdeki borsada malın kendisi yerine emtia payları, sektör endeksleri, lonca hisseleri, krallık endeksleri, tahviller, kervan ortaklıkları, vadeli sözleşmeler ve sigorta alınıp satılır. Fiyatlar canlı kampanya dünyasından etkilenir: köy yağmaları, kuşatmalar, savaşlar, mevsimler, refah ve atölye kârları.

## Klasördeki dosyalar

| Dosya | İçerik | Dil |
|---|---|---|
| `CLAUDE.md` | Claude Code'un her oturumda okuduğu çalışma kuralları | İngilizce |
| `FIRST_PROMPT.md` | Claude Code'a ilk oturumda yapıştıracağın prompt | Türkçe |
| `docs/GDD.md` | Oyun tasarım dokümanı: araçlar, oynanış, UI, denge | Türkçe |
| `docs/ECONOMY_MODEL.md` | Fiyat formülleri, olay tablosu, varsayılan parametreler | Türkçe |
| `docs/PLAN.md` | Faz faz yol haritası, görevler ve kabul kriterleri | Türkçe |
| `docs/API_NOTES.md` | Tasarımın varsaydığı Bannerlord API'leri ve doğrulama durumu | İngilizce |
| `docs/DECISIONS.md` | Mimari kararlar ve gerekçeleri | Türkçe |

`CLAUDE.md` ve `API_NOTES.md` İngilizce, çünkü içinde çok sayıda API ve sınıf adı geçiyor ve Claude Code bunları kodla birebir eşleştirecek. Tasarım dokümanları Türkçe.

## Başlarken

1. Bu klasörü projenin duracağı yere koy (örneğin `D:\Dev\CalradicExchange`).
2. Gerekli araçlar:
   - .NET SDK 8 veya üstü (`dotnet --version`)
   - `ilspycmd`: `dotnet tool install -g ilspycmd`
   - BUTR şablonları: `dotnet new install Bannerlord.Templates` (Claude Code da kurabilir)
   - Git
3. Klasörde `claude` komutuyla Claude Code'u başlat ve `FIRST_PROMPT.md` içindeki metni yapıştır.
4. Bannerlord launcher'ında geliştirici konsolunu kullanabilmek için cheat mode'u açman gerekebilir. Claude Code ilk oturumda bunu da soracak.
