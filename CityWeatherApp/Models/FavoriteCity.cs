namespace CityWeatherApp.Models
{
    using System.ComponentModel.DataAnnotations;
    public class FavoriteCity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public double TempC { get; set; }
    }

}
