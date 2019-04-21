using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Channels.LazyMan.Configuration;
using Jellyfin.Channels.LazyMan.GameApi;
using Jellyfin.Channels.LazyMan.Utils;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Channels.LazyMan
{
    public class LazyManChannel : IChannel, IHasCacheKey
    {
        private readonly ILogger _logger;
        
        private readonly StatsApi _nhlStatsApi;
        private readonly StatsApi _mlbStatsApi;
        private readonly ConcurrentDictionary<string, CacheItem<List<Game>>> _gameCache;
        private readonly PowerSportsApi _powerSportsApi;
        
        private static readonly double CacheExpireTime = TimeSpan.FromSeconds(60).TotalMilliseconds;

        private readonly IJsonSerializer _jsonSerializer;
        
        public LazyManChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _logger = logger;
            
            _nhlStatsApi = new StatsApi(httpClient, _logger, jsonSerializer, "nhl");
            _mlbStatsApi = new StatsApi(httpClient, _logger, jsonSerializer, "mlb");
            
            _powerSportsApi = new PowerSportsApi(httpClient, _logger);
            
            _jsonSerializer = jsonSerializer;
            _gameCache = new ConcurrentDictionary<string, CacheItem<List<Game>>>();
        }
        
        public string Name => Plugin.Instance.Name;
        public string Description => Plugin.Instance.Description;
        public string DataVersion => "1";
        public string HomePageUrl => "https://reddit.com/r/LazyMan";
        public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;
        public bool IsEnabledFor(string userId) => true;
        
        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
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
        
        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            _logger.LogDebug("[LazyMan] GetChannelImage {0}",
                GetType().Namespace + ".Images.LM.png");
             switch (type)
            {
                case ImageType.Primary:
                case ImageType.Thumb:
                {
                    var path = GetType().Namespace + ".Images.LM.png";
                    return Task.FromResult(new DynamicImageResponse
                    {
                        Format = ImageFormat.Png,
                        HasImage = true,
                        Stream = GetType().Assembly.GetManifestResourceStream(path)
                    });
                }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Primary
            };
        }
               
        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"[GetChannelItems] Searching ID: {query.FolderId}");

            
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
            
            // Split parts to see how deep we are
            var querySplit = query.FolderId.Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries);
            
            switch (querySplit.Length)
            {
                case 0:
                    // List sports
                    return GetSportFolders();
                case 1:
                    // List dates
                    return GetDateFolders(query.FolderId);
                case 2:
                    // List games
                    return GetGameFolders(querySplit[0], querySplit[1]);
                case 3:
                    // List feeds
                    return GetFeedFolders(querySplit[0], querySplit[1], querySplit[2]);
                case 4:
                    // List qualities
                    return GetQualityItems(querySplit[0], querySplit[1], querySplit[2], querySplit[3]);
                default:
                    // Unknown, return empty result
                    return Task.FromResult(new ChannelItemResult());
            }
        }

        private async Task<List<Game>> GetGameListAsync(string sport, string date)
        {
            _logger.LogDebug($"[GetGameList] Getting games for {sport} on {date}");
            
            List<Game> gameList;
            var cacheKey = $"{sport}_{date}";
            if (!_gameCache.TryGetValue(cacheKey, out var cacheItem))
            {
                _logger.LogDebug($"[GetGameList] Cache miss for {sport} on {date}");
                
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
                _logger.LogDebug($"[GetGameList] Cache hit for {sport} on {date}");
                gameList = cacheItem.Value;
            }

            return gameList;
        }
        
        /// <summary>
        ///     Return list of Sport folders
        ///         currently only NHL and MLB are supported
        /// </summary>
        /// <returns></returns>
        private Task<ChannelItemResult> GetSportFolders()
        {
            _logger.LogDebug("[GetSportFolders] Get Sport Folders");

            var pingTestDomains = new[]
            {
                "mf.svc.nhl.com",
                "mlb-ws-mf.media.mlb.com",
                "playback.svcs.mlb.com"
            };

            var info = pingTestDomains.Where(domain => !PingTest.IsMatch(domain, _logger))
                .Select(domain => new ChannelItemInfo
                {
                    Id = domain, 
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
                Id = "mlb",
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
        ///     Get Date folders
        /// </summary>
        /// <param name="sport">Selected sport</param>
        /// <returns></returns>
        private Task<ChannelItemResult> GetDateFolders(string sport)
        {
            var today = DateTime.Today;
            const int daysBack = 5;

            _logger.LogDebug($"[GetDateFolders] Sport: {sport}, {today:yyyyMMdd}");
            
            return Task.FromResult(new ChannelItemResult
            {
                Items = Enumerable.Range(0, daysBack)
                    .Select(offset => today.AddDays(-1 * offset))
                    .Select(date =>
                        new ChannelItemInfo
                        {
                            Id = sport + "_" + date.ToString("yyyyMMdd"),
                            Name = date.ToString("d", CultureInfo.CurrentCulture),
                            Type = ChannelItemType.Folder
                        })
                    .ToList(),
                TotalRecordCount = daysBack
            });
        }

        /// <summary>
        ///     Get Game folders for sport and date
        /// </summary>
        /// <param name="sport">Selected sport</param>
        /// <param name="date">Selected date</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private async Task<ChannelItemResult> GetGameFolders(string sport, string date)
        {
            _logger.LogDebug($"[GetGameFolders] Sport: {sport}, Date: {date}");
            
            var gameList = await GetGameListAsync(sport, date).ConfigureAwait(false);
            if(gameList == null)
                return new ChannelItemResult();
            
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
        ///     Get feeds for game
        /// </summary>
        /// <param name="sport">Selected sport</param>
        /// <param name="date">Selected date</param>
        /// <param name="gameId">Selected game id</param>
        /// <returns></returns>
        private async Task<ChannelItemResult> GetFeedFolders(string sport, string date, string gameId)
        {
            _logger.LogDebug($"[GetFeedFolders] Sport: {sport}, Date: {date}, GameId: {gameId}");
            
            var gameList = await GetGameListAsync(sport, date).ConfigureAwait(false);
            if(gameList == null)
                return new ChannelItemResult();

            var foundGame = gameList.FirstOrDefault(g => g.GameId == gameId);
            if (foundGame == null)
                return new ChannelItemResult
                {
                    Items = new List<ChannelItemInfo>
                    {
                        new ChannelItemInfo
                        {
                            Id = null,
                            Name = "No feeds found",
                            Type = ChannelItemType.Media
                        }
                    },
                    TotalRecordCount = 1
                };

            var json = _jsonSerializer.SerializeToString(foundGame);
            _logger.LogDebug($"[GetFeedFolders] Found Game: {json}");
            
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
        ///     Get list of qualities
        /// </summary>
        /// <param name="sport">Selected sports</param>
        /// <param name="date">Selected date</param>
        /// <param name="gameId">Selected game id</param>
        /// <param name="feedId">Selected feed id</param>
        /// <returns></returns>
        private async Task<ChannelItemResult> GetQualityItems(string sport, string date, string gameId, string feedId)
        {
            _logger.LogDebug($"[GetQualityItems] Sport: {sport}, Date: {date}, GameId: {gameId}, FeedId: {feedId}");
            
            var gameList = await GetGameListAsync(sport, date).ConfigureAwait(false);
            if(gameList == null)
                return new ChannelItemResult();

            // Locate game
            var foundGame = gameList.FirstOrDefault(g => g.GameId == gameId);
            if(foundGame == null)
                return new ChannelItemResult();

            // Locate feed
            var foundFeed = foundGame.Feeds.FirstOrDefault(f => f.Id == feedId);
            if(foundFeed == null)
                return new ChannelItemResult();
            
            var gameDateTime = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.CurrentCulture);

            var itemInfoList = new List<ChannelItemInfo>();

            foreach (var quality in PluginConfiguration.FeedQualities)
            {
                var id = $"{sport}_{date}_{gameId}_{feedId}_{quality.Key}";

                var streamUrl = await _powerSportsApi.GetPlaylistUrlAsync(
                    sport,
                    gameDateTime,
                    feedId,
                    PluginConfiguration.Cdn,
                    quality.Value.File,
                    foundGame.State
                ).ConfigureAwait(false);

                var itemInfo = new ChannelItemInfo
                {
                    Id = id,
                    Name = quality.Value.Title,
                    ContentType = ChannelMediaContentType.Movie,
                    Type = ChannelItemType.Media,
                    MediaType = ChannelMediaType.Video,
                    MediaSources = new List<MediaSourceInfo>
                    {
                        new MediaSourceInfo
                        {
                            Path = streamUrl,
                            Protocol = MediaProtocol.Http,
                            Id = id,
                            Bitrate = quality.Value.Bitrate
                        }
                    },
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

        public string GetCacheKey(string userId)
        {
            // Never cache, always return new value
            return DateTime.UtcNow.Ticks.ToString();
        }
    }
}