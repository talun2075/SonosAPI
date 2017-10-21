using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Http;
using ExternalDevices;
using NanoleafAurora;
using SonosAPI.Classes;
using SonosUPNP;

namespace SonosAPI.Controllers
{
    public class DashController : ApiController
    {
        // GET: /Dash/
        private const string retValReload = "Reload";
        private const string retValok = "ok";
        private const string kzPlaylist = SonosConstants.SQ+"77";
        private const string defaultPlaylist = "S://NAS/MUSIK/Playlists/3%20Sterne%20Beide.m3u";

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
            string playlistToLoad = SonosConstants.SQ + "72";
            if (id != "0" && id != "1") return "Wrong ID"; //abfangen von falschen werten.

            try
            {
                SonosPlayer gzmPlayer = SonosHelper.GetPlayer(SonosConstants.GästezimmerName);
                if (gzmPlayer != null)
                {
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
                            var israinloaded = !CheckPlaylist(playlistToLoad, gzmPlayer);
                            if (israinloaded)
                            {
                                id = "1";
                            }
                        }
                        else
                        {
                            var isTempSleepLoad = !CheckPlaylist(SonosConstants.SQ + "74", gzmPlayer);
                            if (isTempSleepLoad)
                            {
                                id = "0";
                            }

                        }
                    }
                    if (gzmPlayer.GetVolume() != SonosConstants.GästezimmerVolume)
                    {
                        gzmPlayer.SetVolume(SonosConstants.GästezimmerVolume);
                        SonosHelper.MessageQueue(new SonosCheckChangesObject {Changed = SonosCheckChangesConstants.Volume,PlayerName = gzmPlayer.Name, Value = SonosConstants.GästezimmerVolume.ToString()});
                    }
                    switch (id)
                    {
                        case "0":
                            Boolean checkplaylist = CheckPlaylist(playlistToLoad, gzmPlayer);
                            if (checkplaylist)
                            {
                                if (!LoadPlaylist(playlistToLoad, gzmPlayer)) return retValReload + " weil Playlist nicht geladen werden konnte";
                            }
                            break;
                        case "1":
                            playlistToLoad = SonosConstants.SQ+"74";
                            Boolean checkplaylist2 = CheckPlaylist(playlistToLoad, gzmPlayer);
                            if (checkplaylist2)
                            {
                                if (!LoadPlaylist(playlistToLoad, gzmPlayer)) return retValReload+" weil Playlist nicht geladen werden konnte";
                            }
                            break;
                    }
                    gzmPlayer.SetPlay();
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Playing, PlayerName = gzmPlayer.Name, Value = "true" });
                    return retValok;
                }
                return retValReload+" kein Gästezimmer gefunden";

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
                                return retValReload + "Block1.1 Exception: Beim prüfen ob ausgeschaltet werden muss:" + ex.Message;
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
                            return retValReload + " Es konnte der Player " + playername + " nicht als Objekt ermittelt werden.";

                        var wasZoneCord = SonosHelper.CheckIsZoneCord(pl);
                        SonosHelper.WaitForTransitioning(pl);
                        if (wasZoneCord)
                        {
                            foundplayed = true;
                            pl.SetPause();
                            SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Playing, PlayerName = pl.Name, Value = "false" });
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                return retValReload + " Exception: Beim prüfen ob ausgeschaltet werden muss:" +ex.Message;
            }
            try
            {
                if (foundplayed)
                {
                    //Daten vom Marantz ermitteln
                    Marantz.Initialisieren(SonosConstants.MarantzUrl);
                    //Ist auf Sonos?
                    if (Marantz.SelectedInput == MarantzInputs.Sonos && Marantz.PowerOn)
                    {
                        //Marantz ausschalten.
                        Marantz.PowerOn = false;
                    }
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.MarantzPower, PlayerName = SonosConstants.EsszimmerName, Value = "off" });
                    
                    if (AuroraWrapper.AurorasList.Count > 0)
                    {
                        foreach (Aurora aurora in AuroraWrapper.AurorasList)
                        {
                            if (aurora.PowerOn)
                                aurora.PowerOn = false;
                        }    
                    }
                    return "ok, Musik wurde ausgeschaltet.";
                }
            }
            catch
            {
                return retValReload + " Exception: Marantz konnte nicht geschaltet werden.";
            }
            #endregion STOPP
            //Aurora einschalten zwischen 18 Uhr und 5 Uhr.
            if (DateTime.Now.Hour > 17 || DateTime.Now.Hour < 6)
            {
                if (AuroraWrapper.AurorasList.Count > 0)
                {
                    foreach (Aurora aurora in AuroraWrapper.AurorasList)
                    {
                        //todo: Prüfen
                        aurora.SetRandomScenario();
                        aurora.Brightness = 50;
                    }
                }

            }

            SonosPlayer esszimmer = SonosHelper.GetPlayer(SonosConstants.EsszimmerName);
            SonosPlayer wohnzimmer = SonosHelper.GetPlayer(SonosConstants.WohnzimmerName);
            int oldcurrenttrack;

            //Alle Player alleine machen und neu zuordnen
            try
            {
                SonosHelper.CheckIsZoneCord(wohnzimmer);
                oldcurrenttrack = esszimmer.GetAktSongInfo().TrackIndex;
                esszimmer.BecomeCoordinatorofStandaloneGroup();
                Thread.Sleep(400);
                wohnzimmer.SetAVTransportURI(SonosConstants.xrincon + esszimmer.UUID);
                Thread.Sleep(200);
                SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.AddToZone, PlayerName = wohnzimmer.Name, Value = SonosConstants.EsszimmerName });
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
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Volume, PlayerName = wohnzimmer.Name, Value = SonosConstants.WohnzimmerVolume.ToString() });
                }
                if (esszimmer.GetVolume() != SonosConstants.EsszimmerVolume)
                {
                    esszimmer.SetVolume(SonosConstants.EsszimmerVolume);
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Volume, PlayerName = esszimmer.Name, Value = SonosConstants.EsszimmerVolume.ToString() });
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
                SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.MarantzPower, PlayerName = SonosConstants.EsszimmerName, Value = "on" });
            }
            catch (Exception ex)
            {
                return retValReload + " Exception beim Marantz: " + ex.Message;
            }
            Thread.Sleep(300);
            try
            {
                //Playlist verarveiten
                Boolean loadPlaylist = CheckPlaylist(defaultPlaylist, esszimmer);
                if (loadPlaylist)
                {
                    if (!LoadPlaylist(defaultPlaylist, esszimmer))
                        return "reload, weil Playlist nicht geladen werden konnte";
                }
                else
                {
                    //alten Song aus der Playlist laden, da immer wieder auf 1 reset passiert.
                    esszimmer.SetTrackInPlaylist(oldcurrenttrack.ToString());
                }

                esszimmer.SetPlay();
                SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Playing, PlayerName = esszimmer.Name, Value = "true" });
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
             * KinderZimmer mit eigener Gruppe und nicht im Esszimmer.
             */
            try
            {
                if (DateTime.Now.Hour > 17 || DateTime.Now.Hour < 6)
                {
                    return MakePlayerFine(SonosConstants.KinderzimmerName, SonosConstants.KinderzimmerVolume, false, kzPlaylist);
                }
                else
                {
                    return MakePlayerFine(SonosConstants.KinderzimmerName, SonosConstants.KinderzimmerVolume);
                }
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
            const string rsh = @"x-rincon-mp3radio://http://regiocast.hoerradar.de/rsh-live-mp3-hq?sABC=59nspp3o%230%233o235r1oo3ps56085s2r8pr26r3n3699%23gharva&amsparams=playerid:tunein;skey:1504693307";
            try
            {
                SonosPlayer essPlayer = SonosHelper.GetPlayer(SonosConstants.EsszimmerName);
                SonosPlayer kuPlayer = SonosHelper.GetPlayer(SonosConstants.KücheName);
                SonosPlayer wzPlayer = SonosHelper.GetPlayer(SonosConstants.WohnzimmerName);
                var oldTransportstate = essPlayer.CurrentState.TransportState;
                essPlayer.BecomeCoordinatorofStandaloneGroup();
                Thread.Sleep(300);
                SonosHelper.MessageQueue(new SonosCheckChangesObject{Changed = SonosCheckChangesConstants.SinglePlayer,PlayerName = essPlayer.Name,Value = ""});
                SonosHelper.CheckIsZoneCord(kuPlayer);
                SonosHelper.WaitForTransitioning(wzPlayer);
                if (wzPlayer.CurrentState.TransportState == PlayerStatus.PLAYING)
                {
                    SonosHelper.CheckIsZoneCord(wzPlayer);
                    wzPlayer.SetPause();
                    //Daten vom Marantz ermitteln
                    Marantz.Initialisieren(SonosConstants.MarantzUrl);
                    if (Marantz.SelectedInput == MarantzInputs.Sonos && Marantz.PowerOn)
                    {
                        Marantz.PowerOn = false;
                        SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.MarantzPower, PlayerName = SonosConstants.EsszimmerName, Value = "off" });
                    }
                }
                SonosHelper.WaitForTransitioning(essPlayer);
                if (essPlayer.CurrentState.TransportState == PlayerStatus.PLAYING || oldTransportstate == PlayerStatus.PLAYING)
                {
                    essPlayer.SetPause();
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Playing, PlayerName = essPlayer.Name, Value = "false" });
                    kuPlayer.SetPause();
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Playing, PlayerName = kuPlayer.Name, Value = "false" });
                    return retValok+" ausgeschaltet.";

                }
                if (essPlayer.GetVolume() != SonosConstants.EsszimmerVolume)
                {
                    essPlayer.SetVolume(SonosConstants.EsszimmerVolume);
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Volume, PlayerName = SonosConstants.EsszimmerName, Value = SonosConstants.EsszimmerVolume.ToString() });
                }
                if (kuPlayer.GetVolume() != SonosConstants.KücheVolume)
                {
                    kuPlayer.SetVolume(SonosConstants.KücheVolume);
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Volume, PlayerName = SonosConstants.KücheName, Value = SonosConstants.KücheVolume.ToString() });
                }
                kuPlayer.SetAVTransportURI(SonosConstants.xrincon + essPlayer.UUID);
                Thread.Sleep(300);
                var aktUri = essPlayer.GetMediaInfoURIMeta()[0];
                if (aktUri != rsh)
                {
                    essPlayer.SetAVTransportURI(rsh, "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\"-1\" parentID=\"-1\" restricted=\"true\"><res protocolInfo=\"x-rincon-mp3radio:*:*:*\">x-rincon-mp3radio://http://regiocast.hoerradar.de/rsh-live-mp3-hq?sABC=59nspp3o%230%233o235r1oo3ps56085s2r8pr26r3n3699%23gharva&amp;amsparams=playerid:tunein;skey:1504693307</res><r:streamContent></r:streamContent><dc:title>RSH</dc:title><upnp:class>object.item.audioItem.audioBroadcast</upnp:class></item></DIDL-Lite>");
                    Thread.Sleep(300);
                }
                essPlayer.SetPlay();
                SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Playing, PlayerName = essPlayer.Name, Value = "true" });
                return retValok+" eingeschaltet und RSH gestartet";
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
            StringBuilder sb = new StringBuilder();
            try
            {
                
                sb.AppendLine("Logging für Checkplaylist");
                Boolean retval = false;
                var evtlStream = sp.GetAktSongInfo();
                sb.AppendLine("Aktueller Song ansehen");
                if (evtlStream.TrackURI.StartsWith("aac") || evtlStream.TrackURI.Contains("mp3radio") || evtlStream.TrackURI.Contains(SonosConstants.xsonosapistream))
                    return true;
                sb.AppendLine("Aktueller Song ist kein Stream");
                var actpl = sp.GetPlaylist(0, 10);
                sb.AppendLine("Aktuelle Playlist laden");
                if (actpl.Count == 0) return true;
                sb.AppendLine("Aktuelle Playlist größer als 0 Einträge"+actpl.Count);
                var toLoadpl = sp.BrowsingWithLimitResults(pl, 10);
                sb.AppendLine("Neue Playlist laden erfolgreich");
                if (toLoadpl.Count == 0) return true;//eigentlich ein Fehler
                sb.AppendLine("Neue Playlist mehr als 0 und wiurd nun durchlaufen");
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
                SonosHelper.TraceLog("Checkplaylist.log",sb.ToString());
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
        private Boolean LoadPlaylist(string pl, SonosPlayer sp)
        {
            StringBuilder stringb = new StringBuilder();
            try
            {
                stringb.AppendLine("Löschen aller Tracks von " + sp.Name);
                sp.RemoveAllTracksFromQueue();
                Thread.Sleep(300);
                stringb.AppendLine("Playlist wird geladen:"+pl);
                var sonospl = sp.BrowsingMeta(pl);
                stringb.AppendLine("Playlist wurde geladen und hat "+sonospl.Count+" Ergebnisse.");
                sp.Enqueue(sonospl[0], true);
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
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Playing, PlayerName = player.Name, Value = "false" });
                    return retValok+" ist ausgeschaltet";
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
                    SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Volume, PlayerName = player.Name, Value = _volume.ToString() });
                }
                //Prüfen, ob Esszimmer spielt
                SonosHelper.WaitForTransitioning(esszimmer);
                if (esszimmer.CurrentState.TransportState == PlayerStatus.PLAYING && addToEsszimmer)
                {
                    player.SetAVTransportURI(SonosConstants.xrincon + esszimmer.UUID);
                    return retValok+" Player zum Esszimmer zugefügt.";
                }
                //Soll selber etwas abspielen.
                Boolean loadpl = CheckPlaylist(_Playlist, player);
                if (loadpl)
                {
                    LoadPlaylist(_Playlist, player);
                }
                player.SetPlay();
                SonosHelper.MessageQueue(new SonosCheckChangesObject { Changed = SonosCheckChangesConstants.Playing, PlayerName = player.Name, Value = "true" });
                return retValok+" Player spielt alleine";
            }
            catch (Exception exceptio)
            {
                return retValReload + " MakePlayerFine:Exception:Block3: " + exceptio.Message;
            }
        }
    }
}
