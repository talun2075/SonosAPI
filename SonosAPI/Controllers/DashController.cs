using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using ExternalDevices;
using SonosAPI.Classes;
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
        private static ushort primaryplayerVolume = SonosConstants.WohnzimmerVolume;
        private const string primaryPlayerName = SonosConstants.WohnzimmerName;
        public void Get()
        {
            DashHelper.PowerOnAruroras();
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
                        var israinloaded = !DashHelper.CheckPlaylist(pl0.ContainerID, gzmPlayer);
                        if (israinloaded)
                        {
                            id = "1";
                        }
                    }
                    else
                    {
                        var isTempSleepLoad = !DashHelper.CheckPlaylist(pl1.ContainerID, gzmPlayer);
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

                        Boolean checkplaylist = DashHelper.CheckPlaylist(pl0.ContainerID, gzmPlayer);
                        if (checkplaylist)
                        {
                            if (!DashHelper.LoadPlaylist(pl0, gzmPlayer)) return retValReload + " weil Playlist nicht geladen werden konnte";
                        }
                        break;
                    case "1":
                        Boolean checkplaylist2 = DashHelper.CheckPlaylist(pl1.ContainerID, gzmPlayer);
                        if (checkplaylist2)
                        {
                            if (!DashHelper.LoadPlaylist(pl1, gzmPlayer)) return retValReload + " weil Playlist nicht geladen werden konnte";
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
            //List<string> foundedPlayer = new List<string>();
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
                    lock (SonosHelper.Sonos.Zones)
                    {
                        foreach (SonosZone sp in SonosHelper.Sonos.Zones)
                        {
                            try
                            {
                                if (sp.Coordinator.CurrentState.TransportState == PlayerStatus.PLAYING)
                                {
                                    foundplayed = true;
                                    sp.Coordinator.SetPause();
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
 }
            catch (Exception ex)
            {
                return retValReload + " Exception: Beim prüfen ob ausgeschaltet werden muss:" + ex.Message;
            }

            if (foundplayed)
            {
                try
                {
                    Task.Factory.StartNew(DashHelper.PowerOffMarantz);
                }
                catch
                {
                    return retValReload + " Exception: Marantz konnte nicht geschaltet werden. ";
                }
                try
                {
                   Task.Factory.StartNew(DashHelper.PowerOffAruroras);
                }
                catch (Exception ex)
                {
                    return retValReload + "exception: Aurora konnten nicht ausgeschaltet werden. " + ex.Message;
                }
                return "ok, Musik wurde ausgeschaltet.";
            }

            #endregion STOPP
            #region Start Devices
            try
            {
                //Aurora einschalten zwischen 18 Uhr und 5 Uhr oder immer Oktober bsi März
                //if (DateTime.Now.Hour > 17 || DateTime.Now.Hour < 6 || DateTime.Now.Month > 9 || DateTime.Now.Month < 4)
                //{
                    Task.Factory.StartNew(DashHelper.PowerOnAruroras);
                //}
            }
            catch (Exception ex)
            {
                return retValReload + "exception: Aurora konnten nicht eingeschaltet werden. " + ex.Message;
            }
            try
            {
                //Marantz Verarbeiten.
                Task.Factory.StartNew(DashHelper.PowerOnMarantz);
            }
            catch (Exception ex)
            {
                return retValReload + " Exception beim Marantz: " + ex.Message;
            }
            #endregion Start Devices
            try
            {
                
                //Alles ins Wohnzimmer legen.
                SonosPlayer primaryplayer = SonosHelper.GetPlayer(primaryPlayerName);
                SonosPlayer secondaryplayer = SonosHelper.GetPlayer(SonosConstants.EsszimmerName);
                SonosPlayer thirdplayer = SonosHelper.GetPlayer(SonosConstants.KücheName);
                if (DashHelper.IsSonosTargetGroupExist(primaryplayer, new List<SonosPlayer> {secondaryplayer,thirdplayer}))
                {
                    //Die Zielarchitektur existiert, daher keine Lautstärkesondern nur Playlist
                    int oldcurrenttrack = primaryplayer.GetAktSongInfo().TrackIndex;
                    var playlists = GetAllPlaylist();
                    var playlist = playlists.FirstOrDefault(x => x.Title.ToLower() == defaultPlaylist.ToLower());
                    Boolean loadPlaylist = false;
                    if (playlist != null)
                    {
                        loadPlaylist = DashHelper.CheckPlaylist(playlist.ContainerID, primaryplayer);
                    }
                    if (loadPlaylist)
                    {
                        if (!DashHelper.LoadPlaylist(playlist, primaryplayer))
                            return "reload, weil Playlist nicht geladen werden konnte";
                    }
                    else
                    {
                        //alten Song aus der Playlist laden, da immer wieder auf 1 reset passiert.
                        primaryplayer.SetTrackInPlaylist(oldcurrenttrack.ToString());
                        Thread.Sleep(100);
                    }
                }
                else
                {
                    //alles neu
                    try
                    {
                        primaryplayer.BecomeCoordinatorofStandaloneGroup();
                        Thread.Sleep(500);
                        secondaryplayer.SetAVTransportURI(SonosConstants.xrincon + primaryplayer.UUID);
                        Thread.Sleep(300);
                        thirdplayer.SetAVTransportURI(SonosConstants.xrincon + primaryplayer.UUID);
                        Thread.Sleep(300);
                    }
                    catch (Exception ex)
                    {
                        return retValReload + " Exception beim Gruppenauflösen: " + ex.Message;
                    }
                    try
                    {
                        ushort secondaryplayerVolume = SonosConstants.EsszimmerVolume;
                        ushort thirdplayerVolume = SonosConstants.KücheVolume;
                        if (secondaryplayer.GetVolume() != secondaryplayerVolume)
                        {
                            secondaryplayer.SetVolume(secondaryplayerVolume);
                        }
                        if (primaryplayer.GetVolume() != primaryplayerVolume)
                        {
                            primaryplayer.SetVolume(primaryplayerVolume);
                        }
                        if (thirdplayer.GetVolume() != thirdplayerVolume)
                        {
                            thirdplayer.SetVolume(thirdplayerVolume);
                        }
                    }
                    catch (Exception ex)
                    {
                        return retValReload + " Exception beim Lautstärke setzen: " + ex.Message;
                    }
                    try
                    {
                        //Playlist verarveiten
                        var playlists = GetAllPlaylist();
                        var playlist = playlists.FirstOrDefault(x => x.Title.ToLower() == defaultPlaylist.ToLower());
                        if (!DashHelper.LoadPlaylist(playlist, primaryplayer))
                            return "reload, weil Playlist nicht geladen werden konnte";
                    }
                    catch (Exception ex)
                    {
                        return retValReload + " Exception beim Playlist setzen: " + ex.Message;
                    }
                }
                try
                {
                    primaryplayer.SetPlay();
                }
                catch (Exception ex)
                {
                    return retValReload + " Exception beim Starten der Wiedergabe: " + ex.Message;
                }
            }
            catch (Exception ex)
            {
                return retValReload + "exception: Großer Block nicht abgefangen: " + ex.Message;
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

                SonosPlayer primaryplayer = SonosHelper.GetPlayer(primaryPlayerName);
                SonosPlayer secondaryplayer = SonosHelper.GetPlayer(SonosConstants.KücheName);
                ushort secondaryPlayerVolume = SonosConstants.KücheVolume;
                SonosPlayer thirdplayer = SonosHelper.GetPlayer(SonosConstants.EsszimmerName);
                ushort thirdPlayerVolume = SonosConstants.EsszimmerVolume;
                var aktUri = primaryplayer.GetMediaInfoURIMeta()[0];
                //scheint schon dash5 gedrückt worden zu sein.
                if (aktUri == rsh)
                {
                    if (primaryplayer.CurrentState.TransportState == PlayerStatus.PLAYING)
                    {
                        //ausschalten
                        primaryplayer.SetPause();
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
                    primaryplayer.SetPlay();
                    return retValok + " RSH eingeschaltet.";

                }
                try
                {
                    //ab hier alles neu
                    var primaryZone = SonosHelper.GetZone(primaryPlayerName);
                    if (primaryZone != null && primaryZone.Players.Count == 2 && primaryZone.Players.Contains(thirdplayer) && primaryZone.Players.Contains(secondaryplayer))
                    {

                    }
                    else
                    {
                        if (primaryZone == null)
                        {
                            primaryplayer.BecomeCoordinatorofStandaloneGroup();
                            Thread.Sleep(200);
                            secondaryplayer.SetAVTransportURI(SonosConstants.xrincon + primaryplayer.UUID);
                            Thread.Sleep(300);
                            thirdplayer.SetAVTransportURI(SonosConstants.xrincon + primaryplayer.UUID);
                            Thread.Sleep(300);
                            if (secondaryplayer.GetVolume() != secondaryPlayerVolume)
                            {
                                secondaryplayer.SetVolume(secondaryPlayerVolume);
                            }
                            if (thirdplayer.GetVolume() != thirdPlayerVolume)
                            {
                                thirdplayer.SetVolume(thirdPlayerVolume);
                            }

                        }
                        else
                        {
                            if (!primaryZone.Players.Contains(secondaryplayer))
                            {
                                secondaryplayer.SetAVTransportURI(SonosConstants.xrincon + primaryplayer.UUID);
                                Thread.Sleep(300);
                                if (secondaryplayer.GetVolume() != secondaryPlayerVolume)
                                {
                                    secondaryplayer.SetVolume(secondaryPlayerVolume);
                                }
                            }
                            if (!primaryZone.Players.Contains(thirdplayer))
                            {
                                thirdplayer.SetAVTransportURI(SonosConstants.xrincon + primaryplayer.UUID);
                                Thread.Sleep(300);
                                if (thirdplayer.GetVolume() != thirdPlayerVolume)
                                {
                                    thirdplayer.SetVolume(thirdPlayerVolume);
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

                if (primaryplayer.GetVolume() != primaryplayerVolume)
                {
                    primaryplayer.SetVolume(primaryplayerVolume);
                }
                if (aktUri != rsh)
                {
                    primaryplayer.SetAVTransportURI(rsh, "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\"F00092020s18353\" parentID=\"F00082064y1%3apopular\" restricted=\"true\"><dc:title>R.SH</dc:title><upnp:class>object.item.audioItem.audioBroadcast</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">SA_RINCON65031_</desc></item></DIDL-Lite>");
                    Thread.Sleep(300);
                }
                primaryplayer.SetPlay();
                return retValok + " eingeschaltet und RSH gestartet";
            }
            catch (Exception ex)
            {
                return ex.Message;
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
        /// <param name="addToPrimary">Soll der Player zum Primären (Aktuell Wohnzimmer) zugefügt werden, falls dieser Abspielt.</param>
        /// <param name="_Playlist">Wiedergabeliste. Wenn keine Angegeben wird, dann wird die default genommen.</param>
        /// <returns>Ok oder ein Fehler</returns>
        private String MakePlayerFine(string _player, ushort _volume, Boolean addToPrimary = true, string _Playlist = defaultPlaylist)
        {
            /*
             * Übergebener Player soll der Primären (esszimmer und Wohnzimmer) zugefügt werden, wenn diese Spielen.
             * Wenn nicht, dann eigene Playlist und single Player
             * Wenn der Player schon Musik macht, dann aus Gruppe nehmen oder Pausieren
             */
            SonosPlayer player;
            SonosZone primaryPlayer;
            try
            {
                player = SonosHelper.GetPlayer(_player);
                if (player == null) return retValReload + _player + " konnte nicht gefunden werden.";
                primaryPlayer = SonosHelper.GetZone(SonosConstants.WohnzimmerName);
                if (primaryPlayer == null) return retValReload + " Primärzone konnte nicht gefunden werden.";
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
                SonosHelper.WaitForTransitioning(primaryPlayer.Coordinator);
                if (primaryPlayer.Coordinator.CurrentState.TransportState == PlayerStatus.PLAYING && addToPrimary)
                {
                    player.SetAVTransportURI(SonosConstants.xrincon + primaryPlayer.CoordinatorUUID);
                    return retValok + " Player zum Esszimmer zugefügt.";
                }
                var playlist = GetAllPlaylist().FirstOrDefault(x => String.Equals(x.Title, _Playlist, StringComparison.CurrentCultureIgnoreCase));
                //Soll selber etwas abspielen.
                if (playlist == null) return "Playlist konnte nicht geladen werden:" + _Playlist;
                Boolean loadpl = DashHelper.CheckPlaylist(playlist.ContainerID, player);
                if (loadpl)
                {
                    DashHelper.LoadPlaylist(playlist, player);
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
