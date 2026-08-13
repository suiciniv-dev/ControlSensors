namespace ControlSensors.Models;

public class WeatherInfoDto {
    public float Temperature { get; set; }
    public int Humidity { get; set; }
    public float TempMax { get; set; }
    public float TempMin { get; set; }
    public string Condition { get; set; } = "Ensolarado";
    public string CityName { get; set; } = "Canoas";
    public string Icon { get; set; } = "☀️";
}
