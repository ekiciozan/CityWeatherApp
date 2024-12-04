using System.ComponentModel.DataAnnotations;

namespace CityWeatherApp.Models
{
    public class WeatherData
    {
        [Key]
        public int Id { get; set; }
        public string LocationName { get; set; }
        public double TempC { get; set; }
        public DateTime LastUpdated { get; set; } 
    }
}