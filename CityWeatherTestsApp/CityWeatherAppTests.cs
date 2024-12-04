using System.Collections.Generic;
using System.Threading.Tasks;
using CityWeatherApp.Data;
using CityWeatherApp.Models;
using CityWeatherApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CityWeatherApp.Dto;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;
using Microsoft.Extensions.Configuration;
using Castle.Core.Configuration;
using System.Diagnostics;
using StackExchange.Redis;


namespace CityWeatherApp.Tests
{
    [TestClass]
    public class FavoriteCityServiceTests
    {
        private FavoriteCityService _service;
        private CityWeatherAppContext _dbContext;
        private Mock<ILogger<FavoriteCityService>> _loggerMock;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<CityWeatherAppContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _dbContext = new CityWeatherAppContext(options);
            _loggerMock = new Mock<ILogger<FavoriteCityService>>();
            _service = new FavoriteCityService(_dbContext, null, _loggerMock.Object);
    
        }

        [TestMethod]
        public async Task AddToFavoritesAsync_WhenCityIsNotInFavorites()
        {
            // Arrange
            var favoriteCity = new FavoriteCity { Name = "Istanbul" };

            // Act
            var result = await _service.AddToFavoritesAsync(favoriteCity);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, await _dbContext.FavoriteCities.CountAsync());
        }

        [TestMethod]
        public async Task RemoveFromFavoritesAsync_ShouldRemoveCity_WhenCityExistsInFavorites()
        {
            // Arrange
            var favoriteCity = new FavoriteCity { Name = "Istanbul" };
            _dbContext.FavoriteCities.Add(favoriteCity);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.RemoveFromFavoritesAsync("Istanbul");

            // Assert
            Assert.AreEqual(0, await _dbContext.FavoriteCities.CountAsync());
        }
    }
    [TestClass]
    public class WeatherServiceTests
    {
        private WeatherService _weatherService;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Mock<ILogger<WeatherService>> _loggerMock;
        private Mock<Microsoft.Extensions.Configuration.IConfiguration> _configurationMock;
        private Mock<IConnectionMultiplexer> _redisConnection;
        private CityWeatherAppContext _dbContext;

        private const string ApiKey = "test_api_key";

        [TestInitialize]
        public void Setup()
        {
            // InMemory veritabanı seçenekleriyle bir DbContext oluşturun
            var options = new DbContextOptionsBuilder<CityWeatherAppContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _dbContext = new CityWeatherAppContext(options);

            // HttpClient ve HttpMessageHandler Mock
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<WeatherService>>();
            _redisConnection = new Mock<IConnectionMultiplexer>();

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);

            // IConfiguration Mock
            _configurationMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _configurationMock.Setup(config => config["WeatherApi:ApiKey"]).Returns(ApiKey);
            var dbMock = new Mock<IDatabase>();
            _redisConnection.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

            // WeatherService örneği
            _weatherService = new WeatherService(httpClient, _configurationMock.Object, _loggerMock.Object, _dbContext, _redisConnection.Object);
        }

        [TestMethod]
        public async Task GetWeatherAsync_ShouldReturnWeatherInfo_WhenApiResponseIsSuccessful()
        {
            // Arrange
            var cityName = "Istanbul";
            var expectedWeatherInfo = new WeatherInfo
            {
                Location = new Location { Name = "Istanbul" },
                Current = new CurrentWeather { TempC = 20 }
            };

            var jsonContent = JsonConvert.SerializeObject(expectedWeatherInfo);
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonContent)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _weatherService.GetWeatherAsync2(cityName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedWeatherInfo.Location.Name, result.Location.Name);
            Assert.AreEqual(expectedWeatherInfo.Current.TempC, result.Current.TempC);
        }

        [TestMethod]
        public async Task GetWeatherAsync_ShouldReturnNull_WhenApiResponseIsFailure()
        {
            // Arrange
            var cityName = "NonExistentCity";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _weatherService.GetWeatherAsync2(cityName);

            // Assert
            Assert.IsNull(result);
        }


        /// <summary>
        /// Verilen bir şehir için `GetWeatherAsync2` metodunun en fazla 5 saniye bekleyip tek bir API isteği
        /// yaptığını doğrular. Eğer 5 saniye içinde başka bir talep yoksa, tek bir sorgu yapılır ve cevap dönülür.
        /// Metodun doğru bir şekilde beklediğini ve API'den alınan yanıtın doğru olduğunu test eder.
        /// </summary>
        [TestMethod]
        public async Task GetWeatherAsync2_ShouldWaitFor5SecondsForSingleRequest()
        {
            // Arrange
            var cityName = "Istanbul";
            var expectedWeatherInfo = new WeatherInfo
            {
                Location = new Location { Name = cityName },
                Current = new CurrentWeather { TempC = 20 }
            };

            var jsonContent = JsonConvert.SerializeObject(expectedWeatherInfo);
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonContent)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _weatherService.GetWeatherAsync2(cityName);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(stopwatch.Elapsed >= TimeSpan.FromSeconds(5), "Talep 5 saniye bekletilmedi.");
            Assert.AreEqual(expectedWeatherInfo.Location.Name, result.Location.Name);
            Assert.AreEqual(expectedWeatherInfo.Current.TempC, result.Current.TempC);
        }


        /// <summary>
        /// Bir şehir için 5 saniye içerisinde yapılan birden fazla isteğin
        /// gruplandırılarak tek bir API çağrısı ile yanıtlanmasını test eder.
        /// Bu test, gelen taleplerin bekleme süresi içinde toplandığını, yalnızca bir API çağrısı yapıldığını 
        /// ve aynı yanıtın tüm taleplerle paylaşıldığını doğrular.
        /// İlk talep gönderildikten 3 saniye sonra ikinci talep gönderilir ve 
        /// her iki talep de aynı hava durumu yanıtını alır.
        /// </summary>
        [TestMethod]
        public async Task GetWeatherAsync2_ShouldReturnSameResponse_ForMultipleRequestsWithin5Seconds()
        {
            // Arrange
            var cityName = "Istanbul";
            var expectedWeatherInfo = new WeatherInfo
            {
                Location = new Location { Name = cityName },
                Current = new CurrentWeather { TempC = 25 }  
            };

            var jsonContent = JsonConvert.SerializeObject(expectedWeatherInfo);
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonContent)
            };

        
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var weatherService = new WeatherService(
                new HttpClient(_httpMessageHandlerMock.Object),
                _configurationMock.Object,
                _loggerMock.Object, _dbContext, _redisConnection.Object);

            var task1 = weatherService.GetWeatherAsync2(cityName);
            await Task.Delay(3000); 
            var task2 = weatherService.GetWeatherAsync2(cityName);

            // Act
            var result1 = await task1;
            var result2 = await task2;

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.AreEqual(expectedWeatherInfo.Location.Name, result1.Location.Name);
            Assert.AreEqual(expectedWeatherInfo.Current.TempC, result1.Current.TempC);

            Assert.AreEqual(result1.Location.Name, result2.Location.Name, "İki talep aynı yanıtı almalı");
            Assert.AreEqual(result1.Current.TempC, result2.Current.TempC, "İki talep aynı sıcaklık değerini almalı");

            // API çağrısının yalnızca bir kez yapılmış olduğunu doğrula
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }


        /// <summary>
        /// `GetWeatherAsync2` metodu için ilk API çağrısının başarısız olduğu durumda 
        /// ikinci API anahtarı ile yeniden çağrı yapıldığını test eder. 
        /// Bu test, ilk API çağrısının hata yanıtı döndürmesi durumunda ikinci API'ye geçiş yapıldığını
        /// ve bu yedek API'nin başarılı bir yanıt döndürdüğünü doğrular.
        /// İki API çağrısı yapılması beklenir: birinci çağrı başarısız olur, ikinci çağrı başarılı bir yanıt döner.
        /// </summary>
        [TestMethod]
        public async Task GetWeatherAsync2_ShouldFallbackToSecondApiKey_WhenFirstApiFails()
        {
            // Arrange
            var cityName = "Istanbul";
            var expectedWeatherInfo = new WeatherInfo
            {
                Location = new Location { Name = cityName },
                Current = new CurrentWeather { TempC = 22 }
            };

            var jsonContent = JsonConvert.SerializeObject(expectedWeatherInfo);
            var successResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonContent)
            };

            var failureResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            };

            _httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(failureResponse) // İlk çağrı başarısız
                .ReturnsAsync(successResponse); // İkinci çağrı başarılı

            var weatherService = new WeatherService(
                new HttpClient(_httpMessageHandlerMock.Object),
                _configurationMock.Object,
                _loggerMock.Object, _dbContext, _redisConnection.Object);

            // Act
            var result = await weatherService.GetWeatherAsync2(cityName);

            // Assert
            Assert.IsNotNull(result, "İkinci API çağrısının başarılı bir yanıt döndürmesi bekleniyor.");
            Assert.AreEqual(expectedWeatherInfo.Location.Name, result.Location.Name);
            Assert.AreEqual(expectedWeatherInfo.Current.TempC, result.Current.TempC);

            // API çağrılarının iki kez yapıldığını doğrula (birincisi başarısız, ikincisi başarılı)
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }


        /// <summary>
        /// İki API çağrısının da başarısız olması durumunda veritabanından hava durumu verisinin getirilip getirilmediğini test eder.
        /// Bu test, GetWeatherAsync2 metodunun hem birinci hem de ikinci API çağrısından başarısız yanıt alması durumunda,
        /// veritabanına başvurarak hava durumu verisini döndürdüğünü doğrular. Eğer veritabanında da veri yoksa null döndürmesi beklenir.
        /// </summary>
        [TestMethod]
        public async Task GetWeatherAsync2_ShouldReturnDataFromDatabase_WhenBothApisFail()
        {
            // Arrange
            var cityName = "Istanbul";
            var expectedWeatherInfo = new WeatherInfo
            {
                Location = new Location { Name = cityName },
                Current = new CurrentWeather { TempC = 18 }
            };

            
            var dbWeatherData = new WeatherData
            {
                LocationName = cityName,
                TempC = 18
            };

           
            _dbContext.WeatherData.Add(dbWeatherData);
            await _dbContext.SaveChangesAsync();

           
            var failureResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            };

            _httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(failureResponse) 
                .ReturnsAsync(failureResponse); 

            var weatherService = new WeatherService(
                new HttpClient(_httpMessageHandlerMock.Object),
                _configurationMock.Object,
                _loggerMock.Object,
                _dbContext , _redisConnection.Object
            );

            // Act
            var result = await weatherService.GetWeatherAsync2(cityName);

            // Assert
            Assert.IsNotNull(result, "Veritabanından bir yanıt döndürülmesi bekleniyor.");
            Assert.AreEqual(expectedWeatherInfo.Location.Name, result.Location.Name, "Veritabanındaki şehir adı eşleşmeli.");
            Assert.AreEqual(expectedWeatherInfo.Current.TempC, result.Current.TempC, "Veritabanındaki sıcaklık değeri eşleşmeli.");

            
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// Aynı şehir için 5 saniyelik bekleme süresi içinde birden fazla isteğin 
        /// gruplandırılarak tek bir API çağrısı ile yanıtlanmasını test eder.
        /// İlk isteğin yapıldıktan 3 saniye sonra aynı şehir için ikinci bir 
        /// isteğin geldiği durumda her iki isteğin de tek bir API çağrısı ile aynı yanıtı 
        /// alacağını doğrular.
        /// İlk talep gönderildikten 3 saniye sonra ikinci talep gönderilir ve her iki talep 
        /// de bekleme süresi içinde gruplandığı için API'ye yalnızca bir kez sorgulama yapılır.
        /// Her iki talebin aynı yanıtı aldığı ve ikinci isteğin yaklaşık 3 saniye 
        /// sonra sonuçlandığı IsTrue ile doğrulanır..
        /// </summary>

        [TestMethod]
        public async Task GetWeatherAsync2_ShouldReturnSameResponse_ForRequestsWithin5Seconds()
        {
            // Arrange
            var cityName = "Istanbul";
            var expectedWeatherInfo = new WeatherInfo
            {
                Location = new Location { Name = cityName },
                Current = new CurrentWeather { TempC = 25 }  
            };

            var jsonContent = JsonConvert.SerializeObject(expectedWeatherInfo);
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonContent)
            };

            // API yanıtını mocklama
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var weatherService = new WeatherService(
                new HttpClient(_httpMessageHandlerMock.Object),
                _configurationMock.Object,
                _loggerMock.Object,
                _dbContext,
                _redisConnection.Object);

            // Act
           
            var task1 = weatherService.GetWeatherAsync2(cityName);
            await Task.Delay(3000); 
            var task2 = weatherService.GetWeatherAsync2(cityName);

            var stopwatch = Stopwatch.StartNew();
            var result1 = await task1;
            var result2 = await task2;
            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result1, "İlk isteğin sonucu null olmamalıdır.");
            Assert.IsNotNull(result2, "İkinci isteğin sonucu null olmamalıdır.");
            Assert.AreEqual(expectedWeatherInfo.Location.Name, result1.Location.Name, "İlk istekte gelen lokasyon adı yanlış.");
            Assert.AreEqual(expectedWeatherInfo.Current.TempC, result1.Current.TempC, "İlk istekte gelen sıcaklık değeri yanlış.");
            Assert.AreEqual(result1.Location.Name, result2.Location.Name, "İki talep aynı yanıtı almalıdır.");
            Assert.AreEqual(result1.Current.TempC, result2.Current.TempC, "İki talep aynı sıcaklık değerini almalıdır.");
            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds >= 3, "İkinci talep, ilk talebin beklemesinin ortasında gerçekleşmeli ve yaklaşık 3 saniye sonra yanıtlanmalı.");

            
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// `FetchWeatherFromApis` metodunun bir şehir için iki farklı API anahtarı kullanarak
        /// veri çekmeye çalıştığını test eder. İlk API anahtarı ile yapılan çağrının başarısız 
        /// olması durumunda ikinci API anahtarını kullanarak veri çekmeye çalışır.
        /// Bu test, ilk API yanıtının başarısız olduğu senaryoda ikinci API çağrısının yapıldığını
        /// ve doğru bir yanıt döndüğünü doğrular.
        /// </summary>
        [TestMethod]
        public async Task FetchWeatherFromApis_ShouldFallbackToSecondApi_WhenFirstApiFails()
        {
            // Arrange
            var cityName = "Istanbul";
            var expectedWeatherInfo = new WeatherInfo
            {
                Location = new Location { Name = cityName },
                Current = new CurrentWeather { TempC = 20 }
            };

            var jsonContent = JsonConvert.SerializeObject(expectedWeatherInfo);
            var successResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonContent)
            };

            var failureResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError 
            };

           
            _httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(failureResponse)  
                .ReturnsAsync(successResponse); 

            var weatherService = new WeatherService(
                new HttpClient(_httpMessageHandlerMock.Object),
                _configurationMock.Object,
                _loggerMock.Object,
                _dbContext,
                _redisConnection.Object
            );

            // Act
            var result = await weatherService.FetchWeatherFromApis(cityName);

            // Assert
            Assert.IsNotNull(result, "İkinci API çağrısının başarılı bir yanıt döndürmesi bekleniyor.");
            Assert.AreEqual(expectedWeatherInfo.Location.Name, result.Location.Name, "Şehir adı doğru dönmedi.");
            Assert.AreEqual(expectedWeatherInfo.Current.TempC, result.Current.TempC, "Sıcaklık değeri doğru dönmedi.");

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// `FetchWeatherFromDatabase` metodunu test eder.
        /// Bu test, verilen bir şehir için veritabanında kayıtlı bir hava durumu verisi olup olmadığını kontrol eder.
        /// Eğer veri varsa `WeatherInfo` tipinde geri döner, yoksa `null` döner.
        /// Test, veritabanında veri olduğunda ve olmadığında metodun doğru sonuçlar döndürdüğünü doğrular.
        /// </summary>
        [TestMethod]
        public async Task FetchWeatherFromDatabase_ShouldReturnWeatherInfo_WhenDataExistsInDatabase()
        {
            // Arrange
            var cityName = "Istanbul";
            var dbWeatherData = new WeatherData
            {
                LocationName = cityName,
                TempC = 22
            };

            _dbContext.WeatherData.Add(dbWeatherData);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _weatherService.FetchWeatherFromDatabase(cityName);

            // Assert
            Assert.IsNotNull(result, "Veritabanında veri olduğu durumda `WeatherInfo` nesnesi dönmelidir.");
            Assert.AreEqual(cityName, result.Location.Name, "Dönen şehir adı veritabanındakiyle eşleşmelidir.");
            Assert.AreEqual(dbWeatherData.TempC, result.Current.TempC, "Dönen sıcaklık değeri veritabanındakiyle eşleşmelidir.");
        }

    }

}
