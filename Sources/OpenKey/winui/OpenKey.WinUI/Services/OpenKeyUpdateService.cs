using System.Text.Json;

namespace OpenKey.WinUI.Services;

/// <summary>
/// Checks the upstream OpenKey version metadata.
/// </summary>
public sealed class OpenKeyUpdateService
{
    private static readonly Uri VersionUri = new("https://raw.githubusercontent.com/tuyenvm/OpenKey/master/version.json");
    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// Gets the latest Windows version name from the OpenKey metadata file.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the HTTP request.</param>
    /// <returns>The latest Windows version name, or <see langword="null"/> when it cannot be read.</returns>
    public async Task<string?> GetLatestWindowsVersionAsync(CancellationToken cancellationToken = default)
    {
        using Stream stream = await _httpClient.GetStreamAsync(VersionUri, cancellationToken).ConfigureAwait(false);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!document.RootElement.TryGetProperty("latestWinVersion", out JsonElement latestVersion))
        {
            return null;
        }

        return latestVersion.TryGetProperty("versionName", out JsonElement versionName)
            ? versionName.GetString()
            : null;
    }
}
