﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SonosUPNP;

namespace SonosAPI.Classes
{
    /// <summary>
    /// Zwischenschicht zu Sonos Player und Controller
    /// </summary>
    public static class SonosHelper
    {
        /// <summary>
        /// Das primäre Sonos Objekt
        /// </summary>
        public static SonosDiscovery Sonos;
        /// <summary>
        /// Timer for MessageQueue
        /// </summary>
        private static Timer messagetimer;
        /// <summary>
        /// Dictonary mit UUID und DateTimes für die letzten Änderungen. Wird Über Events aktualisiert
        /// </summary>
        internal static Dictionary<String, DateTime> ZoneChangeList = new Dictionary<String, DateTime>();

        /// <summary>
        /// Liste mir allen Server Errors.
        /// </summary>
        internal static Dictionary<String, String> serverErrors = new Dictionary<String, String>();

        /// <summary>
        /// Zeitpunkt, wann sich das letzt mal etwas an Zonen oder Anzahl Player geändert hat.
        /// </summary>
        internal static DateTime Topologiechanged { get; set; }
        internal static Boolean WasInitialed { get; private set; }

        internal static List<SonosCheckChangesObject> sccoList = new List<SonosCheckChangesObject>();

        /// <summary>
        /// Initialisierung des Sonos Systems
        /// </summary>
        /// <returns></returns>
        public static Boolean Initialisierung()
        {
            try
            {
                Boolean retval = false;
                if (Sonos == null || Sonos.IsReseted || !WasInitialed)
                {
                    serverErrors.Clear();
                    sccoList.Clear();
                    MessagePolling();
                    retval = InitialSonos();
                    Sonos_TopologyChanged();
                    WasInitialed = retval;
                    SonosStreamRating.LoadRatedItems();
                }
                return retval;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SonosHelper:Initialisierung", ex);
                return false;
            }
        }

        /// <summary>
        /// Sonos Suchen (Start Scan)
        /// </summary>
        private static Boolean InitialSonos()
        {
            try
            {
                // ReSharper disable once InconsistentlySynchronizedField
                Sonos = new SonosDiscovery();
                lock (Sonos)
                {

                    Sonos.StartScan();
                    Boolean ok = false;
                    while (!ok)
                    {
                        if (Sonos.Players.Count > 0)
                        {
                            ok = true;
                        }
                    }
                    Sonos.TopologyChanged += Sonos_TopologyChanged;
                }
                return true;
            }
            catch (Exception x)
            {
                ServerErrorsAdd("SonosHelper:InitialSonos", x);
                return false;
            }
        }

        /// <summary>
        /// Wenn sich an den Zonen/Anzahl Playern etwas ändert, wird dies entsprechend mit Datetime.Now befüllt und kann abgefragt werden. 
        /// Wird einmalig bei der Initialisierung aufgerufen.
        /// </summary>
        public static void Sonos_TopologyChanged()
        {
            Topologiechanged = DateTime.Now;
            lock (ZoneChangeList)
            {
                ZoneChangeList.Clear();
                ZoneChangeList.Add("SonosZones", Topologiechanged);
            }
            try
            {
                if (Sonos == null || Sonos.Zones == null) return;

                lock (Sonos.Zones)
                {
                    lock (ZoneChangeList)
                    {
                        foreach (SonosZone z in Sonos.Zones)
                        {
                            z.Coordinator.StateChanged += Sonos_Player_TopologieChanged;
                            z.Coordinator.IsZoneCoord = true;
                            //Nun alle anderen Player Prüfen
                            if (z.Players.Count > 1)
                            {
                                foreach (SonosPlayer zPlayer in z.Players)
                                {
                                    if (zPlayer.UUID != z.Coordinator.UUID)
                                    {
                                        zPlayer.StateChanged += Sonos_Player_TopologieChanged;
                                        zPlayer.IsZoneCoord = false;
                                    }
                                }
                            }
                            if (!ZoneChangeList.ContainsKey(z.Coordinator.UUID))
                            {
                                ZoneChangeList.Add(z.Coordinator.UUID, z.Coordinator.CurrentState.LastStateChange);
                            }
                        }
                    }
                }
            }
            catch (Exception x)
            {
                ServerErrorsAdd("SonosHelper:Sonos_TopologyChanged", x);
            }
            //EventController.EventTopologieChange(null);
        }

        /// <summary>
        /// Schreibt Servererrors ins Filesystem.
        /// </summary>
        /// <param name="Method"></param>
        /// <param name="ExceptionMes"></param>
        internal static void ServerErrorsAdd(string Method, Exception ExceptionMes)
        {
            if (ExceptionMes.Message.StartsWith("Could not connect to device")) return;
            string error = DateTime.Now.ToString("yyyy-M-d_-_HH-mm-ss") + "_" + DateTime.Now.Ticks + " " + Method + " " +
                           ExceptionMes.Message;

            if (!serverErrors.ContainsKey(Method))
            {
                serverErrors.Add(Method, error);
            }
            var dir = Directory.CreateDirectory(@"C:\NasWeb\Error");
            //string errorfile = error + ".txt";
            string file = dir.FullName + "\\Log.txt";
            //File.Create(file).Dispose();

            if (!File.Exists(file))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(file))
                {
                    sw.WriteLine("Loggingstart:");
                }
            }
            using (StreamWriter sw = File.AppendText(file))
            {
                sw.WriteLine(error);
                sw.WriteLine("TargetSite: " + ExceptionMes.TargetSite);
                sw.WriteLine("Base:Message: " + ExceptionMes.GetBaseException().Message);
            }


        }

        /// <summary>
        /// Wird aufgerufen, wenn sich etwas bei einem Player geändert hat
        /// </summary>
        /// <param name="obj"></param>
        public static void Sonos_Player_TopologieChanged(SonosPlayer obj)
        {
            lock (Sonos.Zones)
            {
                if (ZoneChangeList.ContainsKey(obj.UUID))
                {
                    ZoneChangeList[obj.UUID] = obj.CurrentState.LastStateChange;
                }
                else
                {
                    //Evtl. ein Cordinated Player?
                    foreach (var sz in Sonos.Zones)
                    {
                        if (sz.Players.Contains(obj))
                        {
                            if (ZoneChangeList.ContainsKey(sz.Coordinator.UUID))
                            {
                                ZoneChangeList[sz.Coordinator.UUID] = obj.CurrentState.LastStateChange;
                            }
                            break;
                        }
                    }

                }
            }
            //EventController.EventPlayerChange(obj);
        }

        /// <summary>
        /// Prüft die auzuliefernden Zonen nach DefaultValues und Ändert diese.
        /// Zusätzlich wird die Playersliste so geändert, das Man sich nicht mehr selber dort enthält.
        /// </summary>
        internal static void RemoveCoordinatorFromZonePlayerList()
        {
            try
            {
                if (Sonos == null || Sonos.Zones == null) return;

                lock (Sonos.Zones)
                {
                    foreach (var sonosZone in Sonos.Zones)
                    {
                        //Hier nun Dinge machen, damit keine Nullwerte Kommen. 
                        if (sonosZone.Coordinator == null)
                        {
                            return;
                        }
                        //Sich selber aus der Liste rausnehmen
                        sonosZone.Players.Remove(sonosZone.Coordinator);
                        if (sonosZone.Players.Count > 0)
                        {
                            foreach (SonosPlayer sonosPlayer in sonosZone.Players)
                            {
                                if (sonosPlayer.UUID != sonosZone.Coordinator.UUID)
                                {
                                    if (sonosPlayer.CurrentState != null && sonosPlayer.CurrentState.Volume == 0)
                                    {
                                        sonosPlayer.CurrentState.Volume = sonosPlayer.GetVolume();
                                        //sonosPlayer.ManuellStateChange(DateTime.Now);
                                    }
                                    sonosPlayer.StateChanged += Sonos_Player_TopologieChanged;

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception x)
            {
                ServerErrorsAdd("SonosHelper:RemoveCoordinatorFromZonePlayerList", x);
            }
        }

        /// <summary>
        /// Ersetzt den Pfad für die MP3 Verarbeitung
        /// </summary>
        /// <param name="_uri"></param>
        /// <returns></returns>
        public static String URItoPath(string _uri)
        {
            try
            {
                if (string.IsNullOrEmpty(_uri)) return String.Empty;
                string RemoveFromUri = SonosConstants.xfilecifs;
                _uri = _uri.Replace(RemoveFromUri, "");
                _uri = Uri.UnescapeDataString(_uri);
                return _uri.Replace("/", "\\");
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("URITOPath", ex);
                return String.Empty;
            }
        }
        /// <summary>
        /// Prüft ob IsZoneCord gesetzt ist
        /// Falls nicht FallBack auf Zonen
        /// Falls Player in einer Zone ist, wird dieser aus dieser Rausgenommen. 
        /// </summary>
        /// <param name="sp">Player der geprüft werden soll.</param>
        /// <returns></returns>
        public static Boolean CheckIsZoneCord(SonosPlayer sp)
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
        /// <summary>
        /// Liefert die Zone aufgrund des übergebenen Namen
        /// </summary>
        /// <param name="playername"></param>
        /// <returns></returns>
        public static SonosZone GetZone(string playername)
        {
            lock (Sonos.Zones)
            {
                foreach (SonosZone sonosZone in Sonos.Zones)
                {
                    if (sonosZone.Coordinator.Name == playername)
                    {
                        return sonosZone;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gibt den SonosPlayer aufgrund des übergebenen Names zurück oder Null.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public static SonosPlayer GetPlayer(string playerName)
        {
            lock (Sonos.Players)
            {
                foreach (SonosPlayer sonosPlayer in Sonos.Players)
                {
                    if (sonosPlayer.Name == playerName)
                        return sonosPlayer;
                }
            }
            return null;
        }

        /// <summary>
        /// Check Changes after some Seconds and make it fine if there is a Problem. 
        /// </summary>
        public static void MessageQueue(SonosCheckChangesObject newscco)
        {
            if (!sccoList.Contains(newscco) && newscco != null)
            {
                sccoList.Add(newscco);
            }
            if (!sccoList.Any()) return;
            var itemsToRemove = new List<SonosCheckChangesObject>();
            lock (sccoList)
            {
                foreach (SonosCheckChangesObject sonosCheckChangesObject in sccoList)
                {
                    //Get Player
                    SonosPlayer sp = GetPlayer(sonosCheckChangesObject.PlayerName);
                    if (sp == null) continue;

                    switch (sonosCheckChangesObject.Changed)
                    {
                        //Change Volume
                        case SonosCheckChangesConstants.Volume:
                            int tvol;
                            int.TryParse(sonosCheckChangesObject.Value, out tvol);
                            if (tvol > 0)
                            {
                                var t = sp.GetVolume();
                                if (t != tvol)
                                {
                                    sp.SetVolume((ushort)tvol);
                                }
                                itemsToRemove.Add(sonosCheckChangesObject);
                            }
                            break;
                        //Make Player Standalone
                        case SonosCheckChangesConstants.SinnglePlayer:
                            CheckIsZoneCord(sp);
                            itemsToRemove.Add(sonosCheckChangesObject);
                            break;
                        //Add Player to Zone
                        case SonosCheckChangesConstants.AddToZone:
                            //Check Player is in Zone. If not Add it.
                            SonosZone sz = GetZone(sonosCheckChangesObject.Value);
                            if (sz == null) break;
                            if (sz.Players.Count > 0)
                            {
                                SonosPlayer tsp =
                                    sz.Players.FirstOrDefault(x => x.Name == sonosCheckChangesObject.PlayerName);
                                if (tsp != null) continue; //Player is in there
                                sp.SetAVTransportURI(SonosConstants.xrincon + sz.CoordinatorUUID);
                                itemsToRemove.Add(sonosCheckChangesObject);
                            }
                            else
                            {
                                sp.SetAVTransportURI(SonosConstants.xrincon + sz.CoordinatorUUID);
                                itemsToRemove.Add(sonosCheckChangesObject);
                            }
                            break;
                    }
                }
                if (!itemsToRemove.Any()) return;
                foreach (SonosCheckChangesObject sonosCheckChangesObject in itemsToRemove)
                {
                    sccoList.Remove(sonosCheckChangesObject);
                }
            }
        }
        /// <summary>
        /// MessageQueue Start Polling Timer
        /// </summary>
        private static void MessagePolling()
        {
            if (messagetimer != null)
            {
                return;
            }
            messagetimer = new Timer(MessagePollingState, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(30));
        }
        /// <summary>
        /// MessageQueue Poliing
        /// </summary>
        /// <param name="state"></param>
        private static void MessagePollingState(object state)
        {
            MessageQueue(null);
        }
    }
}