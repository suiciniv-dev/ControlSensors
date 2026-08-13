using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using ControlSensors.Models;

namespace ControlSensors.Services;

public class WeatherService {
    private readonly HttpClient _httpClient;
    private WeatherInfoDto? _cachedWeather;
    private DateTime _lastFetch = DateTime.MinValue;

    private double _lat = -29.91;
    private double _lon = -51.18;
    private string _currentCity = "Canoas";

    public WeatherService() {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ControlSensors/1.0");
    }

    public async Task<bool> SetLocationAsync(string citySearch) {
        try {
            var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(citySearch)}&count=1&language=pt";
            var geoResponse = await _httpClient.GetFromJsonAsync<GeocodingResponse>(geoUrl);

            if (geoResponse?.Results != null && geoResponse.Results.Count > 0) {
                var location = geoResponse.Results[0];
                _lat = location.Latitude;
                _lon = location.Longitude;
                _currentCity = string.IsNullOrEmpty(location.State) ? location.Name : $"{location.Name}, {location.State}";

                _lastFetch = DateTime.MinValue;
                return true;
            }
        } catch { }
        return false;
    }

    public async Task<WeatherInfoDto> GetWeatherAsync() {
        if (_cachedWeather != null && (DateTime.Now - _lastFetch).TotalMinutes < 10) {
            return _cachedWeather;
        }

        try {
            var latStr = _lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = _lon.ToString(CultureInfo.InvariantCulture);
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,weather_code&daily=temperature_2m_max,temperature_2m_min&timezone=auto";

            var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url);

            if (response?.Current != null) {
                var (desc, icon) = MapWeatherCode(response.Current.WeatherCode);

                _cachedWeather = new WeatherInfoDto {
                    CityName = _currentCity,
                    Temperature = response.Current.Temperature,
                    Humidity = response.Current.Humidity,
                    Condition = desc,
                    Icon = icon,
                    TempMax = response.Daily?.MaxTemp?.FirstOrDefault() ?? response.Current.Temperature,
                    TempMin = response.Daily?.MinTemp?.FirstOrDefault() ?? response.Current.Temperature
                };
                _lastFetch = DateTime.Now;
                return _cachedWeather;
            }
        } catch {
            if (_cachedWeather != null) return _cachedWeather;
        }

        return new WeatherInfoDto { CityName = _currentCity };
    }

    private (string Description, string Icon) MapWeatherCode(int code) => code switch {
        0 => ("Céu Limpo", "☀️"),
        1 or 2 or 3 => ("Parcialmente Nublado", "⛅"),
        45 or 48 => ("Nevoeiro", "🌫️"),
        51 or 53 or 55 or 61 or 63 or 65 => ("Chuva fraca", "🌧️"),
        80 or 81 or 82 => ("Pancadas de Chuva", "🌧️"),
        95 or 96 or 99 => ("Tempestade", "⛈️"),
        _ => ("Ensolarado", "☀️")
    };

    private class OpenMeteoResponse {
        [JsonPropertyName("current")] public CurrentData? Current { get; set; }
        [JsonPropertyName("daily")] public DailyData? Daily { get; set; }
    }
    private class CurrentData {
        [JsonPropertyName("temperature_2m")] public float Temperature { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public int Humidity { get; set; }
        [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
    }
    private class DailyData {
        [JsonPropertyName("temperature_2m_max")] public List<float>? MaxTemp { get; set; }
        [JsonPropertyName("temperature_2m_min")] public List<float>? MinTemp { get; set; }
    }
    private class GeocodingResponse {
        [JsonPropertyName("results")] public List<GeocodingResult>? Results { get; set; }
    }
    private class GeocodingResult {
        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("admin1")] public string State { get; set; } = "";
    }
}