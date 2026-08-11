using ControlSensors.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ControlSensors;

public partial class MainWindow : Window {
    private readonly HardwareMonitorService _hardware;
    private readonly MediaSessionService _media;
    private readonly WeatherService _weather;
    private readonly AudioVisualizerService _audioVisualizer;
    private readonly DispatcherTimer _timer;

    private string _currentSongTitle = "";
    private string _currentCoverBase64 = "";
    private int _carouselTicks = 0;
    private int _carouselState = 0;
    private int _weatherTicks = 0;
    private bool _isShowingMusic = false;

    private SystemMetricsDto? _lastMetrics;
    private MediaInfoDto? _lastMediaData;
    private float[] _currentBarHeights = new float[16];

    private AppConfig _config = new AppConfig();
    private readonly string _configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public MainWindow() {
        InitializeComponent();

        this.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        this.MouseDoubleClick += (s, e) => {
            if (e.ChangedButton == MouseButton.Left)
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        };

        _hardware = new HardwareMonitorService();
        _media = new MediaSessionService();
        _weather = new WeatherService();
        _audioVisualizer = new AudioVisualizerService();

        _audioVisualizer.OnBandsUpdated += UpdateAudioBars;

        LoadConfigAsync();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private async void LoadConfigAsync() {
        if (System.IO.File.Exists(_configPath)) {
            try {
                var json = System.IO.File.ReadAllText(_configPath);
                _config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();

                if (!double.IsNaN(_config.WindowLeft) && !double.IsNaN(_config.WindowTop)) {
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                    this.Left = _config.WindowLeft;
                    this.Top = _config.WindowTop;
                }
            } catch { }
        }
        await _weather.SetLocationAsync(_config.City);
        UpdateWeather();
    }

    private void BtnOpenConfig_Click(object sender, MouseButtonEventArgs e) {
        InputCity.Text = _config.City;
        ChkAutoStart.IsChecked = _config.AutoStart;
        TxtConfigStatus.Text = "";

        PanelConfig.Visibility = Visibility.Visible;
        var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(250));
        PanelConfig.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void BtnCancelConfig_Click(object sender, RoutedEventArgs e) {
        CloseConfigPanel();
    }

    private async void BtnSaveConfig_Click(object sender, RoutedEventArgs e) {
        var newCity = InputCity.Text.Trim();
        if (string.IsNullOrEmpty(newCity)) {
            TxtConfigStatus.Text = "Digite uma cidade válida.";
            return;
        }

        TxtConfigStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8"));
        TxtConfigStatus.Text = "Salvando e ajustando tarefas...";

        bool enableAutoStart = ChkAutoStart.IsChecked ?? false;
        _config.AutoStart = enableAutoStart;
        ManageWindowsTaskScheduler(enableAutoStart);

        bool success = await _weather.SetLocationAsync(newCity);
        if (success) {
            _config.City = newCity;
            SaveConfigFile();

            UpdateWeather();
            CloseConfigPanel();
        } else {
            TxtConfigStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
            TxtConfigStatus.Text = "Cidade não encontrada.";
        }
    }

    private void ManageWindowsTaskScheduler(bool enable) {
        try {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(exePath)) return;

            string taskName = "ControlSensorsAutoStart";

            var startInfo = new System.Diagnostics.ProcessStartInfo("schtasks.exe", $"/delete /tn \"{taskName}\" /f") {
                CreateNoWindow = true, UseShellExecute = false
            };
            System.Diagnostics.Process.Start(startInfo)?.WaitForExit();

            if (enable) {
                startInfo.Arguments = $"/create /tn \"{taskName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl highest /f";
                System.Diagnostics.Process.Start(startInfo)?.WaitForExit();
            }
        } catch { }
    }

    private void SaveConfigFile() {
        try {
            var json = System.Text.Json.JsonSerializer.Serialize(_config);
            System.IO.File.WriteAllText(_configPath, json);
        } catch { }
    }

    private void CloseConfigPanel() {
        var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(250));
        fadeOut.Completed += (s, ev) => PanelConfig.Visibility = Visibility.Hidden;
        PanelConfig.BeginAnimation(OpacityProperty, fadeOut);
    }

    private async void Timer_Tick(object? sender, EventArgs e) {
        UpdateClock();

        _lastMetrics = _hardware.GetMetrics();
        UpdateSensors(_lastMetrics);

        await UpdateMedia();
        UpdateCarousel();

        _weatherTicks++;
        if (_weatherTicks >= 600) {
            _weatherTicks = 0;
            UpdateWeather();
        }
    }

    private void UpdateClock() {
        var now = DateTime.Now;
        TxtTime.Text = now.ToString("HH:mm");
        TxtDate.Text = now.ToString("ddd • dd MMM").ToUpper();
    }

    private void UpdateSensors(SystemMetricsDto data) {
        if (data.CpuTemp.HasValue) {
            TxtCpuTemp.Text = $"{Math.Round(data.CpuTemp.Value)}°";
            TxtCpuTemp.Foreground = GetTempColor(data.CpuTemp.Value);
            SetPulseWarning(TxtCpuTemp, BarCpu, data.CpuTemp.Value >= 85);
        }
        if (data.CpuUsage.HasValue) {
            TxtCpuUsage.Text = $"{Math.Round(data.CpuUsage.Value)}%";
            BarCpu.Value = data.CpuUsage.Value;
            BarCpu.Foreground = GetTempColor(data.CpuUsage.Value, isUsage: true);
        }

        if (data.GpuTemp.HasValue) {
            TxtGpuTemp.Text = $"{Math.Round(data.GpuTemp.Value)}°";
            TxtGpuTemp.Foreground = GetTempColor(data.GpuTemp.Value);
            SetPulseWarning(TxtGpuTemp, BarGpu, data.GpuTemp.Value >= 85);
        }
        if (data.GpuUsage.HasValue) {
            TxtGpuUsage.Text = $"{Math.Round(data.GpuUsage.Value)}%";
            BarGpu.Value = data.GpuUsage.Value;
            BarGpu.Foreground = GetTempColor(data.GpuUsage.Value, isUsage: true);
        }

        if (data.RamUsedGb.HasValue && data.RamTotalGb.HasValue) {
            TxtRamDetail.Text = $"{data.RamUsedGb.Value:F1} / {Math.Round(data.RamTotalGb.Value)} GB";
        }
        if (data.RamUsagePct.HasValue) {
            TxtRamPct.Text = $"{Math.Round(data.RamUsagePct.Value)}%";
            BarRam.Value = data.RamUsagePct.Value;
        }
    }

    private void UpdateAudioBars(float[] bands) {
        if (!_isShowingMusic) return;

        Border[] bars = { Bar0, Bar1, Bar2, Bar3, Bar4, Bar5, Bar6, Bar7, Bar8, Bar9, Bar10, Bar11, Bar12, Bar13, Bar14, Bar15 };

        for (int i = 0; i < 16; i++) {
            float targetHeight = Math.Min(45f, Math.Max(4f, bands[i]));
            _currentBarHeights[i] += (targetHeight - _currentBarHeights[i]) * 0.3f;
            bars[i].Height = _currentBarHeights[i];
        }
    }

    private void SetPulseWarning(UIElement textBlock, UIElement progressBar, bool isWarning) {
        if (isWarning) {
            if (textBlock.HasAnimatedProperties == false) {
                var pulse = new DoubleAnimation(1.0, 0.3, TimeSpan.FromMilliseconds(500)) {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                textBlock.BeginAnimation(OpacityProperty, pulse);
                progressBar.BeginAnimation(OpacityProperty, pulse);
            }
        } else {
            textBlock.BeginAnimation(OpacityProperty, null);
            progressBar.BeginAnimation(OpacityProperty, null);
            textBlock.Opacity = 1.0;
            progressBar.Opacity = 1.0;
        }
    }

    private void SwitchMode(bool showMusic) {
        if (_isShowingMusic == showMusic) return;
        _isShowingMusic = showMusic;

        var animDuration = TimeSpan.FromMilliseconds(400);
        var fadeIn = new DoubleAnimation(1, animDuration);
        var fadeOut = new DoubleAnimation(0, animDuration);

        if (showMusic) {
            _audioVisualizer.Start();

            PanelMusic.Visibility = Visibility.Visible;
            fadeOut.Completed += (s, e) => PanelSensors.Visibility = Visibility.Hidden;

            PanelMusic.BeginAnimation(OpacityProperty, fadeIn);
            PanelSensors.BeginAnimation(OpacityProperty, fadeOut);
        } else {
            _audioVisualizer.Stop();

            PanelSensors.Visibility = Visibility.Visible;
            fadeOut.Completed += (s, e) => PanelMusic.Visibility = Visibility.Hidden;

            PanelSensors.BeginAnimation(OpacityProperty, fadeIn);
            PanelMusic.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    private async System.Threading.Tasks.Task UpdateMedia() {
        _lastMediaData = await _media.GetCurrentMediaAsync();

        if (_lastMediaData.IsPlaying) {
            SwitchMode(true);

            if (_lastMediaData.Title != _currentSongTitle || _lastMediaData.CoverBase64 != _currentCoverBase64) {
                _currentSongTitle = _lastMediaData.Title;
                _currentCoverBase64 = _lastMediaData.CoverBase64;

                TxtMusicBigTitle.Text = string.IsNullOrEmpty(_lastMediaData.Title) ? "Desconhecido" : _lastMediaData.Title;
                TxtMusicBigArtist.Text = string.IsNullOrEmpty(_lastMediaData.Artist) ? "---" : _lastMediaData.Artist;

                if (!string.IsNullOrEmpty(_lastMediaData.CoverBase64)) {
                    try {
                        var base64Data = _lastMediaData.CoverBase64.Substring(_lastMediaData.CoverBase64.IndexOf(',') + 1);
                        var imageBytes = Convert.FromBase64String(base64Data);

                        using var ms = new System.IO.MemoryStream(imageBytes);
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();

                        ImgMusicBg.ImageSource = bitmap;
                        ImgMusicCoverCenter.ImageSource = bitmap;
                    } catch {
                        _currentCoverBase64 = "";
                        ImgMusicBg.ImageSource = null;
                        ImgMusicCoverCenter.ImageSource = null;
                    }
                } else {
                    ImgMusicBg.ImageSource = null;
                    ImgMusicCoverCenter.ImageSource = null;
                }
            }
        } else {
            SwitchMode(false);
            _currentSongTitle = "";
            _currentCoverBase64 = "";
        }
    }

    private void UpdateCarousel() {
        if (_lastMediaData == null || !_lastMediaData.IsPlaying) return;

        _carouselTicks++;
        if (_carouselTicks > 4) {
            _carouselTicks = 0;
            _carouselState++;
            if (_carouselState > 3) _carouselState = 0;
        }

        switch (_carouselState) {
            case 0:
                string app = string.IsNullOrEmpty(_lastMediaData.AppName) ? "Mídia" : _lastMediaData.AppName;
                TxtMusicCarousel.Text = $"🎵 Tocando agora";
                break;
            case 1:
                float cpuTemp = _lastMetrics?.CpuTemp ?? 0;
                TxtMusicCarousel.Text = $"💻 CPU: {Math.Round(cpuTemp)}°C";
                break;
            case 2:
                float gpuTemp = _lastMetrics?.GpuTemp ?? 0;
                TxtMusicCarousel.Text = $"🎮 GPU: {Math.Round(gpuTemp)}°C";
                break;
            case 3:
                string cond = TxtWeatherCond.Text;
                string temp = TxtWeatherTemp.Text;
                TxtMusicCarousel.Text = $"☁️ {cond}, {temp}";
                break;
        }
    }

    private async void UpdateWeather() {
        var data = await _weather.GetWeatherAsync();
        TxtWeatherIcon.Text = data.Icon;
        TxtWeatherTemp.Text = $"{Math.Round(data.Temperature)}°C";
        TxtWeatherCity.Text = data.CityName;
        TxtWeatherCond.Text = data.Condition;
        TxtWeatherRange.Text = $"Máx {Math.Round(data.TempMax)}° • Mín {Math.Round(data.TempMin)}°";
    }

    private SolidColorBrush GetTempColor(float value, bool isUsage = false) {
        if (value >= 85) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
        if (value >= 70) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f97316"));
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8"));
    }

    protected override void OnClosed(EventArgs e) {
        _config.WindowLeft = this.Left;
        _config.WindowTop = this.Top;
        SaveConfigFile();

        _hardware.Dispose();
        _audioVisualizer.Dispose();
        base.OnClosed(e);
    }

    private void BtnCloseApp_Click(object sender, RoutedEventArgs e) {
        Application.Current.Shutdown();
    }
}

public class AppConfig {
    public string City { get; set; } = "Canoas";
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public bool AutoStart { get; set; } = false;
}
