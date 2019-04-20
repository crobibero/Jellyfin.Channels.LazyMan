using System;

namespace Jellyfin.Channels.LazyMan.GameApi.Containers
{
    public class StatsApiContainer
    {
        public Date[] Dates { get; set; }
    }

    public class Date
    {
        public Game[] Games { get; set; }
    }

    public class Game
    {
        public string GamePk { get; set; }

        public DateTime GameDate { get; set; }
        public GameTeams Teams { get; set; }
        public Content Content { get; set; }
        public Status Status { get; set; }
    }

    public class Status
    {
        public string DetailedState { get; set; }
    }

    public class Content
    {
        public Media Media { get; set; }
    }


    public class Media
    {
        public Epg[] Epg { get; set; }
    }

    public class Epg
    {
        public string Title { get; set; }
        public Item[] Items { get; set; }
    }

    public class Item
    {
        public string MediaPlaybackId { get; set; }
        public string MediaFeedType { get; set; }
        public string CallLetters { get; set; }
    }

    public class GameTeams
    {
        public FluffyAway Away { get; set; }
        public FluffyAway Home { get; set; }
    }

    public class FluffyAway
    {
        public Team Team { get; set; }
    }

    public class Team
    {

        public string Name { get; set; }
        public string Abbreviation { get; set; }
    }
}