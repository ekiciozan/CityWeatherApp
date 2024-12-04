// CityWeatherApp.Tests/CityWeatherAppTests.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CityWeatherApp.Data;
using CityWeatherApp.Models;
using CityWeatherApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CityWeatherApp.Tests
{
    [TestClass]
    public class CityWeatherAppTests
    {
        private FavoriteCityService _service;
        private Mock<CityWeatherAppContext> _dbContextMock;
        private Mock<ILogger<FavoriteCityService>> _loggerMock;

        [TestInitialize]
        public void Setup()
        {
            // Mock DbContext ve Logger
            _dbContextMock = new Mock<CityWeatherAppContext>(new DbContextOptions<CityWeatherAppContext>());
            _loggerMock = new Mock<ILogger<FavoriteCityService>>();

            // FavoriteCityService'i oluştur
            _service = new FavoriteCityService(_dbContextMock.Object, null, _loggerMock.Object);
        }

        [TestMethod]
        public async Task AddToFavoritesAsync_ShouldAddCity_WhenCityDoesNotExist()
        {
            // Arrange
            var mockSet = new Mock<DbSet<FavoriteCity>>();
            _dbContextMock.Setup(m => m.FavoriteCities).Returns(mockSet.Object);

            var city = new FavoriteCity { Name = "London", AverageTempC = 15 };

            // Act
            var result = await _service.AddToFavoritesAsync(city);

            // Assert
            Assert.IsTrue(result); // Şehir eklendi mi?
            mockSet.Verify(m => m.Add(It.IsAny<FavoriteCity>()), Times.Once); // Şehir eklendi mi?
            _dbContextMock.Verify(m => m.SaveChangesAsync(default), Times.Once); // SaveChanges çağrıldı mı?
            _loggerMock.Verify(l => l.LogInformation(" {CityName} Sehri favorilere eklendi.", city.Name), Times.Once); // Loglandı mı?
        }

        [TestMethod]
        public async Task AddToFavoritesAsync_ShouldNotAddCity_WhenCityAlreadyExists()
        {
            // Arrange
            var city = new FavoriteCity { Name = "London", AverageTempC = 15 };

            var data = new List<FavoriteCity> { city }.AsQueryable();
            var mockSet = new Mock<DbSet<FavoriteCity>>();
            mockSet.As<IQueryable<FavoriteCity>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<FavoriteCity>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<FavoriteCity>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<FavoriteCity>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            _dbContextMock.Setup(m => m.FavoriteCities).Returns(mockSet.Object);

            // Act
            var result = await _service.AddToFavoritesAsync(city);

            // Assert
            Assert.IsFalse(result); // Şehir zaten ekli, eklenmemeli
            _loggerMock.Verify(l => l.LogWarning("Eklenmek istenen {CityName} şehir zaten listede.", city.Name), Times.Once); // Loglandı mı?
        }

        [TestMethod]
        public async Task RemoveFromFavoritesAsync_ShouldRemoveCity_WhenCityExists()
        {
            // Arrange
            var city = new FavoriteCity { Name = "London", AverageTempC = 15 };

            var mockSet = new Mock<DbSet<FavoriteCity>>();
            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).ReturnsAsync(city);

            _dbContextMock.Setup(m => m.FavoriteCities).Returns(mockSet.Object);

            // Act
            await _service.RemoveFromFavoritesAsync(city.Name);

            // Assert
            mockSet.Verify(m => m.Remove(It.IsAny<FavoriteCity>()), Times.Once); // Şehir silindi mi?
            _dbContextMock.Verify(m => m.SaveChangesAsync(default), Times.Once); // SaveChanges çağrıldı mı?
            _loggerMock.Verify(l => l.LogInformation("{CityName} favorilerden kaldirildi.", city.Name), Times.Once); // Loglandı mı?
        }

        [TestMethod]
        public async Task GetAllFavoriteCitiesAsync_ShouldReturnAllCities()
        {
            // Arrange
            var data = new List<FavoriteCity>
            {
                new FavoriteCity { Name = "London", AverageTempC = 15 },
                new FavoriteCity { Name = "Paris", AverageTempC = 20 }
            }.AsQueryable();

            var mockSet = new Mock<DbSet<FavoriteCity>>();
            mockSet.As<IQueryable<FavoriteCity>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<FavoriteCity>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<FavoriteCity>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<FavoriteCity>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            _dbContextMock.Setup(m => m.FavoriteCities).Returns(mockSet.Object);

            // Act
            var cities = await _service.GetAllFavoriteCitiesAsync();

            // Assert
            Assert.AreEqual(2, cities.Count);
            Assert.AreEqual("London", cities[0].Name);
            Assert.AreEqual("Paris", cities[1].Name);
            _loggerMock.Verify(l => l.LogInformation("Database'den tüm favori şehirler alınıyor."), Times.Once); // Loglandı mı?
        }
    }
}
