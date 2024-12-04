using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CityWeatherApp.Data;
using CityWeatherApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CityWeatherApp.Services
{
    public class FavoriteCityService
    {
        private readonly CityWeatherAppContext _context;
        private readonly ILogger<FavoriteCityService> _logger;

        public FavoriteCityService(CityWeatherAppContext context, IConfiguration configuration, ILogger<FavoriteCityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<FavoriteCity>> GetAllFavoriteCitiesAsync()
        {
            try
            {
                _logger.LogInformation("Database'den tüm favori şehirler alınıyor.");
                return await _context.FavoriteCities.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DataBase' den sehirler getirilirken bir hata ile karsilasildi.");
                return new List<FavoriteCity>();
            }
        }

        public async Task<bool> AddToFavoritesAsync(FavoriteCity favoriteCity)
        {
            try
            {
                if (!_context.FavoriteCities.Any(f => f.Name == favoriteCity.Name))
                {
                    _context.FavoriteCities.Add(favoriteCity);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(" {CityName} Sehri favorilere eklendi.", favoriteCity.Name);
                    return true; 
                }
                else
                {
                    _logger.LogWarning("Eklenmek istenen {CityName} şehir zaten listede.", favoriteCity.Name);
                    return false; 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{CityName} sehri favorilere eklenirken bir hata olustu", favoriteCity.Name);
                throw;
            }
        }
        public async Task RemoveFromFavoritesAsync(string cityName)
        {
            try
            {
                var cityToRemove = await _context.FavoriteCities.FirstOrDefaultAsync(c => c.Name == cityName);
                if (cityToRemove != null)
                {
                    _context.FavoriteCities.Remove(cityToRemove);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("{CityName} favorilerden kaldirildi.", cityName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{CityName} sehri favorilerden kaldırılırken bir hata olustu.", cityName);
            }
        }
    }
}