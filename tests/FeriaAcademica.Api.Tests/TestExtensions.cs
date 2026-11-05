using System.Net.Http.Json;
using System.Text.Json;

namespace FeriaAcademica.Api.Tests;

internal static class TestExtensions
{
    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    public static async Task<string> ProblemDetailAsync(this HttpResponseMessage response) =>
        (await response.JsonAsync()).GetProperty("detail").GetString()!;

    public static async Task<bool> IsPdfAsync(this HttpResponseMessage response)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return response.Content.Headers.ContentType?.MediaType == "application/pdf" && bytes.Length > 100 && System.Text.Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-";
    }

    public static async Task<Guid> MyIdAsync(this HttpClient client) =>
        (await (await client.GetAsync("/api/auth/me")).JsonAsync()).GetProperty("id").GetGuid();
}
