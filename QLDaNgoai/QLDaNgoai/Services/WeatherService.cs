using System.Globalization;
using System.Text.Json;

namespace QLDaNgoai.Services;

public enum LookupStatus { Ok, PlaceNotFound, NoData }

public sealed record WeatherInfo(int Code, double TMin, double TMax, int? RainProb);

public sealed record WeatherLookup(LookupStatus Status, WeatherInfo? Info);

/// <summary>Gọi Open-Meteo: (1) geocoding tên địa danh -> toạ độ, (2) forecast 16 ngày.</summary>
public static class WeatherService
{
    // Một HttpClient dùng chung, Timeout 8 giây
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public static async Task<WeatherLookup> LayThoiTietAsync(
        string diaDiem, DateOnly ngayDi, string lang, CancellationToken ct = default)
    {
        var inv = CultureInfo.InvariantCulture;
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Quá khứ hoặc quá 16 ngày -> API không có dữ liệu
        if (ngayDi < today.AddDays(-1) || ngayDi > today.AddDays(16))
            return new WeatherLookup(LookupStatus.NoData, null);

        // ---- Bước 1: geocoding ----
        string geoUrl = "https://geocoding-api.open-meteo.com/v1/search" +
                        $"?name={Uri.EscapeDataString(diaDiem)}&count=1&language={lang}";
        using var geo = await GetJsonAsync(geoUrl, ct);

        if (!geo.RootElement.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array
            || results.GetArrayLength() == 0)
            return new WeatherLookup(LookupStatus.PlaceNotFound, null);

        var first = results[0];
        double lat = first.GetProperty("latitude").GetDouble();
        double lon = first.GetProperty("longitude").GetDouble();

        // ---- Bước 2: dự báo (toạ độ phải định dạng InvariantCulture: dấu chấm) ----
        string fcUrl = "https://api.open-meteo.com/v1/forecast" +
                       $"?latitude={lat.ToString(inv)}&longitude={lon.ToString(inv)}" +
                       "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
                       "&timezone=auto&forecast_days=16";
        using var fc = await GetJsonAsync(fcUrl, ct);

        var daily = fc.RootElement.GetProperty("daily");
        var times = daily.GetProperty("time");

        string key = ngayDi.ToString("yyyy-MM-dd", inv);
        int idx = -1, i = 0;
        foreach (var t in times.EnumerateArray())
        {
            if (t.GetString() == key) { idx = i; break; }
            i++;
        }
        if (idx < 0) return new WeatherLookup(LookupStatus.NoData, null);

        int code = daily.GetProperty("weather_code")[idx].ValueKind == JsonValueKind.Null
            ? -1
            : daily.GetProperty("weather_code")[idx].GetInt32();
        double tmax = daily.GetProperty("temperature_2m_max")[idx].GetDouble();
        double tmin = daily.GetProperty("temperature_2m_min")[idx].GetDouble();

        int? rain = null;
        if (daily.TryGetProperty("precipitation_probability_max", out var rp)
            && rp.ValueKind == JsonValueKind.Array
            && rp[idx].ValueKind == JsonValueKind.Number)
            rain = (int)Math.Round(rp[idx].GetDouble());

        return new WeatherLookup(LookupStatus.Ok, new WeatherInfo(code, tmin, tmax, rain));
    }

    private static async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }
}
