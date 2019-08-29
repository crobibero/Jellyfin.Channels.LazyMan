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
        private readonly ILogger<LazyManChannel> _logger;
        
        private readonly StatsApi _nhlStatsApi;
        private readonly StatsApi _mlbStatsApi;
        private readonly ConcurrentDictionary<string, CacheItem<List<Game>>> _gameCache;
        private readonly PowerSportsApi _powerSportsApi;
        
        private static readonly double CacheExpireTime = TimeSpan.FromSeconds(60).TotalMilliseconds;

        private readonly IJsonSerializer _jsonSerializer;
        
        public LazyManChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger<LazyManChannel> logger)
        {
            _logger = logger;
            
            _nhlStatsApi = new StatsApi(httpClient, _logger, jsonSerializer, "nhl");
            _mlbStatsApi = new StatsApi(httpClient, _logger, jsonSerializer, "MLB");
            
            _powerSportsApi = new PowerSportsApi(httpClient, _logger);
            
            _jsonSerializer = jsonSerializer;
            _gameCache = new ConcurrentDictionary<string, CacheItem<List<Game>>>();
        }
        
        public string Name => Plugin.Instance.Name;
        public string Description => Plugin.Instance.Description;
        public string DataVersion => "2";
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
             
            var path = GetType().Namespace + ".Images.LM.png";
            return Task.FromResult(new DynamicImageResponse
            {
                Format = ImageFormat.Png,
                HasImage = true,
                Stream = GetType().Assembly.GetManifestResourceStream(path)
            });
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return Enum.GetValues(typeof(ImageType)).Cast<ImageType>();
        }
               
        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            _logger.LogDebug("[LazyMan][GetChannelItems] Searching ID: {0}", query.FolderId);
            
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

            _logger.LogDebug("[LazyMan][GetChannelItems] Current Search Key: {0}", query.FolderId);
            
            // Split parts to see how deep we are
            var querySplit = query.FolderId.Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries);
            
            switch (querySplit.Length - 1)
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
            _logger.LogDebug("[LazyMan][GetGameList] Getting games for {0} on {1}", sport, date);
            
            List<Game> gameList;
            var cacheKey = $"{sport}_{date}";
            if (!_gameCache.TryGetValue(cacheKey, out var cacheItem))
            {
                _logger.LogDebug("[LazyMan][GetGameList] Cache miss for {0} on {1}", sport, date);
                
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
                _logger.LogDebug("[LazyMan][GetGameList] Cache hit for {0} on {1}", sport, date);
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
                    Id = $"{domain}_{Guid.NewGuid()}", 
                    Name = $"{domain} IP ERROR",
                    Type = ChannelItemType.Folder
                })
                .ToList();

            info.Add(new ChannelItemInfo
            {
                Id = $"nhl_{Guid.NewGuid()}",
                Name = "NHL",
                Type = ChannelItemType.Folder
            });

            info.Add(new ChannelItemInfo
            {
                Id = $"MLB_{Guid.NewGuid()}",
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

            _logger.LogDebug("[LazyMan][GetDateFolders] Sport: {0}, {1:yyyyMMdd}", sport, today);
            
            return Task.FromResult(new ChannelItemResult
            {
                Items = Enumerable.Range(0, daysBack)
                    .Select(offset => today.AddDays(-1 * offset))
                    .Select(date =>
                        new ChannelItemInfo
                        {
                            Id = sport + "_" + date.ToString("yyyyMMdd") + "_" + Guid.NewGuid(),
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
            _logger.LogDebug("[LazyMan][GetGameFolders] Sport: {0}, Date: {1}", sport, date);
            
            var gameList = await GetGameListAsync(sport, date).ConfigureAwait(false);
            if(gameList == null)
                return new ChannelItemResult();
            
            return new ChannelItemResult
            {
                Items = gameList.Select(game => new ChannelItemInfo
                {
                    Id = $"{sport}_{date}_{game.GameId}_{Guid.NewGuid()}",
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
            _logger.LogDebug("[LazyMan][GetFeedFolders] Sport: {0}, Date: {1}, GameId: {2}", sport, date, gameId);
            
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
            _logger.LogDebug("[LazyMan][GetFeedFolders] Found Game: {0}", json);
            
            return new ChannelItemResult
            {
                Items = foundGame.Feeds.Select(feed => new ChannelItemInfo
                {
                    Id = $"{sport}_{date}_{gameId}_{feed.Id}_{Guid.NewGuid()}",
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
            _logger.LogDebug("[LazyMan][GetQualityItems] Sport: {0}, Date: {1}, GameId: {2}, FeedId: {3}",
                sport, date, gameId, feedId);
            
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

            
            var (status, response) = await _powerSportsApi.GetPlaylistUrlAsync(
                sport,
                gameDateTime,
                feedId,
                PluginConfiguration.Cdn
            ).ConfigureAwait(false);

            if (!status)
            {
                return new ChannelItemResult
                {
                    Items = new List<ChannelItemInfo>
                    {
                        new ChannelItemInfo
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
            
            foreach (var quality in PluginConfiguration.FeedQualities)
            {
                var id = $"{sport}_{date}_{gameId}_{feedId}_{quality.Key}_{Guid.NewGuid()}";

                // Find index of last file
                var lastIndex = response.LastIndexOf('/');
            
                // Remove file, append quality file
                var streamUrl = response.Substring(0, lastIndex) + '/' + quality.Value.File;
            
                // Format string for current stream
                streamUrl = string.Format(streamUrl, foundGame.State == "In Progress" ? "slide" : "complete-trimmed");
                
                _logger.LogDebug("[LazyMan][GetQualityItems] Stream URL: {0}", streamUrl);
                
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