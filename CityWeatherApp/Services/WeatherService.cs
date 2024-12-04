using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CityWeatherApp.Data;
using CityWeatherApp.Models;
using Microsoft.Extensions.Logging;
using CityWeatherApp.Dto;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace CityWeatherApp.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiKey2;
        private readonly ILogger<WeatherService> _logger;
        private readonly CityWeatherAppContext _context;
        private readonly IDatabase _redisDatabase; // Redis veritabanı bağlantısı
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<WeatherInfo>> _requestGroups = new();
        private static readonly ConcurrentDictionary<string, DateTime> _lastRequestTime = new();


        public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger, CityWeatherAppContext context, IConnectionMultiplexer redisConnection)
        {
            _httpClient = httpClient;
            _apiKey = configuration["WeatherApi:ApiKey"];
            _apiKey2 = configuration["WeatherApi:ApiKey2"];
            _logger = logger;
            _context = context; 
            _redisDatabase = redisConnection?.GetDatabase(); 
        }

        public async Task<WeatherInfo> GetWeatherAsync2(string cityName)
        {
            var requestKey = cityName.ToLower();

           
            var cachedWeather = await GetWeatherFromCacheAsync(requestKey);
            if (cachedWeather != null)
            {
                _logger.LogInformation("{CityName} için cache'den veri getirildi.", cityName);
                return cachedWeather;

            }
            var tcs = new TaskCompletionSource<WeatherInfo>();

           
            if (!_requestGroups.TryAdd(requestKey, tcs))
            {
                return await _requestGroups[requestKey].Task;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5));  
                _requestGroups.TryRemove(requestKey, out _);
                
                var weatherInfo = await FetchWeatherFromApis(cityName);

                if (weatherInfo != null)
                {
                    
                    try
                    {
                        await SaveWeatherDataToDatabase(cityName, weatherInfo.Current.TempC);
                    }
                    catch (Exception)
                    {
                        _logger.LogInformation("{CityName} için veri DB' ye kaydedilirken bir hata olustu.", cityName);
                    }

                    var saveresultCache = await SaveWeatherToCacheAsync(requestKey, weatherInfo);
                    if (saveresultCache)
                    {
                        _logger.LogInformation("{CityName} için veri redis'e cachelendi.", cityName);
                    }
                }
                else
                {
                    // API çağrılarından yanıt alınamadıysa veritabanına başvur
                    _logger.LogInformation("{CityName} için veri bulunamadı, veritabanı kontrol ediliyor.", cityName);
                    weatherInfo = await FetchWeatherFromDatabase(cityName);
                    if (weatherInfo == null)
                    {
                        _logger.LogWarning("{CityName} şehri için veritabanında da veri bulunamadı.", cityName);
                    }
                }
                tcs.SetResult(weatherInfo);
                return weatherInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weather API {CityName} şehri için veri getirirken bir hata ile karşılaşıldı.", cityName);
                tcs.SetResult(null);
                return null;
            }
        }

        public async Task<WeatherInfo> FetchWeatherFromApis(string cityName)
        {
           
            var weatherInfo = await FetchWeatherFromApi(cityName, _apiKey);
            if (weatherInfo != null)
                return weatherInfo;

            return await FetchWeatherFromApi(cityName, _apiKey2);
        }

        public async Task<WeatherInfo> FetchWeatherFromDatabase(string cityName)
        {
            var dbWeather = await _context.WeatherData.FirstOrDefaultAsync(w => w.LocationName.ToLower() == cityName.ToLower());

            if (dbWeather != null)
            {
                _logger.LogInformation("Veritabanından {CityName} için hava durumu bilgisi getirildi.", cityName);
                return new WeatherInfo
                {
                    Location = new Location { Name = dbWeather.LocationName },
                    Current = new CurrentWeather { TempC = dbWeather.TempC }
                };
            }

            return null; 
        }

        public async Task<WeatherInfo> FetchWeatherFromApi(string cityName, string apiKey)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.weatherapi.com/v1/forecast.json?key={apiKey}&q={cityName}&days=1&aqi=no&alerts=no");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(content);
                    _logger.LogInformation("Weather API isteği {CityName} için başarılı.", cityName);
                    return weatherInfo;
                }
                else
                {
                    _logger.LogError("Weather API isteği başarısız oldu. Status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weather API isteği sırasında bir hata oluştu.");
                return null;
            }
        }

        public async Task SaveWeatherDataToDatabase(string cityName, double tempC)
        {
            var weatherData = new WeatherData
            {
                LocationName = cityName,
                TempC = tempC,
                LastUpdated = DateTime.UtcNow
            };

            _context.WeatherData.Add(weatherData);
            await _context.SaveChangesAsync();
        }

        public async Task<WeatherInfo> GetWeatherWithoutDatabaseSaveAsync(string cityName)
        {
            var requestKey = cityName.ToLower();
            var tcs = new TaskCompletionSource<WeatherInfo>();

            if (!_requestGroups.TryAdd(requestKey, tcs))
            {
                return await _requestGroups[requestKey].Task;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                _requestGroups.TryRemove(requestKey, out _);
                
                var weatherInfo = await FetchWeatherFromApis(cityName);

                tcs.SetResult(weatherInfo);
                return weatherInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weather API {CityName} şehri için veri getirirken bir hata ile karşılaşıldı.", cityName);
                tcs.SetResult(null);
                return null;
            }
        }

        public async Task<WeatherInfo> GetWeatherFromCacheAsync(string cityName)
        {
            var cachedData = await _redisDatabase.StringGetAsync(cityName);
            if (!cachedData.IsNullOrEmpty)
            {
                return JsonConvert.DeserializeObject<WeatherInfo>(cachedData);
            }
            return null;
        }

        public async Task<Boolean> SaveWeatherToCacheAsync(string cityName, WeatherInfo weatherInfo)
        {
            var serializedData = JsonConvert.SerializeObject(weatherInfo);
            var result = await _redisDatabase.StringSetAsync(cityName, serializedData, TimeSpan.FromMinutes(20)); 
            return result;
        }

    }
}