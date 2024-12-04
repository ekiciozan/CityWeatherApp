using Newtonsoft.Json;

namespace CityWeatherApp.Dto
{
    public class WeatherInfo
    {
        public Location Location { get; set; }
        public CurrentWeather Current { get; set; }
    }

    public class Location
    {
        public string Name { get; set; }
    }
    public class CurrentWeather
    {
        [JsonProperty("temp_c")]
        public double TempC { get; set; } 
       
    }

}
