namespace ReadingList.Models
{
    public enum MediaType
    {
        Book,
        Film,
        Show,
        Game,
        Album,
        Song,
        Podcast,
        Other
    }

    public static class MediaTypeExt
    {
        public static string MediaTypeList => "'Book', 'Film', 'Show', 'Game', 'Album', 'Song', Podcast', and 'Other'";
        public static string ToString(this MediaType input) =>
            input switch
            {
                MediaType.Book      => "Book",  
                MediaType.Film      => "Film",  
                MediaType.Show      => "Show",   
                MediaType.Game      => "Game",  
                MediaType.Album     => "Album",   
                MediaType.Song      => "Song",
                MediaType.Podcast   => "Podcast",
                MediaType.Other     => "Other",
                _ => throw new ArgumentOutOfRangeException()
            };

        public static MediaType ToMediaType(this string input) =>
            input.Trim().ToLowerInvariant() switch
            {
                // Canonical:
                "book"      => MediaType.Book,    
                "film"      => MediaType.Film,    
                "show"      => MediaType.Show,    
                "game"      => MediaType.Game,    
                "album"     => MediaType.Album,   
                "song"      => MediaType.Song,    
                "podcast"   => MediaType.Podcast,
                "other"     => MediaType.Other,

                // Aliases:
                "novel"     => MediaType.Book,
                "movie"     => MediaType.Film,
                "tvshow"    => MediaType.Show,
                "tv"        => MediaType.Show,
                "videogame" => MediaType.Game,

                // Numbers:
                "1"         => MediaType.Book,   
                "2"         => MediaType.Film,   
                "3"         => MediaType.Show,   
                "4"         => MediaType.Game,   
                "5"         => MediaType.Album,  
                "6"         => MediaType.Song,   
                "7"         => MediaType.Podcast,
                "8"         => MediaType.Other,
                _ => throw new ArgumentOutOfRangeException()
            };

        public static bool TryToMediaType(this string input, out MediaType value)
        {
            try
            {
                value = input.ToMediaType();
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                value = default;
                return false;
            }
        }
    }

    public enum MediaStatus
    {
        Planned,
        InProgress,
        Completed,
        Dropped,
        Paused,
        AwaitingNew,
        Other
    }

    public static class MediaStatusExt
    {
        public static string MediaStatusList => "'Planned', 'InProgress', 'Completed', 'Dropped', 'Paused', 'AwaitingNew', and 'Other'";

        public static string ToString(this MediaStatus input) =>
            input switch
            {
                MediaStatus.Planned     => "Planned",
                MediaStatus.InProgress  => "InProgress",
                MediaStatus.Completed   => "Completed",
                MediaStatus.Dropped     => "Dropped",
                MediaStatus.Paused      => "Paused",
                MediaStatus.AwaitingNew => "AwaitingNew",
                MediaStatus.Other       => "Other",
                _ => throw new ArgumentOutOfRangeException()
            };

        public static MediaStatus ToMediaStatus(this string input) =>
            input.Trim().ToLowerInvariant() switch
            {
                // Canonical:
                "planned"       => MediaStatus.Planned,
                "inprogress"    => MediaStatus.InProgress,
                "completed"     => MediaStatus.Completed,
                "dropped"       => MediaStatus.Dropped,
                "paused"        => MediaStatus.Paused,
                "awaitingnew"   => MediaStatus.AwaitingNew,
                "other"         => MediaStatus.Other,

                // Aliases:
                "plan"          => MediaStatus.Planned,
                "future"        => MediaStatus.Planned,
                "current"       => MediaStatus.InProgress,
                "present"       => MediaStatus.InProgress,
                "done"          => MediaStatus.Completed,
                "watched"       => MediaStatus.Completed,
                "read"          => MediaStatus.Completed,
                "finished"      => MediaStatus.Completed,
                "gaveup"        => MediaStatus.Dropped,
                "gavein"        => MediaStatus.Dropped,
                "abandoned"     => MediaStatus.Dropped,
                "caughtup"      => MediaStatus.AwaitingNew,

                // Numbers:
                "1"             => MediaStatus.Planned,
                "2"             => MediaStatus.InProgress,
                "3"             => MediaStatus.Completed,
                "4"             => MediaStatus.Dropped,
                "5"             => MediaStatus.Paused,
                "6"             => MediaStatus.AwaitingNew,
                "7"             => MediaStatus.Other,

                _ => throw new ArgumentOutOfRangeException()
            };

        public static bool TryToMediaStatus(this string input, out MediaStatus value)
        {
            try
            {
                value = input.ToMediaStatus();
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                value = default;
                return false;
            }
        }
    }
}