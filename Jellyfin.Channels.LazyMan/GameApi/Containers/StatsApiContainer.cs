#pragma warning disable SA1402

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Channels.LazyMan.GameApi.Containers
{
    /// <summary>
    /// Stats api container.
    /// </summary>
    public class StatsApiContainer
    {
        /// <summary>
        /// Gets or sets the list of dates.
        /// </summary>
        [JsonPropertyName("dates")]
        public IReadOnlyList<Date> Dates { get; set; } = Array.Empty<Date>();
    }

    /// <summary>
    /// Date container.
    /// </summary>
    public class Date
    {
        /// <summary>
        /// Gets or sets the list of games.
        /// </summary>
        [JsonPropertyName("games")]
        public IReadOnlyList<Game> Games { get; set; } = Array.Empty<Game>();
    }

    /// <summary>
    /// Game container.
    /// </summary>
    public class Game
    {
        /// <summary>
        /// Gets or sets the game pk.
        /// </summary>
        [JsonPropertyName("gamePk")]
        public int? GamePk { get; set; }

        /// <summary>
        /// Gets or sets the game date.
        /// </summary>
        [JsonPropertyName("gameDate")]
        public DateTime? GameDate { get; set; }

        /// <summary>
        /// Gets or sets the teams.
        /// </summary>
        [JsonPropertyName("teams")]
        public GameTeams? Teams { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        [JsonPropertyName("content")]
        public Content? Content { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [JsonPropertyName("status")]
        public Status? Status { get; set; }
    }

    /// <summary>
    /// Status container.
    /// </summary>
    public class Status
    {
        /// <summary>
        /// Gets or sets the detailed state.
        /// </summary>
        [JsonPropertyName("detailedState")]
        public string? DetailedState { get; set; }
    }

    /// <summary>
    /// Content container.
    /// </summary>
    public class Content
    {
        /// <summary>
        /// Gets or sets the media.
        /// </summary>
        [JsonPropertyName("media")]
        public MediaContainer? Media { get; set; }
    }

    /// <summary>
    /// Media container.
    /// </summary>
    public class MediaContainer
    {
        /// <summary>
        /// Gets or sets the list of EPG.
        /// </summary>
        [JsonPropertyName("epg")]
        public IReadOnlyList<Epg> Epg { get; set; } = Array.Empty<Epg>();
    }

    /// <summary>
    /// Epg container.
    /// </summary>
    public class Epg
    {
        /// <summary>
        /// Gets or sets the epg title.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the list of epg items.
        /// </summary>
        [JsonPropertyName("items")]
        public IReadOnlyList<Item> Items { get; set; } = Array.Empty<Item>();
    }

    /// <summary>
    /// Item container.
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the playback id.
        /// </summary>
        [JsonPropertyName("mediaPlaybackId")]
        public string? MediaPlaybackId { get; set; }

        /// <summary>
        /// Gets or sets the media feed type.
        /// </summary>
        [JsonPropertyName("mediaFeedType")]
        public string? MediaFeedType { get; set; }

        /// <summary>
        /// Gets or sets the call letters.
        /// </summary>
        [JsonPropertyName("callLetters")]
        public string? CallLetters { get; set; }
    }

    /// <summary>
    /// Game teams container.
    /// </summary>
    public class GameTeams
    {
        /// <summary>
        /// Gets or sets the away team.
        /// </summary>
        [JsonPropertyName("away")]
        public TeamContainer? Away { get; set; }

        /// <summary>
        /// Gets or sets the home team.
        /// </summary>
        [JsonPropertyName("home")]
        public TeamContainer? Home { get; set; }
    }

    /// <summary>
    /// Team container.
    /// </summary>
    public class TeamContainer
    {
        /// <summary>
        /// Gets or sets the team.
        /// </summary>
        [JsonPropertyName("team")]
        public Team? Team { get; set; }
    }

    /// <summary>
    /// Team.
    /// </summary>
    public class Team
    {
        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the team abbreviation.
        /// </summary>
        [JsonPropertyName("abbreviation")]
        public string? Abbreviation { get; set; }
    }
}