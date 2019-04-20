using System;
using System.Collections.Generic;

namespace Jellyfin.Channels.LazyMan.GameApi
{
    public class Game
    {
        public DateTime GameDateTime { get; set; }
        public string GameId { get; set; }
        public List<Feed> Feeds { get; set; }
        public Team HomeTeam { get; set; }
        public Team AwayTeam { get; set; }
        
        public string State { get; set; }
    }

    public class Team
    {
        public string Name { get; set; }
        public string Abbreviation { get; set; }
    }

    public class Feed
    {
        public string Id { get; set; }
        public string FeedType { get; set; }
        public string CallLetters { get; set; }
    }
}