using System.Net;
using Jellyfin.Channels.LazyMan.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Channels.LazyMan.Utils
{
    /// <summary>
    /// Ping Test.
    /// </summary>
    public static class PingTest
    {
        /// <summary>
        /// Test if host has valid ip.
        /// </summary>
        /// <remarks>
        /// mf.svc.nhl.com.
        /// mlb-ws-mf.media.mlb.com.
        /// playback.svcs.mlb.com.
        /// </remarks>
        /// <param name="testHost">The host to test.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Host validation status.</returns>
        public static bool IsMatch(string testHost, ILogger<LazyManChannel> logger)
        {
            var validIp = Dns.GetHostAddresses(PluginConfiguration.M3U8Url)[0];
            var testIp = Dns.GetHostAddresses(testHost)[0];

            logger.LogDebug(
                "[PingTest] Host: {0} ValidIP: {1} HostIP: {2}",
                testHost,
                validIp,
                testIp);

            return Equals(validIp, testIp);
        }
    }
}