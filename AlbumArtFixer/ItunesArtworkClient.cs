using System.Net;
using System.Text.Json;

namespace AlbumArtFixer;

sealed class ItunesArtworkClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly RateLimiter _rateLimiter = new(maxPerMinute: 18);

    public ItunesArtworkClient()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("AlbumArtFixer/1.0");
    }

    public async Task<byte[]?> FindArtworkAsync(string artist, string album)
    {
        var artworkUrl = await SearchAsync(artist, album);
        if (artworkUrl is null) return null;
        return await DownloadAsync(artworkUrl);
    }

    private async Task<string?> SearchAsync(string artist, string album)
    {
        await _rateLimiter.WaitAsync();

        var term = Uri.EscapeDataString($"{artist} {album}".Trim());
        var url = $"https://itunes.apple.com/search?term={term}&entity=album&media=music&limit=5";

        for (int attempt = 0; attempt <= 2; attempt++)
        {
            try
            {
                var response = await _http.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    // Back off for a full minute then retry (counts against attempt limit)
                    Console.WriteLine("  iTunes rate limit hit — waiting 60s...");
                    await Task.Delay(TimeSpan.FromSeconds(60));
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                foreach (var result in doc.RootElement.GetProperty("results").EnumerateArray())
                {
                    if (result.TryGetProperty("artworkUrl100", out var prop) && prop.GetString() is { } url100)
                        return url100.Replace("100x100bb", "600x600bb");
                }

                return null; // successful search, no results
            }
            catch (Exception ex) when (attempt < 2)
            {
                var delay = TimeSpan.FromSeconds(1 << attempt); // 1s, 2s
                Console.Error.WriteLine($"  iTunes error (retry {attempt + 1}): {ex.Message}");
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  iTunes search failed: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    private async Task<byte[]?> DownloadAsync(string url)
    {
        try
        {
            return await _http.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  Artwork download failed: {ex.Message}");
            return null;
        }
    }

    public void Dispose() => _http.Dispose();
}
