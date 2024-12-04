using CityWeatherApp.Models;
using CityWeatherApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

public class WeatherController : Controller
{
    private readonly WeatherService _weatherService;
    private readonly FavoriteCityService _favoriteCityService;

    public WeatherController(WeatherService weatherService, FavoriteCityService favoriteCityService)
    {
        _weatherService = weatherService;
        _favoriteCityService = favoriteCityService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string cityName)
    {
        var weather = await _weatherService.GetWeatherAsync2(cityName);

        if (weather == null)
        {
            ViewBag.ErrorMessage = "Hava durumu bilgisi alınamadı. Lütfen geçerli bir şehir adı girin.";
            return View();
        }

        return View("WeatherDetail", weather);
    }

    [HttpPost]
    public async Task<IActionResult> AddToFavorites(string cityName)
    {
        var weather = await _weatherService.GetWeatherWithoutDatabaseSaveAsync(cityName);

        if (weather?.Current == null)
        {
            ViewBag.ErrorMessage = "Hava durumu bilgisi eksik, favorilere eklenemedi.";
            return RedirectToAction("Index");
        }

        var favoriteCity = new FavoriteCity
        {
            Name = weather.Location.Name,
            TempC = weather.Current.TempC
        };

        
        var isAdded = await _favoriteCityService.AddToFavoritesAsync(favoriteCity);

        if (!isAdded)
        {
            TempData["WarningMessage"] = $"{cityName} şehri zaten favorilerde.";
            return View("WeatherDetail", weather);
        }

        return RedirectToAction("Favorites");
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromFavorites(string cityName)
    {
        await _favoriteCityService.RemoveFromFavoritesAsync(cityName);
        return RedirectToAction("Favorites");
    }

    public async Task<IActionResult> Favorites()
    {
        var favoriteCities = await _favoriteCityService.GetAllFavoriteCitiesAsync();
        var hottestCity = favoriteCities.OrderByDescending(c => c.TempC).FirstOrDefault();
        var coldestCity = favoriteCities.OrderBy(c => c.TempC).FirstOrDefault();

        ViewBag.HottestCity = hottestCity;
        ViewBag.ColdestCity = coldestCity;

        return View(favoriteCities);
    }
}
