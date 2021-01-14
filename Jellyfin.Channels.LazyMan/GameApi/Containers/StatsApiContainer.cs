#pragma warning disable SA1402

using System;
using System.Collections.Generic;

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
        public int? GamePk { get; set; }

        /// <summary>
        /// Gets or sets the game date.
        /// </summary>
        public DateTime? GameDate { get; set; }

        /// <summary>
        /// Gets or sets the teams.
        /// </summary>
        public GameTeams? Teams { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        public Content? Content { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
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
        public Media? Media { get; set; }
    }

    /// <summary>
    /// Media container.
    /// </summary>
    public class Media
    {
        /// <summary>
        /// Gets or sets the list of EPG.
        /// </summary>
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
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the list of epg items.
        /// </summary>
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
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the playback id.
        /// </summary>
        public string? MediaPlaybackId { get; set; }

        /// <summary>
        /// Gets or sets the media feed type.
        /// </summary>
        public string? MediaFeedType { get; set; }

        /// <summary>
        /// Gets or sets the call letters.
        /// </summary>
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
        public TeamContainer? Away { get; set; }

        /// <summary>
        /// Gets or sets the home team.
        /// </summary>
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
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the team abbreviation.
        /// </summary>
        public string? Abbreviation { get; set; }
    }
}