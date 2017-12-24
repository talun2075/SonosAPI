using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Http;
using ExternalDevices;
using NanoleafAurora;
using SonosAPI.Classes;
using SonosUPnP;
using SonosUPNP;

namespace SonosAPI.Controllers
{
    public class DashController : ApiController
    {
        // GET: /Dash/
        private const string retValReload = "Reload";
        private const string retValok = "ok";
        private const string defaultPlaylist = "3 Sterne Beide.m3u";
        private static IList<SonosItem> allplaylist = new List<SonosItem>();
        public void Get()
        {

        }
        /// <summary>
        /// Dash 1 Lädt je nach übergebener ID eine Playlist im Gästezimmer
        /// </summary>
        /// <param name="id">0 = Regen 1= TempSleep</param>
        [HttpGet]
        public string Dash1(string id)
        {
            if (id != "0" && id != "1") return "Wrong ID"; //abfangen von falschen werten.

            try
            {
                SonosPlayer gzmPlayer = SonosHelper.GetPlayer(SonosConstants.GästezimmerName);
                if (gzmPlayer == null) return retValReload + " kein Gästezimmer gefunden";
                //Plalist Items generieren
                SonosItem pl0;
                SonosItem pl1;
                try
                {
                    //todo: Checkplaylist so umbauen, das die Playlist als String Übergeben werden kann. Danach MErge mit Loadplaylist und in eigene Methode
                    var sonosplaylists = GetAllPlaylist();
                    pl0 = sonosplaylists.FirstOrDefault(x => x.Title.ToLower() == "zzz regen neu");
                    if (pl0 == null) throw new Exception("Kein Item für Playliste Regen gefunden");
                    pl1 = sonosplaylists.FirstOrDefault(x => x.Title.ToLower() == "zzz tempsleep");
                    if (pl1 == null) throw new Exception("Kein Item für Playliste tempsleep");

                }
                catch (Exception ex)
                {
                    return retValReload + "Ermittlung der Playlists Exception:" + ex.Message;
                }
                //hier nun den Code ausführen, der benötigt wird.
                /*
                     * Es soll für das Schlafen Regen bzw. die Tempsleep geladen werden und die Lautstärke auf 1 gesetzt werden.
                     */
                SonosHelper.WaitForTransitioning(gzmPlayer);
                if (gzmPlayer.CurrentState.TransportState == PlayerStatus.PLAYING)
                {
                    //Es wird gespielt und nochmal gedrückt, daher die Playlist wechseln.
                    if (id == "0")
                    {
                        //Prüfen, ob Regen abgespielt wird
                        var israinloaded = !CheckPlaylist(pl0.ContainerID, gzmPlayer);
                        if (israinloaded)
                        {
                            id = "1";
                        }
                    }
                    else
                    {
                        var isTempSleepLoad = !CheckPlaylist(pl1.ContainerID, gzmPlayer);
                        if (isTempSleepLoad)
                        {
                            id = "0";
                        }

                    }
                }
                if (gzmPlayer.GetVolume() != SonosConstants.GästezimmerVolume)
                {
                    gzmPlayer.SetVolume(SonosConstants.GästezimmerVolume);
                }

                switch (id)
                {

                    case "0":

                        Boolean checkplaylist = CheckPlaylist(pl0.ContainerID, gzmPlayer);
                        if (checkplaylist)
                        {
                            if (!LoadPlaylist(pl0, gzmPlayer)) return retValReload + " weil Playlist nicht geladen werden konnte";
                        }
                        break;
                    case "1":
                        Boolean checkplaylist2 = CheckPlaylist(pl1.ContainerID, gzmPlayer);
                        if (checkplaylist2)
                        {
                            if (!LoadPlaylist(pl1, gzmPlayer)) return retValReload + " weil Playlist nicht geladen werden konnte";
                        }
                        break;
                }
                gzmPlayer.SetPlay();
                return retValok;
            }
            catch (Exception ex)
            {
                return retValReload + " Exception:" + ex.Message;
            }
        }

        /// <summary>
        /// Dash2 Soll alle Player stoppen und falls keiner Spielt im Esszimmer und Wohnzimmer Starten starten.
        /// </summary>
        /// <param name="id">Spätere Nutzung Vorbereitet</param>

        [HttpGet]
        public string Dash2(int id)
        {
            //Durch alle Zonen gehen und Gruppen auflösen und Pausieren, falls einer abspielt. 

            #region STOPP

            Boolean foundplayed = false;
            List<string> foundedPlayer = new List<string>();
            try
            {
                try
                {
                    if (SonosHelper.Sonos == null)
                    {
                        SonosHelper.Initialisierung();
                    }
                    if (SonosHelper.Sonos == null)
                    {
                        return retValReload + " Sonos ist null und konnte nicht initialisiert werden!";
                    }
                    lock (SonosHelper.Sonos.Players)
                    {
                        foreach (SonosPlayer sp in SonosHelper.Sonos.Players)
                        {
                            try
                            {
                                if (sp.CurrentState.TransportState == PlayerStatus.PLAYING)
                                {
                                    foundedPlayer.Add(sp.Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                return retValReload + "Block1.1 Exception: Beim prüfen ob ausgeschaltet werden muss:" +
                                       ex.Message;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return retValReload + "Block1 Exception: Beim prüfen ob ausgeschaltet werden muss:" + ex.Message;
                }
                if (foundedPlayer.Any())
                {

                    foreach (string playername in foundedPlayer)
                    {
                        var pl = SonosHelper.GetPlayer(playername);
                        if (pl == null)
                            return retValReload + " Es konnte der Player " + playername +
                                   " nicht als Objekt ermittelt werden.";

                        var wasZoneCord = SonosHelper.CheckIsZoneCord(pl);
                        SonosHelper.WaitForTransitioning(pl);
                        if (wasZoneCord)
                        {
                            foundplayed = true;
                            pl.SetPause();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                return retValReload + " Exception: Beim prüfen ob ausgeschaltet werden muss:" + ex.Message;
            }

            if (foundplayed)
            {
                try
                {
                    //Daten vom Marantz ermitteln
                    Marantz.Initialisieren(SonosConstants.MarantzUrl);
                    //Ist auf Sonos?
                    if (Marantz.SelectedInput == MarantzInputs.Sonos && Marantz.PowerOn)
                    {
                        //Marantz ausschalten.
                        Marantz.PowerOn = false;
                    }
                }
                catch
                {
                    return retValReload + " Exception: Marantz konnte nicht geschaltet werden. ";
                }
                try
                {
                    if (AuroraWrapper.AurorasList.Count > 0)
                    {
                        foreach (Aurora aurora in AuroraWrapper.AurorasList)
                        {
                            if (aurora.PowerOn)
                                aurora.PowerOn = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return retValReload + "exception: Aurora konnten nicht geschaltet werden. " + ex.Message;
                }
                return "ok, Musik wurde ausgeschaltet.";
            }

            #endregion STOPP
            //Aurora einschalten zwischen 18 Uhr und 5 Uhr oder immer Oktober bsi März
            if (DateTime.Now.Hour > 17 || DateTime.Now.Hour < 6 || DateTime.Now.Month > 9 || DateTime.Now.Month < 4)
            {
                if (AuroraWrapper.AurorasList.Count > 0)
                {
                    foreach (Aurora aurora in AuroraWrapper.AurorasList)
                    {
                        if (!aurora.PowerOn)
                        {
                            aurora.SetRandomScenario();
                            aurora.Brightness = 50;
                        }
                    }
                }

            }

            SonosPlayer esszimmer = SonosHelper.GetPlayer(SonosConstants.EsszimmerName);
            SonosPlayer wohnzimmer = SonosHelper.GetPlayer(SonosConstants.WohnzimmerName);
            int oldcurrenttrack;

            //Alle Player alleine machen und neu zuordnen
            try
            {
                oldcurrenttrack = esszimmer.GetAktSongInfo().TrackIndex;
                esszimmer.BecomeCoordinatorofStandaloneGroup();
                Thread.Sleep(200);
                wohnzimmer.SetAVTransportURI(SonosConstants.xrincon + esszimmer.UUID);
                Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                return retValReload + " Exception beim Gruppenauflösen: " + ex.Message;
            }
            try
            {
                if (wohnzimmer.GetVolume() != SonosConstants.WohnzimmerVolume)
                {
                    wohnzimmer.SetVolume(SonosConstants.WohnzimmerVolume);
                }
                if (esszimmer.GetVolume() != SonosConstants.EsszimmerVolume)
                {
                    esszimmer.SetVolume(SonosConstants.EsszimmerVolume);
                }
            }
            catch (Exception ex)
            {
                return retValReload + " Exception beim Lautstärke setzen: " + ex.Message;
            }
            try
            {
                //Marantz Verarbeiten.
                Marantz.Initialisieren(SonosConstants.MarantzUrl);
                if (Marantz.SelectedInput != MarantzInputs.Sonos)
                {
                    Marantz.SelectedInput = MarantzInputs.Sonos;
                }
                if (!Marantz.PowerOn)
                {
                    Marantz.PowerOn = true;
                }
                if (Marantz.Volume != "-30.0")
                {
                    Marantz.Volume = "-30.0";
                }
            }
            catch (Exception ex)
            {
                return retValReload + " Exception beim Marantz: " + ex.Message;
            }
            Thread.Sleep(300);
            try
            {
                //Playlist verarveiten
                var playlists = GetAllPlaylist();
                var playlist = playlists.FirstOrDefault(x => x.Title.ToLower() == defaultPlaylist.ToLower());
                Boolean loadPlaylist = false;
                if (playlist != null)
                {
                    loadPlaylist = CheckPlaylist(playlist.ContainerID, esszimmer);
                }
                if (loadPlaylist)
                {
                    if (!LoadPlaylist(playlist, esszimmer))
                        return "reload, weil Playlist nicht geladen werden konnte";
                }
                else
                {
                    //alten Song aus der Playlist laden, da immer wieder auf 1 reset passiert.
                    esszimmer.SetTrackInPlaylist(oldcurrenttrack.ToString());
                    Thread.Sleep(100);
                }

                esszimmer.SetPlay();
            }
            catch (Exception ex)
            {
                return retValReload + " Exception beim Laden der Playlist und Starten der Wiedergabe: " + ex.Message;
            }
            return retValok;
        }
        /// <summary>
        /// Kinderzimmer verarbeiten
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public string Dash3(int id)
        {
            /*
             * Ankleidezimmer mit eigener Gruppe und nicht im Esszimmer.
             */
            try
            {
                return MakePlayerFine(SonosConstants.AnkleidezimmerName, SonosConstants.AnkleidezimmerVolume);
            }
            catch (Exception exceptio)
            {
                SonosHelper.ServerErrorsAdd("Dash3", exceptio);
                return retValReload + " Exception: " + exceptio.Message;
            }
        }
        /// <summary>
        /// Küche verarbeiten
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public string Dash4(int id)
        {
            /*
             * Küche mit in die Gruppe Esszimmer nehmen, wenn abgespielt wird.
             * Wenn nicht, dann eigene Playlist und single Player
             */
            try
            {
                return MakePlayerFine(SonosConstants.KücheName, SonosConstants.KücheVolume);
            }
            catch (Exception exceptio)
            {
                SonosHelper.ServerErrorsAdd("Dash4", exceptio);
                return retValReload + " Exception: " + exceptio.Message;
            }
        }
        /// <summary>
        /// Mine Spezial
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public string Dash5(string id)
        {
            const string rsh = "x-sonosapi-stream:s18353?sid=254&flags=8224&sn=0";
            try
            {

                SonosPlayer essPlayer = SonosHelper.GetPlayer(SonosConstants.EsszimmerName);
                SonosPlayer kuPlayer = SonosHelper.GetPlayer(SonosConstants.KücheName);
                SonosPlayer wzPlayer = SonosHelper.GetPlayer(SonosConstants.WohnzimmerName);
                var aktUri = essPlayer.GetMediaInfoURIMeta()[0];
                //scheint schon dash5 gedrückt worden zu sein.
                if (aktUri == rsh)
                {
                    if (essPlayer.CurrentState.TransportState == PlayerStatus.PLAYING)
                    {
                        //ausschalten
                        essPlayer.SetPause();
                        //Daten vom Marantz ermitteln
                        if (Marantz.Initialisieren(SonosConstants.MarantzUrl))
                        {
                            if (Marantz.SelectedInput == MarantzInputs.Sonos && Marantz.PowerOn)
                            {
                                Marantz.PowerOn = false;
                            }
                        }
                        return retValok + " RSH ausgeschaltet.";
                    }

                    //Daten vom Marantz ermitteln
                    if (Marantz.Initialisieren(SonosConstants.MarantzUrl))
                    {
                        if (Marantz.SelectedInput != MarantzInputs.Sonos || !Marantz.PowerOn)
                        {
                            if (Marantz.PowerOn)
                            {
                                Marantz.SelectedInput = MarantzInputs.Sonos;
                            }
                            else
                            {
                                Marantz.PowerOn = true;
                            }
                            if (Marantz.SelectedInput != MarantzInputs.Sonos)
                                Marantz.SelectedInput = MarantzInputs.Sonos;
                        }
                    }
                    essPlayer.SetPlay();
                    return retValok + " RSH eingeschaltet.";

                }
                try
                {
                    //ab hier alles neu
                    var essZone = SonosHelper.GetZone(SonosConstants.EsszimmerName);
                    if (essZone != null && essZone.Players.Count == 2 && essZone.Players.Contains(wzPlayer) && essZone.Players.Contains(kuPlayer))
                    {
                        
                    }
                    else
                    {
                        if (essZone == null)
                        {
                            essPlayer.BecomeCoordinatorofStandaloneGroup();
                            Thread.Sleep(200);
                            kuPlayer.SetAVTransportURI(SonosConstants.xrincon + essPlayer.UUID);
                            Thread.Sleep(300);
                            wzPlayer.SetAVTransportURI(SonosConstants.xrincon + essPlayer.UUID);
                            Thread.Sleep(300);
                            if (kuPlayer.GetVolume() != SonosConstants.KücheVolume)
                            {
                                kuPlayer.SetVolume(SonosConstants.KücheVolume);
                            }
                            if (wzPlayer.GetVolume() != SonosConstants.WohnzimmerVolume)
                            {
                                wzPlayer.SetVolume(SonosConstants.WohnzimmerVolume);
                            }

                        }
                        else
                        {
                            if (!essZone.Players.Contains(kuPlayer))
                            {
                                kuPlayer.SetAVTransportURI(SonosConstants.xrincon + essPlayer.UUID);
                                Thread.Sleep(300);
                                if (kuPlayer.GetVolume() != SonosConstants.KücheVolume)
                                {
                                    kuPlayer.SetVolume(SonosConstants.KücheVolume);
                                }
                            }
                            if (!essZone.Players.Contains(wzPlayer))
                            {
                                wzPlayer.SetAVTransportURI(SonosConstants.xrincon + essPlayer.UUID);
                                Thread.Sleep(300);
                                if (wzPlayer.GetVolume() != SonosConstants.WohnzimmerVolume)
                                {
                                    wzPlayer.SetVolume(SonosConstants.WohnzimmerVolume);
                                }
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    return "Dash5:Block1:" + ex.Message;
                }
                try
                {
                    //Daten vom Marantz ermitteln
                    if (Marantz.Initialisieren(SonosConstants.MarantzUrl))
                    {
                        if (Marantz.SelectedInput != MarantzInputs.Sonos || !Marantz.PowerOn)
                        {
                            if (Marantz.PowerOn)
                            {
                                Marantz.SelectedInput = MarantzInputs.Sonos;
                            }
                            else
                            {
                                Marantz.PowerOn = true;
                            }
                            if (Marantz.SelectedInput != MarantzInputs.Sonos)
                                Marantz.SelectedInput = MarantzInputs.Sonos;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return "Dash5:Block2:" + ex.Message;
                }

                if (essPlayer.GetVolume() != SonosConstants.EsszimmerVolume)
                {
                    essPlayer.SetVolume(SonosConstants.EsszimmerVolume);
                }
                if (aktUri != rsh)
                {
                    essPlayer.SetAVTransportURI(rsh, "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\"F00092020s18353\" parentID=\"F00082064y1%3apopular\" restricted=\"true\"><dc:title>R.SH</dc:title><upnp:class>object.item.audioItem.audioBroadcast</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">SA_RINCON65031_</desc></item></DIDL-Lite>");
                    Thread.Sleep(300);
                }
                essPlayer.SetPlay();
                return retValok + " eingeschaltet und RSH gestartet";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Prüft die übergebene Playlist mit dem Übergeben Player ob neu geladen werden muss.
        /// </summary>
        /// <param name="pl">Playliste, die geladen werden soll.</param>
        /// <param name="sp">Coordinator aus der Führenden Zone</param>
        /// <returns>True muss neu geladen werden</returns>
        private Boolean CheckPlaylist(string pl, SonosPlayer sp)
        {
            try
            {
                Boolean retval = false;
                var evtlStream = sp.GetAktSongInfo();
                if (SonosItemHelper.CheckItemForStreamingUriCheck(evtlStream.TrackURI))
                    return true;
                var actpl = sp.GetPlaylist(0, 10);
                if (actpl.Count == 0) return true;
                var toLoadpl = sp.BrowsingWithLimitResults(pl, 10);
                if (toLoadpl.Count == 0) return true;//eigentlich ein Fehler
                for (int i = 0; i < actpl.Count; i++)
                {
                    if (actpl[i].Title == toLoadpl[i].Title) continue;
                    retval = true;
                    break;
                }
                return retval;
            }
            catch (Exception ex)
            {
                SonosHelper.ServerErrorsAdd("Dash2:CheckPlaylist", ex);
                return true;
            }
        }
        /// <summary>
        /// Läd die übergebene Playlist
        /// </summary>
        /// <param name="pl"></param>
        /// <param name="sp"></param>
        /// <returns></returns>
        private Boolean LoadPlaylist(SonosItem pl, SonosPlayer sp)
        {
            //laden der übergebenen Playlist
            StringBuilder stringb = new StringBuilder();
            try
            {
                stringb.AppendLine(sp.Name);
                //stringb.AppendLine("Suchen nach Playlist" + pl);
                //    var playlists = GetAllPlaylist();
                //var playlist = playlists.FirstOrDefault(x => x.Title.ToLower() == pl.ToLower());
                //if(playlist == null) throw new NullReferenceException("Playlist nicht gefunden");
                //stringb.AppendLine("Playlist gefunden" + playlist.Title);


                stringb.AppendLine("Löschen aller Tracks von " + sp.Name);
                sp.RemoveAllTracksFromQueue();
                Thread.Sleep(300);
                sp.Enqueue(pl, true);
                Thread.Sleep(200);
                stringb.AppendLine("Playlist wurde ersetzt.");
                sp.SetAVTransportURI(SonosConstants.xrinconqueue + sp.UUID + "#0");
                stringb.AppendLine("SetAVTransportURI wurde ersetzt.");
                Thread.Sleep(500);
                return true;
            }
            catch
            {
                SonosHelper.TraceLog("Loadplaylist.log", stringb.ToString());
                return false;
            }
        }
        /// <summary>
        /// Füllt eine Liste mit allen Playlisten
        /// </summary>
        /// <returns></returns>
        private IList<SonosItem> GetAllPlaylist()
        {
            if (allplaylist.Count == 0)
            {
                if (SonosHelper.Sonos != null && SonosHelper.Sonos.Players != null &&
                    SonosHelper.Sonos.Players.Count != 0)
                {
                    allplaylist = SonosHelper.Sonos.Players[0].GetallPlaylist();
                }
            }

            return allplaylist;
        }
        /// <summary>
        /// Starte und Stoppt übergebenen Player. 
        /// Fügt diesen der primär Gruppe hinzu, wenn diese Spielt
        /// </summary>
        /// <param name="_player">Name des Players</param>
        /// <param name="_volume">Läutstärke des Players</param>
        /// <param name="addToEsszimmer">Soll der Player zum Esszimmer zugefügt werden, falls dieser Abspielt.</param>
        /// <param name="_Playlist">Wiedergabeliste. Wenn keine Angegeben wird, dann wird die default genommen.</param>
        /// <returns>Ok oder ein Fehler</returns>
        private String MakePlayerFine(string _player, ushort _volume, Boolean addToEsszimmer = true, string _Playlist = defaultPlaylist)
        {
            /*
             * Übergebener Player soll der Primären (esszimmer und Wohnzimmer) zugefügt werden, wenn diese Spielen.
             * Wenn nicht, dann eigene Playlist und single Player
             * Wenn der Player schon Musik macht, dann aus Gruppe nehmen oder Pausieren
             */
            SonosPlayer player;
            SonosPlayer esszimmer;
            try
            {
                player = SonosHelper.GetPlayer(_player);
                if (player == null) return retValReload + _player + " konnte nicht gefunden werden.";
                esszimmer = SonosHelper.GetPlayer(SonosConstants.EsszimmerName);
                if (esszimmer == null) return retValReload + " Esszimmer konnte nicht gefunden werden.";
            }
            catch (Exception exceptio)
            {
                return retValReload + " MakePlayerFine:Exception:Block0: " + exceptio.Message;
            }
            try
            {
                if (!SonosHelper.CheckIsZoneCord(player))
                {
                    return retValok + " Player ausgeschaltet";
                }
            }
            catch (Exception exceptio)
            {
                return retValReload + " MakePlayerFine:Exception:Block1: " + exceptio.Message;
            }
            try
            {
                //Prüfen, ob er abspielt
                SonosHelper.WaitForTransitioning(player);
                if (player.CurrentState.TransportState == PlayerStatus.PLAYING)
                {
                    player.SetPause();
                    return retValok + " ist ausgeschaltet";
                }
            }
            catch (Exception exceptio)
            {
                return retValReload + " MakePlayerFine:Exception:Block2: " + exceptio.Message;
            }
            try
            {
                if (player.GetVolume() != _volume)
                {
                    player.SetVolume(_volume);
                }
                //Prüfen, ob Esszimmer spielt
                SonosHelper.WaitForTransitioning(esszimmer);
                if (esszimmer.CurrentState.TransportState == PlayerStatus.PLAYING && addToEsszimmer)
                {
                    player.SetAVTransportURI(SonosConstants.xrincon + esszimmer.UUID);
                    return retValok + " Player zum Esszimmer zugefügt.";
                }
                var playlist = GetAllPlaylist().FirstOrDefault(x => x.Title.ToLower() == _Playlist.ToLower());
                //Soll selber etwas abspielen.
                if (playlist == null) return "Playlist konnte nicht geladen werden:" + _Playlist;
                Boolean loadpl = CheckPlaylist(playlist.ContainerID, player);
                if (loadpl)
                {
                    LoadPlaylist(playlist, player);
                }
                player.SetPlay();
                return retValok + " Player spielt alleine";
            }
            catch (Exception exceptio)
            {
                return retValReload + " MakePlayerFine:Exception:Block3: " + exceptio.Message;
            }
        }
    }
}
