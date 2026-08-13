namespace ControlSensors.Models;

public class SystemMetricsDto {
    public float? CpuUsage { get; set; }
    public float? CpuTemp { get; set; }
    public float? GpuUsage { get; set; }
    public float? GpuTemp { get; set; }
    public float? RamUsedGb { get; set; }
    public float? RamTotalGb { get; set; }
    public float? RamUsagePct { get; set; }
    public List<DiskInfo> Disks { get; set; } = new();
    public List<FanInfo> Fans { get; set; } = new();
}
