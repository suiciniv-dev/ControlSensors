using System;
using System.Net.NetworkInformation;
using ControlSensors.Models;

namespace ControlSensors.Services;

public class NetworkTrafficService {
    private long _lastBytesReceived;
    private long _lastBytesSent;
    private DateTime _lastCheckTime;

    public NetworkTrafficService() {
        _lastBytesReceived = GetTotalBytesReceived();
        _lastBytesSent = GetTotalBytesSent();
        _lastCheckTime = DateTime.Now;
    }

    public NetworkMetricsDto GetMetrics() {
        var now = DateTime.Now;
        var elapsedSeconds = (now - _lastCheckTime).TotalSeconds;
        if (elapsedSeconds <= 0) elapsedSeconds = 1;

        long currentBytesReceived = GetTotalBytesReceived();
        long currentBytesSent = GetTotalBytesSent();

        long receivedDelta = currentBytesReceived - _lastBytesReceived;
        long sentDelta = currentBytesSent - _lastBytesSent;

        _lastBytesReceived = currentBytesReceived;
        _lastBytesSent = currentBytesSent;
        _lastCheckTime = now;

        double downloadKbps = (receivedDelta / 1024.0) / elapsedSeconds;
        double uploadKbps = (sentDelta / 1024.0) / elapsedSeconds;

        return new NetworkMetricsDto {
            DownloadSpeedKbps = Math.Max(0, downloadKbps),
            UploadSpeedKbps = Math.Max(0, uploadKbps)
        };
    }

    private long GetTotalBytesReceived() {
        long total = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) {
            if (nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
                try {
                    total += nic.GetIPv4Statistics().BytesReceived;
                } catch { }
            }
        }
        return total;
    }

    private long GetTotalBytesSent() {
        long total = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) {
            if (nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
                try {
                    total += nic.GetIPv4Statistics().BytesSent;
                } catch { }
            }
        }
        return total;
    }
}