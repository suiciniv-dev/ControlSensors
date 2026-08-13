namespace ControlSensors.Models;

public class NetworkMetricsDto {
    public double DownloadSpeedKbps { get; set; }
    public double UploadSpeedKbps { get; set; }
    public string DownloadFormatted => FormatSpeed(DownloadSpeedKbps);
    public string UploadFormatted => FormatSpeed(UploadSpeedKbps);

    private static string FormatSpeed(double kbps) {
        if (kbps >= 1024 * 1024)
            return $"{kbps / (1024 * 1024):F1} GB/s";
        if (kbps >= 1024)
            return $"{kbps / 1024:F1} MB/s";
        return $"{kbps:F0} KB/s";
    }
}
