using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MP3File
{
    public class MP3File
    {
        /// <summary>
        /// Erstellen des Objekts MP3 ohne Übergabeparameter
        /// </summary>
        public MP3File()
        {
            Bewertung = "0";
            Aufwecken = false;
            BewertungMine = "0";
            Genre = "leer";
            Laufzeit = new TimeSpan();
            ArtistPlaylist = false;
            HatCover = true;
            Verlag = String.Empty;
            Typ = String.Empty;
            Kommentar = String.Empty;
        }
        public String Titel { get; set; }
        public String Artist { get; set; }
        public String Album { get; set; }
        public String Lyric { get; set; }
        public Int32 Tracknumber { get; set; }
        public String Pfad { get; set; }
        public Boolean VerarbeitungsFehler { get; set; }
        public String Jahr { get; set; }
        public String Komponist { get; set; }
        public String Bewertung { get; set; }
        public Enums.Gelegenheit Gelegenheit { get; set; } = Enums.Gelegenheit.None;
        public Enums.Geschwindigkeit Geschwindigkeit { get; set; } = Enums.Geschwindigkeit.None;
        public Enums.Stimmung Stimmung { get; set; } = Enums.Stimmung.None;
        public Boolean Aufwecken { get; set; }
        public Boolean ArtistPlaylist { get; set; }
        public String BewertungMine { get; set; }
        public String Genre { get; set; }
        public TimeSpan Laufzeit { get; set; }
        public Boolean HatCover { get; set; }
        public String Verlag { get; set; }
        public String Typ { get; set; }
        public String Kommentar { get; set; }
    }
    /// <summary>
    /// Verarbeitet Fehler beim Schreiben von Songs
    /// Z.Zt. nur Bewertung
    /// </summary>
    public static class MP3ReadWrite
    {
        /// <summary>
        /// Aktuell nicht zu verarbeitende Songs
        /// Meistens weil die gerade abgespielt werden.
        /// </summary>
        public static List<MP3File> listOfCurrentErrors = new List<MP3File>();
        /// <summary>
        /// Verarbeitungsfehler zufügen.
        /// </summary>
        /// <param name="_song">Falls übergebener Song schon vorhanden, werden die neuen Daten genommen.</param>
        public static void Add(MP3File _song)
        {
            MP3File song = listOfCurrentErrors.Find(r => r.Pfad == _song.Pfad);
            if (song == null)
            {
                listOfCurrentErrors.Add(_song);
            }
            else
            {
                song.Bewertung = _song.Bewertung;
                song.Geschwindigkeit = _song.Geschwindigkeit;
                song.Stimmung = _song.Stimmung;
                song.Gelegenheit = _song.Gelegenheit;
                song.Aufwecken = _song.Aufwecken;
                song.BewertungMine = _song.BewertungMine;
                song.Genre = _song.Genre;
                song.Laufzeit = _song.Laufzeit;
            }
        }
        /// <summary>
        /// Versucht die Fehlerliste abzuarbeiten 
        /// </summary>
        public static void WriteNow()
        {
            //Falls mal Fehler vorhanden waren diese nun abarbeiten
            if (listOfCurrentErrors.Count > 0)
            {
                List<int> worked = new List<int>();
                int counter = 0;
                foreach (MP3File item in listOfCurrentErrors)
                {
                    //Versuchen die Fehler abzuarbeiten
                    if (WriteMetaData(item))
                    {
                        worked.Add(counter);
                    }
                    counter++;
                }
                if (worked.Count > 0)
                {
                    //Es konnte etwas abgearbeitet werden und wir nun aus der Fehlerliste entfernt
                    foreach (int erledigt in worked)
                    {
                        try
                        {
                            MP3File k = listOfCurrentErrors[erledigt];
                            listOfCurrentErrors.Remove(k);
                        }
                        // ReSharper disable once EmptyGeneralCatchClause
                        catch
                        {
                        }
                    }

                }
            }

        }
        /// <summary>
        /// Schreibt die Metadata des übergebenen Songs
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public static Boolean WriteMetaData(MP3File song)
        {
            String orgrating = String.Empty;
            try
            {
                //Taglib Objekt erstellen
                TagLib.File taglibobjekt = TagLib.File.Create(song.Pfad);
                //taglibobjekt.Tag.Genres[0] = song.Genre;
                switch (taglibobjekt.MimeType)
                {
                    case "taglib/flac":
                    case "taglib/m4a":
                        if (song.Bewertung != "No")
                        {
                            //MM Behandelt Bomben bei FLAc anders als bei MP3 
                            //Beim setzten wird hier nun -1 auf 0 gesetzt und 0 als nicht vorhanden.
                            orgrating = song.Bewertung; //Für ein Catch den alten wert merken.
                            if (song.Bewertung == "0")
                            {
                                song.Bewertung = "";
                            }
                            if (song.Bewertung == "-1")
                            {
                                song.Bewertung = "0";

                            }
                            taglibobjekt.Tag.Rating = song.Bewertung;
                        }
                        taglibobjekt.Tag.Mood = song.Stimmung.ToString();
                        taglibobjekt.Tag.Occasion = song.Gelegenheit.ToString();
                        taglibobjekt.Tag.Tempo = song.Geschwindigkeit.ToString().Replace("_", " ");
                        taglibobjekt.Tag.MMCustom1 = song.BewertungMine;
                        taglibobjekt.Tag.MMCustom2 = (song.Aufwecken) ? "Aufwecken" : String.Empty;
                        taglibobjekt.Tag.MMCustom3 = (song.ArtistPlaylist) ? "true" : String.Empty;
                        break;
                    case "taglib/mp3":
                        TagLib.Id3v2.Tag id3v2tag = taglibobjekt.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
                        #region Rating
                        if (song.Bewertung != "No")
                        {
                            TagLib.Id3v2.PopularimeterFrame popm = TagLib.Id3v2.PopularimeterFrame.Get(id3v2tag, "no@email", true);
                            //Das Rating wurde entfernt oder gesetzt
                            if (song.Bewertung == "0")
                            {
                                if (id3v2tag != null) id3v2tag.RemoveFrame(popm);
                            }
                            else
                            {
                                int ratingval = 0;//Bombe
                                if (song.Bewertung == "10")//0,5
                                {
                                    ratingval = 30;
                                }
                                if (song.Bewertung == "20")//1
                                {
                                    ratingval = 45;
                                }
                                if (song.Bewertung == "30")//1,5
                                {
                                    ratingval = 55;
                                }
                                if (song.Bewertung == "40")//2
                                {
                                    ratingval = 100;
                                }
                                if (song.Bewertung == "50")//2,5
                                {
                                    ratingval = 120;
                                }
                                if (song.Bewertung == "60")//3
                                {
                                    ratingval = 153;
                                }
                                if (song.Bewertung == "70")//3,5
                                {
                                    ratingval = 180;
                                }
                                if (song.Bewertung == "80")//4
                                {
                                    ratingval = 202;
                                }
                                if (song.Bewertung == "90")//4,5
                                {
                                    ratingval = 245;
                                }
                                if (song.Bewertung == "100")//5
                                {
                                    ratingval = 253;
                                }

                                popm.Rating = Convert.ToByte(ratingval);
                            }
                        }

                        #endregion Rating
                        #region Gelegenenheiten
                        /*Ermitteln und ändern falls vorhanden. Andernfalls neu generien*/
                        if (id3v2tag != null)
                        {
                            IEnumerable<TagLib.Id3v2.Frame> comm = id3v2tag.GetFrames("COMM");
                            Boolean setgelegenheit = false;
                            Boolean setgeschwindigkeit = false;
                            Boolean setstimmung = false;
                            Boolean aufwecken = false;
                            Boolean artisplaylist = false;
                            Boolean setratingMine = false;
                            // Boolean ratingmine = false;
                            foreach (var b in comm)
                            {
                                string des = ((TagLib.Id3v2.CommentsFrame)b).Description;

                                switch (des)
                                {
                                    case "MusicMatch_Situation":
                                    case "Songs-DB_Occasion":
                                        ((TagLib.Id3v2.CommentsFrame)b).Text = song.Gelegenheit.ToString();
                                        setgelegenheit = true;
                                        break;
                                    case "MusicMatch_Tempo":
                                    case "Songs-DB_Tempo":
                                        ((TagLib.Id3v2.CommentsFrame)b).Text = song.Geschwindigkeit.ToString().Replace("_", " ");
                                        setgeschwindigkeit = true;
                                        break;
                                    case "MusicMatch_Mood":
                                    case "Songs-DB_Mood":
                                        ((TagLib.Id3v2.CommentsFrame)b).Text = song.Stimmung.ToString();
                                        setstimmung = true;
                                        break;
                                    case "Songs-DB_Custom2":
                                        ((TagLib.Id3v2.CommentsFrame)b).Text = song.Aufwecken ? "Aufwecken" : "";
                                        aufwecken = true;

                                        break;
                                    case "Songs-DB_Custom3":
                                        ((TagLib.Id3v2.CommentsFrame)b).Text = song.ArtistPlaylist ? "true" : "";
                                        artisplaylist = true;

                                        break;
                                    case "Songs-DB_Custom1":
                                        ((TagLib.Id3v2.CommentsFrame)b).Text = song.BewertungMine;
                                        setratingMine = true;
                                        break;
                                }
                            }//Ende foreach
                            if (!aufwecken && song.Aufwecken)
                            {
                                TagLib.Id3v2.CommentsFrame mms = new TagLib.Id3v2.CommentsFrame("Songs-DB_Custom2", "xxx")
                                {
                                    Text = "Aufwecken"
                                };
                                id3v2tag.AddFrame(mms);
                            }
                            if (!artisplaylist && song.ArtistPlaylist)
                            {
                                TagLib.Id3v2.CommentsFrame mms = new TagLib.Id3v2.CommentsFrame("Songs-DB_Custom3", "xxx")
                                {
                                    Text = "true"
                                };
                                id3v2tag.AddFrame(mms);
                            }
                            if (!setratingMine)
                            {
                                TagLib.Id3v2.CommentsFrame mms = new TagLib.Id3v2.CommentsFrame("Songs-DB_Custom1", "xxx")
                                {
                                    Text = song.BewertungMine
                                };
                                id3v2tag.AddFrame(mms);
                            }
                            if (!setgelegenheit)
                            {
                                TagLib.Id3v2.CommentsFrame mms = new TagLib.Id3v2.CommentsFrame("MusicMatch_Situation", "xxx");
                                TagLib.Id3v2.CommentsFrame sdo = new TagLib.Id3v2.CommentsFrame("Songs-DB_Occasion", "xxx");
                                mms.Text = song.Gelegenheit.ToString();
                                sdo.Text = song.Gelegenheit.ToString();
                                id3v2tag.AddFrame(mms);
                                id3v2tag.AddFrame(sdo);
                            }
                            if (!setgeschwindigkeit)
                            {
                                TagLib.Id3v2.CommentsFrame mms = new TagLib.Id3v2.CommentsFrame("MusicMatch_Tempo", "xxx");
                                TagLib.Id3v2.CommentsFrame sdo = new TagLib.Id3v2.CommentsFrame("Songs-DB_Tempo", "xxx");
                                mms.Text = song.Geschwindigkeit.ToString().Replace("_", " ");
                                sdo.Text = song.Geschwindigkeit.ToString().Replace("_", " ");
                                id3v2tag.AddFrame(mms);
                                id3v2tag.AddFrame(sdo);
                            }
                            if (!setstimmung)
                            {
                                TagLib.Id3v2.CommentsFrame mms = new TagLib.Id3v2.CommentsFrame("MusicMatch_Mood", "xxx");
                                TagLib.Id3v2.CommentsFrame sdo = new TagLib.Id3v2.CommentsFrame("Songs-DB_Mood", "xxx");
                                mms.Text = song.Stimmung.ToString();
                                sdo.Text = song.Stimmung.ToString();
                                id3v2tag.AddFrame(mms);
                                id3v2tag.AddFrame(sdo);
                            }
                        }

                        #endregion Gelegenheiten
                        break;
                }


                taglibobjekt.Save();
                taglibobjekt.Dispose();

                //For Debuging
                /*
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Update Done");
                sb.AppendLine(lied.Pfad);
                sb.AppendLine(lied.Bewertung);
                using (StreamWriter outfile = new StreamWriter(@"C:\done.txt"))
                {
                    outfile.Write(sb.ToString());

                }
                 */
                return true;
            }
            catch
            {
                if (!String.IsNullOrEmpty(orgrating))
                {
                    song.Bewertung = orgrating; //OriginaleBewertung wieder herstellen.
                }
                return false;
            }
        }
        /// <summary>
        /// Liest aus dem Übergebenen Pfad die Metadaten des Songs aus
        /// </summary>
        /// <param name="pfad">Ort wo die Datei liegt</param>
        /// <returns>Das entsprechende MP3File Objekt</returns>
        public static MP3File ReadMetaData(string pfad)
        {
            MP3File lied = new MP3File();
            try
            {
                lied.Pfad = pfad;
                //Taglib Objekt erstellen
                //Wenn die Datei existiert verarbeiten.
                if (File.Exists(lied.Pfad))
                {
                    TagLib.File taglibobjekt = TagLib.File.Create(lied.Pfad);
                    
                    if (taglibobjekt.Tag.Lyrics != null)
                    {
                        string dummy = Regex.Replace(taglibobjekt.Tag.Lyrics, @"[\r\n]+", "<br />");
                        dummy = Regex.Replace(dummy, "Deutsch:", "<b>Deutsch:</b>");
                        lied.Lyric = Regex.Replace(dummy, "Englisch:", "<b>Englisch:</b>");
                    }
                    else
                    {
                        lied.Lyric = MP3Lyrics.NoLyrics.ToString();
                    }
                    //Global
                    lied.Typ = taglibobjekt.MimeType;
                    lied.Genre = (taglibobjekt.Tag.Genres != null && taglibobjekt.Tag.Genres.Length > 0 ? taglibobjekt.Tag.Genres[0] : "leer");
                    lied.Laufzeit = taglibobjekt.Properties.Duration;
                    lied.Kommentar = taglibobjekt.Tag.Comment;
                    //Cover
                    if (taglibobjekt.Tag.Pictures.Length == 0)
                    {
                        lied.HatCover = false;
                    }
                    if (!String.IsNullOrEmpty(taglibobjekt.Tag.Publisher))
                    {
                        lied.Verlag = taglibobjekt.Tag.Publisher;
                    }
                    if (!String.IsNullOrEmpty(taglibobjekt.Tag.FirstComposer))
                    {
                        lied.Komponist = taglibobjekt.Tag.FirstComposer;
                    }
                    string art = "";
                    switch (taglibobjekt.MimeType)
                    {
                        case "taglib/flac":
                        case "taglib/m4a":
                            lied.Album = taglibobjekt.Tag.Album;
                            if (!String.IsNullOrEmpty(taglibobjekt.Tag.FirstPerformer))
                            {
                                art = taglibobjekt.Tag.FirstPerformer;
                            }
                            if (!String.IsNullOrEmpty(taglibobjekt.Tag.FirstAlbumArtist))
                            {
                                art = taglibobjekt.Tag.FirstAlbumArtist;
                            }
                            lied.Artist = art;
                            if (taglibobjekt.Tag.Rating != null && taglibobjekt.Tag.Rating != "Not Set")
                            {
                                int r;
                                int.TryParse(taglibobjekt.Tag.Rating, out r);
                                lied.Bewertung = taglibobjekt.Tag.Rating;
                                //Flac wird von MM eine Bome gleich 0 gesetzt Für die Verarbeitung von allen anderen Dingen wird hier das Verarbeiten 
                                //Wie bei MP3 auf -1 gesetzt.
                                if (r == 0)
                                {
                                    lied.Bewertung = "-1";
                                }
                            }
                            else
                            {
                                lied.Bewertung = "0";

                            }
                            Enums.Gelegenheit lge = Enums.Gelegenheit.None;
                            if (!string.IsNullOrEmpty(taglibobjekt.Tag.Occasion))
                            {
                                Enum.TryParse(taglibobjekt.Tag.Occasion, false, out lge);
                            }
                            lied.Gelegenheit = lge;
                            Enums.Geschwindigkeit lg = Enums.Geschwindigkeit.None;
                            if (!string.IsNullOrEmpty(taglibobjekt.Tag.Tempo))
                            {
                                Enum.TryParse(taglibobjekt.Tag.Tempo.Replace(" ", "_"), false, out lg);
                            }
                            lied.Geschwindigkeit = lg;
                            lied.Jahr = taglibobjekt.Tag.Year.ToString();
                            if (string.IsNullOrEmpty(taglibobjekt.Tag.Mood))
                            {
                                lied.Stimmung = Enums.Stimmung.None;
                            }
                            else
                            {
                                Enums.Stimmung ls;
                                Enum.TryParse(taglibobjekt.Tag.Mood, false, out ls);
                                lied.Stimmung = ls;
                            }
                            lied.Titel = taglibobjekt.Tag.Title;
                            lied.Aufwecken = !String.IsNullOrEmpty(taglibobjekt.Tag.MMCustom2);
                            lied.ArtistPlaylist = !String.IsNullOrEmpty(taglibobjekt.Tag.MMCustom3);
                            lied.BewertungMine = taglibobjekt.Tag.MMCustom1 ?? "0";

                            break;
                        case "taglib/mp3":
                            #region mp3
                            
                            TagLib.Id3v2.Tag id3v2tag = taglibobjekt.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
                            if (id3v2tag != null)
                            {
                                lied.Album = id3v2tag.Album;
                                art = "";
                                if (!String.IsNullOrEmpty(id3v2tag.FirstPerformer))
                                {
                                    art = taglibobjekt.Tag.FirstPerformer;
                                }
                                if (!String.IsNullOrEmpty(id3v2tag.FirstAlbumArtist))
                                {
                                    art = taglibobjekt.Tag.FirstAlbumArtist;
                                }
                                lied.Artist = art;
                                lied.Jahr = id3v2tag.Year.ToString();
                                lied.Komponist = id3v2tag.FirstComposer;
                                lied.Titel = id3v2tag.Title;
                                #region Rating
                                TagLib.Id3v2.PopularimeterFrame popm = TagLib.Id3v2.PopularimeterFrame.Get(id3v2tag, "no@email", false);
                                if (popm != null)
                                {
                                    int songratingstring = Convert.ToInt16(popm.Rating);
                                    int resultval = -1; //Bombe
                                    if (songratingstring > 8 && songratingstring < 40)
                                    {
                                        resultval = 10;//0,5
                                    }
                                    if ((songratingstring > 39 && songratingstring < 50) || songratingstring == 1)
                                    {
                                        resultval = 20;//1
                                    }
                                    if (songratingstring > 49 && songratingstring < 60)
                                    {
                                        resultval = 30;//1,5
                                    }
                                    if (songratingstring > 59 && songratingstring < 114)
                                    {
                                        resultval = 40;//2
                                    }
                                    if (songratingstring > 113 && songratingstring < 125)
                                    {
                                        resultval = 50;//2,5
                                    }
                                    if (songratingstring > 124 && songratingstring < 168)
                                    {
                                        resultval = 60;//3
                                    }
                                    if (songratingstring > 167 && songratingstring < 192)
                                    {
                                        resultval = 70;//3,5
                                    }
                                    if (songratingstring > 191 && songratingstring < 219)
                                    {
                                        resultval = 80;//4
                                    }
                                    if (songratingstring > 218 && songratingstring < 248)
                                    {
                                        resultval = 90;//4,5
                                    }
                                    if (songratingstring > 247)
                                    {
                                        resultval = 100;//5
                                    }
                                    lied.Bewertung = resultval.ToString();

                                }
                                else { lied.Bewertung = "0"; }
                                #endregion Rating
                                //Gelegenheiten und Custom MM DB auslesen auslesen.
                                #region Gelegenheiten
                                IEnumerable<TagLib.Id3v2.Frame> comm = id3v2tag.GetFrames("COMM");
                                Enums.Gelegenheit gelegenheit = Enums.Gelegenheit.None;
                                Enums.Geschwindigkeit geschwindigkeit = Enums.Geschwindigkeit.None;
                                Enums.Stimmung stimmung = Enums.Stimmung.None;
                                Boolean aufwecken = false;
                                String ratingmine = "0";
                                Boolean artistplaylist = false;
                                foreach (var b in comm)
                                {
                                    if (((TagLib.Id3v2.CommentsFrame)b).Description == "MusicMatch_Situation")
                                    {
                                        string t = ((TagLib.Id3v2.CommentsFrame)b).Text;
                                        if (!string.IsNullOrEmpty(t))
                                        {
                                            Enum.TryParse(t, false, out gelegenheit);
                                        }
                                    }
                                    if (((TagLib.Id3v2.CommentsFrame)b).Description == "MusicMatch_Tempo")
                                    {
                                        var k = ((TagLib.Id3v2.CommentsFrame)b).Text.Replace(" ", "_");
                                        if (!string.IsNullOrEmpty(k))
                                        {
                                            Enum.TryParse(k, false, out geschwindigkeit);
                                        }
                                    }
                                    if (((TagLib.Id3v2.CommentsFrame)b).Description == "MusicMatch_Mood")
                                    {
                                        var x = ((TagLib.Id3v2.CommentsFrame)b).Text;
                                        if (!string.IsNullOrEmpty(x))
                                        {
                                            Enum.TryParse(x, false, out stimmung);
                                        }
                                    }
                                    //aufwecken
                                    if (((TagLib.Id3v2.CommentsFrame)b).Description == "Songs-DB_Custom2")
                                    {
                                        aufwecken = !String.IsNullOrEmpty(((TagLib.Id3v2.CommentsFrame)b).Text);
                                    }
                                    //Rating Mine
                                    if (((TagLib.Id3v2.CommentsFrame)b).Description == "Songs-DB_Custom1")
                                    {
                                        ratingmine = String.IsNullOrEmpty(((TagLib.Id3v2.CommentsFrame)b).Text) ? "0" : ((TagLib.Id3v2.CommentsFrame)b).Text;
                                    }
                                    //ArtistPlaylist
                                    if (((TagLib.Id3v2.CommentsFrame)b).Description == "Songs-DB_Custom3")
                                    {
                                        artistplaylist = !String.IsNullOrEmpty(((TagLib.Id3v2.CommentsFrame)b).Text);
                                    }
                                }
                                lied.Gelegenheit = gelegenheit;
                                lied.Geschwindigkeit = geschwindigkeit;
                                lied.Stimmung = stimmung;
                                lied.Aufwecken = aufwecken;
                                lied.ArtistPlaylist = artistplaylist;
                                #endregion Gelegenheiten
                                lied.BewertungMine = ratingmine;
                            }
                            #endregion mp3
                            break;

                    }//Ende Switch für die MIMETypes

                    taglibobjekt.Dispose();
                    return lied;
                }
                //Songpfad existiert nicht
                lied.VerarbeitungsFehler = true;
                return lied;
            }
            catch
            {
                lied.VerarbeitungsFehler = true;
                return lied;
            }

        }

        /// <summary>
        /// Schreibt eine Playliste
        /// </summary>
        /// <param name="mp3files">Liste mit den songs, die geschrieben werden sollen</param>
        /// <param name="PlaylistName">Name der Playliste</param>
        /// <param name="plsavepath">Ort wohin die gespeichert werden soll.</param>
        /// <param name="changeMusicPath">Wenn ein anderer Pfad für den Song in einer Playlist genommen werden soll 
        /// Folgender Aufbau: OldValue|NewValue</param>
        /// <returns></returns>
        public static Boolean WritePlaylist(List<MP3File> mp3files, string PlaylistName, string plsavepath, string changeMusicPath = null)
        {
            try
            {

                var dir = Directory.CreateDirectory(plsavepath);
                string m3uname = PlaylistName + ".m3u";
                string file = dir.FullName + "\\" + m3uname;

                //Datei erzeugen und in UTF8 Konvertieren
                FileStream playliststream = File.Create(file);
                StreamWriter pm3u = new StreamWriter(playliststream, Encoding.Default);
                pm3u.WriteLine("#EXTM3U");
                //Durchlaufen und schreiben
                string[] peaches = { };
                if (!String.IsNullOrEmpty(changeMusicPath))
                {
                    peaches = changeMusicPath.Split('|');
                }

                foreach (MP3File song in mp3files)
                {
                    string m3ustring = "#EXTINF:" + Math.Round(song.Laufzeit.TotalSeconds) + "," + song.Artist + " - " + song.Titel;
                    pm3u.WriteLine(m3ustring);
                    if (!String.IsNullOrEmpty(changeMusicPath))
                    {
                        song.Pfad = song.Pfad.Replace(peaches[0], peaches[1]);
                    }
                    /*
                     Encoding utf8 = Encoding.UTF8;
                     Byte[] encodedBytes = utf8.GetBytes(song.Pfad);
                     Byte[] convertedBytes = Encoding.Convert(Encoding.UTF8, Encoding.UTF7, encodedBytes);
                     Encoding utf7 = Encoding.UTF7;
                     song.Pfad = utf7.GetString(convertedBytes);
                   */
                    pm3u.WriteLine(song.Pfad);
                }
                pm3u.Close();
                pm3u.Dispose();
                playliststream.Close();
                playliststream.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public enum MP3Lyrics
    {
        NoLyrics
    }
}
