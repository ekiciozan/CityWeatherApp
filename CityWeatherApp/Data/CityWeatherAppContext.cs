using Microsoft.EntityFrameworkCore;
using CityWeatherApp.Models;
namespace CityWeatherApp.Data
{
    public class CityWeatherAppContext : DbContext
    {
      
        public CityWeatherAppContext(DbContextOptions<CityWeatherAppContext> options) : base(options) { }

        public DbSet<FavoriteCity> FavoriteCities { get; set; }
        public DbSet<WeatherData> WeatherData { get; set; }
    }
}