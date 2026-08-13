namespace ControlSensors.Configuration;

public class AppConfig {
    public string City { get; set; } = "Canoas";
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public bool AutoStart { get; set; } = false;
}
