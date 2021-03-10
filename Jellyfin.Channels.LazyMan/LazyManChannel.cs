using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Channels.LazyMan.Configuration;
using Jellyfin.Channels.LazyMan.GameApi;
using Jellyfin.Channels.LazyMan.Utils;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Channels.LazyMan
{
    /// <summary>
    /// The LazyMan channel.
    /// </summary>
    public class LazyManChannel : IChannel, IHasCacheKey, IRequiresMediaInfoCallback
    {
        private static readonly double CacheExpireTime = TimeSpan.FromSeconds(60).TotalMilliseconds;

        private readonly ILogger<LazyManChannel> _logger;

        private readonly StatsApi _nhlStatsApi;
        private readonly StatsApi _mlbStatsApi;
        private readonly ConcurrentDictionary<string, CacheItem<List<Game>>> _gameCache;
        private readonly PowerSportsApi _powerSportsApi;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyManChannel"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="powerSportsApi">Instance of the <see cref="PowerSportsApi"/>.</param>
        public LazyManChannel(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, PowerSportsApi powerSportsApi)
        {
            _logger = loggerFactory.CreateLogger<LazyManChannel>();
            var statsApiLogger = loggerFactory.CreateLogger<StatsApi>();

            _nhlStatsApi = new StatsApi(httpClientFactory, statsApiLogger, "nhl");
            _mlbStatsApi = new StatsApi(httpClientFactory, statsApiLogger, "MLB");
            _powerSportsApi = powerSportsApi;

            _gameCache = new ConcurrentDictionary<string, CacheItem<List<Game>>>();
        }

        /// <inheritdoc />
        public string Name => LazyManPlugin.Instance!.Name;

        /// <inheritdoc />
        public string Description => LazyManPlugin.Instance!.Description;

        /// <inheritdoc />
        public string DataVersion => "5";

        /// <inheritdoc />
        public string HomePageUrl => "https://reddit.com/r/LazyMan";

        /// <inheritdoc />
        public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

        /// <inheritdoc />
        public bool IsEnabledFor(string userId) => true;

        /// <inheritdoc />
        public InternalChannelFeatures GetChannelFeatures()
        {
            return new ()
            {
                MaxPageSize = 50,
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.TvExtra
                },
                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                }
            };
        }

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            _logger.LogDebug("[LazyMan] GetChannelImage {ImagePath}", GetType().Namespace + ".Images.LM.png");
            var path = GetType().Namespace + ".Images.LM.png";
            return Task.FromResult(new DynamicImageResponse
            {
                Format = ImageFormat.Png,
                HasImage = true,
                Stream = GetType().Assembly.GetManifestResourceStream(path)
            });
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return Enum.GetValues(typeof(ImageType)).Cast<ImageType>();
        }

        /// <inheritdoc />
        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            _logger.LogDebug("[LazyMan][GetChannelItems] Searching ID: {FolderId}", query.FolderId);

            /*
             *    id: {sport}_{date}_{gameId}_{network}_{quality}
             */

            /*
             *    Structure:
             *         Sport
             *             Date - Past 7 days?
             *                 Game Id
             *                     Home vs Away
             *                         Network - (Home/Away/3-Camera)
             *                             Quality
             */

            // At root, return Sports
            if (string.IsNullOrEmpty(query.FolderId))
            {
                return GetSportFolders();
            }

            _logger.LogDebug("[LazyMan][GetChannelItems] Current Search Key: {FolderId}", query.FolderId);

            // Split parts to see how deep we are
            var querySplit = query.FolderId.Split('_', StringSplitOptions.RemoveEmptyEntries);

            switch (querySplit.Length)
            {
                case 0:
                    // List sports
                    return GetSportFolders();
                case 1:
                    // List dates
                    return GetDateFolders(querySplit[0]);
                case 2:
                    // List games
                    return GetGameFolders(querySplit[0], querySplit[1]);
                case 3:
                    // List feeds
                    return GetFeedFolders(querySplit[0], querySplit[1], int.Parse(querySplit[2], CultureInfo.InvariantCulture));
                case 4:
                    // List qualities
                    return GetQualityItems(querySplit[0], querySplit[1], int.Parse(querySplit[2], CultureInfo.InvariantCulture), querySplit[3]);
                default:
                    // Unknown, return empty result
                    return Task.FromResult(new ChannelItemResult());
            }
        }

        private async Task<List<Game>?> GetGameListAsync(string sport, string date)
        {
            _logger.LogDebug("[LazyMan][GetGameList] Getting games for {Sport} on {Date}", sport, date);

            List<Game> gameList;
            var cacheKey = $"{sport}_{date}";
            if (!_gameCache.TryGetValue(cacheKey, out var cacheItem))
            {
                _logger.LogDebug("[LazyMan][GetGameList] Cache miss for {Sport} on {Date}", sport, date);

                // not in cache, populate cache and return
                StatsApi statsApi;
                if (sport.Equals("nhl", StringComparison.OrdinalIgnoreCase))
                {
                    statsApi = _nhlStatsApi;
                }
                else if (sport.Equals("mlb", StringComparison.OrdinalIgnoreCase))
                {
                    statsApi = _mlbStatsApi;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(sport), $"Unknown sport: {sport}");
                }

                var gameDate = DateTime.ParseExact(date, "yyyyMMdd", DateTimeFormatInfo.CurrentInfo);
                gameList = await statsApi.GetGamesAsync(gameDate).ConfigureAwait(false);

                cacheItem = new CacheItem<List<Game>>(cacheKey, gameList, CacheExpireTime, _gameCache);
                _gameCache.TryAdd(cacheKey, cacheItem);
            }
            else
            {
                _logger.LogDebug("[LazyMan][GetGameList] Cache hit for {Sport} on {Date}", sport, date);
                gameList = cacheItem.Value;
            }

            return gameList;
        }

        /// <summary>
        ///     Return list of Sport folders
        ///         currently only NHL and MLB are supported.
        /// </summary>
        /// <returns>The channel item result.</returns>
        private Task<ChannelItemResult> GetSportFolders()
        {
            _logger.LogDebug("[LazyMan][GetSportFolders] Get Sport Folders");

            var pingTestDomains = new[]
            {
                "mf.svc.nhl.com",
                "mlb-ws-mf.media.mlb.com",
                "playback.svcs.mlb.com"
            };

            var info = pingTestDomains.Where(domain => !PingTest.IsMatch(domain, _logger))
                .Select(domain => new ChannelItemInfo
                {
                    Id = $"{domain}",
                    Name = $"{domain} IP ERROR",
                    Type = ChannelItemType.Folder
                })
                .ToList();

            info.Add(new ChannelItemInfo
            {
                Id = "nhl",
                Name = "NHL",
                Type = ChannelItemType.Folder
            });

            info.Add(new ChannelItemInfo
            {
                Id = "MLB",
                Name = "MLB",
                Type = ChannelItemType.Folder
            });

            return Task.FromResult(new ChannelItemResult
            {
                Items = info,
                TotalRecordCount = info.Count
            });
        }

        /// <summary>
        ///     Get Date folders.
        /// </summary>
        /// <param name="sport">Selected sport.</param>
        /// <returns>The channel item result.</returns>
        private Task<ChannelItemResult> GetDateFolders(string sport)
        {
            var today = DateTime.Today;
            const int daysBack = 5;

            _logger.LogDebug("[LazyMan][GetDateFolders] Sport: {Sport}, {Today:yyyyMMdd}", sport, today);

            return Task.FromResult(new ChannelItemResult
            {
                Items = Enumerable.Range(0, daysBack)
                    .Select(offset => today.AddDays(-1 * offset))
                    .Select(date =>
                        new ChannelItemInfo
                        {
                            Id = sport + "_" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                            Name = date.ToString("d", CultureInfo.CurrentCulture),
                            Type = ChannelItemType.Folder
                        })
                    .ToList(),
                TotalRecordCount = daysBack
            });
        }

        /// <summary>
        ///     Get Game folders for sport and date.
        /// </summary>
        /// <param name="sport">Selected sport.</param>
        /// <param name="date">Selected date.</param>
        /// <returns>The channel item result.</returns>
        private async Task<ChannelItemResult> GetGameFolders(string sport, string date)
        {
            _logger.LogDebug("[LazyMan][GetGameFolders] Sport: {Sport}, Date: {Date}", sport, date);

            var gameList = await GetGameListAsync(sport, date).ConfigureAwait(false);
            if (gameList == null)
            {
                return new ChannelItemResult();
            }

            return new ChannelItemResult
            {
                Items = gameList.Select(game => new ChannelItemInfo
                {
                    Id = $"{sport}_{date}_{game.GameId}",
                    Name = $"{game.HomeTeam.Name} vs {game.AwayTeam.Name}",
                    Type = ChannelItemType.Folder
                }).ToList(),
                TotalRecordCount = gameList.Count
            };
        }

        /// <summary>
        ///     Get feeds for game.
        /// </summary>
        /// <param name="sport">Selected sport.</param>
        /// <param name="date">Selected date.</param>
        /// <param name="gameId">Selected game id.</param>
        /// <returns>The channel item result.</returns>
        private async Task<ChannelItemResult> GetFeedFolders(string sport, string date, int gameId)
        {
            _logger.LogDebug("[LazyMan][GetFeedFolders] Sport: {Sport}, Date: {Date}, GameId: {GameId}", sport, date, gameId);

            var gameList = await GetGameListAsync(sport, date).ConfigureAwait(false);
            if (gameList == null)
            {
                return new ChannelItemResult();
            }

            var foundGame = gameList.FirstOrDefault(g => g.GameId == gameId);
            if (foundGame == null)
            {
                return new ChannelItemResult
                {
                    Items = new List<ChannelItemInfo>
                    {
                        new ()
                        {
                            Id = null,
                            Name = "No feeds found",
                            Type = ChannelItemType.Media
                        }
                    },
                    TotalRecordCount = 1
                };
            }

            return new ChannelItemResult
            {
                Items = foundGame.Feeds.Select(feed => new ChannelItemInfo
                {
                    Id = $"{sport}_{date}_{gameId}_{feed.Id}",
                    Name = string.IsNullOrEmpty(feed.CallLetters)
                        ? feed.FeedType
                        : $"{feed.CallLetters} ({feed.FeedType})",
                    Type = ChannelItemType.Folder
                }).ToList(),
                TotalRecordCount = foundGame.Feeds.Count
            };
        }

        /// <summary>
        /// Get list of qualities.
        /// </summary>
        /// <param name="sport">Selected sports.</param>
        /// <param name="date">Selected date.</param>
        /// <param name="gameId">Selected game id.</param>
        /// <param name="feedId">Selected feed id.</param>
        /// <returns>The channel item result.</returns>
        private async Task<ChannelItemResult> GetQualityItems(string sport, string date, int gameId, string feedId)
        {
            _logger.LogDebug(
                "[LazyMan][GetQualityItems] Sport: {Sport}, Date: {Date}, GameId: {GameId}, FeedId: {FeedId}",
                sport,
                date,
                gameId,
                feedId);

            var gameList = await GetGameListAsync(sport, date).ConfigureAwait(false);
            if (gameList == null)
            {
                return new ChannelItemResult();
            }

            // Locate game
            var foundGame = gameList.FirstOrDefault(g => g.GameId == gameId);
            if (foundGame == null)
            {
                return new ChannelItemResult();
            }

            // Locate feed
            var foundFeed = foundGame.Feeds.FirstOrDefault(f => f.Id == feedId);
            if (foundFeed == null)
            {
                return new ChannelItemResult();
            }

            var gameDateTime = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.CurrentCulture);

            var itemInfoList = new List<ChannelItemInfo>();

            var (status, response) = await _powerSportsApi.GetPlaylistUrlAsync(
                    sport,
                    gameDateTime,
                    feedId,
                    PluginConfiguration.Cdn)
                .ConfigureAwait(false);

            if (!status)
            {
                return new ChannelItemResult
                {
                    Items = new List<ChannelItemInfo>
                    {
                        new ()
                        {
                            Id = $"{sport}_{date}_{gameId}_{feedId}_null_null",
                            Name = response,
                            ContentType = ChannelMediaContentType.Clip,
                            Type = ChannelItemType.Media,
                            MediaType = ChannelMediaType.Photo
                        }
                    },
                    TotalRecordCount = 1
                };
            }

            foreach (var (key, value) in PluginConfiguration.FeedQualities)
            {
                var id = $"{sport}_{date}_{gameId}_{feedId}_{key}";

                var itemInfo = new ChannelItemInfo
                {
                    Id = id,
                    Name = value.Title,
                    ContentType = ChannelMediaContentType.Movie,
                    Type = ChannelItemType.Media,
                    MediaType = ChannelMediaType.Video,
                    IsLiveStream = true
                };

                itemInfoList.Add(itemInfo);
            }

            return new ChannelItemResult
            {
                Items = itemInfoList,
                TotalRecordCount = itemInfoList.Count
            };
        }

        /// <inheritdoc />
        public string GetCacheKey(string userId)
        {
            // Never cache, always return new value
            return DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var split = id.Split('_', StringSplitOptions.RemoveEmptyEntries);
            string sport = split[0],
                date = split[1],
                feedId = split[3],
                qualityKey = split[4];
            var gameId = int.Parse(split[2], CultureInfo.InvariantCulture);

            var gameList = await GetGameListAsync(sport, date).ConfigureAwait(false);
            if (gameList == null)
            {
                return Enumerable.Empty<MediaSourceInfo>();
            }

            // Locate game
            var foundGame = gameList.FirstOrDefault(g => g.GameId == gameId);
            if (foundGame == null)
            {
                return Enumerable.Empty<MediaSourceInfo>();
            }

            // Locate feed
            var foundFeed = foundGame.Feeds.FirstOrDefault(f => f.Id == feedId);
            if (foundFeed == null)
            {
                return Enumerable.Empty<MediaSourceInfo>();
            }

            var gameDateTime = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.CurrentCulture);

            var (_, file, bitrate) = PluginConfiguration.FeedQualities[qualityKey];

            var (_, response) = await _powerSportsApi.GetPlaylistUrlAsync(
                    sport,
                    gameDateTime,
                    feedId,
                    PluginConfiguration.Cdn)
                .ConfigureAwait(false);

            // Find index of last file
            var lastIndex = response.LastIndexOf('/');

            // Remove file, append quality file
            var streamUrl = response.Substring(0, lastIndex) + '/' + file;

            // Format string for current stream
            streamUrl = string.Format(CultureInfo.InvariantCulture, streamUrl, foundGame.State == "Final" ? "complete-trimmed" : "slide");

            return new List<MediaSourceInfo>
            {
                new ()
                {
                    Path = streamUrl,
                    Protocol = MediaProtocol.Http,
                    Id = id,
                    Bitrate = bitrate,
                    SupportsProbing = false
                }
            };
        }
    }
}