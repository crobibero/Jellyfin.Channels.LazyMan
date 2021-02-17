using System;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Channels.LazyMan.Configuration;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Channels.LazyMan.GameApi
{
    /// <summary>
    /// Powersports Api.
    /// </summary>
    public class PowerSportsApi
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PowerSportsApi> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerSportsApi"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{PowerSportsApi}"/> interface.</param>
        public PowerSportsApi(IHttpClientFactory httpClientFactory, ILogger<PowerSportsApi> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Gets the playlist url.
        /// </summary>
        /// <param name="league">Sport league.</param>
        /// <param name="date">Game date.</param>
        /// <param name="mediaId">Media id.</param>
        /// <param name="cdn">cdn to use.</param>
        /// <returns>The response status and response.</returns>
        public async Task<(bool Status, string Response)> GetPlaylistUrlAsync(
            string league,
            DateTime date,
            string mediaId,
            string cdn)
        {
            var endpoint = $"https://{PluginConfiguration.M3U8Url}/getM3U8.php?league={league}&date={date:yyyy-MM-dd}&id={mediaId}&cdn={cdn}";

            var url = await _httpClientFactory.CreateClient(NamedClient.Default)
                .GetStringAsync(endpoint);

            _logger.LogDebug("[LazyMan][GetStreamUrlAsync] Response: {Url}", url);

            // stream not ready yet
            if (url.Contains("Not"))
            {
                _logger.LogWarning("[LazyMan][GetStreamUrlAsync] Response contains Not!");
                return (false, url);
            }

            // url expired
            if (url.Contains("exp=", StringComparison.OrdinalIgnoreCase))
            {
                var expLocation = url.IndexOf("exp=", StringComparison.OrdinalIgnoreCase);
                var expStart = expLocation + 4;
                var expEnd = url.IndexOf('~', expLocation);
                var expStr = url.Substring(expStart, expEnd - expStart);
                var expiresOn = long.Parse(expStr);
                var currently = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
                if (expiresOn < currently)
                {
                    _logger.LogWarning("[LazyMan][GetStreamUrlAsync] Stream URL is expired");
                    return (false, "Stream URL is expired");
                }
            }

            return (true, url);
        }
    }
}