using LibreHardwareMonitor.Hardware;

namespace ControlSensors.Services;

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

public class DiskInfo {
    public string Name { get; set; } = string.Empty;
    public float? Temp { get; set; }
}

public class FanInfo {
    public string Name { get; set; } = string.Empty;
    public float? SpeedRpm { get; set; }
}

public class HardwareMonitorService : IDisposable {
    private readonly Computer _computer;

    public HardwareMonitorService() {
        _computer = new Computer {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true
        };

        _computer.Open();
    }

    public SystemMetricsDto GetMetrics() {
        var metrics = new SystemMetricsDto();

        foreach (var hardware in _computer.Hardware) {
            hardware.Update();

            if (hardware.HardwareType == HardwareType.Cpu) {
                float? cpuTempFallback = null;

                ProcessCpuSensors(hardware, metrics, ref cpuTempFallback);

                foreach (var subHardware in hardware.SubHardware) {
                    subHardware.Update();
                    ProcessCpuSensors(subHardware, metrics, ref cpuTempFallback);
                }

                if (metrics.CpuTemp == null && cpuTempFallback.HasValue) {
                    metrics.CpuTemp = cpuTempFallback;
                }
            }

            if (hardware.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel) {
                foreach (var sensor in hardware.Sensors) {
                    if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue) {
                        if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                            sensor.Name.Contains("D3D", StringComparison.OrdinalIgnoreCase) ||
                            metrics.GpuUsage == null) {
                            metrics.GpuUsage = sensor.Value;
                        }
                    } else if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value > 0) {
                        if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                            sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                            metrics.GpuTemp == null) {
                            metrics.GpuTemp = sensor.Value;
                        }
                    }
                }
            }

            if (hardware.HardwareType == HardwareType.Memory) {
                float? used = null;
                float? available = null;

                foreach (var sensor in hardware.Sensors) {
                    if (sensor.SensorType == SensorType.Data) {
                        if (sensor.Name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase)) used = sensor.Value;
                        if (sensor.Name.Contains("Memory Available", StringComparison.OrdinalIgnoreCase)) available = sensor.Value;
                    }
                    if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase)) {
                        metrics.RamUsagePct = sensor.Value;
                    }
                }

                if (used.HasValue && available.HasValue) {
                    metrics.RamUsedGb = used.Value;
                    metrics.RamTotalGb = used.Value + available.Value;
                }
            }
        }

        return metrics;
    }

    private static void ProcessCpuSensors(IHardware hardware, SystemMetricsDto metrics, ref float? fallbackTemp) {
        foreach (var sensor in hardware.Sensors) {
            if (sensor.SensorType == SensorType.Load) {
                if (sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) || metrics.CpuUsage == null) {
                    metrics.CpuUsage = sensor.Value;
                }
            }
            else if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value > 0) {
                var name = sensor.Name;

                if (name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("CCD", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Core Max", StringComparison.OrdinalIgnoreCase)) {
                    metrics.CpuTemp = sensor.Value;
                } else if (fallbackTemp == null) {
                    fallbackTemp = sensor.Value;
                }
            }
        }
    }

    public object GetAllRawSensors() {
        var list = new List<object>();

        foreach (var hardware in _computer.Hardware) {
            hardware.Update();
            var hwObj = new {
                hardware.Name,
                Type = hardware.HardwareType.ToString(),
                Sensors = hardware.Sensors.Select(s => new { s.Name, Type = s.SensorType.ToString(), Value = s.Value }),
                SubHardware = hardware.SubHardware.Select(sub => {
                    sub.Update();
                    return new {
                        sub.Name,
                        Sensors = sub.Sensors.Select(s => new { s.Name, Type = s.SensorType.ToString(), Value = s.Value })
                    };
                })
            };
            list.Add(hwObj);
        }

        return list;
    }

    public void Dispose() {
        _computer.Close();
    }
}