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
        private readonly ILogger _logger;
        
        public PowerSportsApi(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> GetPlaylistUrlAsync(string league, DateTime date, string mediaId, string cdn,
            string quality, string state)
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
                return null;
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
                    return null;   
                }
            }
            
            // Find index of last file
            var lastIndex = url.LastIndexOf('/');
            
            // Remove file, append quality file
            url = url.Substring(0, lastIndex) + '/' + quality;
            
            // Format string for current stream
            url = string.Format(url, state == "In Progress" ? "slide" : "complete-trimmed");

            return url;
        }
    }
}