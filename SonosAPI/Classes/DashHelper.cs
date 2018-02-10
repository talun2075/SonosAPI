using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ExternalDevices;
using NanoleafAurora;
using SonosUPnP;
using SonosUPNP;

namespace SonosAPI.Classes
{
    public static class DashHelper
    {
        /// <summary>
        /// Auroras einschalten
        /// </summary>
        public static void PowerOnAruroras()
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
        /// <summary>
        /// Auroras ausschalten
        /// </summary>
        public static void PowerOffAruroras()
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

        public static void PowerOffMarantz()
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

        public static void PowerOnMarantz()
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
        /// <summary>
        /// Prüft ob das Ziel Konstrukt schon vorhanden ist.
        /// </summary>
        /// <param name="primaray"></param>
        /// <param name="listOfPlayers"></param>
        /// <returns></returns>
        public static Boolean IsSonosTargetGroupExist(SonosPlayer primaray, List<SonosPlayer> listOfPlayers)
        {
            SonosZone sz = SonosHelper.GetZone(primaray.Name);
            if(sz == null || sz.Players.Count != listOfPlayers.Count || sz.Players.Count == 0 || listOfPlayers.Count == 0 ) return false;

            foreach (SonosPlayer sonosPlayer in sz.Players)
            {
                if (!listOfPlayers.Contains(sonosPlayer)) return false;
            }
            


            return true;
        }

        /// <summary>
        /// Prüft die übergebene Playlist mit dem Übergeben Player ob neu geladen werden muss.
        /// </summary>
        /// <param name="pl">Playliste, die geladen werden soll.</param>
        /// <param name="sp">Coordinator aus der Führenden Zone</param>
        /// <returns>True muss neu geladen werden</returns>
        public static Boolean CheckPlaylist(string pl, SonosPlayer sp)
        {
            //todo: nochmals prüfen wegen Radio, da scheint es probleme zu geben.
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
        public static Boolean LoadPlaylist(SonosItem pl, SonosPlayer sp)
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
                Thread.Sleep(500);
                return true;
            }
            catch
            {
                SonosHelper.TraceLog("Loadplaylist.log", stringb.ToString());
                return false;
            }
        }
    }
}