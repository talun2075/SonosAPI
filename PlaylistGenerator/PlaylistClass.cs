using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;
using MP3File;

namespace PlaylistGenerator
{
    /// <summary>
    /// Enthält eine Playlist
    /// </summary>
    public class PlaylistClass
    {
        private String _SortOrder = "Keine";
        public List<String> conditionGroupsNames = new List<string>();
        /// <summary>
        /// Erzeugt eine Playlist
        /// </summary>
        public PlaylistClass()
        {
            PlaylistConditionGroups = new List<ConditionObjectGroup>();
        }
        /// <summary>
        /// Erzeugt eine Playlist
        /// </summary>
        /// <param name="_name">Name der Playlist</param>
        public PlaylistClass(String _name)
        {
            Playlist = _name;
            PlaylistConditionGroups = new List<ConditionObjectGroup>();
        }
        /// <summary>
        /// Name der Playlist
        /// </summary>
        public String Playlist { get; set; }
        /// <summary>
        /// Sortierreihenfolge der Playlist, wenn sortiert werden soll.
        /// Erlaubt: Titel, Artist, Random
        /// </summary>
        public String Sort
        {
            get { return _SortOrder; }
            set
            {
                string t = value.ToLower();
                if (t.StartsWith("titel") || t.StartsWith("interpret") || t.StartsWith("zufall"))
                {
                    _SortOrder = value;
                }
                else
                {
                    _SortOrder = "Keine";
                }
            }
        }
        /// <summary>
        /// Alle Bedingungsgruppen
        /// </summary>
        public List<ConditionObjectGroup> PlaylistConditionGroups { get; set; }
        /// <summary>
        /// Alle Namen der Bedingungsgruppen
        /// </summary>
        public List<String> GetConditionGroupNames
        {
            get
            {
                conditionGroupsNames.Clear();
                if (PlaylistConditionGroups != null && PlaylistConditionGroups.Count > 0)
                {

                    foreach (ConditionObjectGroup cop in PlaylistConditionGroups)
                    {
                        conditionGroupsNames.Add(cop.Name);
                    }
                }
                return conditionGroupsNames;

            }
        }
    }
    /// <summary>
    /// enthält alle Playlisten
    /// </summary>
    static public class Playlists
    {
        static private List<PlaylistClass> pll = new List<PlaylistClass>();
        static private readonly List<String> playlistNames = new List<string>();
        static private XmlSerializer xmls = new XmlSerializer(typeof(List<PlaylistClass>));
        /// <summary>
        /// Gibt eine Liste mit allen Playlists
        /// </summary>
        static public List<PlaylistClass> GetPlaylists
        {
            get { return pll; }
        }
        /// <summary>
        /// Liefert eine Liste mit allen Playlistnamen
        /// </summary>
        static public List<String> GetPlaylistNames
        {
            get
            {
                playlistNames.Clear();
                foreach (PlaylistClass pc in pll)
                {
                    playlistNames.Add(pc.Playlist);
                }

                return playlistNames;
            }
        }
        /// <summary>
        /// Hinzufügen einer Playlist
        /// </summary>
        /// <param name="p"></param>
        static public void Add(PlaylistClass p)
        {
            pll.Add(p);
        }
        /// <summary>
        /// Entfernen einer Playlist
        /// </summary>
        /// <param name="p"></param>
        static public void Remove(PlaylistClass p)
        {
            pll.Remove(p);
        }
        /// <summary>
        /// Alles löschen
        /// </summary>
        static public void Clear()
        {
            pll.Clear();
        }
        /// <summary>
        /// Speichern in den Pfad (inkl. Datei)
        /// </summary>
        /// <param name="path">Pfad wird ohne überprüfung versucht zu speichern.</param>
        static public void Save(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return;
            }
            StreamWriter textWriter = new StreamWriter(path);
            xmls.Serialize(textWriter, GetPlaylists);
            textWriter.Close();
        }
        static public Nullable<bool> Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }
                StreamReader reader = new StreamReader(path);
                List<PlaylistClass> playlistlist = (List<PlaylistClass>)xmls.Deserialize(reader);
                Clear();
                pll = playlistlist.OrderBy(x => x.Playlist).ToList();
                foreach (PlaylistClass pl in pll)
                {
                    playlistNames.Add(pl.Playlist);
                }
                return true;
            }
            catch
            {

                return false;
            }
        }



    }

    public class Playlistwriter
    {
        /// <summary>
        /// Ermittelt für die Playlist die entsprechenden Songs und schreibt die M3U
        /// </summary>
        /// <param name="music">Alle Songs</param>
        /// <param name="savepath">Speicherpfad für die M3U</param>
        /// <param name="bw">Wenn mit einem Backourndworker gearbeitet werden soll, dann dieser</param>
        /// <param name="BWStartvalue">Start Wert des Backgroundworkers</param>
        /// <param name="ChangeMusicPath">Wenn ein anderer Pfad für den Song in einer Playlist genommen werden soll 
        /// Folgender Aufbau: OldValue|NewValue </param>
        /// <param name="clearPlaylistPath">Soll der Playlistordner gelerrt werden?</param>
        public Playlistwriter(List<String> music, string savepath, BackgroundWorker bw = null, int BWStartvalue = 0, String ChangeMusicPath = null, Boolean clearPlaylistPath = false)
        {
            if (BWStartvalue > 80)
            {
                return;
            }

            int reportvalue = 0;
            if (bw != null)
            {
                bw.ReportProgress(BWStartvalue, "Wiedergabelisten werden in den Speicher geladen");
            }

            //Liste für die MP3Files pro liste vorbereiten
            List<List<MP3File.MP3File>> plnames = new List<List<MP3File.MP3File>>();
            plnames.Clear();
            for (int i = 0; i < Playlists.GetPlaylistNames.Count; i++)
            {
                plnames.Add(new List<MP3File.MP3File>());
            }
            int counter = 0;
            //Wenn der Song existiert wird er ausgelesen und geprüft
            foreach (string path in music)
            {
                if (bw != null)
                {
                    reportvalue = BWStartvalue + ((counter * (100 - BWStartvalue - 10)) / music.Count);
                    bw.ReportProgress(reportvalue, path + " wird geprüft");
                    counter++;
                }
                if (File.Exists(path))
                {
                    var file = MP3ReadWrite.ReadMetaData(path);
                    if (file.VerarbeitungsFehler && bw != null)
                    {
                        bw.ReportProgress(100, "Fehler beim Song:" + path);
                        return;
                    }
                    int playlistcounter = 0;
                    foreach (PlaylistClass playlist in Playlists.GetPlaylists)
                    {
                        bool resval = true;
                        bool[] groupconditioner = new bool[playlist.PlaylistConditionGroups.Count];
                        int gcc = 0;
                        foreach (ConditionObjectGroup playlistConditionGroup in playlist.PlaylistConditionGroups)
                        {
                            //Wenn resval einmal auf false ist, dann wurde bei einer Gruppe False gesetzt und dann wird weiter gemacht
                            //Gruppen sind mit AND verbunden.
                            if (resval == false)
                            {
                                continue;
                            }
                            resval = ConditionCecker.Ceck(playlistConditionGroup, file);
                            groupconditioner[gcc] = resval;
                            gcc++;
                        }
                        bool resvalgc = true;
                        foreach (var b in groupconditioner)
                        {
                            if (b == false)
                            {
                                resvalgc = false;
                                break;
                            }
                        }
                        //song der Liste zufügen, weil er passt.
                        if (resvalgc)
                        {
                            plnames[playlistcounter].Add(file);
                        }
                        playlistcounter++;
                    }//Ende der GetPlaylists schleife


                }


            }
            if (clearPlaylistPath)
            {
                var files = Directory.GetFiles(savepath);
                foreach (var pltoKill in files)
                {
                    try
                    {
                        if (File.Exists(pltoKill))
                        {
                            File.Delete(pltoKill);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            //hier die Playlist schreiben lassen.
            if (plnames.Count > 0)
            {
                if (bw != null)
                {
                    reportvalue = 90;
                    bw.ReportProgress(reportvalue, "Wiedergabelisten fertig ermittelt und werden nun geschrieben.");
                }
                for (int i = 0; i < plnames.Count; i++)
                {
                    if (bw != null)
                    {
                        if (plnames.Count > 1)
                        {
                            reportvalue = reportvalue + ((i * (10)) / plnames.Count);
                        }
                        else
                        {
                            reportvalue = 100;
                        }
                        bw.ReportProgress(reportvalue, "Wiedergabeliste " + Playlists.GetPlaylistNames[i] + " wird geschrieben.");
                    }

                    if (plnames[i].Count > 0)
                    {
                        string sortorder = Playlists.GetPlaylists[i].Sort;
                        if (Playlists.GetPlaylists[i].Sort.ToLower() != "keine")
                        {
                            if (sortorder.ToLower() == "titel")
                            {
                                plnames[i] = plnames[i].OrderBy(x => x.Titel).ToList();
                            }
                            if (sortorder.ToLower() == "interpret")
                            {
                                plnames[i] = plnames[i].OrderBy(x => x.Artist).ToList();
                            }
                            if (sortorder.ToLower() == "zufall")
                            {

                                //Random rng = new Random();
                                //List<MP3File.MP3File> list = plnames[i];
                                //int n = list.Count;
                                //while (n > 1)
                                //{
                                //    n--;
                                //    int k = rng.Next(n + 1);
                                //    MP3File.MP3File value = list[k];
                                //    list[k] = list[n];
                                //    list[n] = value;
                                //}
                                //todo: Prüfen, ob besser
                                RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
                                List<MP3File.MP3File> list = plnames[i];
                                int n = list.Count;
                                while (n > 1)
                                {
                                    byte[] box = new byte[1];
                                    do provider.GetBytes(box);
                                    while (!(box[0] < n * (Byte.MaxValue / n)));
                                    int k = (box[0] % n);
                                    n--;
                                    MP3File.MP3File value = list[k];
                                    list[k] = list[n];
                                    list[n] = value;
                                }

                            }
                            if (sortorder.ToLower() == "bewertung")
                            {
                                plnames[i] = plnames[i].OrderBy(x => x.Bewertung).ToList();
                            }
                            if (sortorder.ToLower() == "bewertung mine")
                            {
                                plnames[i] = plnames[i].OrderBy(x => x.BewertungMine).ToList();
                            }
                        }

                        if (!MP3ReadWrite.WritePlaylist(plnames[i], Playlists.GetPlaylistNames[i], savepath, ChangeMusicPath))
                        {
                            //Fehler
                        }
                    }
                    else
                    {
                        //Prüfen, ob Datei schon vorhanden obwohl null, dann löschen
                        var dir = Directory.CreateDirectory(savepath);
                        string m3uname = Playlists.GetPlaylistNames[i] + ".m3u";
                        string file = dir.FullName + "\\" + m3uname;
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }


                    }
                }
            }
        }

    }
}


