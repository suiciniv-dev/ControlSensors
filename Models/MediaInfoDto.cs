namespace ControlSensors.Models;

public class MediaInfoDto {
    public bool IsPlaying { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string CoverBase64 { get; set; } = string.Empty;
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
}
