
# CityWeatherApp

CityWeatherApp, seçtiğiniz şehirlerin hava durumu bilgilerini görüntüleyebilmenizi sağlayan bir .NET Core projesidir. Uygulama, favori şehirlerinizi ekleyerek ortalama sıcaklıkları hızlıca görmenizi sağlar ve en sıcak ve en soğuk şehirleri listeleme özelliği sunar.

## Özellikler

- Şehirlerin güncel hava durumu bilgilerini görüntüleyin.
- Favori şehirlerinizi ekleyin ve ortalama sıcaklık bilgilerini hızlıca inceleyin.
- En sıcak ve en soğuk şehirleri listeleyin.
- Serilog entegrasyonu ile hata ve olay kayıtlarını veritabanına ve dosyaya kaydedin.
- Redis Cache ile performansı artırın; tekrar eden sorgularda veriler cache üzerinden alınır.
  


### Gereksinimler

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) veya üzeri
- [Redis](https://redis.io/download) (Cache için)
- [MSSQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Veritabanı için)
- Docker (isteğe bağlı, Redis ve MSSQL gibi servisleri yönetmek için)

### Başlangıç

1. Bu repository'yi klonlayın:

   ```bash
   git clone https://github.com/kullanici-adi/CityWeatherApp.git
   cd CityWeatherApp
   ```

2. Gerekli bağımlılıkları yükleyin:

   ```bash
   dotnet restore
   ```

3. Veritabanı bağlantı ayarlarını `appsettings.json` dosyasında yapılandırın. MSSQL bağlantısı için:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=CityWeatherAppDb;User Id=sa;Password=YourPassword;"
   }
   ```

4. Redis ve MSSQL gibi servisleri Docker ile başlatmak için Docker Compose kullanabilirsiniz. Docker Compose dosyasını (`docker-compose.yml`) şu şekilde yapılandırın:

   ```yaml
   version: '3.8'
   services:
     redis:
       image: "redis:latest"
       ports:
         - "6379:6379"
     mssql:
       image: "mcr.microsoft.com/mssql/server"
       environment:
         - ACCEPT_EULA=Y
         - SA_PASSWORD=YourPassword
       ports:
         - "1433:1433"
   ```

5. Docker servislerini başlatın:

   ```bash
   docker-compose up -d
   ```

6. Uygulamayı çalıştırın:

   ```bash
   dotnet run --project CityWeatherApp
   ```

## Kullanım

- `http://localhost:5000` adresinden uygulamaya erişebilirsiniz.
- API istekleri için Swagger kullanabilirsiniz: `http://localhost:5000/swagger`

## Proje Yapısı

- **CityWeatherApp**: Uygulamanın ana projesi.
- **CityWeatherApp.Tests**: Birim testleri içerir.
- **Serilog**: Loglama işlemleri için kullanılır, hatalar ve olaylar veritabanına kaydedilir.
- **Redis Cache**: Hava durumu sorgularını cache’leyerek performansı artırır.

## Katkıda Bulunma

1. Bu repository'yi forklayın.
2. Yeni bir branch oluşturun: `git checkout -b yeni-ozellik`.
3. Yaptığınız değişiklikleri commit edin: `git commit -m 'Yeni bir özellik eklendi'`.
4. Branch’i pushlayın: `git push origin yeni-ozellik`.
5. Bir Pull Request oluşturun.

## Lisans

Bu proje MIT Lisansı ile lisanslanmıştır. Daha fazla bilgi için `LICENSE` dosyasına bakın.
