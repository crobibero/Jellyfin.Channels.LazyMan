#pragma warning disable SA1402

using System;
using System.Collections.Generic;

namespace Jellyfin.Channels.LazyMan.GameApi
{
    /// <summary>
    /// Game model.
    /// </summary>
    public class Game
    {
        /// <summary>
        /// Gets or sets the game date time.
        /// </summary>
        public DateTime? GameDateTime { get; set; }

        /// <summary>
        /// Gets or sets teh game id.
        /// </summary>
        public string? GameId { get; set; }

        /// <summary>
        /// Gets or sets the list of feeds.
        /// </summary>
        public List<Feed> Feeds { get; set; } = new ();

        /// <summary>
        /// Gets or sets the home team.
        /// </summary>
        public Team HomeTeam { get; set; } = new ();

        /// <summary>
        /// Gets or sets the away team.
        /// </summary>
        public Team AwayTeam { get; set; } = new ();

        /// <summary>
        /// Gets or sets the game state.
        /// </summary>
        public string? State { get; set; }
    }

    /// <summary>
    /// The team model.
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

    /// <summary>
    /// The feed model.
    /// </summary>
    public class Feed
    {
        /// <summary>
        /// Gets or sets the feed id.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the feed type.
        /// </summary>
        public string? FeedType { get; set; }

        /// <summary>
        /// Gets or sets the feed call letters.
        /// </summary>
        public string? CallLetters { get; set; }
    }
}