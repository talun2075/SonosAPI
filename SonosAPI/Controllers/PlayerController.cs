using System;
using System.Collections.Generic;
using System.Web.Http;
using SonosUPNP;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using MP3File;
using SonosAPI.Classes;
using SonosUPnP;

namespace SonosAPI.Controllers
{
    public class PlayerController : ApiController
    {
        
        /// <summary>
        /// Liste mit allen Functionen. 
        /// </summary>
        /// <returns>Stringarray mit allen Funktionen.</returns>
        public string[] Get()
        {
            string[] d = new string[19];
            d[0] = "Folgende funktionen sind als Get Aufrufe vorhanden. Die ID ist immer die Nummer des Players / Rincon immer die UUID /0=Parameter egal aber es muss einer übergeben werden.";
            d[1] = "GetPlayerName/id = Name des Gerätes bekommen";
            d[2] = "GetTopologieChanged/0 = Liste mit Änderungen aller ZoneCordinator + SonosZones falls es eine Änderung in der Topologie gibt.";
            d[3] = "BaseURL/Rincon = URL inkl Port";
            d[4] = "GetUpdateIndexInProgress/Rincon = Liefert ob es gerade ein Update der Medien Bibliothek gibt.";
            d[5] = "SetUpdateMusicIndex/Rincon = Startet ein Update der Medien Bibliothek.";
            d[6] = "Play/Rincon = Startet die Wiedergabe des Players bzw. Pausiert diese, wenn schon auf Play ist";
            d[7] = "Pause/Rincon = Pausiert die Wiedergabe des Players";
            d[8] = "Pause/Rincon = Stoppt die Wiedergabe des Players";
            d[9] = "Next/Rincon = Nächster Song des Players";
            d[10] = "Previous/Rincon = Vorheriger Song des Players";
            d[11] = "SetMute/Rincon = Stumm und nicht Stumm setzen";
            d[12] = "GetMute/Rincon = Ermittelt ob Stumm geschaltet wurde";
            d[13] = "GetSleepTimer/Rincon = Ermittelt ob es einen SleepTimer gibt.";
            d[14] = "GetPlaylists/0 = Ermittelt alle gespeicherten Playlist";
            d[15] = "GetErrorListCount/0 = Ermittelt ob es noch Fehler beim schreiben der Bewertungen gibt und Liefert die Anzahl zurück. Fehler tauchen auf, wenn der Song gerade abgespielt wird.";
            d[16] = "GetErrorList/0 = Liefert die Sonsgs, die noch nicht verarbeitet werden konnten";
            d[17] = "GetServerErrorList/0 = Sonositem mit dem aktuellem Track";
            d[18] = "GetAlarms/Rincon = Liefert eine Liste mit allen Weckern aller Player";
            return d;
        }

        #region Frontend GET Fertig
        /// <summary>
        /// Liefert einen ZoneCordinator zurück.
        /// Wird beim Eventing genutzt um dem Client entsprechend die geänderte Zone zu liefern.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public SonosZone GetZonebyRincon(string id)
        {
            try
            {
                if (SonosHelper.Sonos == null)
                {
                    return null;
                }
                return SonosHelper.Sonos.Zones.FirstOrDefault(firstzone => firstzone.Coordinator.UUID == id);
            }
            catch (Exception x)
            {
                AddServerErrors("GetZonebyRincon", x);
                return null;
            }
        }
       
            /// <summary>
        /// Setzen des Wiedergabemodus wie Schuffe und Repeat
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v">SHUFFLE,NORMAL,SHUFFLE_NOREPEAT,REPEAT_ALL</param>
        [HttpGet]
        public Boolean SetPlaymode(string id, string v)
        {
            try
            {
                GetPlayerbyRincon(id).SetPlayMode(v);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetPlaymode", x);
                return false;
            }

        }
        /// <summary>
        /// Ändert den Aktivierungsstatus eines Weckers
        /// </summary>
        /// <param name="id">egal eine Playerid</param>
        /// <param name="v">ID des Weckers</param>
        /// <param name="v2">Bool an/aus</param>
        /// <returns></returns>
        [HttpGet]
        public Boolean AlarmEnable(string id, string v, string v2)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                Boolean ena = Convert.ToBoolean(v2);
                int alid;
                int.TryParse(v, out alid);
                var alarmsl = pl.Alarms;
                foreach (Alarm al in alarmsl)
                {
                    if (al.ID == alid)
                    {
                        al.Enabled = ena;
                        pl.UpdateAlarm(al);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                AddServerErrors("AlarmEnable", ex);
                return false;
            }
        }
        /// <summary>
        /// Umsortierung eines Songs in der Playlist
        /// </summary>
        /// <param name="id">Player</param>
        /// <param name="v">alteposition</param>
        /// <param name="v2">neueposition</param>
        [HttpGet]
        public Boolean ReorderTracksInQueue(string id, string v, string v2)
        {
            try
            {
                int oldposition;
                int newposition;
                int.TryParse(v, out oldposition);
                int.TryParse(v2, out newposition);
                if (newposition > 0 && oldposition < newposition)
                    newposition++;
                if (oldposition != newposition && oldposition > 0 && newposition > 0)
                {
                    GetPlayerbyRincon(id).ReorderTracksinQueue(oldposition, newposition);
                }
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("ReorderTracksInQueue", x);
                return false;
            }

        }
        /// <summary>
        /// Liefert die genaue Zeit, wann sich das letzte mal an den Zonen/Anzahl Playern bzw. ein Currentstate etwas geändert hat.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Dictionary<String, DateTime> GetTopologieChanged()
        {
            try
            {
                if (SonosHelper.Sonos == null)
                {
                    SonosHelper.Initialisierung();
                }
                return SonosHelper.ZoneChangeList;
            }
            catch (Exception x)
            {
                AddServerErrors("GetTopologieChanged", x);
                return new Dictionary<string, DateTime>();
            }

        }

        /// <summary>
        /// Liefert die Baseulr für den Angegeben Player zurück.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public string BaseURL(string id)
        {
            try
            {

                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return String.Empty;
                return pl.BaseUrl;
            }
            catch (Exception x)
            {
                AddServerErrors("BaseURL", x);
                return SonosConstants.empty;
            }
        }
        /// <summary>
        /// Wird der Musikindex gerade AKtualisiert?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean GetUpdateIndexInProgress(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return false;
                return GetPlayerbyRincon(id).GetMusicIndexinProgress();
            }
            catch (Exception x)
            {
                AddServerErrors("GetUpdateIndexingProgress", x);
                return false;
            }
        }
        /// <summary>
        /// Player zum Absoielen bewegen
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean Play(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return false;
                GetPlayerbyRincon(id).SetPlay();
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("Play", x);
                return false;
            }
        }
        /// <summary>
        /// Setzen von Pause
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean Pause(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return false;
                GetPlayerbyRincon(id).SetPause();
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("Pause", x);
                return false;
            }
        }
        /// <summary>
        /// Player Stoppen
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean Stop(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return false;
                GetPlayerbyRincon(id).SetStop();
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("Stop", x);
                return false;
            }
        }
        /// <summary>
        /// Nächster Song
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean Next(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return false;
                GetPlayerbyRincon(id).SetPlayNext();
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("Next", x);
                return false;
            }
        }
        /// <summary>
        /// Vorheriger Song
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean Previous(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return false;
                GetPlayerbyRincon(id).SetPlayPrevious();
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("Previous", x);
                return false;
            }
        }
        /// <summary>
        /// Stummschalten eines Players
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean SetMute(string id)
        {
            try
            {
                GetPlayerbyRincon(id).SetMute();
                //foreach (var zone in SonosHelper.Sonos.Zones.Where(zone => zone.Coordinator.UUID == id))
                //{
                //    zone.Coordinator.SetMute();
                //}
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetMute", x);
                return false;
            }
        }
        /// <summary>
        /// Aktualisiert den Musik index
        /// </summary>
        /// <param name="id"></param>
        [HttpGet]
        public Boolean SetUpdateMusicIndex(string id)
        {
            try
            {
                GetPlayerbyRincon(id).UpdateMusicIndex();
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetUpdateMusicIndex", x);
                return false;
            }
        }
        [HttpGet]
        public Boolean GetMute(string id)
        {
            try
            {
                return GetPlayerbyRincon(id).GetMute();
            }
            catch (Exception ex)
            {
                AddServerErrors("GetMute", ex);
                return false;
            }
        }
        /// <summary>
        /// Setzen des Schlummermodus
        /// </summary>
        /// <param name="id">Rincon des Players</param>
        /// <param name="v">Dauer in hh:mm:ss oder "aus"</param>
        [HttpPost]
        public Boolean SetSleepTimer(string id, [FromBody]string v)
        {
            try
            {
                var k = new Regex(@"^(?:[01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$");
                if (k.IsMatch(v) || v == "aus")
                {
                    if (v == "aus")
                    {
                        v = String.Empty;
                    }
                    GetPlayerbyRincon(id).SetRemainingSleepTimerDuration(v);
                }
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetSleepTimer", x);
                return false;
            }
        }
        /// <summary>
        /// Ermitteln des Sleeptimers.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public string GetSleepTimer(string id)
        {
            try
            {
                var pla = GetPlayerbyRincon(id);
                if (pla == null)
                {
                    return "aus";
                }
                string rm = pla.GetRemainingSleepTimerDuration();
                return rm == "00:00:00" ? "aus" : rm;
            }
            catch (Exception x)
            {
                AddServerErrors("SetPlaymode", x);
                return "aus";
            }
        }
        /// <summary>
        /// Ermitteln der Lautstärke
        /// </summary>
        /// <param name="id">Rincon des Players</param>
        /// <returns>Wert zwischen 1 und 100</returns>
        [HttpGet]
        public int GetVolume(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return 1;
                return GetPlayerbyRincon(id).GetVolume();
            }
            catch (Exception x)
            {
                AddServerErrors("GetVolume", x);
                return 1;
            }
        }
        /// <summary>
        /// Ermittelt alle Playlists, die es gibt. 
        /// Importierte und Sonos
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IList<SonosItem> GetPlaylists(int id)
        {
            try
            {

                if (SonosHelper.Sonos == null || SonosHelper.Sonos.Players == null || SonosHelper.Sonos.Players.Count == 0)
                {
                    return null;
                }
                //Es wird eine Zone genommen bei der der Coordinator ein ContentDirectory hat
                IList<SonosItem> k = new List<SonosItem>();
                foreach (SonosZone zone in SonosHelper.Sonos.Zones)
                {
                    if (zone.Coordinator.ContentDirectory != null)
                    {
                        try
                        {
                            k = zone.Coordinator.GetallPlaylist();
                        }
                        catch
                        {
                            continue;
                        }
                        break;
                    }
                }

                foreach (SonosItem si in k)
                {
                    if (si.Title.Contains(".m3u"))
                    {
                        si.Title = si.Title.Substring(0, si.Title.Length - 4);
                        si.Description = "M3U";
                    }
                    else
                    {
                        si.Description = "Sonos";
                    }
                }
                return k;
            }
            catch (Exception x)
            {
                //AddServerErrors("GetPlaylists", x);
                return new List<SonosItem>{ new SonosItem{ Title = "Exception" },new SonosItem {Title = x.Message} };
            }
        }
        /// <summary>
        /// Liefert alle gespeicherten Favoriten.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IList<SonosItem> GetFavorites(string id)
        {
            return GetPlayerbyRincon(id).GetFavorites();
        }
        /// <summary>
        /// Ermittelt den Fade Mode
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean GetFadeMode(string id)
        {
            return GetPlayerbyRincon(id).GetCrossfadeMode();
        }

        /// <summary>
        ///Falls ein AudioIn Element vorhanden ist, dann kann dieses hier gesetzt werden.
        /// </summary>
        /// <param name="id"></param>
        [HttpGet]
        public Boolean SetAudioIn(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl.CurrentState.CurrentTrack.Uri == null || pl.CurrentState.CurrentTrack.Uri.StartsWith(SonosConstants.xrinconstream) && (pl.CurrentState.CurrentTrack.StreamContent == SonosConstants.AudioEingang || pl.CurrentState.CurrentTrack.Title == "Heimkino"))
                {
                    //Normale Playlist laden
                    pl.SetAVTransportURI(SonosConstants.xrinconqueue + pl.UUID + "#0");
                }
                else
                {
                    //Stream laden
                    pl.SetAVTransportURI(SonosConstants.xrinconstream + pl.UUID);
                }
                //Thread.Sleep(200);
                pl.CurrentState.CurrentTrack = SonosItemHelper.CheckItemForStreaming(pl.CurrentState.CurrentTrack, pl);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetAudioIn", x);
                return false;
            }
        }
        /// <summary>
        /// Liefert die Liste von Ratingsfehlern
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public int GetErrorListCount()
        {
            return DevicesController.MP3ErrorsCounter;
        }
        /// <summary>
        /// Liefert die ServerErrorliste
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Dictionary<String, String> GetServerErrorList(int id)
        {
            return SonosHelper.serverErrors;
        }
        /// <summary>
        /// Liefert die Namen der kaputten Songs
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public List<MP3File.MP3File> GetErrorList()
        {
            try
            {
                foreach (MP3File.MP3File mp3 in MP3ReadWrite.listOfCurrentErrors)
                {
                    if (String.IsNullOrEmpty(mp3.Titel))
                    {
                        mp3.Titel = MP3ReadWrite.ReadMetaData(mp3.Pfad).Titel;
                    }
                }

                return MP3ReadWrite.listOfCurrentErrors;
            }
            catch (Exception x)
            {
                AddServerErrors("GetErrorList", x);
                return new List<MP3File.MP3File>();
            }
        }
        /// <summary>
        /// Liefert alle Wecker als Liste
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IList<Alarm> GetAlarms(string id)
        {
            try
            {
                SonosPlayer pl = GetPlayerbyRincon(id);
                if (pl == null) return null;
                return GetPlayerbyRincon(id).Alarms;
            }
            catch (Exception x)
            {
                AddServerErrors("GetAlarms", x);
                return null;

            }
        }
        /// <summary>
        /// Lautstärke setzen
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpGet]
        public Boolean SetVolume(string id, string v)
        {
            try
            {
                UInt16 value = Convert.ToUInt16(v);
                if (value > 100)
                {
                    value = 100;
                }
                if (value < 1)
                {
                    value = 1;
                }
                GetPlayerbyRincon(id).SetVolume(value);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetVolume", x);
                return false;

            }
        }
        /// <summary>
        /// Setzen der Gruppenlautstärke
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        [HttpGet]
        public Boolean SetGroupVolume(string id, string v)
        {
            try
            {
                UInt16 value = Convert.ToUInt16(v);
                if (value > 100)
                {
                    value = 100;
                }
                if (value < 1)
                {
                    value = 1;
                }
                //Seit Update muss das Volume Relativ zum alten Wert für jeden Player gesetzt werden.
                var sz = SonosHelper.Sonos.Zones.FirstOrDefault(x => x.Coordinator.UUID == id);
                if (sz != null)
                {
                    int relValue = value - sz.Coordinator.GroupVolume;
                    ushort newCoVol = (ushort) (sz.Coordinator.CurrentState.Volume + relValue);
                    sz.Coordinator.SetVolume(newCoVol);
                    foreach (SonosPlayer sp in sz.Players)
                    {
                        var vol = (ushort)(sp.CurrentState.Volume+relValue);
                        sp.SetVolume(vol);
                    }
                }

                GetPlayerbyRincon(id).SetGroupVolume(value);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetVolume", x);
                return false;

            }
        }
        /// <summary>
        /// Soll überbelendet werden
        /// </summary>
        /// <param name="id">Rincon</param>
        /// <param name="v">true/false</param>
        [HttpGet]
        public Boolean SetFadeMode(string id, Boolean v)
        {
            try
            {
                GetPlayerbyRincon(id).SetCrossfadeMode(v);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetFadeMode", x);
                return false;
            }
        }
        /// <summary>
        /// Songs aus der Wiedergabeliste entfernen
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpGet]
        public Boolean RemoveSongInPlaylist(string id, string v)
        {
            try
            {
                GetPlayerbyRincon(id).RemoveFromQueue(v);
                Thread.Sleep(300);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("SetPlaymode", x);
                return false;
            }
        }
        /// <summary>
        /// Aktuelle Playlist zurück geben (alt)
        /// </summary>
        /// <param name="id">ID des Players</param>
        /// <param name="v">Nummer ab wo die Rückgabe erfolgen soll. (Es werden immer nur 100 zurück gegeben)</param>
        /// <returns></returns>
        [HttpGet]
        public IList<SonosItem> GetCurrentPlaylist(string id, string v)
        {
            try
            {
                var pl = GetPlayerbyRincon(id); //Player
                var pli = pl.GetMediaInfoURIMeta(); //Aktuelle Infos

                //Hier kommen wir an, wenn ein Eingang Stream eines Players benutzt wird durchgeführt wird. 
                if (pli[0].StartsWith(SonosConstants.xrinconstream))
                {
                    //Stream eines Eingangs von einem Player
                    string[] pieces = pli[0].Split(new[] { ":" }, StringSplitOptions.None);
                    string streamPlayerName = GetPlayerbyRincon(pieces[1]).Name;
                    SonosItem en = new SonosItem
                    {
                        Title = SonosConstants.AudioEingang+": " + streamPlayerName,
                        MetaData = "ShowNoInfos"
                    };
                    IList<SonosItem> len = new List<SonosItem>();
                    len.Add(en);
                    return len;
                }
                //Hier bei MP3 Radio
                if (pli[0].StartsWith("x-rincon-mp3radio") || pli[0].StartsWith(SonosConstants.xsonosapistream))
                {
                    int posleft = pli[1].IndexOf("<dc:title>", StringComparison.Ordinal);
                    string stream = pli[1].Substring(posleft);
                    int posright = stream.IndexOf("</dc:title>", StringComparison.Ordinal);
                    stream = stream.Substring(0, posright);
                    //Stream eines Eingans von einem Player
                    SonosItem en = new SonosItem { Title = "Radio Stream: " + stream, MetaData = "ShowNoInfos" };
                    IList<SonosItem> len = new List<SonosItem>();
                    len.Add(en);
                    return len;
                }
                var playlist = pl.GetPlaylist(Convert.ToUInt32(v)); // Playlist
                if (playlist.Count > 0)
                {
                    //return RateList(playlist, null); //Da das Rating so lange dauert wurde das nun entfernt und wird am Client gemacht.
                    return playlist;
                }
                return null;
            }
            catch (Exception x)
            {
                AddServerErrors("GetCurrentPlalist", x);
                return null;

            }
        }
        /// <summary>
        /// Für den Übergebenen Player den Typ Playlist zurückgeben
        /// </summary>
        /// <param name="id">Rincon des Players</param>
        /// <returns>Playlist mit SonosItems und TotalMatches</returns>
        [HttpGet]
        public Playlist GetPlayerPlaylist(string id)
        {
            Playlist plist = new Playlist();
            try
            {
                var pl = GetPlayerbyRincon(id); //Player
                if (pl == null) return plist;
                try
                {
                    plist.FillPlaylist(pl);
                }
                catch (Exception ex)
                {
                    AddServerErrors("GetPlayerPlaylist FillPlaylist", ex);
                    return plist;
                }
                if (plist.NumberReturned > 0 && pl.CurrentState.NumberOfTracks == 0)
                {
                    pl.CurrentState.NumberOfTracks = (int)plist.NumberReturned;
                    pl.ManuellStateChange(DateTime.Now);
                }
                return plist;
            }
            catch (Exception x)
            {
                AddServerErrors("GetPlayerPlaylist", x);
                return plist;
            }
        }
        /// <summary>
        /// Liefert den Playmode (Wiedergabeart) zurück
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public String GetPlayMode(string id)
        {
            try
            {
                SonosPlayer pla = GetPlayerbyRincon(id);
                pla.CurrentState.CurrentPlayMode = pla.GetPlaymode();
                return pla.CurrentState.CurrentPlayMode;
            }
            catch (Exception x)
            {
                AddServerErrors("GEtPlayMode", x);
                return String.Empty;
            }
        }
        /// <summary>
        /// Liefert ob ein Player pausiert, gestoppt oder abspielend ist.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public string GetPlayState(string id)
        {
            try
            {
                return GetPlayerbyRincon(id).GetPlayerStatus().ToString();
            }
            catch (Exception x)
            {
                AddServerErrors("GetPlayState", x);
                return PlayerStatus.STOPPED.ToString();
            }
        }
        #endregion Frontend GET Fertig
        #region Test
        [HttpGet]
        public IList<SonosPlayer> DevTestGet()
        {
            //Prüfen, was übergeben wird.
            //var base64EncodedBytes = Convert.FromBase64String(v2);
            //return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

            //window.btoa("x-file-cifs://NAS/Musik/Musik/Herbert%20Gr%c3%b6nemeyer/Mensch/10.%20Zum%20Meer.flac")
            if (SonosHelper.Sonos != null)
            {
                return SonosHelper.Sonos.Players;
            }
            return null;
        }

        [HttpPost]
        public Boolean DevTestPost(string id, [FromBody] string v)
        {
            //var pl = GetPlayerbyRincon(id);
            return true;
        }
        #endregion Test
        #region PrivateFunctions

        /// <summary>
        /// Durchläuft die Liste und gibt diese zurück mit Ratings und Gelegenehiten, falls vorhanden
        /// </summary>
        /// <param name="_list">Liste mit Sonositems (z.B. durch Browse geliefert)</param>
        /// <param name="_del">Liste mit Sonositems die gefiltert werden müssen. oder Null</param>
        /// <param name="srf">SonosRatingFilter zum Prüfen ob der Filter zieht.</param>
        /// <returns></returns>
        private IList<SonosItem> RateList(IList<SonosItem> _list, IList<SonosItem> _del, SonosRatingFilter srf)
        {
            try
            {
                foreach (SonosItem item in _list)
                {
                    MP3File.MP3File lied = new MP3File.MP3File();
                    //Wenn kein Song weiter machen.
                    if (item.ContainerID != null)
                    {
                        item.MP3 = lied;
                        continue;
                    }

                    try
                    {
                        if (item.Uri != null)
                        {
                            string itemp = URItoPath(item.Uri);
                            lied = MP3ReadWrite.ReadMetaData(itemp);
                        }
                        else
                        {
                            lied.VerarbeitungsFehler = true;
                        }

                    }
                    catch
                    {
                        lied.VerarbeitungsFehler = true;
                    }
                    item.MP3 = lied;
                    if (!srf.IsDefault && _del !=null)
                    {
                        if (!srf.CheckSong(lied))
                        {
                            _del.Add(item);
                        }

                    }
                } //Foreach alle Items
                return _list;
            }
            catch (Exception x)
            {
                AddServerErrors("RateList", x);
                return _list;
            }
        }
        /// <summary>
        /// Ändert die URI zu einem Pfad
        /// </summary>
        /// <param name="_uri">SonosItem URI</param>
        /// <returns>Pfad auf dem Server</returns>
        private String URItoPath(string _uri)
        {
            _uri = _uri.Replace(DevicesController.RemoveFromUri, "");
            _uri = Uri.UnescapeDataString(_uri);
            return _uri.Replace("/", "\\");

        }
        /// <summary>
        /// Liefert den Player aufgrund des Parameters zurück
        /// </summary>
        /// <param name="rincon">Rincon eines Players</param>
        /// <returns></returns>
        private SonosPlayer GetPlayerbyRincon(string rincon)
        {
            try
            {
                if (SonosHelper.Sonos == null)
                {
                    SonosHelper.Initialisierung();
                    //AddServerErrors("GetPlayerbyRincon", new Exception("Sonos ist null"));
                    return null;
                }
                foreach (SonosPlayer pl in SonosHelper.Sonos.Players)
                {
                    if (pl.UUID == rincon)
                        return pl;
                }
                //Prüfen, ob der in der LastChangeliste vorhanden ist und dann löschen
                try
                {
                    if (SonosHelper.ZoneChangeList.ContainsKey(rincon))
                    {
                        SonosHelper.ZoneChangeList.Remove(rincon);
                    }
                }
                catch (Exception ex)
                {
                    AddServerErrors("GetPlayerByRincon:RemoveLastChange", ex);
                }
                return null;
            }
            catch (Exception x)
            {
                AddServerErrors("GetPlayerbyRincon", x);
                return null;
            }
        }
        /// <summary>
        /// Der Übergebene Container wird, zu einer gültigen URI. 
        /// </summary>
        /// <param name="_cont">Container ID die zu einer URI werden soll.</param>
        /// <param name="playerid">Id des SonosPlayers in der Liste umd die UUID zu bestimmen</param>
        /// <returns>URI</returns>
        private String ContainertoURI(string _cont, string playerid)
        {
            //Kein Filter angesetzt
            string rinconpl = String.Empty;
            if (_cont.StartsWith("S:"))
            {
                rinconpl = _cont.Replace("S:", SonosConstants.xfilecifs); //Playlist
            }
            if (_cont.StartsWith(SonosConstants.xfilecifs))
            {
                rinconpl = _cont; //Song
            }
            if (String.IsNullOrEmpty(rinconpl))
            {
                rinconpl = "x-rincon-playlist:" + playerid + "#" + _cont; //Container
            }
            return rinconpl;
        }
        /// <summary>
        /// Fügt Exception zum Sonoshelper
        /// </summary>
        /// <param name="Func"></param>
        /// <param name="ex"></param>
        private void AddServerErrors(string Func, Exception ex)
        {
            SonosHelper.ServerErrorsAdd(Func, ex);
        }

        #endregion PrivateFunctions
        #region Frontend POST Fertig
        /// <summary>
        /// Entfernt ein übergebenes FAvoriten Item
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <param name="v">Favorititem Muster: FV:2/XX</param>
        [HttpPost]
        public Boolean RemoveFavItem(string id, [FromBody] string v)
        {
            if (v == null || !v.StartsWith(SonosConstants.FV2)) return false;
            try
            {
                GetPlayerbyRincon(id).DestroyObject(v);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("RemoveFavItem", x);
                return false;
            }
        }
        /// <summary>
        /// Fügt das beigefügte Item den Favoriten hinzu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpPost]
        public Boolean AddFavItem(string id, [FromBody] string v)
        {
            try
            {
                GetPlayerbyRincon(id).CreateFavorite(v);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("AddFavItem", x);
                return false;
            }
        }

        /// <summary>
        /// Liefert eine Liste mit Sonositems zurück. Bei Tracks wird das Rating mit ausgelesen und ausgegeben.
        /// </summary>
        /// <param name="id">Id des Players</param>
        /// <param name="v">Der Browsingparameter wie z.B. A: / S: / A:Artist</param>
        /// <returns></returns>
        [HttpPost]
        public IList<SonosItem> Browsing(string id, [FromBody]string v)
        {
            var pl = GetPlayerbyRincon(id);
            if (pl == null) return null;
            IList<SonosItem> browselist = pl.Browsing(v);
            List<SonosItem> itemstodelete = new List<SonosItem>();
            browselist = RateList(browselist, itemstodelete,pl.RatingFilter);
            if (itemstodelete.Count > 0)
            {
                foreach (SonosItem item in itemstodelete)
                {
                    browselist.Remove(item);
                }
            }
            return browselist;
        }

        /// <summary>
        /// Wenn currentstate nicht funktioniert liefert die funktion dennoch Daten
        /// </summary>
        /// <param name="id">Playerid</param>
        /// <param name="v">Sollen die MP3 Daten von der Platte geladen werden?</param>
        /// <param name="v2">URI des aktuellen Songs</param>
        /// <returns></returns>
        [HttpPost]
        public PlayerState GetAktSongInfo(string id, [FromUri]Boolean v, [FromBody]string v2)
        {
            try
            {
                SonosPlayer pla = GetPlayerbyRincon(id);
                if (pla == null || pla.CurrentState == null)
                {
                    return new PlayerState();
                }
                PlayerInfo pl = pla.GetAktSongInfo();
                PlayerState current = new PlayerState();
                if (pl.TrackMetaData != "NOT_IMPLEMENTED") //Kommt, wenn kein Song in Playlist
                {
                    
                    SonosItem song = new SonosItem();
                    try
                    {
                        if (pl.isDefault())
                        {
                            //MediaInfo lesen
                            var mi = pla.GetMediaInfoURIMeta();
                            if (mi != null && mi.Count == 3)
                            {
                                song = SonosItem.ParseSingleItem(mi[2]);
                                song.Uri = mi[0];
                            }
                        }
                        else
                        {
                            song = SonosItem.ParseSingleItem(pl.TrackMetaData);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddServerErrors("GetAktSongInfo:Block1", ex);
                        return null;
                    }
                    try
                    {
                        current = pla.CurrentState;
                        if (song.Uri != current.CurrentTrack.Uri)
                        {
                            var cump3 = current.CurrentTrack.MP3;
                            current.CurrentTrack = song;
                            if (cump3 != null && song.Artist == cump3.Artist && song.Title == cump3.Titel &&
                                song.Album == cump3.Album)
                            {
                                current.CurrentTrack.MP3 = cump3; //MP3 wieder schreiben.
                            }
                        }
                        current.CurrentTrackDuration = pl.TrackDuration;
                        current.CurrentTrackNumber = pl.TrackIndex;
                        current.RelTime = pl.RelTime;
                        current.CurrentTrack = SonosItemHelper.CheckItemForStreaming(current.CurrentTrack, pla);
                        if (current.CurrentTrack.Uri.Contains(".mp4") &&
                            current.CurrentTrack.Uri.StartsWith("x-sonos-http:"))
                        {
                            //Hier MP3 Prüfen und überschreiben
                            MP3File.MP3File tmp3 =
                                SonosStreamRating.RatedListItems.Find(x => x.Pfad == current.CurrentTrack.Uri) ??
                                new MP3File.MP3File() {Pfad = v2};
                            current.CurrentTrack.MP3 = tmp3;
                            return current;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddServerErrors("GetAktSongInfo:Block2", ex);
                        return null;
                    }
                    try
                    {
                        if ((v2 != pl.TrackURI && current.CurrentTrack.Stream == false) || v ||
                            current.CurrentTrack.MP3 == null)
                        {
                            //Bei Songwechsel Zugriff aufs Dateisystem außer es wird als Parameter übergeben.
                            string u = SonosHelper.URItoPath(song.Uri);
                            current.CurrentTrack.MP3 = MP3ReadWrite.ReadMetaData(u);
                        }

                    }
                    catch (Exception ex)
                    {
                        AddServerErrors("GetAktSongInfo:Block3", ex);
                        return null;
                    }
                }
                else
                {
                    try
                    {
                        if (pl.TrackURI.StartsWith(SonosConstants.xrinconstream))
                        {
                            current.CurrentTrack.Uri = pl.TrackURI;
                            current.CurrentTrack = SonosItemHelper.CheckItemForStreaming(current.CurrentTrack, pla);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddServerErrors("GetAktSongInfo:Block4", ex);
                        return null;
                    }
                }

                return current;
            }
            catch (Exception ex)
            {
                AddServerErrors("GetAktSongInfo", ex);
                return null;
            }
        }
        /// <summary>
        /// Läd die MetaDaten aus dem Übergebenen Parameter über die TagLib
        /// </summary>
        /// <param name="id">0</param>
        /// <param name="v">URI zum Track</param>
        /// <returns></returns>
        [HttpPost]
        public MP3File.MP3File GetSongMeta(string id, [FromBody]string v)
        {
            //Prüfen, ob schon in RatingFehlerliste enthalten.
            try
            {
                //Streaming Rating.
                if (v.StartsWith("x-sonos-http:") && v.Contains(".mp4"))
                {
                    var ret = SonosStreamRating.RatedListItems.Find(x => x.Pfad == v);
                    if (ret == null)
                    {
                        return new MP3File.MP3File();
                    }
                    return ret;
                }

                v = URItoPath(v);
                    if (MP3ReadWrite.listOfCurrentErrors.Any())
                    {
                        var k = MP3ReadWrite.listOfCurrentErrors.Find(x => x.Pfad == v);
                        if (k != null) return k;
                    }
                    return MP3ReadWrite.ReadMetaData(v);
               
            }
            catch (Exception ex)
            {
                AddServerErrors("GetSongMeta", ex);
                return new MP3File.MP3File();
            }
        }

        [HttpPost]
        public Boolean SetGroups(string id, [FromBody]string[] v)
        {
            try
            {
                SonosPlayer master = GetPlayerbyRincon(id);
                SonosHelper.ZoneChangeList.Clear(); //sollte durch Events wieder gefüllt werden.
                //Es wurde keiner gewählt was dazu führt, das alle Gruppen aufgelöst werden.
                if (v[0].ToLower() == SonosConstants.empty)
                {
                    foreach (var player in SonosHelper.Sonos.Players)
                    {
                        player.BecomeCoordinatorofStandaloneGroup();
                        Thread.Sleep(200);
                    }
                }
                else
                {
                    List<string> tocordinatedplayer = v.ToList();
                    //Prüfen ob der Player in einer Zone vorhanden ist.
                    //Wenn nicht die anderen zu Ihm zufügen. 
                    foreach (var zone in SonosHelper.Sonos.Zones)
                    {
                        //Selektierter Player
                        if (zone.Coordinator.UUID == master.UUID)
                        {
                            if (zone.Players.Count == 0)
                            {
                                //er ist alleine und nun fügen wir alle hinzu außer sich selber
                                for (int i = 0; i < tocordinatedplayer.Count; i++)
                                {
                                    if (id != tocordinatedplayer[i])
                                    {
                                        GetPlayerbyRincon(v[i]).SetAVTransportURI(SonosConstants.xrincon + master.UUID);
                                        Thread.Sleep(200);
                                    }
                                }
                            }
                            else
                            {
                                //Zone enthält mehr als einen Player
                                //hier nun prüfen, ob alle gewählten player auch drin bleiben sollen; ggf neu zufügen bzw. entfernen
                                //aus valuuid die rausnehmen, die verarbeitet wurden.
                                foreach (var zoneplayers in zone.Players)
                                {
                                    if (tocordinatedplayer.Contains(zoneplayers.UUID))
                                    {
                                        //Der Player ist schon in der Zone und soll bleiben. Daher wird er aus der liste entfernt
                                        tocordinatedplayer.Remove(zoneplayers.UUID);
                                    }
                                    else
                                    {
                                        //Der Player soll aus anderen zonen gehen.
                                        zoneplayers.BecomeCoordinatorofStandaloneGroup();
                                        Thread.Sleep(200);
                                    }
                                }
                                //hier nun den rest machen
                                foreach (var k in tocordinatedplayer)
                                {
                                    if (k != id)
                                    {
                                        //alle verbleibenden nun zum master übergeben
                                        //DevicesController.d.Players[k].BecomeCoordinatorofStandaloneGroup();
                                        //Thread.Sleep(200);
                                        GetPlayerbyRincon(k).SetAVTransportURI(SonosConstants.xrincon + master.UUID);
                                        Thread.Sleep(200);
                                    }
                                }
                            }
                            //hier nun aus der Foreach raus, weil sich diese nun geändert hat
                            break;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                AddServerErrors("SetGroups",ex);
                return false;
            }
        }
        /// <summary>
        /// Liefert die Änderung des Players zurück.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DateTime GetPlayerChange(string id)
        {
            return GetPlayerbyRincon(id).CurrentState.LastStateChange;
        }
        /// <summary>
        /// Setzt die übergebenen Parameter (Rating/PopM) inkl. Fehlerhandling, falls song gerade abgespielt wird.
        /// </summary>
        /// <param name="id">UUID Des Players</param>
        /// <param name="v">PFAD#Rating'Gelegenheit</param>
        [HttpPost]
        public Boolean SetSongMeta(string id, [FromBody]string v)
        {
            var pla = GetPlayerbyRincon(id);
            /*Übergabe v= uri#ratevalue#gelegenheit#geschwindigkeit#stimmung#aufwecken#BewertungMine
             * Wenn Rating = No, dann das Rating nicht ändern sondern nur die gelegenenheit ==>Wird in WriteMetadata erledigt
             * Wenn gelegenheit = leer diese nicht ändern.==>Wird in WriteMetadata erledigt
             */

            MP3File.MP3File lied = new MP3File.MP3File();
            try
            {

                string[] pieces = v.Split(new[] { "#" }, StringSplitOptions.None);
                string pfad = pieces[0];
                lied.Bewertung = pieces[1];
                Enums.Gelegenheit lge;
                //var s = Enum.IsDefined(typeof (Enums.Gelegenheit), pieces[2]);
                Enum.TryParse(pieces[2], false, out lge);
                if(!Enum.IsDefined(typeof(Enums.Gelegenheit), lge))
                    lge = Enums.Gelegenheit.None;
                lied.Gelegenheit = lge;
                Enums.Geschwindigkeit lg;
                Enum.TryParse(pieces[3], false, out lg);
                if (!Enum.IsDefined(typeof(Enums.Geschwindigkeit), lg))
                    lg = Enums.Geschwindigkeit.None;

                lied.Geschwindigkeit = lg;
                Enums.Stimmung ls;
                Enum.TryParse(pieces[4], false, out ls);
                if (!Enum.IsDefined(typeof(Enums.Stimmung), ls))
                    ls = Enums.Stimmung.None;
                lied.Stimmung = ls;
                lied.Aufwecken = Convert.ToBoolean(pieces[5]);
                lied.ArtistPlaylist = Convert.ToBoolean(pieces[6]);
                lied.BewertungMine = pieces[7];
                if (lied.Bewertung == "No" && lied.Gelegenheit == Enums.Gelegenheit.unset)
                {
                    return true;
                }
                lied.Pfad = URItoPath(pfad);

                //Stream Rating
                Boolean streaming = false;
                if (lied.Pfad.StartsWith("x-sonos-http:") && lied.Pfad.Contains(".mp4"))
                {
                    lied.Pfad = pfad;
                    SonosStreamRating.AddItem(lied);
                    streaming = true;
                }
                if (pla.CurrentState.CurrentTrack.Uri == pfad)
                {
                    //wenn aktueller song diesen hinterlegen
                    pla.CurrentState.CurrentTrack.MP3 = lied;
                }
                if (!streaming)
                {
                    if (!MP3ReadWrite.WriteMetaData(lied))
                    {
                        MP3ReadWrite.Add(lied);
                    }
                   }
                return true;
            }
            catch
            {
                //Kein Catch, da dies über Finally gemacht wird
                return true;
            }
            finally
            {
                //Falls mal Fehler vorhanden waren diese nun abarbeiten und hoffen, das dies geht
                if (MP3ReadWrite.listOfCurrentErrors.Count > 0)
                {
                    MP3ReadWrite.WriteNow();
                }
            }
        }
        /// <summary>
        /// Setzen des Filters für Songs aufgrund der Ratings
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpPost]
        public Boolean SetRatingFilter(string id, [FromBody] SonosRatingFilter v)
        {
            var pl = GetPlayerbyRincon(id);
            if (pl != null && v.IsValid)
            {
                if (pl.RatingFilter.CheckSonosRatingFilter(v)) return false;
                pl.RatingFilter = v;
                pl.ManuellStateChange(DateTime.Now);
            }
            return true;
        }
        /// <summary>
        /// Im Song vorspulen
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpPost]
        public Boolean Seek(string id, [FromBody]string v)
        {
            GetPlayerbyRincon(id).Seek(v);
            return true;
        }
        /// <summary>
        /// Etwas der Wiedergabeliste zufügen
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpPost]
        public Boolean Enqueue(string id, [FromBody]string v)
        {

            SonosPlayer pl = GetPlayerbyRincon(id);
            if (!pl.RatingFilter.IsDefault)
            {
                //Wenn es ein Genre ist, wird bei Klick auf ALL unabhängig der Ebene der Filter angewendet. Bei Interpreten führt das zu Fehlern.
                if (v.StartsWith(SonosConstants.aGenre))
                {
                    int count;
                    if (v.Substring(v.Length - 1) == "/")
                    {
                        //Letztes Zeichen ist ein / und es handelt sich um ALL als Auswahl
                        count = v.Count(c => c == '/');
                        if (count == 2)
                        {
                            v += "/";
                        }

                    }
                    else
                    {
                        //Kein Slash am Ende aber in der Mitte, es wird eines zugefügt, weil wir uns im Root befinden und bei Über gabe ohne würde browse zuviel ermitteln.
                        //Zwei weil es sich um ALL handelt
                        count = v.Count(c => c == '/');
                        if (count == 1)
                        {
                            v += "//";
                        }
                        if (count == 2)
                        {
                            v += "/";
                        }
                    }
                }
                if (v.StartsWith(SonosConstants.aAlbumArtist))
                {
                    if (v.Substring(v.Length - 1) != "/")
                    {
                        int count = v.Count(c => c == '/');
                        //Root von Artist
                        if (count == 1)
                        {
                            v += "/";
                        }
                    }
                }
                if (v.StartsWith(SonosConstants.xfilecifs))
                {
                    //Es soll ein bereits gefilterter Song genommen werden, daher muß hier kein Browsing gemacht werden und es ist eine URI
                    pl.Enqueue(new SonosItem { Uri = v });
                }
                else
                {
                    IList<SonosItem> k = Browsing(id, v);
                    SonosItem multi = new SonosItem();
                    int counter = 0;
                    foreach (SonosItem item in k)
                    {
                        //  DevicesController.Sonos.Players[id].Enqueue(item);
                        multi.Uri += item.Uri + " ";
                        multi.MetaData += item.MetaData + " ";
                        counter++;
                        if (counter == 10)
                        {
                            //Zwischendurch absetzen, weil Metadata auf dem Sonos Maximiert ist und diese sonst zu groß sind.
                            pl.EnqueueMulti(Convert.ToUInt16(counter), multi);
                            multi.Uri = String.Empty;
                            multi.MetaData = String.Empty;
                            counter = 0;
                        }
                    }
                    if (counter > 0)
                    {
                        pl.EnqueueMulti(Convert.ToUInt16(counter), multi);
                    }
                }
            }
            else
            {
                if (v.StartsWith(SonosConstants.FV2))
                {
                    var meta = pl.BrowsingMeta(v)[0];
                    pl.Enqueue(meta);
                    return true;
                }
                var rincpl = ContainertoURI(v, id);
                pl.Enqueue(new SonosItem { Uri = rincpl });
            }
            return true;
        }
        /// <summary>
        /// Aktuelle wiedergabeliste Speichern.
        /// </summary>
        /// <param name="id">Rincon</param>
        /// <param name="v">Name der Wiedergabeliste. Falls schon vorhanden wird diese überschrieben</param>
        [HttpPost]
        public Boolean SaveQueue(string id, [FromBody]string v)
        {
            try
            {
                SonosPlayer pla = GetPlayerbyRincon(id);
                IList<SonosItem> sonosplaylists = pla.GetSonosPlaylists();
                string sonosid = String.Empty;
                foreach (SonosItem pl in sonosplaylists)
                {
                    if (pl.Title == v)
                    {
                        sonosid = pl.ContainerID;
                    }
                }
                if (!String.IsNullOrEmpty(sonosid))
                {
                    pla.SaveQueue(v, sonosid);
                }
                else
                {
                    pla.SaveQueue(v);
                }
                return true;
            }
            catch
            {
                return false;
            }


        }
        /// <summary>
        /// Esportieren der aktuellen Wiedergabeliste
        /// </summary>
        /// <param name="id">Rincon</param>
        /// <param name="v">Titel des exports</param>
        [HttpPost]
        public Boolean ExportQueue(string id, [FromBody]string v)
        {
            try
            {
                SonosPlayer pla = GetPlayerbyRincon(id);
                //Playlist ermitteln und in Datei schreiben
                IList<SonosItem> pl = pla.GetPlaylist(0);
                var br = pla.Browsing("S:");
                string nas = br[0].Title;
                List<MP3File.MP3File> mpfiles = new List<MP3File.MP3File>();
                foreach (SonosItem song in pl)
                {
                    string pfad = SonosHelper.URItoPath(song.Uri);
                    mpfiles.Add(MP3ReadWrite.ReadMetaData(pfad));

                }
                MP3ReadWrite.WritePlaylist(mpfiles, v, nas + "\\Playlistexport");
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Aktuelle Wiedergabeliste ersetzen
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpPost]
        public Boolean ReplacePlaylist(string id, [FromBody]string v)
        {
            SonosPlayer pl = GetPlayerbyRincon(id);
            pl.RemoveAllTracksFromQueue();
            //Filter wurde gewählt
            if (!pl.RatingFilter.IsDefault && !v.StartsWith(SonosConstants.SQ) && !v.StartsWith(SonosConstants.FV2))
            {
                //Wenn es ein Genre ist, wird bei Klick auf ALL unabhängig der Ebene der Filter angewendet. Bei Interpreten führt das zu Fehlern.
                if (v.StartsWith(SonosConstants.aGenre))
                {
                    int count;
                    if (v.Substring(v.Length - 1) == "/")
                    {
                        //Letztes Zeichen ist ein / und es handelt sich um ALL als Auswahl
                        count = v.Count(c => c == '/');
                        if (count == 2)
                        {
                            v += "/";
                        }

                    }
                    else
                    {
                        //Kein Slash am Ende aber in der Mitte, es wird eines zugefügt, weil wir uns im Root befinden und bei Über gabe ohne würde browse zuviel ermitteln.
                        //Zwei weil es sich um ALL handelt
                        count = v.Count(c => c == '/');
                        if (count == 1)
                        {
                            v += "//";
                        }
                        if (count == 2)
                        {
                            v += "/";
                        }
                    }
                }
                if (v.StartsWith(SonosConstants.aAlbumArtist))
                {
                    if (v.Substring(v.Length - 1) != "/")
                    {
                        int count = v.Count(c => c == '/');
                        //Root von Artist
                        if (count == 1)
                        {
                            v += "/";
                        }
                    }
                }
                if (v.StartsWith(SonosConstants.xfilecifs))
                {
                    //Es soll ein bereits gefilterter Song genommen werden, daher muß hier kein Browsing gemacht werden und es ist eine URI
                    pl.Enqueue(new SonosItem { Uri = v }, true);
                }
                else
                {
                    IList<SonosItem> k = Browsing(id, v);
                    SonosItem multi = new SonosItem();
                    int counter = 0;
                    foreach (SonosItem item in k)
                    {
                        //  DevicesController.Sonos.Players[id].Enqueue(item);
                        multi.Uri += item.Uri + " ";
                        multi.MetaData += item.MetaData + " ";
                        counter++;
                        if (counter == 10)
                        {
                            //Zwischendurch absetzen, weil Metadata auf dem Sonos Maximiert ist und diese sonst zu groß sind.
                            pl.EnqueueMulti(Convert.ToUInt16(counter), multi);
                            multi.Uri = String.Empty;
                            multi.MetaData = String.Empty;
                            counter = 0;
                        }
                    }
                    if (counter > 0)
                    {
                        pl.EnqueueMulti(Convert.ToUInt16(counter), multi);
                    }
                }
            }
            else
            {
                if (v.StartsWith(SonosConstants.SQ))
                {
                    //Sonos Playlisten werden nie gefiltert.
                    var sonospl = pl.BrowsingMeta(v);
                    pl.Enqueue(sonospl[0], true);
                }
                //Favoriten
                else if (v.StartsWith(SonosConstants.FV2))
                {
                    var favpl = pl.BrowsingMeta(v)[0];
                    if (favpl.Uri.StartsWith(SonosConstants.xsonosapistream))
                    {
                        //RadioStream
                        favpl.Artist = favpl.Title;
                        pl.SetAVTransportURI(favpl.Uri, favpl.MetaData);
                        Thread.Sleep(100);
                        pl.SetPlay();
                        return true;
                    }
                    pl.Enqueue(favpl);
                }
                else
                {
                    //Kein Filter angesetzt und alles außer einer Sonos Playlist
                    string rinconpl = ContainertoURI(v, id);
                    pl.Enqueue(new SonosItem { Uri = rinconpl }, true);
                }
            }
            pl.SetAVTransportURI(SonosConstants.xrinconqueue + pl.UUID + "#0");
            Thread.Sleep(100);
            pl.SetPlay();
            return true;
        }
        /// <summary>
        /// Editiert einen Alarm oder setzt diesen neu.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpPost]
        public Boolean SetAlarm(string id, [FromBody]string v)
        {
            try
            {
                string[] valuesplit = v.Split('#');
                string alarmid = valuesplit[0];
                Boolean enabled = Convert.ToBoolean(valuesplit[1]);
                string starttime = valuesplit[2];
                string uuid = valuesplit[3];
                SonosPlayer pl = GetPlayerbyRincon(uuid);
                //Nicht den aktiven Player nehmen sondern den aus dem Wecker, damit der Name gesetzt werden kann.
                string days = valuesplit[4];
                ushort vol = Convert.ToUInt16(valuesplit[5]);
                string duration = valuesplit[6];
                string playlist = valuesplit[7];
                Boolean includelinkedzones = Convert.ToBoolean(valuesplit[8]);
                string random = valuesplit[9];
                string playlistname = valuesplit[10];
                random = random == "true" ? "SHUFFLE_NOREPEAT" : "NORMAL";
                if (alarmid.ToLower() == "neu")
                {
                    //Neuer Wecker
                    Alarm nal = new Alarm();
                    nal.Enabled = enabled;
                    nal.IncludeLinkedZones = includelinkedzones;
                    nal.PlayMode = random;
                    nal.Duration = duration;
                    var meta =
                        "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\"" +
                        playlist + "\" parentID=\"A:PLAYLISTS\" restricted=\"true\"><dc:title>" + playlistname +
                        "</dc:title><upnp:class>object.container.playlistContainer</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">RINCON_AssociatedZPUDN</desc></item></DIDL-Lite>";
                    //Browsing nach Playlist inkl Metadata
                    var alarmmeta = pl.BrowsingMeta(playlist);
                    nal.ProgramMetaData = meta;
                    nal.ProgramURI = alarmmeta[0].Uri;
                    nal.Recurrence = days;
                    nal.RoomName = pl.Name;
                    nal.RoomUUID = uuid;
                    nal.StartTime = starttime;
                    nal.Volume = vol;
                    pl.CreateAlarm(nal);
                }
                else
                {
                    var alarmsl = pl.Alarms;
                    foreach (var al in alarmsl)
                    {
                        if (al.ID.ToString() == alarmid)
                        {
                            al.Enabled = enabled;
                            al.Duration = duration;
                            al.IncludeLinkedZones = includelinkedzones;
                            al.PlayMode = random;
                            if (playlist.ToLower() != "unchanged")
                            {
                                var meta =
                                    "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\"" +
                                    playlist + "\" parentID=\"A:PLAYLISTS\" restricted=\"true\"><dc:title>" +
                                    playlistname +
                                    "</dc:title><upnp:class>object.container.playlistContainer</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">RINCON_AssociatedZPUDN</desc></item></DIDL-Lite>";
                                //Browsing nach Playlist inkl Metadata
                                var alarmmeta = pl.BrowsingMeta(playlist);
                                al.ProgramMetaData = meta;
                                al.ProgramURI = alarmmeta[0].Uri;
                            }
                            al.Recurrence = days;
                            al.RoomName = pl.Name;
                            al.RoomUUID = uuid;
                            al.StartTime = starttime;
                            al.Volume = vol;
                            pl.UpdateAlarm(al);
                            break;
                        }
                    }

                    }
                return true;
            }
            catch (Exception ex)
            {
                AddServerErrors("SetAlarm",ex);
                return false;
            }
        }
        /// <summary>
        /// Löscht einen Alarm
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpPost]
        public Boolean DestroyAlarm(string id, [FromBody] string v)
        {
            try
            {
                var k = GetPlayerbyRincon(id);
                Alarm al = k.Alarms.FirstOrDefault(alarm => alarm.ID.ToString() == v);//Alarm ermitteln und löschen.
                k.DestroyAlarm(al);
                return true;
            }
            catch (Exception x)
            {
                AddServerErrors("DestroyAlarms", x);
                return false;
            }
        }

        /// <summary>
        /// Song in der Wiedergabe liste setzen und abspielen
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v"></param>
        [HttpPost]
        public Boolean SetSongInPlaylist(string id, [FromBody]string v)
        {
            SonosPlayer pl = GetPlayerbyRincon(id);
            pl.SetTrackInPlaylist(v);
            Thread.Sleep(100);
            pl.SetPlay();
            return true;
        }
        #endregion Frontend POST Fertig

    }
}
