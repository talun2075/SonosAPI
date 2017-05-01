using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;


namespace PlaylistWPF
{
    internal static class Functions
    {
        #region Variablen und Properties
        private static string startDir = String.Empty;
        private static string playlistxml = String.Empty;
        private static string playlistsavepath = String.Empty;
        private static String playlistsortorder = "Nein";
        private static readonly List<String> allSongs = new List<string>();
        private static readonly List<GenreItem> allViewedGenres = new List<GenreItem>();
        private static readonly List<GenreItem> allViewedHiddenGenres = new List<GenreItem>();
        private static int allGenresCount;
        private static string changeMusicPath = String.Empty;

        /// <summary>
        /// Zeigt an, ob die Playlist in der aktuellen Session geändert wurde. 
        /// </summary>
        public static Boolean PlaylistChanged { get; set; }
        /// <summary>
        /// Verzeichnis ab dem die Musik Rekursiv gesucht werden soll
        /// </summary>
        public static String StartDir { get { return startDir; } }
        /// <summary>
        /// True wenn das Programm alleine loslaufen soll.
        /// </summary>
        public static Boolean Autorun { get; private set; }
        /// <summary>
        /// Gibt an, ob die Playlist beim Starten geladen werden soll.
        /// </summary>
        public static Boolean PlaylistAutoLoad { get; private set; }
        /// <summary>
        /// Soll der Ordner für die Wiedergabelisten vor der Generierung gelerrt werden?
        /// </summary>
        public static Boolean PlaylistClearFolder { get; private set; }
        /// <summary>
        /// Bestimmt, ob die Autrons aus der Settings.xml überschrieben werden soll.
        /// </summary>
        public static Boolean AutorunArgs { get; set; }
        /// <summary>
        /// Ist nicht leer wenn der Pfad der Musikdateien und der Pfad in der Playlist unterschiedlich sein müssen.
        /// </summary>
        public static String ChangeMusicPath { get { return changeMusicPath; } }
        /// <summary>
        /// Die Playlisten XML 
        /// </summary>
        public static String PlaylistXML { get { return playlistxml; } }
        /// <summary>
        /// Ort wo die Playlisten gespeichert werden sollen.
        /// </summary>
        public static String PlaylistSavePath { get { return playlistsavepath; } }
        /// <summary>
        /// Gibtan, ob in der Settings die Sortorder angepasst wurde
        /// </summary>
        public static String PlaylistSortOrder { get { return playlistsortorder; } }
        /// <summary>
        /// Liefert alle gefunden Songs
        /// </summary>
        public static List<String> AllSongs { get { return allSongs; } }
        /// <summary>
        /// Liefert alle anzuzeigenden Genres aus
        /// </summary>
        public static List<GenreItem> AllViewedGenres { get { return allViewedGenres; } }
        /// <summary>
        /// Liefert alle Genres auch die ausgeblendeten
        /// Hier ist eine andere logik. True gibt zurück, ob die angezeigt werden soll oder nicht.
        /// </summary>
        public static List<GenreItem> AllViewedHiddenGenres { get { return allViewedHiddenGenres; } }
        public static int AllGenres { get { return allGenresCount; } }
        #endregion Variablen und Properties
        /// <summary>
        /// Initialisieren der Umgebungsvariablen
        /// </summary>
        /// <param name="Playlistexists">Ist Playlists schon befüllt</param>
        /// <returns></returns>
        internal static Boolean Initialisieren(bool Playlistexists)
        {
            ReadSettingsXML();
            try
            {
                //startDir = Properties.Settings.Default.MusicPath;
                //Prüfen, ob hinten ein slash geschrieben wurde.
                int lastslash = startDir.LastIndexOf("\\", StringComparison.Ordinal);
                if (lastslash != startDir.Length - 1)
                {
                    startDir = startDir + "\\";
                }
                //playlistxml = Properties.Settings.Default.playlistxml;
                //playlistsavepath = Properties.Settings.Default.playlistsavepath;
                lastslash = playlistsavepath.LastIndexOf("\\", StringComparison.Ordinal);
                if (lastslash != playlistsavepath.Length - 1)
                {
                    playlistsavepath = playlistsavepath + "\\";
                }
                if (Playlistexists == false && !File.Exists(PlaylistXML))
                {
                    //Es wurden keine Playlist geladen und die aus den Settings existiert nicht. 
                    return false;
                }
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }
        /// <summary>
        /// Liest alle Musikstücke aus dem StartDir
        /// </summary>
        /// <returns></returns>
        internal static Boolean ReadFiles()
        {
            try
            {
                //Startverzeichnis durchsuchen nach MP3
                var tempallesongs = Directory.GetFiles(startDir, "*.*", SearchOption.AllDirectories).ToList();
                allSongs.Clear();

                for (int i = 0; i < tempallesongs.Count; i++)
                {
                    if (tempallesongs[i].ToLower().EndsWith(".flac") || tempallesongs[i].ToLower().EndsWith(".mp3") || tempallesongs[i].ToLower().EndsWith(".m4a"))
                    {
                        allSongs.Add(tempallesongs[i]);
                    }
                }


                //Verzeichnisse definieren.
                if (!Directory.Exists(playlistsavepath))
                    Directory.CreateDirectory(playlistsavepath);
                return true;
            }
            catch (DirectoryNotFoundException)
            {
               MessageBox.Show(@"Das Verzeichis aus der App.Config für x existiert nicht", @"Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

        }

        /// <summary>
        /// Liest die Settings und Genres aus 
        /// </summary>
        internal static void ReadSettingsXML()
        {
            XDocument doc = XDocument.Load("settings.dat");
            var settings = doc.Descendants("setting");
            allViewedGenres.Clear();
            allViewedHiddenGenres.Clear();
            foreach (var setting in settings)
            {
                switch (setting.FirstAttribute.Value)
                {
                    case "playlistsavepath":
                        playlistsavepath = setting.Value;
                        break;
                    case "playlistxml":
                        playlistxml = setting.Value;
                        break;
                    case "MusicPath":
                        startDir = setting.Value;
                        break;
                    case "playlistsortorder":
                        playlistsortorder = setting.Value;
                        break;
                    case "ChangeMusicPath":
                        string t = setting.Value;
                        if (!String.IsNullOrEmpty(t) && t.Contains("|"))
                        {
                            changeMusicPath = t;
                        }
                        break;
                    case "PlaylistAutoLoad":
                        try
                        {
                            PlaylistAutoLoad = Convert.ToBoolean(setting.Value);
                        }
                        catch
                        {
                            PlaylistAutoLoad = false;
                        }
                        break;
                    case "PlaylistClearFolder":
                        try
                        {
                            PlaylistClearFolder = Convert.ToBoolean(setting.Value);
                        }
                        catch
                        {
                            PlaylistClearFolder = false;
                        }
                        break;
                    case "Autorun":
                        try
                        {
                            Autorun = Convert.ToBoolean(setting.Value);
                        }
                        catch
                        {
                            Autorun = false;
                        }
                        if (AutorunArgs)
                        {
                            Autorun = true;
                        }
                        break;
                }

            }
            var genres = doc.Descendants("genre");
            var xElements = genres as IList<XElement> ?? genres.ToList();
            allGenresCount = xElements.Count();
            foreach (var genre in xElements)
            {
                string name = genre.FirstAttribute.Value;
                if (name.Contains(" and "))
                {
                    name = name.Replace(" and ", "&");
                }
                if (genre.Value.ToLower() == "true")
                {
                    allViewedGenres.Add(new GenreItem { Check = false, Genre = name });
                    allViewedHiddenGenres.Add(new GenreItem { Check = true, Genre = name });
                }
                else
                {
                    allViewedHiddenGenres.Add(new GenreItem { Check = false, Genre = name });
                }

            }


        }

        internal static void WriteSettingsXML(string settingtyp, string wert)
        {
            XDocument doc = XDocument.Load("settings.dat");
            var settings = doc.Descendants("setting");
            foreach (var setting in settings)
            {
                if (settingtyp == setting.FirstAttribute.Value)
                {
                    setting.Value = wert;
                }
                
            }
            doc.Save("settings.dat");

        }
        internal static void WriteGenresXML()
        {
            XDocument doc = XDocument.Load("settings.dat");
            var genresnode = doc.Descendants("genres").FirstOrDefault();
            if (genresnode != null)
            {
                var genres = genresnode.Descendants("genre");
                int counter = 0;
                foreach (var genre in genres)
                {
                    genre.Value = allViewedHiddenGenres[counter].Check.ToString();
                    counter++;
                }
                if (allViewedHiddenGenres.Count > counter)
                {
                    for (int i = counter; i < allViewedHiddenGenres.Count; i++)
                    {
                        XElement k = new XElement("genre");
                        k.Value = allViewedHiddenGenres[i].Check.ToString();

                        string name = allViewedHiddenGenres[i].Genre;
                        if (name.Contains("&"))
                        {
                            name = name.Replace(" & ", " and ");
                            name = name.Replace("&", " and ");
                        }
                        k.SetAttributeValue("name", name);
                        genresnode.Add(k);
                    }
                }
                doc.Save("settings.dat");
            }

        }
    }


    /// <summary>
    /// Enthält ein Genre
    /// </summary>
    public class GenreItem
    {
        public Boolean Check { get; set; }
        public string Genre { get; set; }
    }
}
