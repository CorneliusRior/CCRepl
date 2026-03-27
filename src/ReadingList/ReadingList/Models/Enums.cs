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
        public static string ToDisplayString(this MediaType input) =>
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

        /// <summary>
        /// Present tense verb, i.e., "reading", "watching", "playing".        
        /// </summary>
        /// <remarks>Could expand to include other tenses, capitalization maybe?</remarks>
        public static string ToVerb(this MediaType input, VerbTense tense = VerbTense.Present, bool capitalize = false)
        {
            string output;

            if (tense == VerbTense.Past) output = input switch
            {
                MediaType.Book      => "read",
                MediaType.Film      => "watched",
                MediaType.Show      => "watched",
                MediaType.Game      => "played",
                MediaType.Album     => "listened",
                MediaType.Song      => "listened",
                MediaType.Podcast   => "listened",
                MediaType.Other     => "consumed",
                _ => throw new ArgumentOutOfRangeException()
            };

            else if (tense == VerbTense.Present) output = input switch
            {
                MediaType.Book      => "reading",
                MediaType.Film      => "watching",
                MediaType.Show      => "watching",
                MediaType.Game      => "playing",
                MediaType.Album     => "listening",
                MediaType.Song      => "listening",
                MediaType.Podcast   => "listening",
                MediaType.Other     => "consuming",
                _ => throw new ArgumentOutOfRangeException()
            };

            else /*if (tense == VerbTense.Future)*/ output = input switch
            {
                MediaType.Book      => "read",
                MediaType.Film      => "watch",
                MediaType.Show      => "watch",
                MediaType.Game      => "play",
                MediaType.Album     => "listen",
                MediaType.Song      => "listen",
                MediaType.Podcast   => "listen",
                MediaType.Other     => "consume",
                _ => throw new ArgumentOutOfRangeException()
            };

            if (capitalize)
            {
                output = output[0].ToString().ToUpper() + output.Skip(1);
            }

            return output;
        }
            

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

        public static string ToDisplayString(this MediaStatus input) =>
            input switch
            {
                MediaStatus.Planned     => "Planned",
                MediaStatus.InProgress  => "In Progress",
                MediaStatus.Completed   => "Completed",
                MediaStatus.Dropped     => "Dropped",
                MediaStatus.Paused      => "Paused",
                MediaStatus.AwaitingNew => "Awaiting New",
                MediaStatus.Other       => "Other",
                _ => throw new ArgumentOutOfRangeException()
            };        

        public static MediaStatus ToMediaStatus(this string input) =>
            input.Trim().ToLowerInvariant().Replace(" ", "") switch
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

    public enum VerbTense
    {
        Past,
        Present,
        Future
    }
}