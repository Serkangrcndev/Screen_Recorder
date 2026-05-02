<div align="center">

# 🎬 Screen Recorder

**Kurumsal Düzeyde Windows Ekran Kayıt Çözümü**

[![.NET](https://img.shields.io/badge/.NET%2010.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows%2010%2F11-0078D6?style=flat-square&logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![FFmpeg](https://img.shields.io/badge/FFmpeg-000000?style=flat-square&logo=ffmpeg&logoColor=white)](https://ffmpeg.org/)
[![VB.NET](https://img.shields.io/badge/VB.NET-512BD4?style=flat-square&logo=visual-basic&logoColor=white)](https://docs.microsoft.com/dotnet/visual-basic/)

<img width="429" height="83" alt="Ekran görüntüsü 2026-05-02 220155" src="https://github.com/user-attachments/assets/737e1cc4-9c48-4652-b0a9-6644e6202ec0" />
<img width="1180" height="185" alt="Ekran görüntüsü 2026-05-02 220207" src="https://github.com/user-attachments/assets/1ce6cccd-d11f-484b-9050-baacbcc49740" />


</div>

## 📋 Proje Genel Bakış

Screen Recorder, Windows işletim sistemi için geliştirilmiş, kurumsal kullanıma uygun, yüksek performanslı bir ekran kayıt uygulamasıdır. Modern mimarisi, kapsamlı özellik seti ve kullanıcı dostu arayüzü ile profesyonel ekran kayıt ihtiyaçlarını karşılamak üzere tasarlanmıştır.

---

## ✨ Temel Özellikler

| Özellik | Açıklama |
|---------|----------|
| 🎥 **HD Ekran Kaydı** | Yüksek kaliteli, düşük gecikmeli ekran yakalama |
| 🔴 **Yüzen Kontrol Paneli** | Her zaman erişilebilir, sürüklenebilir kontrol arayüzü |
| ⌨️ **Küresel Kısayol Tuşları** | Uygulama arka plandayken bile çalışan hotkey desteği |
| 🎙️ **Profesyonel Ses Kaydı** | Sistem ve mikrofon sesi senkronizasyonu (NAudio entegrasyonu) |
| 🔒 **Gelişmiş Gizlilik Modu** | Hassas alanların dinamik maskeleme özelliği |
| 🎬 **FFmpeg Kodlama** | Endüstri standardı video kodlama ve işleme |
| ⚙️ **Gelişmiş Ayarlar** | Kapsamlı özelleştirme seçenekleri |
| 🖥️ **Sistem Entegrasyonu** | Windows sistem tepsisi ve bildirim desteği |
| ✂️ **Video Düzenleme** | Dahili video kırpma ve düzenleme araçları |
| 📊 **Performans Optimizasyonu** | Kaynak kullanımı optimizasyonu ve yönetimi |

---

## 🏗️ Sistem Mimarisi

```
screen_recorder/
├── 📁 Core/                    # Çekirdek İşlem Katmanı
│   ├── ScreenCaptureEngine.vb  # Ekran yakalama motoru
│   ├── FFmpegEncoder.vb        # Video kodlama/modülasyon
│   ├── AudioCapture.vb         # Ses yakalama ve işleme
│   ├── FFmpegInstaller.vb      # Otomatik bağımlılık kurulumu
│   ├── GlobalHotkeyManager.vb  # Küresel kısayol yönetimi
│   └── PerformanceManager.vb   # Sistem optimizasyonu
│
├── 📁 UI/                      # Kullanıcı Arayüzü Katmanı
│   ├── FloatingControlBar.vb   # Yüzen kontrol paneli
│   ├── SettingsForm.vb         # Ayarlar penceresi
│   ├── PrivacyMaskForm.vb      # Gizlilik maskeleme arayüzü
│   ├── InstallerWizard.vb      # Kurulum sihirbazı
│   ├── SystemTrayManager.vb    # Sistem tepsisi yönetimi
│   ├── VideoTrimEditor.vb      # Video düzenleme arayüzü
│   ├── OverlayDrawingForm.vb   # Çizim ve annotasyon
│   └── FluentTheme.vb          # UI tema motoru
│
├── 📁 Models/                  # Veri ve Model Katmanı
│   └── RecordingSettings.vb    # Kayıt ayarları veri modeli
│
├── 📁 My Project/              # Uygulama Yapılandırması
│   └── Application.myapp       # Windows Forms uygulama tanımı
│
└── MainController.vb           # Ana uygulama denetleyicisi
```

---

## 💻 Teknoloji Yığını

| Bileşen | Teknoloji | Sürüm |
|---------|-----------|-------|
| **Framework** | .NET Windows Forms | 10.0 |
| **Hedef Platform** | Windows | 10.0.22621.0+ |
| **Minimum OS** | Windows | 10.0.19041.0 |
| **Programlama Dili** | VB.NET | - |
| **Medya İşleme** | FFmpeg | Latest |
| **Ses İşleme** | NAudio | 2.2.1 |
| **Sistem Yönetimi** | System.Management | 8.0.0 |

---

## 🚀 Kurulum ve Derleme

### Gereksinimler

- Windows 10 veya Windows 11 (x64)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 veya VS Code (önerilen)
- FFmpeg (otomatik kurulum destekli)

### Hızlı Başlangıç

```bash
# 1. Repoyu klonlayın
git clone [repository-url]
cd screen_recorder

# 2. Projeyi derleyin
dotnet build screen_recorder.slnx --configuration Release

# 3. Uygulamayı çalıştırın
dotnet run --project screen_recorder/screen_recorder.vbproj
```

### Visual Studio ile Kurulum

1. `screen_recorder.slnx` dosyasını Visual Studio'da açın
2. NuGet paketlerinin geri yüklendiğinden emin olun
3. `F5` ile derleyin ve çalıştırın

---

## ⌨️ Klavye Kısayolları

| Kısayol | İşlem |
|---------|-------|
| `F9` | Kayıt Başlat / Durdur |
| `F10` | Kayıt Duraklat / Devam |
| `F11` | Gizlilik Modu Aç / Kapat |

> **Not:** Tüm kısayol tuşları ayarlar menüsünden özelleştirilebilir.

---

## 🛠️ Yapılandırma

Uygulama ayarları `config.json` dosyası üzerinden yönetilebilir:

```json
{
  "OutputPath": "C:\\Kayitlar",
  "VideoFormat": "mp4",
  "VideoCodec": "h264",
  "AudioCodec": "aac",
  "FrameRate": 30,
  "Quality": "high",
  "Hotkeys": {
    "StartStop": "F9",
    "PauseResume": "F10",
    "PrivacyMode": "F11"
  }
}
```

---

## 📦 Dağıtım

### Bağımsız Uygulama Olarak Paketleme

```bash
# Tek dosya yürütülebilir olarak yayınlayın
dotnet publish screen_recorder/screen_recorder.vbproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish
```

---

## 🔐 Güvenlik ve Gizlilik

- **Gizlilik Modu:** Hassas ekran bölgelerini otomatik maskeleme
- **Yerel İşleme:** Tüm video işleme yerel olarak yapılır, bulut bağlantısı yok
- **Güvenli Kayıt:** Kayıtlar kullanıcı tanımlı dizinde şifrelenmiş olarak saklanabilir

---

## 📝 Kurumsal Kullanım Senaryoları

- 📹 **Eğitim Materyali Hazırlama** - E-learning içerik üretimi
- 🎮 **Oyun Kaydı ve Yayın** - Profesyonel gameplay kayıtları
- 💼 **Yazılım Tanıtım Videoları** - Ürün demonstrasyonları
- 📊 **Rapor ve Analiz** - Ekran aktivite kayıtları
- 🔧 **Teknik Destek** - Hata bildirimleri ve çözüm videoları
- 👨‍💻 **Kod İnceleme ve Pair Programming** - Asenkron işbirliği

---

## 🤝 Katkıda Bulunma

Kurumsal projeler için katkı rehberi ve süreçleri için lütfen proje yöneticisi ile iletişime geçin.

---

## 📄 Lisans

Bu proje kurumsal özel lisans altında lisanslanmıştır. Tüm hakları saklıdır.

---

<div align="center">

**[Proje Adı]** Kurumsal Yazılım Çözümleri

📧 [destek@sirketiniz.com](mailto:destek@sirketiniz.com) | 🌐 [www.sirketiniz.com](https://www.sirketiniz.com)

</div>
