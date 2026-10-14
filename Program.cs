using System.Diagnostics;
using System.Text;
using System.Text.Json;
using KlavyeAyazi;

// Klavye Ayazı — terminalde Türkçe klavye hız testi.

int sure = 30;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-h" or "--yardim" or "--help":
            Console.WriteLine("""
                klavye-ayazi — terminalde Türkçe klavye hız testi

                Kullanım:
                  klavye-ayazi            30 saniyelik test
                  klavye-ayazi -s 60      60 saniyelik test (15, 30, 60 ya da 120)

                Test sırasında:
                  Tab           baştan başla (yeni kelimeler)
                  Ctrl+Backspace  son kelimeyi sil
                  Esc           çık
                """);
            return 0;
        case "-s" or "--sure":
            if (i + 1 < args.Length && int.TryParse(args[++i], out var s) && s is 15 or 30 or 60 or 120) sure = s;
            else { Console.Error.WriteLine("Süre 15, 30, 60 ya da 120 olmalı."); return 1; }
            break;
        case "--test":
            return Testler.Calistir();
        default:
            Console.Error.WriteLine($"Bilinmeyen seçenek: {args[i]}  (yardım için: klavye-ayazi --yardim)");
            return 1;
    }
}

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    Console.Error.WriteLine("Klavye Ayazı gerçek bir terminal penceresinde çalışır.");
    return 1;
}

Console.OutputEncoding = Encoding.UTF8;
try { Console.InputEncoding = Encoding.UTF8; } catch (IOException) { }
Console.CursorVisible = false;
Console.CancelKeyPress += (_, _) => { Console.ResetColor(); Console.CursorVisible = true; };
try
{
    while (true)
    {
        var sonuc = Ekran.Test(sure);
        if (sonuc == null) break;                       // Esc
        if (!Ekran.SonucGoster(sonuc, sure)) break;     // Esc
    }
}
finally
{
    Console.ResetColor();
    Console.CursorVisible = true;
    Console.Clear();
}
return 0;

namespace KlavyeAyazi
{
    /// <summary>Bir yazma testinin durumu: hedef metin, yazılanlar, vuruşlar. Ekrandan bağımsızdır.</summary>
    sealed class Oturum
    {
        readonly StringBuilder yazilan = new();

        public Oturum(IEnumerable<string> kelimeler) => Metin = string.Join(' ', kelimeler);

        public string Metin { get; }
        public string Yazilan => yazilan.ToString();
        public int Konum => yazilan.Length;
        public int Vurus { get; private set; }
        public int HataliVurus { get; private set; }

        public void Tus(char c)
        {
            if (yazilan.Length >= Metin.Length) return;
            Vurus++;
            if (Metin[yazilan.Length] != c) HataliVurus++;
            yazilan.Append(c);
        }

        public void Sil()
        {
            if (yazilan.Length > 0) yazilan.Length--;
        }

        /// <summary>Ctrl+Backspace: imleçten geriye, bir önceki kelimenin başına kadar siler.</summary>
        public void KelimeSil()
        {
            while (yazilan.Length > 0 && yazilan[^1] == ' ') yazilan.Length--;
            while (yazilan.Length > 0 && yazilan[^1] != ' ') yazilan.Length--;
        }

        public Sonuc Hesapla(TimeSpan gecen)
        {
            int dogruKarakter = 0;
            for (int i = 0; i < yazilan.Length; i++) if (yazilan[i] == Metin[i]) dogruKarakter++;

            // Tamamen ve doğru yazılan kelimeler (son kelime bitmemişse sayılmaz)
            int dogruKelime = 0, yanlisKelime = 0, bas = 0;
            while (bas < yazilan.Length)
            {
                int son = Metin.IndexOf(' ', bas);
                if (son < 0) son = Metin.Length;
                if (son > yazilan.Length) break;
                if (string.CompareOrdinal(Metin, bas, Yazilan, bas, son - bas) == 0) dogruKelime++; else yanlisKelime++;
                bas = son + 1;
            }

            double dakika = Math.Max(gecen.TotalMinutes, 1.0 / 600);
            return new Sonuc(
                Net: dogruKarakter / 5.0 / dakika,
                Ham: yazilan.Length / 5.0 / dakika,
                Dogruluk: Vurus == 0 ? 0 : 100.0 * (Vurus - HataliVurus) / Vurus,
                DogruKelime: dogruKelime,
                YanlisKelime: yanlisKelime,
                Vurus: Vurus);
        }

        public static Oturum Rastgele(int kelime = 200)
        {
            var r = Random.Shared;
            var liste = new List<string>(kelime);
            for (int i = 0; i < kelime; i++)
            {
                string k;
                do k = Kelimeler.Liste[r.Next(Kelimeler.Liste.Length)];
                while (liste.Count > 0 && liste[^1] == k);   // aynı kelime art arda gelmesin
                liste.Add(k);
            }
            return new Oturum(liste);
        }
    }

    /// <summary>Kelime/dakika: 5 karakter bir "kelime" sayılır (dünyada yaygın ölçü).</summary>
    sealed record Sonuc(double Net, double Ham, double Dogruluk, int DogruKelime, int YanlisKelime, int Vurus);

    static class Rekorlar
    {
        static string Yol => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KlavyeAyazi", "rekorlar.json");

        public static Dictionary<string, double> Oku()
        {
            try { return JsonSerializer.Deserialize<Dictionary<string, double>>(File.ReadAllText(Yol)) ?? new(); }
            catch (Exception) { return new(); }
        }

        /// <summary>Yeni rekorsa kaydeder ve önceki rekoru döndürür.</summary>
        public static double? Kaydet(int sure, double net)
        {
            var r = Oku();
            r.TryGetValue(sure.ToString(), out var eski);
            if (net <= eski) return eski;
            r[sure.ToString()] = Math.Round(net, 1);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Yol)!);
                File.WriteAllText(Yol, JsonSerializer.Serialize(r));
            }
            catch (Exception) { }
            return null;
        }
    }

    static class Ekran
    {
        const int Ust = 2;   // metnin başladığı satır

        static void Yaz(string s, ConsoleColor on, ConsoleColor? arka = null)
        {
            Console.ForegroundColor = on;
            if (arka is { } a) Console.BackgroundColor = a;
            Console.Write(s);
            Console.ResetColor();
        }

        static int Genislik => Math.Clamp(Console.WindowWidth - 6, 20, 76);

        /// <summary>Metni kelime sınırlarından satırlara böler; her satırın başlangıç konumunu verir.</summary>
        static List<(int bas, int uzunluk)> Satirlar(string metin, int en)
        {
            var satirlar = new List<(int, int)>();
            int bas = 0;
            while (bas < metin.Length)
            {
                int son = Math.Min(bas + en, metin.Length);
                if (son < metin.Length)
                {
                    int bosluk = metin.LastIndexOf(' ', son, son - bas);
                    if (bosluk > bas) son = bosluk + 1;   // boşluk satırın sonunda kalsın
                }
                satirlar.Add((bas, son - bas));
                bas = son;
            }
            return satirlar;
        }

        static void Baslik(int sure, string sag)
        {
            Console.SetCursorPosition(0, 0);
            Yaz("  ❄ Klavye Ayazı", ConsoleColor.Cyan);
            Yaz($"  ·  {sure} saniye  ·  Tab: yeniden  ·  Esc: çık", ConsoleColor.DarkGray);
            var bosluk = Math.Max(1, Console.WindowWidth - 1 - Console.CursorLeft - sag.Length - 2);
            Console.Write(new string(' ', bosluk));
            Yaz(sag, ConsoleColor.Yellow);
        }

        static void MetniCiz(Oturum o)
        {
            var satirlar = Satirlar(o.Metin, Genislik);
            int imlecSatiri = satirlar.FindIndex(s => o.Konum < s.bas + s.uzunluk);
            if (imlecSatiri < 0) imlecSatiri = satirlar.Count - 1;
            int ilk = Math.Max(0, imlecSatiri - 1);   // yazılan satır her zaman ikinci sırada dursun
            var yazilan = o.Yazilan;

            for (int sira = 0; sira < 3; sira++)
            {
                Console.SetCursorPosition(0, Ust + 1 + sira * 2);
                Console.Write("   ");
                int si = ilk + sira;
                if (si < satirlar.Count)
                {
                    var (bas, uzunluk) = satirlar[si];
                    for (int i = bas; i < bas + uzunluk; i++)
                    {
                        char beklenen = o.Metin[i];
                        if (i < yazilan.Length)
                        {
                            if (yazilan[i] == beklenen) Yaz(beklenen.ToString(), ConsoleColor.White);
                            else Yaz(beklenen == ' ' ? "·" : beklenen.ToString(), ConsoleColor.White, ConsoleColor.DarkRed);
                        }
                        else if (i == yazilan.Length) Yaz(beklenen.ToString(), ConsoleColor.Black, ConsoleColor.Cyan);
                        else Yaz(beklenen.ToString(), ConsoleColor.DarkGray);
                    }
                    Console.Write(new string(' ', Math.Max(0, Genislik - uzunluk + 2)));
                }
                else Console.Write(new string(' ', Genislik + 2));
            }
        }

        /// <summary>Bir test oynatır. Esc'ye basılırsa null döner.</summary>
        public static Sonuc? Test(int sure)
        {
            while (true)
            {
                var o = Oturum.Rastgele();
                var saat = new Stopwatch();
                Console.Clear();
                Baslik(sure, $"{sure} sn");
                Console.SetCursorPosition(3, Ust + 8);
                Yaz("Yazmaya başlayınca süre işlemeye başlar.", ConsoleColor.DarkGray);
                MetniCiz(o);
                int sonGenislik = Console.WindowWidth;
                bool yeniden = false;
                long sonSaat = -1;

                while (!yeniden)
                {
                    if (saat.IsRunning && saat.Elapsed.TotalSeconds >= sure) return o.Hesapla(TimeSpan.FromSeconds(sure));

                    if (!Console.KeyAvailable)
                    {
                        Thread.Sleep(25);
                        if (Console.WindowWidth != sonGenislik) { sonGenislik = Console.WindowWidth; Console.Clear(); MetniCiz(o); sonSaat = -1; }
                        long kalan = saat.IsRunning ? sure - (long)saat.Elapsed.TotalSeconds : sure;
                        if (kalan != sonSaat)
                        {
                            sonSaat = kalan;
                            var canli = saat.IsRunning ? o.Hesapla(saat.Elapsed) : null;
                            Baslik(sure, canli == null ? $"{sure} sn" : $"{kalan} sn  ·  {canli.Net:0} k/dk");
                        }
                        continue;
                    }

                    var k = Console.ReadKey(intercept: true);
                    switch (k.Key)
                    {
                        case ConsoleKey.Escape: return null;
                        case ConsoleKey.Tab: yeniden = true; continue;
                        case ConsoleKey.Backspace:
                            if (k.Modifiers.HasFlag(ConsoleModifiers.Control)) o.KelimeSil(); else o.Sil();
                            break;
                        default:
                            if (k.KeyChar == '\u007f' || k.KeyChar == '\u0017') { o.KelimeSil(); break; }   // bazı terminallerde Ctrl+Backspace
                            if (char.IsControl(k.KeyChar) || k.KeyChar == '\0') continue;
                            if (!saat.IsRunning)
                            {
                                saat.Start();
                                Console.SetCursorPosition(3, Ust + 8);
                                Console.Write(new string(' ', 50));
                            }
                            o.Tus(k.KeyChar);
                            break;
                    }
                    MetniCiz(o);
                    if (o.Konum >= o.Metin.Length) return o.Hesapla(saat.Elapsed);
                }
            }
        }

        /// <summary>Sonuç ekranı. Enter ya da Tab: yeniden; Esc: çık (false).</summary>
        public static bool SonucGoster(Sonuc s, int sure)
        {
            var eskiRekor = Rekorlar.Kaydet(sure, s.Net);
            Console.Clear();
            Baslik(sure, "bitti");
            int y = Ust + 1;
            void Satir(string etiket, string deger, ConsoleColor renk)
            {
                Console.SetCursorPosition(4, y++);
                Yaz(etiket.PadRight(18), ConsoleColor.Gray);
                Yaz(deger, renk);
            }

            Console.SetCursorPosition(4, y);
            Yaz($"{s.Net:0}", ConsoleColor.Cyan);
            Yaz("  kelime / dakika", ConsoleColor.Gray);
            y += 2;
            Satir("Doğruluk", $"%{s.Dogruluk:0}", s.Dogruluk >= 95 ? ConsoleColor.Green : s.Dogruluk >= 85 ? ConsoleColor.Yellow : ConsoleColor.Red);
            Satir("Ham hız", $"{s.Ham:0} k/dk (hatalar dahil)", ConsoleColor.White);
            Satir("Kelimeler", $"{s.DogruKelime} doğru, {s.YanlisKelime} yanlış", ConsoleColor.White);
            Satir("Tuş vuruşu", s.Vurus.ToString(), ConsoleColor.White);
            y++;
            Console.SetCursorPosition(4, y++);
            if (eskiRekor == null) Yaz($"★ Yeni rekor! ({sure} saniye)", ConsoleColor.Yellow);
            else Yaz($"Rekorun: {eskiRekor:0} k/dk ({sure} saniye)", ConsoleColor.DarkGray);
            y++;
            Console.SetCursorPosition(4, y++);
            Yaz(Seviye(s.Net), ConsoleColor.Gray);
            y++;
            Console.SetCursorPosition(4, y);
            Yaz("Enter: yeniden  ·  Esc: çık", ConsoleColor.DarkGray);

            while (true)
            {
                var k = Console.ReadKey(intercept: true).Key;
                if (k == ConsoleKey.Escape) return false;
                if (k is ConsoleKey.Enter or ConsoleKey.Tab or ConsoleKey.Spacebar) return true;
            }
        }

        static string Seviye(double net) => net switch
        {
            < 20 => "❄ Kar tanesi: acele yok, önce doğru yaz, hız kendiliğinden gelir.",
            < 35 => "⛄ Kardan adam: güzel gidiyor!",
            < 50 => "🛷 Kızak: ortalamanın üstündesin.",
            < 70 => "🌨 Tipi: çok hızlısın!",
            _ => "🧙 Ayaz Ata: parmakların buz gibi keskin!",
        };
    }

    /// <summary>klavye-ayazi --test : yazma motorunu ekran olmadan sınar.</summary>
    static class Testler
    {
        public static int Calistir()
        {
            Console.OutputEncoding = Encoding.UTF8;
            int gecen = 0, kalan = 0;
            void Dogrula(bool k, string ad) { if (k) gecen++; else kalan++; Console.WriteLine((k ? "  ✓ " : "  ✗ ") + ad); }

            var o = new Oturum(new[] { "kar", "yağıyor", "çok", "güzel" });
            foreach (var c in "kar yağıyor ") o.Tus(c);
            var s = o.Hesapla(TimeSpan.FromMinutes(1));
            Dogrula(s.Net == 12 / 5.0, $"12 doğru karakter / 1 dk = 2,4 k/dk ({s.Net})");
            Dogrula(s.DogruKelime == 2 && s.YanlisKelime == 0, "iki kelime doğru");
            Dogrula(s.Dogruluk == 100, "doğruluk %100");

            foreach (var c in "cok") o.Tus(c);   // ç yerine c: bir hata
            s = o.Hesapla(TimeSpan.FromMinutes(1));
            Dogrula(o.HataliVurus == 1, "Türkçe harf hatası sayıldı");
            Dogrula(s.DogruKelime == 2, "bitmemiş kelime sayılmaz");
            o.Tus(' ');
            s = o.Hesapla(TimeSpan.FromMinutes(1));
            Dogrula(s.YanlisKelime == 1, "hatalı kelime biter bitmez yanlış sayılır");

            o.KelimeSil();
            Dogrula(o.Yazilan == "kar yağıyor ", "Ctrl+Backspace son kelimeyi sildi: '" + o.Yazilan + "'");
            foreach (var c in "çok güzel") o.Tus(c);
            s = o.Hesapla(TimeSpan.FromSeconds(30));
            Dogrula(s.DogruKelime == 4, "düzeltince dört kelime doğru");
            Dogrula(Math.Abs(s.Dogruluk - 100.0 * 24 / 25) < 0.01, $"silinen hata doğruluğu yine düşürür: 25 vuruşta 1 hata = %96 ({s.Dogruluk:0.0})");
            Dogrula(Math.Abs(s.Net - 21 / 5.0 * 2) < 0.01, $"30 saniyede 21 doğru karakter = 8,4 k/dk ({s.Net:0.0})");

            o.Tus('x');
            Dogrula(o.Konum == o.Metin.Length, "metnin sonundan öteye yazılmaz");

            var r = Oturum.Rastgele(500);
            var k = r.Metin.Split(' ');
            Dogrula(k.Length == 500 && k.Zip(k.Skip(1)).All(p => p.First != p.Second), "rastgele metin: 500 kelime, art arda tekrar yok");

            Console.WriteLine($"\n{gecen} geçti, {kalan} kaldı");
            return kalan == 0 ? 0 : 1;
        }
    }
}
