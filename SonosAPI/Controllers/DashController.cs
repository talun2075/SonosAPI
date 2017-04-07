using System;
using System.Threading;
using System.Web.Http;
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

        public string Get()
        {
            Marantz.Initialisieren("http://192.168.0.243");
            return "get";

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
                SonosPlayer gzmPlayer = GetPlayer(SonosConstants.GästezimmerName);
                if (gzmPlayer != null)
                {
                    //hier nun den Code ausführen, der benötigt wird.
                    /*
                     * Es soll für das Schlafen Regen bzw. die Tempsleep geladen werden und die Lautstärke auf 1 gesetzt werden.
                     */
                    WaitForTransitioning(gzmPlayer);
                    if (gzmPlayer.CurrentState.TransportState == PlayerStatus.PLAYING)
                    {
                        //Es wird gespielt und nochmal gedrückt, daher die Playlist wechseln.
                        id = id == "0" ? "1" : "0";
                    }
                    if (gzmPlayer.GetVolume() != SonosConstants.GästezimmerVolume)
                    {
                        gzmPlayer.SetVolume(SonosConstants.GästezimmerVolume);
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
            try
            {
                

                foreach (SonosPlayer sp in SonosHelper.Sonos.Players)
                {
                    var oldTransport = sp.CurrentState.TransportState;
                    CheckIsZoneCord(sp);
                    WaitForTransitioning(sp);
                    if (sp.CurrentState.TransportState != PlayerStatus.PLAYING && oldTransport != PlayerStatus.PLAYING) continue;
                    foundplayed = true;
                    sp.SetPause();
                }
            }
            catch(Exception ex)
            {
                return retValReload + " Exception: Beim prüfen ob ausgeschaltet werden muss:" +ex.Message;
            }
            try
            {
                //Daten vom Marantz ermitteln
                if (!Marantz.IsInitialisiert)
                {
                    Marantz.Initialisieren("http://192.168.0.243");
                }
                if (foundplayed)
                {
                    //Ist auf Sonos?
                    if (Marantz.SelectedInput == MarantzInputs.Sonos && Marantz.PowerOn)
                    {
                        //Marantz ausschalten.
                        Marantz.PowerOn = false;
                    }
                    return "ok, Musik wurde ausgeschaltet.";
                }
            }
            catch
            {
                return retValReload + " Exception: Marantz konnte nicht geschaltet werden.";
            }
            #endregion STOPP

            SonosPlayer esszimmer = GetPlayer(SonosConstants.EsszimmerName);
            SonosPlayer wohnzimmer = GetPlayer(SonosConstants.WohnzimmerName);


            //Alle Player alleine machen und neu zuordnen
            try
            {
                CheckIsZoneCord(wohnzimmer);
                esszimmer.BecomeCoordinatorofStandaloneGroup();
                Thread.Sleep(400);
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
                if (!Marantz.IsInitialisiert)
                {
                    Marantz.Initialisieren("http://192.168.0.243");
                }
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
                Boolean loadPlaylist = CheckPlaylist(defaultPlaylist, esszimmer);
                if (loadPlaylist)
                {
                    if (!LoadPlaylist(defaultPlaylist, esszimmer))
                        return "reload, weil Playlist nicht geladen werden konnte";
                    Thread.Sleep(200);
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
             * KinderZimmer mit eigener Gruppe und nicht im Esszimmer.
             */
            try
            {
                if (DateTime.Now.Hour > 17)
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
            const string rsh = "x-sonosapi-stream:s18353?sid=254&amp;flags=8224&amp;sn=0";
            try
            {
                SonosPlayer essPlayer = GetPlayer(SonosConstants.EsszimmerName);
                SonosPlayer kuPlayer = GetPlayer(SonosConstants.KücheName);
                SonosPlayer wzPlayer = GetPlayer(SonosConstants.WohnzimmerName);
                var oldTransportstate = essPlayer.CurrentState.TransportState;
                essPlayer.BecomeCoordinatorofStandaloneGroup();
                Thread.Sleep(300);
                CheckIsZoneCord(kuPlayer);
                WaitForTransitioning(wzPlayer);
                if (wzPlayer.CurrentState.TransportState == PlayerStatus.PLAYING)
                {
                    CheckIsZoneCord(wzPlayer);
                    wzPlayer.SetPause();
                    //Daten vom Marantz ermitteln
                    if (!Marantz.IsInitialisiert)
                    {
                        Marantz.Initialisieren("http://192.168.0.243");
                    }
                    if (Marantz.SelectedInput == MarantzInputs.Sonos && Marantz.PowerOn)
                        Marantz.PowerOn = false;
                }
                WaitForTransitioning(essPlayer);
                if (essPlayer.CurrentState.TransportState == PlayerStatus.PLAYING || oldTransportstate == PlayerStatus.PLAYING)
                {
                    essPlayer.SetPause();
                    kuPlayer.SetPause();
                    return retValok+" ausgeschaltet.";
                }
                if (essPlayer.GetVolume() != SonosConstants.EsszimmerVolume)
                {
                    essPlayer.SetVolume(SonosConstants.EsszimmerVolume);
                }
                if (kuPlayer.GetVolume() != SonosConstants.KücheVolume)
                {
                    kuPlayer.SetVolume(SonosConstants.KücheVolume);
                }
                kuPlayer.SetAVTransportURI(SonosConstants.xrincon + essPlayer.UUID);
                Thread.Sleep(300);
                var aktUri = essPlayer.GetMediaInfoURIMeta()[0];
                if (aktUri != rsh)
                {
                    essPlayer.SetAVTransportURI(rsh, "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\"F00092020s18353\" parentID=\"F00082064y1%3apopular\" restricted=\"true\"><dc:title>R.SH 102.4 (Top 40/Pop)</dc:title><upnp:class>object.item.audioItem.audioBroadcast</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">SA_RINCON65031_</desc></item></DIDL-Lite>");
                    Thread.Sleep(300);
                }
                essPlayer.SetPlay();
                return retValok+" eingeschaltet und RSH gestartet";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        /// <summary>
        /// Gibt den SonosPlayer aufgrund des übergebenen Names zurück oder Null.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private SonosPlayer GetPlayer(string playerName)
        {
            foreach (SonosPlayer sonosPlayer in SonosHelper.Sonos.Players)
            {
                if (sonosPlayer.Name == playerName)
                    return sonosPlayer;
            }
            return null;
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
                if (evtlStream.TrackURI.StartsWith("aac") || evtlStream.TrackURI.Contains("mp3radio") || evtlStream.TrackURI.Contains(SonosConstants.xsonosapistream))
                    return true;

                var actpl = sp.GetPlaylist(0, 10);
                if (actpl.Count == 0) return true;
                var toLoadpl = sp.BrowsingWithLimitResults(pl, 10);
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
        private Boolean LoadPlaylist(string pl, SonosPlayer sp)
        {
            try
            {
                sp.RemoveAllTracksFromQueue();
                Thread.Sleep(300);
                var sonospl = sp.BrowsingMeta(pl);
                sp.Enqueue(sonospl[0], true);
                Thread.Sleep(200);
                sp.SetAVTransportURI(SonosConstants.xrinconqueue + sp.UUID + "#0");
                Thread.Sleep(400);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Liefert die Zone aufgrund des übergebenen Namen
        /// </summary>
        /// <param name="playername"></param>
        /// <returns></returns>
        private SonosZone GetZone(string playername)
        {
            foreach (SonosZone sonosZone in SonosHelper.Sonos.Zones)
            {
                if (sonosZone.Coordinator.Name == playername)
                {
                    return sonosZone;
                }
            }
            return null;
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
                player = GetPlayer(_player);
                esszimmer = GetPlayer(SonosConstants.EsszimmerName);
            }
            catch (Exception exceptio)
            {
                return retValReload + " MakePlayerFine:Exception:Block0: " + exceptio.Message;
            }
            try
            {
                if (!CheckIsZoneCord(player))
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
                WaitForTransitioning(player);
                if (player.CurrentState.TransportState == PlayerStatus.PLAYING)
                {
                    player.SetPause();
                    return retValok+" ist ausgeschaltet";
                }
            }
            catch (Exception exceptio)
            {
                return retValReload + " MakePlayerFine:Exception:Block2: " + exceptio.Message;
            }
            try
            {
                if (player.GetVolume() == _volume)
                {
                    player.SetVolume(_volume);
                }
                //Prüfen, ob Esszimmer spielt
                WaitForTransitioning(esszimmer);
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
                return retValok+" Player spielt alleine";
            }
            catch (Exception exceptio)
            {
                return retValReload + " MakePlayerFine:Exception:Block3: " + exceptio.Message;
            }
        }
        /// <summary>
        /// Prüft ob IsZoneCord gesetzt ist
        /// Falls nicht FallBack auf Zonen
        /// Falls Player in einer Zone ist, wird dieser aus dieser Rausgenommen. 
        /// </summary>
        /// <param name="sp">Player der geprüft werden soll.</param>
        /// <returns></returns>
        private Boolean CheckIsZoneCord(SonosPlayer sp)
        {
            if (sp.IsZoneCoord == false)
            {
                sp.BecomeCoordinatorofStandaloneGroup();
                Thread.Sleep(300);
                return false;
            }
            if (sp.IsZoneCoord == null)
            {
                //Wenn IsZoneCoord Null und keine Zone gefunden wurde, dann ist der Player in einer Gruppe.
                var sz = GetZone(sp.Name);
                if (sz == null)
                {
                    sp.IsZoneCoord = false;
                    sp.BecomeCoordinatorofStandaloneGroup();
                    Thread.Sleep(300);
                    return false;
                }
                sp.IsZoneCoord = true;
            }
            return true;
        }

        private void WaitForTransitioning(SonosPlayer sp)
        {
            if (sp.CurrentState.TransportState == PlayerStatus.TRANSITIONING)
            {
                Boolean trans = false;
                int counter = 0;
                while (!trans)
                {
                    if (sp.CurrentState.TransportState != PlayerStatus.TRANSITIONING || counter > 5)
                    {
                        trans = true;
                    }
                    Thread.Sleep(200);
                    counter++;
                }
            }
        }
    }
}
