using LibreHardwareMonitor.Hardware;
using ControlSensors.Models;

namespace ControlSensors.Services;

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
                float? bestGpuLoad = null;
                float? bestGpuTemp = null;
                int loadPriority = 0;
                int tempPriority = 0;

                foreach (var sensor in hardware.Sensors) {
                    if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue) {
                        int currentLoadPriority = 0;

                        if (sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase)) {
                            currentLoadPriority = 10;
                        } else if (sensor.Name.Contains("D3D 3D", StringComparison.OrdinalIgnoreCase)) {
                            currentLoadPriority = 9;
                        } else if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) && sensor.Name.Contains("Load", StringComparison.OrdinalIgnoreCase)) {
                            currentLoadPriority = 8;
                        } else if (sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase)) {
                            currentLoadPriority = 5;
                        }

                        if (currentLoadPriority > loadPriority) {
                            bestGpuLoad = sensor.Value;
                            loadPriority = currentLoadPriority;
                        }
                    } else if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value > 0 && sensor.Value < 150) {
                        int currentTempPriority = 0;

                        if (sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase) ||
                            sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)) {
                            currentTempPriority = 10;
                        } else if (sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase)) {
                            currentTempPriority = 8;
                        } else if (sensor.Name.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase)) {
                            currentTempPriority = 7;
                        }

                        if (currentTempPriority > tempPriority) {
                            bestGpuTemp = sensor.Value;
                            tempPriority = currentTempPriority;
                        }
                    }
                }

                if (bestGpuLoad.HasValue) metrics.GpuUsage = bestGpuLoad;
                if (bestGpuTemp.HasValue) metrics.GpuTemp = bestGpuTemp;
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
        float? bestCpuTemp = null;
        int tempPriority = 0;

        foreach (var sensor in hardware.Sensors) {
            if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue) {
                if (sensor.Name.Contains("CPU Total", StringComparison.OrdinalIgnoreCase)) {
                    metrics.CpuUsage = sensor.Value;
                } else if (sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) && metrics.CpuUsage == null) {
                    metrics.CpuUsage = sensor.Value;
                }
            } else if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value > 0 && sensor.Value < 150) {
                var name = sensor.Name;
                int currentPriority = 0;

                if (name.Contains("Tctl", StringComparison.OrdinalIgnoreCase)) {
                    currentPriority = 10;
                } else if (name.Contains("Tdie", StringComparison.OrdinalIgnoreCase)) {
                    currentPriority = 9;
                } else if (name.Contains("CPU Package", StringComparison.OrdinalIgnoreCase) || name.Contains("Package", StringComparison.OrdinalIgnoreCase)) {
                    currentPriority = 8;
                } else if (name.Contains("Core Max", StringComparison.OrdinalIgnoreCase)) {
                    currentPriority = 7;
                } else if (name.Contains("CCD", StringComparison.OrdinalIgnoreCase) && name.Contains("Average", StringComparison.OrdinalIgnoreCase)) {
                    currentPriority = 6;
                } else if (name.Contains("CPU", StringComparison.OrdinalIgnoreCase)) {
                    currentPriority = 5;
                } else if (name.Contains("Core", StringComparison.OrdinalIgnoreCase)) {
                    currentPriority = 1;
                }

                if (currentPriority > tempPriority) {
                    bestCpuTemp = sensor.Value;
                    tempPriority = currentPriority;
                }

                if (fallbackTemp == null && currentPriority > 0) {
                    fallbackTemp = sensor.Value;
                }
            }
        }

        if (bestCpuTemp.HasValue) {
            metrics.CpuTemp = bestCpuTemp;
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