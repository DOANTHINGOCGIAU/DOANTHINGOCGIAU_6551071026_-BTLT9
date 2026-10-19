using System.Text.Json;

namespace QLNhaTro.Services;

/// <summary>Một mục địa chỉ (tỉnh hoặc phường/xã). Tên thuộc tính viết thường để
/// khớp DisplayMember = "name", ValueMember = "code".</summary>
public sealed class DiaChi
{
    public int code { get; set; }
    public string name { get; set; } = "";
}

/// <summary>Gọi API hành chính Việt Nam (provinces.open-api.vn, bản v2).</summary>
public static class DiaChiApi
{
    // Một HttpClient dùng chung cho cả ứng dụng, Timeout 10 giây
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private const string BaseUrl = "https://provinces.open-api.vn/api/v2/";

    public static async Task<List<DiaChi>> LayTinhAsync(CancellationToken ct = default)
    {
        using var doc = await GetJsonAsync(BaseUrl, ct);
        return ParseArray(doc.RootElement);
    }

    public static async Task<List<DiaChi>> LayPhuongXaAsync(int maTinh, CancellationToken ct = default)
    {
        using var doc = await GetJsonAsync($"{BaseUrl}p/{maTinh}?depth=2", ct);
        var root = doc.RootElement;

        if (root.TryGetProperty("wards", out var wards) && wards.ValueKind == JsonValueKind.Array)
            return ParseArray(wards);

        // Dự phòng: nếu API trả về cấp huyện thì liệt kê huyện
        if (root.TryGetProperty("districts", out var districts) && districts.ValueKind == JsonValueKind.Array)
            return ParseArray(districts);

        return new List<DiaChi>();
    }

    private static async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private static List<DiaChi> ParseArray(JsonElement arr)
    {
        var list = new List<DiaChi>();
        if (arr.ValueKind != JsonValueKind.Array) return list;

        foreach (var e in arr.EnumerateArray())
        {
            if (!e.TryGetProperty("code", out var c) || !e.TryGetProperty("name", out var n)) continue;

            int code;
            if (c.ValueKind == JsonValueKind.Number) code = c.GetInt32();
            else if (!int.TryParse(c.GetString(), out code)) continue;

            list.Add(new DiaChi { code = code, name = n.GetString() ?? "" });
        }
        return list;
    }
}
