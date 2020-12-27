using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Channels.LazyMan.GameApi.Containers;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Channels.LazyMan.GameApi
{
    /// <summary>
    /// Stats Api.
    /// </summary>
    public class StatsApi
    {
        private const string NhlLink =
            "https://statsapi.web.nhl.com/api/v1/schedule?startDate={0}&endDate={0}&expand=schedule.teams,schedule.linescore,schedule.game.content.media.epg";

        private const string MlbLink =
            "https://statsapi.mlb.com/api/v1/schedule?sportId=1&startDate={0}&endDate={0}&hydrate=team,linescore,game(content(summary,media(epg)))&language=en";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LazyManChannel> _logger;
        private readonly string _gameType;
        private readonly JsonSerializerOptions _jsonSerializerOptions = JsonDefaults.GetOptions();

        /// <summary>
        /// Initializes a new instance of the <see cref="StatsApi"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{StatsApi}"/> interface..</param>
        /// <param name="gameType">The game type.</param>
        public StatsApi(
            IHttpClientFactory httpClientFactory,
            ILogger<LazyManChannel> logger,
            string gameType)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _gameType = gameType;
        }

        /// <summary>
        /// Get the list of games.
        /// </summary>
        /// <param name="inputDate">Date to get games for.</param>
        /// <returns>The list of games.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Unknown game type.</exception>
        public async Task<List<Game>> GetGamesAsync(DateTime inputDate)
        {
            string? url;
            if (_gameType.Equals("nhl", StringComparison.OrdinalIgnoreCase))
            {
                url = NhlLink;
            }
            else if (_gameType.Equals("mlb", StringComparison.OrdinalIgnoreCase))
            {
                url = MlbLink;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(_gameType), "Unknown Game Type");
            }

            url = string.Format(url, inputDate.ToString("yyyy-MM-dd"));

            _logger.LogDebug("[GetGamesAsync] Getting games from {0}", url);
            var container = await _httpClientFactory.CreateClient(NamedClient.Default)
                .GetFromJsonAsync<StatsApiContainer>(url, _jsonSerializerOptions);

            return container == null ? new List<Game>() : ContainerToGame(container);
        }

        private static List<Game> ContainerToGame(StatsApiContainer container)
        {
            var games = new List<Game>();

            foreach (var date in container.Dates)
            {
                foreach (var game in date.Games)
                {
                    var tmp = new Game
                    {
                        GameId = game.GamePk,
                        GameDateTime = game.GameDate,
                        HomeTeam = new Team
                        {
                            Name = game.Teams?.Home?.Team?.Name,
                            Abbreviation = game.Teams?.Home?.Team?.Abbreviation
                        },
                        AwayTeam = new Team
                        {
                            Name = game?.Teams?.Away?.Team?.Name,
                            Abbreviation = game?.Teams?.Away?.Team?.Abbreviation
                        },
                        Feeds = new List<Feed>(),
                        State = game?.Status?.DetailedState
                    };

                    if (game?.Content?.Media?.Epg != null)
                    {
                        foreach (var epg in game.Content.Media.Epg)
                        {
                            foreach (var item in epg.Items)
                            {
                                tmp.Feeds.Add(
                                    new Feed
                                    {
                                        Id = item.MediaPlaybackId ?? item.Id,
                                        FeedType = epg.Title + " - " + item.MediaFeedType,
                                        CallLetters = item.CallLetters
                                    });
                            }
                        }
                    }
                    else
                    {
                        tmp.Feeds.Add(
                            new Feed
                            {
                                Id = "nofeed",
                                FeedType = "No Feed Available",
                                CallLetters = string.Empty
                            });
                    }

                    games.Add(tmp);
                }
            }

            return games;
        }
    }
}