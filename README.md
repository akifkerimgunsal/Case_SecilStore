# SecilStore - Dinamik Konfigürasyon Yönetim Sistemi

## Proje Hakkında

SecilStore, dağıtık uygulamalar için dinamik ve merkezi bir konfigürasyon yönetim sistemidir. Bu sistem, web.config, app.config, appsettings.json gibi dosyalarda tutulan yapılandırma değerlerinin ortak ve dinamik bir yapıyla erişilebilir olmasını ve deployment veya restart gerektirmeden güncellenebilmesini sağlar.

## Temel Özellikler

- **Merkezi Depolama**: Tüm konfigürasyon değerleri MongoDB'de merkezi olarak saklanır
- **Dinamik Güncelleme**: Çalışan uygulamaları yeniden başlatmadan konfigürasyon değerlerini güncelleme
- **Tip Güvenliği**: Integer, string, double ve boolean tiplerini destekler
- **Uygulama İzolasyonu**: Her uygulama yalnızca kendi konfigürasyon kayıtlarına erişebilir
- **Kullanıcı Arayüzü**: Konfigürasyonları görüntülemek, düzenlemek ve eklemek için web tabanlı arayüz
- **Filtreleme**: Konfigürasyonları isimlerine göre filtreleme imkanı
- **Merkezi Veri Yönetimi**: Tüm uygulamalar aynı MongoDB veritabanını kullanarak tutarlı veri erişimi sağlar

## Sistem Mimarisi

Proje üç ana bileşenden oluşmaktadır:

1. **SecilStore_ConfigLibrary**: Konfigürasyon değerlerini yöneten temel kütüphane
2. **SecilStore_ConfigWebUI**: Konfigürasyon değerlerini görüntülemek ve yönetmek için basit web arayüzü
3. **Case_SecilStore**: Konfigürasyon yönetimi için tam özellikli web uygulaması

## Teknik Detaylar

### Kütüphane Kullanımı

Kütüphane, aşağıdaki parametrelerle başlatılır:

```csharp
new ConfigurationReader(applicationName, connectionString, refreshTimerIntervalInMs);
```

- **applicationName**: Üzerinde çalışacağı uygulamanın adı
- **connectionString**: Storage bağlantı bilgileri
- **refreshTimerIntervalInMs**: Ne kadar aralıklarla storage'ın kontrol edileceği bilgisi

Konfigürasyon değerlerine erişmek için:

```csharp
T value = _configurationReader.GetValue<T>(key);
```

### Gereksinimlerin Karşılanması

| Gereksinim | Karşılama Yöntemi |
|------------|-------------------|
| .NET 8 Desteği | Proje .NET 8 ile geliştirilmiştir |
| Offline Çalışma | Kütüphane, storage'a erişemediğinde son başarılı konfigürasyon kayıtları ile çalışır |
| Tip Dönüşümü | Kütüphane her tipe ait dönüş bilgisini kendi içerisinde halleder |
| Periyodik Kontrol | Sistem parametrik olarak verilen süre periyodunda yeni kayıtları ve değişiklikleri kontrol eder |
| Uygulama İzolasyonu | Her servis yalnızca kendi konfigürasyon kayıtlarına erişebilir |

## Kurulum ve Çalıştırma

### Gereksinimler

- .NET 8 SDK
- Docker ve Docker Compose (opsiyonel)

### Adımlar

1. Repoyu klonlayın:
   ```
   git clone https://github.com/akifkerimgunsal/Case_SecilStore.git
   ```

2. Proje dizinine gidin:
   ```
   cd Case_SecilStore
   ```

3. Projeyi derleyin:
   ```
   dotnet build
   ```

4. Uygulamayı çalıştırma seçenekleri:

   **a) Docker ile çalıştırma (önerilen):**
   ```
   docker-compose up -d
   ```
   - MongoDB, Redis, RabbitMQ ve SecilStore_ConfigWebUI servisleri otomatik olarak başlatılacaktır
   - SecilStore_ConfigWebUI uygulaması http://localhost:8090 adresinde çalışacaktır
   - MongoDB portu 27017'den dışarı açılmıştır, yerel uygulamalar da buna bağlanabilir

   **b) Visual Studio ile çalıştırma:**
   - Docker Compose'u başlatın: `docker-compose up -d mongodb`
   - Visual Studio'da Case_SecilStore projesini açın
   - F5 tuşuna basarak veya "Başlat" düğmesine tıklayarak uygulamayı çalıştırın
   - Uygulama https://localhost:7296 adresinde çalışacaktır (HTTPS)

   **c) Komut satırı ile çalıştırma:**
   - Docker Compose'u başlatın: `docker-compose up -d mongodb`
   - Yeni bir terminal açın ve şu komutu çalıştırın:
   ```
   cd Case_SecilStore
   dotnet run
   ```
   - Uygulama varsayılan olarak http://localhost:5000 adresinde çalışacaktır (HTTP)
   
   **d) Özel port ile çalıştırma:**
   - Docker Compose'u başlatın: `docker-compose up -d mongodb`
   - Yeni bir terminal açın ve şu komutu çalıştırın:
   ```
   cd Case_SecilStore
   dotnet run --urls="http://localhost:5000"
   ```

5. Tarayıcınızda ilgili adresi açın (çalıştırma yöntemine göre):
   - Visual Studio: https://localhost:7296
   - Komut satırı: http://localhost:5000
   - Docker: http://localhost:8090

### Önemli Not

Her iki uygulama da (localhost:5000 ve localhost:8090) aynı MongoDB veritabanını kullanacak şekilde yapılandırılmıştır. Bu sayede:
- Bir uygulamada yapılan değişiklikler diğerinde de görünür
- Tüm konfigürasyon verileri merkezi olarak yönetilir
- Tutarlı veri erişimi sağlanır

Docker Compose ile çalıştırıldığında, MongoDB otomatik olarak başlatılır ve her iki uygulama da bu MongoDB'ye bağlanır. Yerel uygulama "localhost:27017" adresine, Docker'daki uygulama ise "mongodb:27017" adresine bağlanır (her ikisi de aynı MongoDB container'ına işaret eder).

## Konfigürasyon Değeri Ekleme (Web Arayüzü Üzerinden)

1. Web arayüzünde "New Configuration" butonuna tıklayın
2. Gerekli alanları doldurun:
   - Application Name: Uygulamanın adı (örn. "SERVICE-A")
   - Configuration Name: Konfigürasyon adı (örn. "MaxItemCount")
   - Configuration Type: Değer tipi (örn. "int")
   - Configuration Value: Değer (örn. "50")
   - Active: Aktif olup olmadığı
3. "Save" butonuna tıklayın

## Ekstra Özellikler ve Gereksinimler

Proje, aşağıdaki ekstra puan gereksinimlerini karşılamaktadır:

| Gereksinim | Nasıl Karşılandığı |
|------------|-------------------|
| **Message Broker Kullanımı** | RabbitMQ entegrasyonu ile konfigürasyon değişikliklerinin anlık olarak diğer servislere iletilmesi sağlanmıştır. Docker Compose ile RabbitMQ servisi otomatik olarak başlatılır. |
| **TPL, async/await Kullanımı** | Tüm veritabanı işlemleri ve servis çağrıları async/await pattern kullanılarak asenkron olarak gerçekleştirilmiştir. Bu sayede uygulama daha verimli çalışır ve bloklanma olmaz. |
| **Concurrency Problemlerini Engelleme** | MemoryConfigurationCache sınıfında thread-safe önbellekleme mekanizması ve MongoDB işlemlerinde optimistic concurrency control uygulanmıştır. |
| **Design & Architectural Pattern'ler** | Repository Pattern, Dependency Injection, Factory Pattern ve Observer Pattern gibi tasarım desenleri kullanılmıştır. |
| **TDD Yaklaşımı** | Proje geliştirme sürecinde Test-Driven Development yaklaşımı benimsenmiş, önce testler yazılmış sonra kodlar geliştirilmiştir. |
| **Unit Testler** | SecilStore_ConfigLibrary.Tests projesi altında kapsamlı birim testleri bulunmaktadır. Repository sınıfları ve önbellekleme mekanizmaları test edilmiştir. |
| **MongoDB ve Redis Kullanımı** | Konfigürasyon verileri için MongoDB, önbellekleme için Redis kullanılmıştır. Her iki veritabanı da Docker Compose ile otomatik olarak başlatılır. |
| **Çalışır Halde Gönderim** | Proje, kurulum adımları takip edildiğinde herhangi bir ek konfigürasyon gerektirmeden çalışır durumdadır. |
| **Proje Dokümantasyonu** | Bu README dosyası ve kod içindeki XML belgelendirmeleri ile kapsamlı dokümantasyon sağlanmıştır. |
| **Source Control Üzerinden Paylaşım** | Proje GitHub üzerinden paylaşılmıştır: https://github.com/akifkerimgunsal/Case_SecilStore |
| **Docker Compose ile Çalıştırılabilirlik** | Tüm ekosistem (MongoDB, Redis, RabbitMQ ve web uygulaması) docker-compose.yml dosyası ile tek komutla başlatılabilir. |