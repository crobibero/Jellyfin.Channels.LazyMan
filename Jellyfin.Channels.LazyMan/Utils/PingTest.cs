using System.Net;
using Jellyfin.Channels.LazyMan.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Channels.LazyMan.Utils
{
    public static class PingTest
    {
        /*
         * mf.svc.nhl.com
         * mlb-ws-mf.media.mlb.com
         * playback.svcs.mlb.com
         */
        
        public static bool IsMatch(string testHost, ILogger logger)
        {
            var validIp = Dns.GetHostAddresses(PluginConfiguration.M3U8Url)[0];
            var testIp = Dns.GetHostAddresses(testHost)[0];

            logger.LogInformation("[PingTest] Host: {0} ValidIP: {1} HostIP: {2}",
                testHost, validIp, testIp);

            return Equals(validIp, testIp);
        }
    }
}