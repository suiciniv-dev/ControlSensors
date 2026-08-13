using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using ControlSensors.Models;

namespace ControlSensors.Services;

public class GameSessionService {
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private readonly string[] _blacklistedProcesses = {
        "explorer", "devenv", "chrome", "msedge", "firefox",
        "opera", "brave", "discord", "spotify", "code", "vlc",
        "mspaint", "cmd", "powershell", "windowsterminal", "notepad"
    };

    public GameSessionDto CheckActiveGame() {
        try {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return new GameSessionDto { IsInGame = false };

            GetWindowThreadProcessId(hwnd, out uint processId);
            using var proc = Process.GetProcessById((int)processId);
            string procName = proc.ProcessName.ToLower();

            foreach (var blacklisted in _blacklistedProcesses) {
                if (procName.Contains(blacklisted)) {
                    return new GameSessionDto { IsInGame = false };
                }
            }

            if (GetWindowRect(hwnd, out RECT rect)) {
                int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                bool isFullscreen = width >= screenWidth - 15 && height >= screenHeight - 15;

                if (isFullscreen) {
                    return new GameSessionDto {
                        IsInGame = true,
                        GameName = FormatNiceName(proc.ProcessName)
                    };
                }
            }
        } catch { }

        return new GameSessionDto { IsInGame = false };
    }

    private string FormatNiceName(string processName) {
        return char.ToUpper(processName[0]) + processName[1..];
    }
}