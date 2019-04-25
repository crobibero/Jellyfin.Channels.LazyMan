using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Channels.LazyMan.Configuration;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Channels.LazyMan.GameApi
{
    public class PowerSportsApi
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger<LazyManChannel> _logger;
        
        public PowerSportsApi(IHttpClient httpClient, ILogger<LazyManChannel> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(bool Status, string Response)> GetPlaylistUrlAsync(string league, DateTime date, string mediaId, string cdn)
        {
            var endpoint = $"https://{{0}}/getM3U8.php?league={league}&date={date:yyyy-MM-dd}&id={mediaId}&cdn={cdn}";
            
            var request = new HttpRequestOptions
            {
                Url = string.Format(endpoint, PluginConfiguration.M3U8Url),
                RequestHeaders =
                {
                    // Requires a User-Agent header
                    {"User-Agent", "Mozilla/5.0 Gecko Firefox"}
                }
            };

            _logger.LogDebug($"[GetStreamUrlAsync] Getting stream url from: {endpoint}");

            var response = await _httpClient.GetResponse(request).ConfigureAwait(false);

            _logger.LogDebug($"[GetStreamUrlAsync] ResponseCode: {response.StatusCode}");

            string url;
            using (var reader = new StreamReader(response.Content))
            {
                url = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            _logger.LogDebug($"[GetStreamUrlAsync] Response: {url}");

            // stream not ready yet
            if (url.Contains("Not"))
            {
                _logger.LogWarning("[GetStreamUrlAsync] Response contains Not!");
                return (false, url);
            }
                

            // url expired
            if (url.Contains("exp="))
            {
                var expLocation = url.IndexOf("exp=", StringComparison.OrdinalIgnoreCase);
                var expStart = expLocation + 4;
                var expEnd = url.IndexOf("~", expLocation, StringComparison.OrdinalIgnoreCase);
                var expStr = url.Substring(expStart, expEnd - expStart);
                var expiresOn = long.Parse(expStr);
                var currently = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
                if (expiresOn < currently)
                {
                    _logger.LogWarning("[GetStreamUrlAsync] Stream URL is expired.");
                    return (false, "Stream URL is expired");   
                }
            }
            
            return (true, url);
        }
    }
}