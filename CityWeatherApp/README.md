
# CityWeatherApp

CityWeatherApp, seçtiğiniz şehirlerin hava durumu bilgilerini görüntüleyebilmenizi sağlayan bir .NET Core projesidir. Uygulama, favori şehirlerinizi ekleyerek  sıcaklıkları hızlıca görmenizi sağlar ve en sıcak ve en soğuk şehirleri listeleme özelliği sunar.

## Özellikler

- Şehirlerin güncel hava durumu bilgilerini görüntüleme
- Favori şehirlerinizi ekleme çıkarma.
- En sıcak ve en soğuk şehirleri listeleme.
- Serilog entegrasyonu ile hata ve olay kayıtlarını loglara kaydetme.
- Redis Cache ile performansı artırımı ile api istekleri daha az çalılşır.
- Unit Testler CityWeatgerTestsApp de yer almakadır.

### Gereksinimler

- [.NET 6 SDK]
- [Redis]
- [MSSQL Server]
- [Docker] 
- MSSQL DB üzerinde çalışılmıştır.
- Docker compose için yml dosyası proje içinde yer almaktadır.
- DB şeması aşağıda yer almaktadır. 

## Proje Yapısı

- **CityWeatherApp**: Uygulamanın ana projesi.
- **CityWeatherApp.Tests**: Birim testleri içerir.
- **Serilog**: Loglama işlemleri için kullanılır, hatalar ve olaylar veritabanına kaydedilir.
- **Redis Cache**: Hava durumu sorgularını cache’leyerek performansı artırır.

## By
Ozan Ekici

## DB Şema
CREATE DATABASE CityWeatherAppDb;
GO

CREATE TABLE FavoriteCities (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100),
    TempC FLOAT
);
GO
CREATE TABLE WeatherData (
    Id INT PRIMARY KEY IDENTITY,
    LocationName NVARCHAR(100),
    TempC FLOAT,
    LastUpdated DATETIME
);
