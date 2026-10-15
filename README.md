<div align="center">

![Klavye Ayazı](docs/afis.svg)

[![İndir](https://img.shields.io/badge/⬇_İndir-klavye--ayazi.exe-4fb3e0?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/Enskygr234/klavye-ayazi/releases/latest)
[![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Lisans](https://img.shields.io/badge/lisans-MIT-e63946?style=for-the-badge)](LICENSE)

</div>

Terminalde **Türkçe** klavye hız testi. Ekrandaki kelimeleri yaz, süre bitince kaç kelime/dakika yazdığını, doğruluğunu ve rekorunu gör.

```
  ❄ Klavye Ayazı  ·  30 saniye  ·  Tab: yeniden  ·  Esc: çık          18 sn  ·  42 k/dk

   kartal atmaca büyük kale kedi pilav kurt eski başarı temiz film kapı yavaş

   mutlu ekim söz takım sakin salon kar ben balon futbol karlı poyraz baykuş

   börek hava ördek kule harita lokum defter yorgun bere geyik park baş iş
```

## Özellikler

- **Türkçe kelimeler:** ç, ğ, ı, ö, ş, ü dahil; Türkçe klavyede gerçek yazma hızını ölçer.
- **Renkli geri bildirim:** Doğru harf beyaz, yanlış harf kırmızı, sıradaki harf vurgulu.
- **Süreler:** 15, 30, 60 ya da 120 saniye. Süre ilk tuşa basınca başlar.
- **Sonuç ekranı:** Net hız, ham hız, doğruluk, doğru/yanlış kelime ve her süre için ayrı rekor.
- **Hata düzeltme:** Backspace harf, Ctrl + Backspace kelime siler. Silinen hata doğruluğu yine düşürür: dürüst ölçüm!

## Kurulum

1. [Releases](https://github.com/Enskygr234/klavye-ayazi/releases/latest) sayfasından `klavye-ayazi.exe` dosyasını indir.
2. Terminal (PowerShell ya da Windows Terminal) aç, dosyanın olduğu klasörde çalıştır:

```
.\klavye-ayazi.exe          30 saniyelik test
.\klavye-ayazi.exe -s 60    60 saniyelik test
```

Çalışması için [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) gerekir.

## Ölçüm

Kelime/dakika, dünyada yaygın ölçüye göre hesaplanır: **5 karakter = 1 kelime**. Net hız yalnızca doğru yazılan karakterleri sayar. Rekorlar `%AppData%\KlavyeAyazi\rekorlar.json` dosyasında durur.

## Kaynaktan derle

[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ile `build.bat` çalıştır. Yazma motorunun testleri: `dotnet run -- --test`

## Lisans

[MIT](LICENSE) © Winterdeveloper

<!-- ENS:ALT -->
---

<div align="center">

**❄️ ENS Developer'ın diğer projeleri**

🖥️ Windows: [Buz Arşiv](https://github.com/Enskygr234/buz-arsiv) · [Ayaz Launcher](https://github.com/Enskygr234/Ayaz-launcher) · [Masaüstü Kar](https://github.com/Enskygr234/masaustu-kar) · [ayaz](https://github.com/Enskygr234/ayaz) · [Güvenlik Kilidi](https://github.com/Enskygr234/GuvenlikKilidi)

[🌐 Bütün projeler](https://enskygr234.github.io/ensdewloper.github.io/) · [👤 GitHub profilim](https://github.com/Enskygr234)

<sub>Kış gelir, kod yazılır.</sub>

</div>
<!-- /ENS:ALT -->
