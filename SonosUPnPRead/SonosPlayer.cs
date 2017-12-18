using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml.Linq;
using OSTL.UPnP;
using SonosUPnP;
using System.Web;

namespace SonosUPNP
{
    [Serializable]
    [DataContract]
    public class SonosPlayer
    {
        #region Klassenvariable
        private UPnPDevice mediaRenderer;
        private UPnPService devicepropertie;
        private UPnPService groupManagement;
        private UPnPService zonegroupTopologie;
        private UPnPService avTransport;
        private UPnPDevice mediaServer;
        private UPnPService alarmclock;
        private UPnPService renderingControl;
        private UPnPService queueControl;
        private UPnPService grouprenderingControl;
        private UPnPService contentDirectory;
        private UPnPService connectionManager;
        private UPnPService audioIn;
        public event Action<SonosPlayer> StateChanged;
        private Timer positionTimer;
        private Boolean subscripeToAudioInSuccess;
        #endregion Klassenvariable

        #region Error
        /// <summary>
        /// Fügt bei Fehlern diese der LogDatei hinzu. 
        /// </summary>
        /// <param name="Method"></param>
        /// <param name="ExceptionMes"></param>
        private void ServerErrorsAdd(string Method, Exception ExceptionMes)
        {
            if (ExceptionMes.Message.StartsWith("Could not connect to device")) return;
            string error = DateTime.Now.ToString("yyyy-M-d_-_hh-mm-ss") + "_" + DateTime.Now.Ticks + " " + Method + " " + ExceptionMes.Message;
            var dir = Directory.CreateDirectory(@"C:\NasWeb\Error");
            string file = dir.FullName + "\\Log_" + Name + ".txt";
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
        #endregion
        #region Eventing
        /// <summary>
        /// AVTRansport, RenderingControl Events abonieren
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                AVTransport.Subscribe(600, (service, subscribeok) =>
                {
                    if (!subscribeok)
                        return;

                    var lastChangeStateVariable = service.GetStateVariableObject("LastChange");
                    lastChangeStateVariable.OnModified += ChangeTriggered;
                });
                //RenderingControl Changed
                RenderingControl.Subscribe(600, (service, subscribeok) =>
                {
                    if (!subscribeok)
                        return;

                    var lastRCChangeStateVariable = service.GetStateVariableObject("LastChange");
                    lastRCChangeStateVariable.OnModified += RenderingControlChangeTriggered;
                });
                GroupRenderingControl.Subscribe(600, (service, subscribeok) =>
                {
                    if (!subscribeok)
                        return;

                    var lastRCChangeStateVariable = service.GetStateVariableObject("GroupVolume");
                    lastRCChangeStateVariable.OnModified += RenderingControlGroupVolumeChangeTriggered;
                });
                GroupManagement.Subscribe(600, (service, subscribeok) =>
                {
                    if (!subscribeok)
                        return;

                    var lastRCChangeStateVariable = service.GetStateVariableObject("GroupCoordinatorIsLocal");
                    lastRCChangeStateVariable.OnModified += GroupCoordinatorIsLocalEventTrigger;
                });
                ContentDirectory.Subscribe(600, (service, subscribeok) =>
                {
                    if (!subscribeok)
                        return;

                    var playlistChangeVariable = service.GetStateVariableObject("ContainerUpdateIDs");
                    playlistChangeVariable.OnModified += PlaylistChangeEventTrigger;
                });
                //QueueControl.Subscribe(600, (service, subscribeok) =>
                //{
                //    if (!subscribeok)
                //        return;

                //    var playlistChangeVariable = service.GetStateVariableObject("LastChange");
                //    playlistChangeVariable.OnModified += TestEventTrigger;
                //});
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SubscripteToEvents", ex);
            }
            SubscripeToAudioIn();

        }
        /// <summary>
        /// Event, der IsZoneCoord setzt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void GroupCoordinatorIsLocalEventTrigger(UPnPStateVariable sender, object value)
        {
            IsZoneCoord = (Boolean) value;
        }
        /// <summary>
        /// Wird aufgerufen, wenn sich an der Playlist etwas ändert
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void PlaylistChangeEventTrigger(UPnPStateVariable sender, object value)
        {

            // ReSharper disable once UnusedVariable
            String newState = sender.Value.ToString();
            /*
            Aufruf gibt einenen Wert wie Q:0,xxx mit xxx ist ein Counter, der hochgezählt wird. 

            Q: = Playliste
            S: = ???

            Das kommt, immer, wenn an der Playliste etwas geändert wird. Nicht beim Streaming von Radio oder Eingängen, da die Playlist gleich bleibt.
            Wenn man drauf reagieren möchte, dann muss geprüft werden, ob man selber etwas geändert hat
            Beim Löschen brauche ich kein Refresh, weil ich das einfach an der Oberfläche mache. 
            Beim Adden, lade ich aktuell die Playlist neu. 
            Beim Moven brauche ich auch nicht neu laden. 
            */
        }
        /// <summary>
        /// Da AudioIn oft Null ist hier Prüfungen einbauen.
        /// </summary>
        private void SubscripeToAudioIn()
        {
            //AudioIn (nicht jeder Player hat ein AudioIn
            try
            {
                AudioIn.Subscribe(600, (service, subscribeok) =>
                {
                    if (!subscribeok)
                        return;

                    var lastChangeStateVariable = service.GetStateVariableObject("LineInConnected");
                    lastChangeStateVariable.OnModified += AudioInChangeTriggered;
                });
                subscripeToAudioInSuccess = true;
            }
            catch
            {
                HasAudioIn = false;
            }
        }
        /// <summary>
        /// Trigger, der aufgerufen wird, wenn sich eine Veränderung beim Event ergibt. Ruft nur ParseChangeXML auf
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void ChangeTriggered(UPnPStateVariable sender, object value)
        {
            try
            {
                String newState = sender.Value.ToString();
                ParseChangeXML(newState);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("ChangeTriggered", ex);
            }
        }
        /// <summary>
        /// Changes from Renderingcontrol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void RenderingControlChangeTriggered(UPnPStateVariable sender, object value)
        {
            try
            {
                string newState = sender.Value.ToString();
                Boolean changed = false;
                var xEvent = XElement.Parse(newState);
                XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/RCS/";
                XElement instance = xEvent.Element(ns + "InstanceID");
                if (instance == null) return;
                //volume
                XElement vol = instance.Element(ns + "Volume");
                if (vol != null && vol.Attribute("channel").Value == "Master")
                {
                    var tvol = Convert.ToInt16(vol.Attribute("val").Value);
                    if (tvol != CurrentState.Volume)
                    {
                        CurrentState.Volume = tvol;
                        changed = true;
                    }
                }
                //Mute
                XElement mutElement = instance.Element(ns + "Mute");
                if (mutElement != null && mutElement.Attribute("channel").Value == "Master")
                {
                    var tmute = (mutElement.Attribute("val").Value == "1");
                    if (Mute != tmute)
                    {
                        Mute = tmute;
                        changed = true;
                    }
                }
                /*
            XElement basElement = instance.Element(ns + "Bass");
             * if (basElement != null)
            {
                var Bass = Convert.ToBoolean(basElement.Attribute("val").Value);
            }
            XElement trebleElement = instance.Element(ns + "Treble");
            if (trebleElement != null)
            {
                var Treble = Convert.ToBoolean(trebleElement.Attribute("val").Value);
            }
            XElement presetNameListElemtElement = instance.Element(ns + "PresetNameList");
            if (presetNameListElemtElement != null)
            {
                var PresetNameList = Convert.ToBoolean(presetNameListElemtElement.Attribute("val").Value);
            }
            XElement loudnessElement = instance.Element(ns + "Loudness");
            if (loudnessElement != null)
            {
                var Loudness = Convert.ToBoolean(loudnessElement.Attribute("val").Value);
            }
            XElement outputFixedeElement = instance.Element(ns + "OutputFixed");
            if (outputFixedeElement != null)
            {
                var OutputFixed = Convert.ToBoolean(outputFixedeElement.Attribute("val").Value);
            }
            XElement headphoneConnectedeElement = instance.Element(ns + "HeadphoneConnected");
            if (headphoneConnectedeElement != null)
            {
                var HeadphoneConnected = Convert.ToBoolean(headphoneConnectedeElement.Attribute("val").Value);
            }
            */
                if (changed)
                {
                    ManuellStateChange(DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("RenderingControllTrigger", ex);
            }
        }
        /// <summary>
        /// Changes für die Gruppenlautstärke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void RenderingControlGroupVolumeChangeTriggered(UPnPStateVariable sender, object value)
        {
            try
            {
                int cv;
                int.TryParse(sender.Value.ToString(), out cv);
                GroupVolume = cv;
                /*Wenn genutzt werden soll, dann gibt es noch GroupMute
                  Property muss dann noch als DataMember definiert werden  
                */

            }
            catch (Exception ex)
            {
                ServerErrorsAdd("RenderingControlGroupVolumeChangeTriggered", ex);
            }
        }
        /// <summary>
        /// Wird aufgerufen, wenn der AudioIn am Gerät aktiviert/deaktiviert wird
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void AudioInChangeTriggered(UPnPStateVariable sender, object value)
        {
            try
            {
                string newState = sender.Value.ToString().ToLower();
                HasAudioIn = newState == "true";
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("AudioInChangeTriggered", ex);
            }
        }
        /// <summary>
        /// Dient dazu manuelle Änderungen als Event zu feuern und den LastChange entsprechend zu setzen.
        /// </summary>
        /// <param name="_lastchange"></param>
        public void ManuellStateChange(DateTime _lastchange)
        {
            try
            {
                if (CurrentState == null || StateChanged == null) return;
                CurrentState.LastStateChange = _lastchange;
                StateChanged.Invoke(this);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("ManuellStateChange", ex);
            }
        }
        /// <summary>
        /// Bei Eventchange wird hier Currentstate gefüllt.
        /// </summary>
        /// <param name="newState"></param>
        private void ParseChangeXML(string newState)
        {
            Boolean changed = false;
            XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/AVT/";
            XNamespace nsnext = "urn:schemas-rinconnetworks-com:metadata-1-0/";
            XElement instance;
            try
            {
                var xEvent = XElement.Parse(newState);
                instance = xEvent.Element(ns + "InstanceID");
                // We can receive other types of change events here.
                if (instance == null) return;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("ParseChangeXMLXMLError", ex);
                return;
            }
            try
            {
                //SleepTimerverarbeiten
                XElement sleepTimerGene = instance.Element(nsnext + "SleepTimerGeneration");
                if (sleepTimerGene != null)
                {
                    int stg;
                    var trystate = int.TryParse(sleepTimerGene.Attribute("val").Value, out stg);
                    //Hier wurde der SleepTimer geändert
                    if (trystate && (SleepTimerRunning == false && stg > 0 || SleepTimerRunning && stg <= 0))
                    {
                        SleepTimerRunning = stg > 0;
                        ManuellStateChange(DateTime.Now);
                    }
                    return; //Es kann kein Transportstate kommen. 
                }

                if (instance.Element(ns + "TransportState") == null)
                {
                    return;
                }

                //Fademode
                XElement currentCrossfadeMode = instance.Element(ns + "CurrentCrossfadeMode");
                if (currentCrossfadeMode != null)
                {
                    string t = currentCrossfadeMode.Attribute("val").Value;
                    if (CurrentState.CurrentCrossfadeMode && t != "1" ||
                        !CurrentState.CurrentCrossfadeMode&& t == "1")
                    {
                        CurrentState.CurrentCrossfadeMode = t == "1";
                        changed = true;
                    }
                }
                //Transportstate
                XElement transportStatElement = instance.Element(ns + "TransportState");
                if (transportStatElement != null)
                {
                    var ts = transportStatElement.Attribute("val").Value;
                    if (ts != CurrentState.TransportStateString)
                    {
                        changed = true;
                        switch (ts)
                        {
                            case "PLAYING":
                                CurrentState.TransportState = PlayerStatus.PLAYING;
                                break;
                            case "PAUSED":
                            case "PAUSED_PLAYBACK":
                                CurrentState.TransportState = PlayerStatus.PAUSED_PLAYBACK;
                                break;
                            case "STOPPED":
                                CurrentState.TransportState = PlayerStatus.STOPPED;
                                break;
                            default:
                                CurrentState.TransportState = PlayerStatus.TRANSITIONING;
                                break;
                        }
                        CurrentState.TransportStateString = CurrentState.TransportState.ToString();
                    }
                }
                //Playmode
                XElement currentPlayModeElement = instance.Element(ns + "CurrentPlayMode");
                if (currentPlayModeElement != null)
                {
                    string tcpm = currentPlayModeElement.Attribute("val").Value;
                    if (tcpm != CurrentState.CurrentPlayMode)
                    {
                        CurrentState.CurrentPlayMode = tcpm;
                        changed = true;
                    }
                }
                //NumberofTRacks
                XElement numberOfTracksElement = instance.Element(ns + "NumberOfTracks");
                if (numberOfTracksElement != null)
                {
                    int not;
                    int.TryParse(numberOfTracksElement.Attribute("val").Value, out not);
                    if (CurrentState.NumberOfTracks != not)
                    {
                        CurrentState.NumberOfTracks = not;
                        changed = true;
                    }
                }
                //CurrentTrackNumber
                XElement currentTrackNumberElement = instance.Element(ns + "CurrentTrack");
                if (currentTrackNumberElement != null)
                {
                    var tctn = Convert.ToInt32(currentTrackNumberElement.Attribute("val").Value);
                    if (CurrentState.CurrentTrackNumber != tctn)
                    {
                        CurrentState.CurrentTrackNumber = tctn;
                        changed = true;
                    }
                }
                //CurrentTRackDuration
                XElement currentTrackDurationElement = instance.Element(ns + "CurrentTrackDuration");
                if (currentTrackDurationElement != null)
                {
                    var tctd = ParseDuration(currentTrackDurationElement.Attribute("val").Value);
                    if (CurrentState.CurrentTrackDuration != tctd)
                    {
                        CurrentState.CurrentTrackDuration = tctd;
                        changed = true;
                    }
                }
                //Wenn NextTrack aktiviert ist, diesen durchlaufgen lassen und entsprechend anzeigen lassen
                XElement nextTrackMetaData = instance.Element(nsnext + "NextTrackMetaData");
                if (nextTrackMetaData != null)
                {
                    string b = nextTrackMetaData.Attribute("val").Value;
                    if (!String.IsNullOrEmpty(b))
                    {
                        var tnext = SonosItem.ParseSingleItem(b);
                        if (CurrentState.NextTrack == null || CurrentState.NextTrack.Title != tnext.Title && CurrentState.NextTrack.Artist != tnext.Artist)
                        {
                            CurrentState.NextTrack = tnext;
                            changed = true;
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("ParseChangeXML", ex);
            }
            try
            {
                //CurrentTrack
                XElement currentTrackMetaDataElement = instance.Element(ns + "CurrentTrackMetaData");
                string ctmdevalue = String.Empty;
                if (currentTrackMetaDataElement != null)
                {
                    ctmdevalue = currentTrackMetaDataElement.Attribute("val").Value;
                }
                if (!string.IsNullOrEmpty(ctmdevalue))
                {

                    var tct = SonosItem.ParseSingleItem(ctmdevalue);
                    if (CurrentState.CurrentTrack.Artist != tct.Artist && CurrentState.CurrentTrack.Title != tct.Title &&
                        CurrentState.CurrentTrack.Album != tct.Album)
                    {
                        CurrentState.CurrentTrack = SonosItemHelper.CheckItemForStreaming(tct,this);
                        changed = true;
                    }
                    
                    
                }
            }
            catch (Exception ex)
            {
                SonosItem xyz = new SonosItem { Artist = "leer" };
                CurrentState.CurrentTrack = xyz;
                ServerErrorsAdd("ParseChangeXMLCurrentTrack", ex);
            }
            if (changed)
            {
                ManuellStateChange(DateTime.Now);
            }
        }
        /// <summary>
        /// Timer, der immer wieder den CurrentState und die Zone ermittelt
        /// </summary>
        public void StartPolling()
        {
            if (positionTimer != null)
            {
                return;
            }
            positionTimer = new Timer(UpdateCurrenStateTrack, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10));
        }
        /// <summary>
        /// Wenn nicht gespielt wird, wird dieser hochgezählt beim dritten mal wird dennoch geprüft
        /// </summary>
        private int UPdateStateCounter = 10;
        /// <summary>
        /// Prüft, ob der AudioIn vorhanden ist. Einige Player haben aber kein AudioIn und da ist immer Null
        /// </summary>
        private int UPdateStateCounterAudioIn;
        /// <summary>
        /// Aktualisiert den Currentstate / Track
        /// </summary>
        /// <param name="state"></param>
        private void UpdateCurrenStateTrack(object state)
        {
            UPdateStateCounter++;
            if (CurrentState.TransportState != PlayerStatus.PLAYING || IsZoneCoord != true && CurrentState.TransportState == PlayerStatus.PLAYING)
            {
                if (UPdateStateCounter < 10)
                {
                    UPdateStateCounter++;
                    return; //theory nur jedes 9. mal wird alles geprüft.
                }
            }
            UPdateStateCounter = 0;
            Boolean changed = false;
            var ct = CurrentState;
            //var positionInfo = GetAktSongInfo();
            //if (positionInfo.TrackMetaData != "NOT_IMPLEMENTED") //Kommt, wenn kein Song in Playlist
            //{
            //    SonosItem song = SonosItem.ParseSingleItem(positionInfo.TrackMetaData);

            //    var cump3 = ct.CurrentTrack.MP3;

            //    if (ct.CurrentTrack.Uri != song.Uri)
            //    {
            //        ct.CurrentTrack = song;
            //        //ct.CurrentTrackMetaData = positionInfo.TrackMetaData;
            //        if (cump3 != null && song.Artist == cump3.Artist && song.Title == cump3.Titel &&
            //            song.Album == cump3.Album)
            //        {
            //            ct.CurrentTrack.MP3 = cump3; //MP3 wieder schreiben.
            //        }
            //        ct.CheckForStream();
            //        //Cover setzen für Streams
            //        if (ct.CurrentTrack.RadioStream)
            //        {
            //            List<string> k = GetMediaInfoURIMeta();
            //            ct.CurrentTrack.AlbumArtURI = "/getaa?s=1&u=" + k[0];
            //        }
            //        changed = true;
            //    }
            //    if (ct.CurrentTrackDuration != positionInfo.TrackDuration)
            //    {
            //        ct.CurrentTrackDuration = positionInfo.TrackDuration;
            //        changed = true;
            //    }
            //    if (ct.CurrentTrackNumber != positionInfo.TrackIndex)
            //    {
            //        ct.CurrentTrackNumber = positionInfo.TrackIndex;
            //        changed = true;
            //    }
            //    if (ct.RelTime != positionInfo.RelTime)
            //    {
            //        ct.RelTime = positionInfo.RelTime;
            //        changed = true;
            //    }
            //    if (ct.CurrentTrack.RadioStream == false && ct.CurrentTrack.MP3 == null)
            //    {
            //        //Bei Songwechsel Zugriff aufs Dateisystem außer es wird als Parameter übergeben.
            //        string u = URItoPath(song.Uri);
            //        ct.CurrentTrack.MP3 = MP3ReadWrite.ReadMetaData(u);
            //    }
            //}
            if (subscripeToAudioInSuccess == false && UPdateStateCounterAudioIn < 2)
            {
                UPdateStateCounterAudioIn++;
                SubscripeToAudioIn();
            }
            if (ct.NumberOfTracks == 0 && ct.CurrentTrackNumber > 0)
            {
                ct.NumberOfTracks = Convert.ToInt16(GetMediaInfoURIMeta()[1]);
                changed = true;
            }
            //Defaultwerte ermitteln
            if (!InitialCheck)
            {
                try
                {
                    if (ct.Volume == 0)
                    {
                        var tvol = GetVolume();
                        if (ct.Volume != tvol)
                        {
                            ct.Volume = tvol;
                            changed = true;
                        }
                    }
                    if (string.IsNullOrEmpty(ct.CurrentPlayMode))
                    {
                        var tplaymode = GetPlaymode();
                        if (!string.IsNullOrEmpty(tplaymode))
                        {
                            ct.CurrentPlayMode = tplaymode;
                            changed = true;
                        }
                    }
                    if (ct.TransportState == PlayerStatus.TRANSITIONING)
                    {
                        var tplaystat = GetPlayerStatus();
                        if (ct.TransportState != tplaystat)
                        {
                            ct.TransportState = tplaystat;
                            ct.TransportStateString = ct.TransportState.ToString();
                            if (IsZoneCoord != true && tplaystat == PlayerStatus.PLAYING)
                            {

                            }
                            else
                            {
                                changed = true;
                            }
                        }
                    }
                    //GroupVolume
                    if (GroupVolume == 0)
                    {
                        var t = GetGroupVolume();
                        if (t != 0)
                        {
                            GroupVolume = t;
                            changed = true;
                        }
                    }

                }
                catch (Exception ex)
                {
                    ServerErrorsAdd("UpdateCurrenStateTrack:InitialCheck", ex);
                }
                InitialCheck = true;
            }
            if (SleepTimerRunning)
            {
                var tsellptimer = GetRemainingSleepTimerDuration();
                if (ct.RemainingSleepTimerDuration != tsellptimer)
                {
                    ct.RemainingSleepTimerDuration = tsellptimer;
                    changed = true;
                }
            }
            if (changed)
            {
                ManuellStateChange(DateTime.Now);
            }
        }
        #endregion Eventing

        #region Funktionen (Public)
        /// <summary>
        /// Aktiviert das Gerät
        /// </summary>
        /// <param name="playerDevice"></param>
        public void SetDevice(UPnPDevice playerDevice)
        {
            Device = playerDevice;
            BaseUrl = Device.RemoteEndPoint.ToString();
            // Subscribe to LastChange event
            SubscribeToEvents();

            // Start a timer that polls for PositionInfo
            StartPolling();
        }

        #region Wecker
        /// <summary>
        /// Ermittelt alle Wecker 
        /// </summary>
        /// <returns></returns>
        private string[] GetAlarms()
        {
            try
            {
                var arguments = new UPnPArgument[2];
                arguments[0] = new UPnPArgument("CurrentAlarmList", null);
                arguments[1] = new UPnPArgument("CurrentAlarmListVersion", null);
                AlarmClock.InvokeAsync("ListAlarms", arguments);
                Thread.Sleep(200);
                string[] result = new String[2];
                result[0] = arguments[0].DataValue.ToString();
                result[1] = arguments[1].DataValue.ToString();
                return result;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetAlarms", ex);
                return null;
            }

        }
        /// <summary>
        /// Liefert eine Liste mit den vorhandenen Weckern.
        /// </summary>
        /// <returns></returns>
        public IList<Alarm> GetAlarmList()
        {
            try
            {
                string[] alarms = GetAlarms();
                return Alarm.Parse(alarms[0]);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetAlarmList", ex);
                return null;
            }
        }
        /// <summary>
        /// Aktualisiert den Übergebenen Wecker
        /// </summary>
        /// <param name="al">Wecker, der angepasst werden soll.</param>
        public Boolean UpdateAlarm(Alarm al)
        {
            try
            {

                var arguments = new UPnPArgument[11];
                arguments[0] = new UPnPArgument("ID", al.ID);
                arguments[1] = new UPnPArgument("StartLocalTime", al.StartTime);
                arguments[2] = new UPnPArgument("Duration", al.Duration);
                arguments[3] = new UPnPArgument("Recurrence", al.Recurrence);
                arguments[4] = new UPnPArgument("Enabled", al.Enabled);
                arguments[5] = new UPnPArgument("RoomUUID", al.RoomUUID);
                arguments[6] = new UPnPArgument("ProgramURI", al.ProgramURI);
                arguments[7] = new UPnPArgument("ProgramMetaData", al.ProgramMetaData);
                arguments[8] = new UPnPArgument("PlayMode", al.PlayMode);
                arguments[9] = new UPnPArgument("Volume", al.Volume);
                arguments[10] = new UPnPArgument("IncludeLinkedZones", al.IncludeLinkedZones);
                AlarmClock.InvokeAsync("UpdateAlarm", arguments);
                return true;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("UpdateAlarm", ex);
                return false;
            }
        }
        /// <summary>
        /// Erstellt den Übergeben Wecker
        /// </summary>
        /// <param name="al">Wecker Objekt, das angepasst werden soll.</param>
        public Boolean CreateAlarm(Alarm al)
        {
            try
            {
                var arguments = new UPnPArgument[11];
                arguments[0] = new UPnPArgument("AssignedID", null);
                arguments[1] = new UPnPArgument("StartLocalTime", al.StartTime);
                arguments[2] = new UPnPArgument("Duration", al.Duration);
                arguments[3] = new UPnPArgument("Recurrence", al.Recurrence);
                arguments[4] = new UPnPArgument("Enabled", al.Enabled);
                arguments[5] = new UPnPArgument("RoomUUID", al.RoomUUID);
                arguments[6] = new UPnPArgument("ProgramURI", al.ProgramURI);
                arguments[7] = new UPnPArgument("ProgramMetaData", al.ProgramMetaData);
                arguments[8] = new UPnPArgument("PlayMode", al.PlayMode);
                arguments[9] = new UPnPArgument("Volume", al.Volume);
                arguments[10] = new UPnPArgument("IncludeLinkedZones", al.IncludeLinkedZones);
                AlarmClock.InvokeAsync("CreateAlarm", arguments);
                return true;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("CreateAlarm", ex);
                return false;
            }
        }
        /// <summary>
        /// Löscht einen übergebenen Alarm aus dem system.
        /// </summary>
        /// <param name="al"></param>
        public Boolean DestroyAlarm(Alarm al)
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("ID", al.ID);
                AlarmClock.InvokeAsync("DestroyAlarm", arguments);
                return true;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("DestroyAlarm", ex);
                return false;
            }

        }
        #endregion Wecker
        #region Schlummermodus
        /// <summary>
        /// Ermittelt den Schlummermodus
        /// </summary>
        /// <returns></returns>
        public String GetRemainingSleepTimerDuration()
        {
            try
            {
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("RemainingSleepTimerDuration", null);
                arguments[2] = new UPnPArgument("CurrentSleepTimerGeneration", null);
                AVTransport.InvokeAsync("GetRemainingSleepTimerDuration", arguments);
                Thread.Sleep(100);
                return (string)arguments[1].DataValue;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetRemainingSleepTimerDuration", ex);
                return "aus";
            }

        }
        /// <summary>
        /// Setzt den Schlummermodus
        /// </summary>
        /// <param name="sleeptimer">Dauer hh:mm:ss oder String.Empty für Aus</param>
        public void SetRemainingSleepTimerDuration(string sleeptimer)
        {
            try
            {
                if (CurrentState != null)
                {
                    CurrentState.RemainingSleepTimerDuration = sleeptimer;
                }
                var arguments = new UPnPArgument[2];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("NewSleepTimerDuration", sleeptimer);
                AVTransport.InvokeAsync("ConfigureSleepTimer", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetRemainingSleepTimerDuration", ex);
            }
        }
        #endregion Schlummermodus
        #region MusicIndex
        /// <summary>
        /// Aktualisiert den Musixindex
        /// </summary>
        public void UpdateMusicIndex()
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("AlbumArtistDisplayOption", String.Empty);
                ContentDirectory.InvokeAsync("RefreshShareIndex", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("UpdateMusicIndex", ex);
            }

        }
        /// <summary>
        /// Prüft, ob der Musikindex gerade aktualisiert wird.
        /// </summary>
        /// <returns></returns>
        public Boolean GetMusicIndexinProgress()
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("IsIndexing", null);
                ContentDirectory.InvokeAsync("GetShareIndexInProgress", arguments);
                Thread.Sleep(200);
                return Convert.ToBoolean(arguments[0].DataValue);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetMusicIndexinProgress", ex);
                return false;
            }
        }
        #endregion MusicIndex
        #region Geraeteinfos
        /// <summary>
        /// Schaltet die LED an und aus.
        /// </summary>
        public void SetLed()
        {
            try
            {
                string d = GetLedState();
                string led = "Off";
                if (d == "Off")
                {
                    led = "On";
                }
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("DesiredLEDState", led);
                DevicePropertie.InvokeAsync("SetLEDState", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetLED", ex);
            }
        }
        /// <summary>
        /// Ermittelt ob die LED Lampe aktiv ist.
        /// </summary>
        /// <returns></returns>
        public string GetLedState()
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("CurrentLEDState", null);
                DevicePropertie.InvokeAsync("GetLEDState", arguments);
                Thread.Sleep(500);
                return arguments[0].DataValue.ToString();
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetLED", ex);
                return String.Empty;
            }

        }
        /// <summary>
        /// Player aus vorhandene Zonen entfernen.
        /// </summary>
        public void BecomeCoordinatorofStandaloneGroup()
        {
            try
            {
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("DelegatedGroupCoordinatorID", null);
                arguments[2] = new UPnPArgument("NewGroupID", null);
                AVTransport.InvokeAsync("BecomeCoordinatorOfStandaloneGroup", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("BecomeCoordinatorofStandaloneGroup", ex);
            }
        }
        #endregion Geraeteinfos
        #region Wiedergabeliste speichern
        /// <summary>
        /// Speichert die Wiedergabeliste als Sonoswiedergabeliste
        /// </summary>
        /// <param name="_title">Titel der Wiedergabeliste</param>
        public void SaveQueue(string _title)
        {
            try
            {
                var arguments = new UPnPArgument[4];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("Title", _title);
                arguments[2] = new UPnPArgument("ObjectID", null);
                arguments[3] = new UPnPArgument("AssignedObjectID", null);
                AVTransport.InvokeAsync("SaveQueue", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SaveQueue:" + _title, ex);
            }
        }
        /// <summary>
        /// Überschreibt die übergebene ID der Wiedergabeliste mit der aktuellen Wiedergabeliste und dem übergebenen Titel
        /// </summary>
        /// <param name="_title">Titel der Wiedergabeliste</param>
        /// <param name="_id">ID der vorhandenen Wiedergabeliste.</param>
        public void SaveQueue(string _title, string _id)
        {
            try
            {
                var arguments = new UPnPArgument[4];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("Title", _title);
                arguments[2] = new UPnPArgument("ObjectID", _id);
                arguments[3] = new UPnPArgument("AssignedObjectID", _id);
                AVTransport.InvokeAsync("SaveQueue", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SaveQueuewithID:" + _title + " Id:" + _id, ex);
            }
        }
        #endregion Wiedergabeliste speichern
        #region SongPlaylistenInfos
        /// <summary>
        /// Liefert den Aktuellen Song
        /// </summary>
        /// <returns></returns>
        public PlayerInfo GetAktSongInfo()
        {
            try
            {
                if (AVTransport == null)
                {
                    ServerErrorsAdd("GetAktSongInfo:AvTRansport", new Exception("AVTransport ist null"));
                    return null;
                }
                var arguments = new UPnPArgument[9];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("Track", 0u);
                arguments[2] = new UPnPArgument("TrackDuration", null);
                arguments[3] = new UPnPArgument("TrackMetaData", null);
                arguments[4] = new UPnPArgument("TrackURI", null);
                arguments[5] = new UPnPArgument("RelTime", null);
                arguments[6] = new UPnPArgument("AbsTime", null);
                arguments[7] = new UPnPArgument("RelCount", 0);
                arguments[8] = new UPnPArgument("AbsCount", 0);
                AVTransport.InvokeSync("GetPositionInfo", arguments);

                TimeSpan trackDuration;
                TimeSpan relTime;

                TimeSpan.TryParse((string)arguments[2].DataValue, out trackDuration);
                TimeSpan.TryParse((string)arguments[5].DataValue, out relTime);
                return new PlayerInfo
                {
                    TrackIndex = Convert.ToInt32(arguments[1].DataValue.ToString()),
                    TrackMetaData = (string)arguments[3].DataValue,

                    TrackURI = (string)arguments[4].DataValue,
                    TrackDuration = trackDuration,
                    RelTime = relTime
                };
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetAktSongInfo", ex);
                return null;
            }
        }
        /// <summary>
        /// Löscht die Playlist
        /// </summary>
        public void RemoveAllTracksFromQueue()
        {
            SetPause();
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                AVTransport.InvokeAsync("RemoveAllTracksFromQueue", arguments);
                Thread.Sleep(400);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("RemoveAllTracksFromQueue", ex);
            }
        }
        /// <summary>
        /// Fügt einen Song/Playlist zur aktuellen Wiedergabe zu
        /// </summary>
        /// <param name="track">Ein SonosTrack</param>
        /// <param name="asNext">Wiedergabe als nächstes</param>
        /// <returns>Liefert die Songnummer des hinzugefügten Songs zurück</returns>
        public uint Enqueue(SonosItem track, bool asNext = false)
        {
            try
            {
                var arguments = new UPnPArgument[8];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("EnqueuedURI", track.Uri);
                arguments[2] = new UPnPArgument("EnqueuedURIMetaData", track.MetaData);
                arguments[3] = new UPnPArgument("DesiredFirstTrackNumberEnqueued", 0u);
                arguments[4] = new UPnPArgument("EnqueueAsNext", asNext);
                arguments[5] = new UPnPArgument("FirstTrackNumberEnqueued", null);
                arguments[6] = new UPnPArgument("NumTracksAdded", null);
                arguments[7] = new UPnPArgument("NewQueueLength", null);
                AVTransport.InvokeSync("AddURIToQueue", arguments);
                return (uint)arguments[5].DataValue;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("Enqueue:" + track.Uri, ex);
                return 0;
            }
        }
        /// <summary>
        /// Fügt mehrere Songs/Playlisten hinzu.
        /// </summary>
        /// <param name="numberoftracks">Anzahl der Tracks</param>
        /// <param name="tracks">Tracks</param>
        /// <param name="asNext">true = ersetzten, false = hinzufügen.</param>
        /// <returns>Liefert die Songnummer des hinzugefügten Songs zurück</returns>
        public uint EnqueueMulti(uint numberoftracks, SonosItem tracks, bool asNext = false)
        {
            try
            {
                var arguments = new UPnPArgument[13];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("UpdateID", 0u);
                arguments[2] = new UPnPArgument("NumberOfURIs", numberoftracks);
                arguments[3] = new UPnPArgument("EnqueuedURIs", tracks.Uri);
                arguments[4] = new UPnPArgument("EnqueuedURIsMetaData", tracks.MetaData);
                arguments[5] = new UPnPArgument("ContainerURI", String.Empty);
                arguments[6] = new UPnPArgument("ContainerMetaData", String.Empty);
                arguments[7] = new UPnPArgument("DesiredFirstTrackNumberEnqueued", 0u);
                arguments[8] = new UPnPArgument("EnqueueAsNext", asNext);
                arguments[9] = new UPnPArgument("FirstTrackNumberEnqueued", null);
                arguments[10] = new UPnPArgument("NumTracksAdded", null);
                arguments[11] = new UPnPArgument("NewQueueLength", null);
                arguments[12] = new UPnPArgument("NewUpdateID", null);
                AVTransport.InvokeSync("AddMultipleURIsToQueue", arguments);

                return (uint)arguments[9].DataValue;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("EnqueueMulti:" + numberoftracks, ex);
                return 0;
            }
        }
        /// <summary>
        /// Springt zur angegeben Position innerhalb eines Songs
        /// </summary>
        /// <param name="position">String in der Angabe hh:mm:ss</param>
        public void Seek(string position)
        {
            try
            {
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("Unit", "REL_TIME");
                arguments[2] = new UPnPArgument("Target", position);
                AVTransport.InvokeAsync("Seek", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("Seek:" + position, ex);
            }
        }
        /// <summary>
        /// Spielt den angegeben Track in einer Playlist ab.
        /// </summary>
        /// <param name="songnumber">Nummer des Songs in der Liste beginnend mit 1</param>
        public void SetTrackInPlaylist(string songnumber)
        {
            try
            {
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("Unit", "TRACK_NR");
                arguments[2] = new UPnPArgument("Target", songnumber);
                AVTransport.InvokeAsync("Seek", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetTRackInPlaylist:" + songnumber, ex);
            }
        }
        /// <summary>
        /// Entfernt einen song aus der aktuellen Wiedergabeliste
        /// </summary>
        /// <param name="songnumber">Nummer des Songs aus der Liste beginned mit 1</param>
        public void RemoveFromQueue(string songnumber)
        {
            try
            {
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("ObjectID", "Q:0/" + songnumber);
                arguments[2] = new UPnPArgument("UpdateID", 0u);
                AVTransport.InvokeAsync("RemoveTrackFromQueue", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("RemovefromQueue:" + songnumber, ex);
            }
        }

        /// <summary>
        /// Setzt einen Song als aktuellen Song unabhängig von der Playlist, wird benötigt, wenn kein Song vorhanden ist (Stromlos)
        /// oder man mit Internetradio arbeiten möchte. Beim Replace von der Playlist wird das benötigt.
        /// </summary>
        /// <param name="_uri">Wenn man noch keinen Song hatte z.b. nach einen neu Start muß man die akktuelle Queue in form von x-rincon-queue:UUID#0 übergeben</param>
        /// <param name="CurrentURIMetaData">Optional die Metadaten</param>
        public void SetAVTransportURI(string _uri, string CurrentURIMetaData = null)
        {
            try
            {
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("CurrentURI", _uri);
                arguments[2] = new UPnPArgument("CurrentURIMetaData", CurrentURIMetaData);
                AVTransport.InvokeAsync("SetAVTransportURI", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetAVTransportURI:" + _uri, ex);
            }
        }
        /// <summary>
        /// Liefert die aktuelle Playlist in Form einer Sonositem Liste zurück. 
        /// </summary>
        /// <param name="startindex">Wert bei dem begonnen wird. Default 0</param>
        /// <param name="maxresult">Anzahl der maximalen Ergebnisse. Default 100</param>
        /// 
        /// <returns></returns>
        public IList<SonosItem> GetPlaylist(UInt32 startindex, UInt32 maxresult = 100u)
        {
            var xml = Browse("Q:0", startindex, maxresult);
            var xmlresult1 = SonosItem.Parse(xml[0]);
            return xmlresult1;
        }

        /// <summary>
        /// Erhalte die angeforderte Playlist mit einem Array der Gesamtzahl
        /// </summary>
        /// <param name="startindex"></param>
        /// <param name="maxresult"></param>
        /// <param name="threadSleep"></param>
        /// <returns></returns>
        public string[] GetPlaylistWithTotalNumbers(UInt32 startindex, UInt32 maxresult = 100u,int threadSleep =0)
        {
            return Browse("Q:0", startindex, maxresult, "BrowseDirectChildren",threadSleep);
        }
        /// <summary>
        /// Liefert eine Liste der Favoriten
        /// </summary>
        /// <returns></returns>
        public virtual IList<SonosItem> GetFavorites()
        {
            var xml = Browse("FV:2");
            var tracks = SonosItem.Parse(xml[0]);
            return tracks;
        }
        /// <summary>
        /// Zerstört ein übergebenes Element wie z.B. ein Eintrag aus der Favoriten Liste
        /// <param name="item">Objet zum vernichten</param>
        /// </summary>
        public virtual void DestroyObject(string item)
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("ObjectID", item);
                ContentDirectory.InvokeAsync("DestroyObject", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("DestroyObject", ex);
            }
        }
        /// <summary>
        /// Erzeugt ein Objekt, welches über den container definiert wird
        /// </summary>
        /// <param name="metadata">Sonos MetaDaten</param>
        /// <param name="container">Container in dem das Objekt erstellt werden soll. Default wird ein Favorit erzeugt.</param>
        public virtual void CreateObject(string metadata, string container = "FV:2")
        {
            try
            {
                var arguments = new UPnPArgument[4];
                arguments[0] = new UPnPArgument("ContainerID", container);
                arguments[1] = new UPnPArgument("Elements", metadata);
                arguments[2] = new UPnPArgument("ObjectID", null);
                arguments[3] = new UPnPArgument("Result", null);
                ContentDirectory.InvokeAsync("CreateObject", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("CreateObject", ex);
            }
        }
        /// <summary>
        /// Generiert aufgrund der Übergabe ein Favoriten DIDL
        /// </summary>
        /// <param name="objectID">Sonos ID des zufügenden Elements</param>
        /// <returns></returns>
        public virtual void CreateFavorite(String objectID)
        {
            String description;
            String didlmd;
            String didlstring;
            String didlmdItemID;
            String didlstringItemId;
            //Prüfen, ob es ein Song ist
            if (objectID.StartsWith("x-file-cifs"))
            {
                objectID = objectID.Replace("x-file-cifs", "S");
            }

            SonosItem item = BrowsingMeta(objectID)[0];
            switch (item.ClassType)
            {
                //hie nun die upno Class ermitteln
                case "object.container.genre.musicGenre":
                case "object.container.person.musicArtist":
                case "object.container.playlistContainer":
                    if (item.ClassType.EndsWith("musicGenre"))
                    {
                        description = "Musikrichtung";
                        didlmdItemID = item.ContainerID;
                        didlstringItemId = "x-rincon-playlist:"+UUID+"#" + item.ContainerID;
                    }
                    else if (item.ClassType.EndsWith("musicArtist"))
                    {
                        description = "Interpret";
                        didlmdItemID = item.ContainerID;
                        didlstringItemId = "x-rincon-playlist:" + UUID + "#" + item.ContainerID;
                    }
                    else
                    {
                        description = "Musikbibliothek Playliste";
                        didlmdItemID = item.ItemID;
                        didlstringItemId = item.Uri;
                    }
                    didlmd = "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">" +
                             "<item id=\"" + didlmdItemID + "\" parentID=\"" + item.ParentID + "\" restricted=\"true\"><dc:title>" + item.Title + "</dc:title><upnp:class>" + item.ClassType + "</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">RINCON_AssociatedZPUDN</desc></item></DIDL-Lite>";
                    didlstring = "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\"    xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\"    xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\"    xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">" +
                                 "<item><dc:title>" + item.Title + "</dc:title>" +
                                 "<r:type>instantPlay</r:type><res protocolInfo=\"x-rincon-playlist:*:*:*\">" + didlstringItemId + "</res><r:description>" + description + "</r:description>" +
                                 "<r:resMD>" + HttpUtility.HtmlEncode(didlmd) + "</r:resMD></item></DIDL-Lite>";
                    break;
                case "object.item.audioItem.musicTrack":
                case "object.container.album.musicAlbum":
                    SonosItem parentItem = BrowsingMeta(item.ParentID)[0];
                    if (item.ClassType.EndsWith("musicTrack"))
                    {
                        description = "Track von " + item.Artist;
                        didlmdItemID = item.ItemID;
                        didlstringItemId = item.Uri;
                    }
                    else
                    {
                        description = "Album von " + parentItem.Title;
                        didlmdItemID = item.ContainerID;
                        didlstringItemId = "x-rincon-playlist:" + UUID + "#" + item.ContainerID;
                    }

                    didlmd = "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">" +
                             "<item id=\"" + didlmdItemID + "\" parentID=\"" + item.ParentID + "\" restricted=\"true\"><dc:title>" + item.Title + "</dc:title><upnp:class>" + item.ClassType + "</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">RINCON_AssociatedZPUDN</desc></item></DIDL-Lite>";
                    didlstring = "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\"    xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\"    xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\"    xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">" +
                                 "<item><dc:title>" + item.Title + "</dc:title><r:type>instantPlay</r:type>" + "<upnp:albumArtURI>" + item.AlbumArtURI.Replace("&", "&amp;") + "</upnp:albumArtURI>" +
                                 "<res protocolInfo=\"x-rincon-playlist:*:*:*\">" + didlstringItemId + "</res><r:description>" + description + "</r:description>" +
                                 "<r:resMD>" + HttpUtility.HtmlEncode(didlmd) + "</r:resMD></item></DIDL-Lite>";
                    break;
                default:
                    return;

            }
            CreateObject(didlstring);
        }

        /// <summary>
        /// Liefert alle Wiedergabelisten arten
        /// </summary>
        /// <returns>Liste mit Importierten und Sonos Wiedergabelisten</returns>
        public virtual IList<SonosItem> GetallPlaylist()
        {
            var xml = Browse("SQ:");
            var tracks = SonosItem.Parse(xml[0]);
            var xml2 = Browse("A:PLAYLISTS");
            var tracks2 = SonosItem.Parse(xml2[0]);
            return tracks.Union(tracks2).ToList();
        }
        /// <summary>
        /// Liefert alle Wiedergabelisten arten
        /// </summary>
        /// <returns>Liste mit Importierten und Sonos Wiedergabelisten</returns>
        public virtual IList<SonosItem> GetSonosPlaylists()
        {
            var xml = Browse("SQ:");
            return SonosItem.Parse(xml[0]);
        }
        /// <summary>
        /// Liefert die Anzahl der Tracks einer Playlist zurück.
        /// </summary>
        /// <returns></returns>
        public int GetMediaInfo()
        {
            try
            {
                var arguments = new UPnPArgument[10];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("NrTracks", 0u);
                arguments[2] = new UPnPArgument("MediaDuration", null);
                arguments[3] = new UPnPArgument("CurrentURI", null);
                arguments[4] = new UPnPArgument("CurrentURIMetaData", null);
                arguments[5] = new UPnPArgument("NextURI", null);
                arguments[6] = new UPnPArgument("NextURIMetaData", null);
                arguments[7] = new UPnPArgument("PlayMedium", null);
                arguments[8] = new UPnPArgument("RecordMedium", null);
                arguments[9] = new UPnPArgument("WriteStatus", null);
                AVTransport.InvokeSync("GetMediaInfo", arguments);

                return Convert.ToInt32(arguments[1].DataValue.ToString());
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetMediaInfo", ex);
                return 0;
            }
        }
        /// <summary>
        /// Liefert die MetaDaten und URI zurück zurück
        /// </summary>
        /// <returns>Liste Eintrag1: URI und Eintrag 2 TRackNumber</returns>
        public List<String> GetMediaInfoURIMeta()
        {
            try
            {
                var arguments = new UPnPArgument[10];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("NrTracks", 0u);
                arguments[2] = new UPnPArgument("MediaDuration", null);
                arguments[3] = new UPnPArgument("CurrentURI", null);
                arguments[4] = new UPnPArgument("CurrentURIMetaData", null);
                arguments[5] = new UPnPArgument("NextURI", null);
                arguments[6] = new UPnPArgument("NextURIMetaData", null);
                arguments[7] = new UPnPArgument("PlayMedium", null);
                arguments[8] = new UPnPArgument("RecordMedium", null);
                arguments[9] = new UPnPArgument("WriteStatus", null);
                AVTransport.InvokeSync("GetMediaInfo", arguments);

                List<String> data = new List<string> { (string)arguments[3].DataValue, arguments[1].DataValue.ToString(), arguments[4].DataValue.ToString() };
                return data;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetMediaInfoURIMeta", ex);
                return new List<string>();
            }

        }
        /// <summary>
        /// Ändern der Reihenfolge in der aktuellen Playlist
        /// </summary>
        /// <param name="oldposition">Alte Position</param>
        /// <param name="newposition">Neue Position</param>
        public void ReorderTracksinQueue(int oldposition, int newposition)
        {
            try
            {
                var arguments = new UPnPArgument[5];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("StartingIndex", Convert.ToUInt32(oldposition));
                arguments[2] = new UPnPArgument("NumberOfTracks", 1u);
                arguments[3] = new UPnPArgument("InsertBefore", Convert.ToUInt32(newposition));
                arguments[4] = new UPnPArgument("UpdateID", 0u);
                AVTransport.InvokeSync("ReorderTracksInQueue", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("ReorderTracksinQueue:" + oldposition + " New:" + newposition, ex);
            }

        }
        #endregion SongPlaylistenInfos
        #region WiedergabeEinstellungen
        /// <summary>
        /// Wiedergabemodus definieren
        /// </summary>
        /// <param name="playmode">NORMAL,REPEAT_ALL,SHUFFLE_NOREPEAT,SHUFFLE</param>
        public void SetPlayMode(string playmode)
        {
            try
            {
                if (playmode != "NORMAL" && playmode != "REPEAT_ALL" && playmode != "SHUFFLE_NOREPEAT" &&
                    playmode != "SHUFFLE") return;
                var arguments = new UPnPArgument[2];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("NewPlayMode", playmode);
                AVTransport.InvokeAsync("SetPlayMode", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetPlayMode:" + playmode, ex);
            }
        }
        /// <summary>
        /// Startet die Wiedergabe
        /// </summary>
        public void SetPlay()
        {
            try
            {
                var arguments = new UPnPArgument[2];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("Speed", "1");
                AVTransport.InvokeAsync("Play", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetPlay", ex);
            }
        }
        /// <summary>
        /// Stoppt die Wiedergabe
        /// </summary>
        public void SetStop()
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                AVTransport.InvokeAsync("Stop", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetStop", ex);
            }
        }
        /// <summary>
        /// Nächsten Song abspielen
        /// </summary>
        public void SetPlayNext()
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                AVTransport.InvokeAsync("Next", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetPlayNext", ex);
            }
        }
        /// <summary>
        /// Vorherigen Song abspielen
        /// </summary>
        public void SetPlayPrevious()
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                AVTransport.InvokeAsync("Previous", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetPlayPrev", ex);
            }
        }
        /// <summary>
        /// Paussiert den SonosPlayer
        /// </summary>
        public void SetPause()
        {
            try
            {
                var arguments = new UPnPArgument[1];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                AVTransport.InvokeAsync("Pause", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetPause", ex);
            }
        }
        /// <summary>
        /// Liefert die Wiedergabeart zurück
        /// </summary>
        /// <returns></returns>
        public string GetPlaymode()
        {
            try
            {
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("PlayMode", null);
                arguments[2] = new UPnPArgument("RecQualityMode", null);
                AVTransport.InvokeSync("GetTransportSettings", arguments);
                Thread.Sleep(100);
                return (string)arguments[1].DataValue;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetPlaymode", ex);
                return String.Empty;
            }
        }
        /// <summary>
        /// Liefert den Überblendungs Modus zurück
        /// </summary>
        /// <returns></returns>
        public Boolean GetCrossfadeMode()
        {
            try
            {
                var arguments = new UPnPArgument[2];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("CrossfadeMode", null);
                AVTransport.InvokeSync("GetCrossfadeMode", arguments);
                return (Boolean)arguments[1].DataValue;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetCrossfadeMode", ex);
                return false;
            }
        }
        /// <summary>
        /// Setzen des Überblenden
        /// </summary>
        /// <param name="v"></param>
        public void SetCrossfadeMode(Boolean v)
        {
            try
            {
                var arguments = new UPnPArgument[2];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("CrossfadeMode", v);
                AVTransport.InvokeSync("SetCrossfadeMode", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetCrossfadeMode", ex);
            }
        }
        /// <summary>
        /// Liefert zurück, ob ein Gerät Pause macht oder gerade abspielt.
        /// </summary>
        /// <returns></returns>
        public PlayerStatus GetPlayerStatus()
        {
            try
            {
                if (AVTransport == null)
                {
                    return PlayerStatus.STOPPED;
                }
                var arguments = new UPnPArgument[4];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("CurrentTransportState", "");
                arguments[2] = new UPnPArgument("CurrentTransportStatus", "");
                arguments[3] = new UPnPArgument("CurrentSpeed", "");
                Thread.Sleep(100);
                AVTransport.InvokeSync("GetTransportInfo", arguments);
                //PlayerStatus status;
                switch ((string)arguments[1].DataValue)
                {
                    case "PLAYING":
                        return PlayerStatus.PLAYING;
                    case "PAUSED":
                    case "PAUSED_PLAYBACK":
                        return PlayerStatus.PAUSED_PLAYBACK;
                    case "STOPPED":
                        return PlayerStatus.STOPPED;
                    default:
                        return PlayerStatus.TRANSITIONING;
                }
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetPlayerStatus", ex);
                return PlayerStatus.TRANSITIONING;
            }
        }
        #endregion WiedergabeEinstellungen
        #region Volumne
        /// <summary>
        /// Lautstärke anpassen (Wert von 0 - 100)
        /// </summary>
        public void SetVolume(UInt16 vol)
        {
            try
            {
                if (vol > 0 && vol < 101)
                {
                    var arguments = new UPnPArgument[3];
                    arguments[0] = new UPnPArgument("InstanceID", 0u);
                    arguments[1] = new UPnPArgument("Channel", "Master");
                    arguments[2] = new UPnPArgument("DesiredVolume", vol);
                    RenderingControl.InvokeAsync("SetVolume", arguments);
                }
                else { throw new Exception("The Volume is out of Range"); }
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetVolume:" + vol, ex);
            }
        }
        /// <summary>
        /// Setzt die Gruppenlautstärke
        /// </summary>
        /// <param name="vol"></param>
        public void SetGroupVolume(UInt16 vol)
        {
            try
            {
                if (vol > 0 && vol < 101)
                {
                    var arguments = new UPnPArgument[2];
                    arguments[0] = new UPnPArgument("InstanceID", 0u);
                    arguments[1] = new UPnPArgument("DesiredVolume", vol);
                    GroupRenderingControl.InvokeAsync("SetGroupVolume", arguments);
                }
                else { throw new Exception("The Volume is out of Range"); }
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetGroupVolume:" + vol, ex);
            }
        }
        public int GetGroupVolume()
        {
            try
            {
                var arguments = new UPnPArgument[2];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("CurrentVolume", null);
                GroupRenderingControl.InvokeAsync("GetGroupVolume", arguments);
                Thread.Sleep(300);
                if (arguments[1].DataValue == null || String.IsNullOrEmpty(arguments[1].DataValue.ToString()))
                {
                    return 0;
                }
                return Convert.ToInt32(arguments[1].DataValue);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetGroupVolume:", ex);
                return 0;
            }
        }
        /// <summary>
        /// Lautstärke ermitteln
        /// </summary>
        public int GetVolume()
        {
            try
            {
                if (RenderingControl == null)
                {
                    ServerErrorsAdd("GetVolume:KeinRenderingControl", new Exception("GetVolume:Kein RenderingControl beim Player gefunden"));
                    return 0;

                }
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("Channel", "Master");
                arguments[2] = new UPnPArgument("CurrentVolume", null);
                RenderingControl.InvokeAsync("GetVolume", arguments);
                Thread.Sleep(300);
                if (arguments[2].DataValue == null || String.IsNullOrEmpty(arguments[2].DataValue.ToString()))
                {
                    return 0;
                }
                return Convert.ToInt32(arguments[2].DataValue);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetVolume", ex);
                return 0;
            }
        }
        /// <summary>
        /// Stummschaltung
        /// </summary>
        public void SetMute()
        {
            try
            {
                var d = !Mute;
                var arguments = new UPnPArgument[3];
                arguments[0] = new UPnPArgument("InstanceID", 0u);
                arguments[1] = new UPnPArgument("Channel", "Master");
                arguments[2] = new UPnPArgument("DesiredMute", d);
                RenderingControl.InvokeAsync("SetMute", arguments);
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("SetMute:" + !Mute, ex);
            }
        }
        /// <summary>
        /// Ermittelt ob Stummschaltung aktiv ist.
        /// </summary>
        /// <returns></returns>
        public Boolean GetMute()
        {
            try
            {
                var arguments1 = new UPnPArgument[3];
                arguments1[0] = new UPnPArgument("InstanceID", 0u);
                arguments1[1] = new UPnPArgument("Channel", "Master");
                arguments1[2] = new UPnPArgument("CurrentMute", null);
                RenderingControl.InvokeAsync("GetMute", arguments1);
                return Convert.ToBoolean(arguments1[2].DataValue);

            }
            catch (Exception ex)
            {
                ServerErrorsAdd("GetMute", ex);
                return false;
            }

        }
        #endregion Volumne
        #region Browsing
        /// <summary>
        /// Durchsucht das Sonos Gerät nach angegeben Parametern
        /// </summary>
        /// <param name="_search">S: = Shares,A: Übersicht Musikbereich,SQ: Sonos Playlisten,SV: Sonos Favoriten  </param>
        /// <returns></returns>
        public IList<SonosItem> Browsing(string _search)
        {
            var xml = Browse(_search);
            return SonosItem.Parse(xml[0]);
        }

        /// <summary>
        /// Durchsucht das Sonos Gerät nach angegeben Parametern
        /// </summary>
        /// <param name="_search">S: = Shares,A: Übersicht Musikbereich,SQ: Sonos Playlisten,SV: Sonos Favoriten  </param>
        /// <param name="limit">Limitiert die zurück gegebenen Ergebnisse</param>
        /// <returns></returns>
        public IList<SonosItem> BrowsingWithLimitResults(string _search, uint limit)
        {
            var xml = Browse(_search, 0, limit);
            return SonosItem.Parse(xml[0]);
        }
        /// <summary>
        /// Liefert die Metadata für das entsprechende element zurück.
        /// </summary>
        /// <param name="_search"></param>
        /// <returns></returns>
        public IList<SonosItem> BrowsingMeta(string _search)
        {
            var xml = Browse(_search, 0u, 0u, "BrowseMetadata");
            return SonosItem.Parse(xml[0]);
        }
        #endregion Browsing
        #endregion Funktionen (Public)

        #region Funktionen (Private)
        /// <summary>
        /// String zu Zeit
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private TimeSpan ParseDuration(string value)
        {
            if (string.IsNullOrEmpty(value))
                return TimeSpan.FromSeconds(0);
            return TimeSpan.Parse(value);
        }

        /// <summary>
        /// Ermöglicht das durchsuchen
        /// </summary>
        /// <param name="action">Übergabeparameter nachdem gesucht werden soll</param>
        /// <param name="startingIndex">Optional die _Zahl ab der wieder verarbeitet werden soll, falls mehr als 540 Einträge vorhanden sind</param>
        /// <param name="requestedCount">Anzahl der Songs, die maximal zurück geleifert werden sollen.</param>
        /// <param name="browseFlag">BrowseDirectChildren/BrowseMetadata</param>
        /// <param name="sleep"></param>
        /// <returns>Stringarray 0 = XML mit Items 1= Anzahl der komplet vorhandene</returns>
        private string[] Browse(string action, UInt32 startingIndex = 0u, UInt32 requestedCount = 0u, string browseFlag = "BrowseDirectChildren", int sleep=0)
        {
            try
            {
                if (ContentDirectory == null) return new[] { String.Empty };

                var arguments = new UPnPArgument[10];
                arguments[0] = new UPnPArgument("ObjectID", action);
                arguments[1] = new UPnPArgument("BrowseFlag", browseFlag);
                arguments[2] = new UPnPArgument("Filter", "");
                arguments[3] = new UPnPArgument("StartingIndex", startingIndex);
                arguments[4] = new UPnPArgument("RequestedCount", requestedCount);
                arguments[5] = new UPnPArgument("SortCriteria", "");
                arguments[6] = new UPnPArgument("Result", "");
                arguments[7] = new UPnPArgument("NumberReturned", 0u);
                arguments[8] = new UPnPArgument("TotalMatches", 0u);
                arguments[9] = new UPnPArgument("UpdateID", 0u);

                ContentDirectory.InvokeSync("Browse", arguments);
                if (sleep != 0)
                {
                    Thread.Sleep(sleep);
                }
                else
                {
                    Thread.Sleep(200);
                }
                string[] result = new String[2];
                result[0] = arguments[6].DataValue as string;
                result[1] = Convert.ToString(arguments[8].DataValue);

                return result;
            }
            catch (Exception ex)
            {
                ServerErrorsAdd("Browse:" + action, ex);
                return new string[0];
            }
        }
        #endregion Funktionen (Private)

        #region Eigenschaften
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string UUID { get; set; }
        /// <summary>
        /// Eigenschaft welches Gerät aktiv ist
        /// </summary>
        public UPnPDevice Device { get; set; }
        /// <summary>
        /// Liefert Informationen des Aktuellen Gerätes (Aktueller Song, Spielen oder pausierend, Position des aktuellen Songs)
        /// </summary>
        [DataMember]
        public PlayerState CurrentState { get; set; }
        /// <summary>
        /// Pfadangaben des Sonos Geräts (ohne XML)
        /// </summary>
        [DataMember]
        public string BaseUrl { get; set; }
        /// <summary>
        /// Definiert, ob es ein AudioIn Eingang gibt und dieser genutzt wird. 
        /// </summary>
        [DataMember]
        public Boolean HasAudioIn { get; set; }
        /// <summary>
        /// Gruppenlautstärke
        /// </summary>
        [DataMember]
        public int GroupVolume { get; set; }
        /// <summary>
        /// Stummschaltung
        /// </summary>
        [DataMember]
        public Boolean Mute { get; set; }
        /// <summary>
        /// Prüft, ob der Defaultwerte Wert schon Initial ermittelt wurden. 
        /// </summary>
        public Boolean InitialCheck { get; private set; }
        /// <summary>
        /// Liefert ob ein Sleeptimer am laufen ist.
        /// </summary>
        public Boolean SleepTimerRunning { get; private set; }
        /// <summary>
        /// Liefert eine Liste von Alarmen
        /// </summary>
        public IList<Alarm> Alarms
        {
            get
            {
                try
                {
                    return GetAlarmList().OrderBy(o => o.RoomUUID).ToList();
                }
                catch (Exception ex)
                {
                    ServerErrorsAdd("Alarms", ex);
                }
                return new List<Alarm>();
            }
        }
        /// <summary>
        /// Uri des Players, wird für das Discovery Fallback genutzt
        /// </summary>
        public Uri DeviceLocation { get; set; }
        /// <summary>
        /// Rating Filter für das durchsuchen. Ist der Filter aktiv, wird dieser genommen um beim Browsen Songs zu filtern.
        /// </summary>
        [DataMember]
        public SonosRatingFilter RatingFilter { get; set; } = new SonosRatingFilter();

        /// <summary>
        /// Liefert zurück, ob der Player alleine ist oder in einer Zone. True = Alleine
        /// Kann auch Null sein, da das Event evtl. noch nicht befeuert wurde.
        /// </summary>
        [DataMember]
        public Boolean? IsZoneCoord { get; set; }
        #endregion Eigenschaften

        #region Eigenschaften (UPNP)

        private void LoadDevice()
        {
            if (ControlPoint != null && !string.IsNullOrEmpty(DeviceLocation.ToString()))
            {
                ControlPoint.ForceDeviceAddition(DeviceLocation);
            }
        }
        /// <summary>
        /// Liefert den Devicepropertie zurück (UPNP)
        /// </summary>
        public UPnPService DevicePropertie
        {
            get
            {
                if (devicepropertie != null)
                    return devicepropertie;
                if (Device == null)
                {
                    LoadDevice();
                    return null;
                }
                devicepropertie = Device.GetService("urn:upnp-org:serviceId:DeviceProperties");
                return devicepropertie;
            }
        }
        /// <summary>
        /// Liefert den GroupManagement zurück (UPNP)
        /// </summary>
        public UPnPService GroupManagement
        {
            get
            {
                if (groupManagement != null)
                    return groupManagement;
                if (Device == null)
                {
                    LoadDevice();
                    return null;
                }
                groupManagement = Device.GetService("urn:upnp-org:serviceId:GroupManagement");

                return groupManagement;
            }
        }
        /// <summary>
        /// Zonenverwaltung (Liefert zurück, ob ein Player in einer Zone ist.
        /// </summary>
        public UPnPService ZoneGroupTopologie
        {
            get
            {
                if (zonegroupTopologie != null)
                    return zonegroupTopologie;
                if (Device == null)
                {
                    LoadDevice();
                    return null;
                }
                zonegroupTopologie = Device.GetService("urn:upnp-org:serviceId:ZoneGroupTopology");
                return zonegroupTopologie;
            }
        }
        /// <summary>
        /// Liefert den AudioIn Service zurück.
        /// </summary>
        public UPnPService AudioIn
        {
            get
            {
                if (audioIn != null)
                    return audioIn;
                if (Device == null)
                {
                    LoadDevice();
                    return null;
                }
                audioIn = Device.GetService("urn:upnp-org:serviceId:AudioIn");
                //audioIn = Device.GetServices("urn:upnp-org:serviceId:AudioIn")[0];
                return audioIn;
            }
        }

        //todo: Wenn Null muss ein fallback gebaut werden,damit dies nicht mehr null ist. Gilt für alle UPNPDevices
        /// <summary>
        /// Liefert den MediaRenderer zurück (Benutzt von RenderingControl und AVTransport) (UPNP)
        /// </summary>
        public UPnPDevice MediaRenderer
        {
            get
            {
                if (mediaRenderer != null)
                    return mediaRenderer;
                if (Device == null)
                {
                    LoadDevice();
                    return null;
                }
                mediaRenderer = Device.EmbeddedDevices.FirstOrDefault(d => d.DeviceURN == "urn:schemas-upnp-org:device:MediaRenderer:1");
                return mediaRenderer;
            }
        }
        /// <summary>
        /// Liefert den AlarmClock zurück (UPNP)
        /// </summary>
        public UPnPService AlarmClock
        {
            get
            {
                if (alarmclock != null)
                    return alarmclock;
                if (Device == null)
                {
                    LoadDevice();
                    return null;
                }
                alarmclock = Device.GetService("urn:upnp-org:serviceId:AlarmClock");
                return alarmclock;
            }
        }
        /// <summary>
        /// Liefert den Mediaserver zurück (UPNP)
        /// </summary>
        public UPnPDevice MediaServer
        {
            get
            {
                if (mediaServer != null)
                    return mediaServer;
                if (Device == null)
                {
                    LoadDevice();
                    return null;
                }
                mediaServer = Device.EmbeddedDevices.FirstOrDefault(d => d.DeviceURN == "urn:schemas-upnp-org:device:MediaServer:1");
                return mediaServer;
            }
        }
        /// <summary>
        /// Liefert den RenderingControl zurück
        /// </summary>
        public UPnPService RenderingControl
        {
            get
            {
                if (renderingControl != null)
                    return renderingControl;
                if (MediaRenderer == null)
                    return null;
                renderingControl = MediaRenderer.GetService("urn:upnp-org:serviceId:RenderingControl");
                return renderingControl;
            }
        }

        public UPnPService QueueControl
        {
            get
            {
                if (queueControl != null)
                    return queueControl;
                if (MediaRenderer == null)
                    return null;
                queueControl = MediaRenderer.GetService("urn:sonos-com:serviceId:Queue");
                return queueControl;
            }
        }
        public UPnPService GroupRenderingControl
        {
            get
            {
                if (grouprenderingControl != null)
                    return grouprenderingControl;
                if (MediaRenderer == null)
                    return null;
                grouprenderingControl = MediaRenderer.GetService("urn:upnp-org:serviceId:GroupRenderingControl");
                return grouprenderingControl;
            }
        }
        /// <summary>
        /// Liefert den AVTRansport zurück. (Dient zum Übermitteln von Befehlen wie Play und Pause) (UPNP)
        /// </summary>
        public UPnPService AVTransport
        {
            get
            {
                if (avTransport != null)
                    return avTransport;
                if (MediaRenderer == null)
                    return null;
                avTransport = MediaRenderer.GetService("urn:upnp-org:serviceId:AVTransport");
                return avTransport;
            }
        }
        /// <summary>
        /// Unbekannt
        /// </summary>
        public UPnPSmartControlPoint ControlPoint { get; set; }
        /// <summary>
        /// Wird genutzt um die Inhalte zu durchsuchen 
        /// </summary>
        public UPnPService ContentDirectory
        {
            get
            {
                if (contentDirectory != null)
                    return contentDirectory;
                if (MediaServer == null)
                    return null;
                contentDirectory = MediaServer.GetService("urn:upnp-org:serviceId:ContentDirectory");
                return contentDirectory;
            }
        }
        public UPnPService ConnectionManager
        {
            get
            {
                if (connectionManager != null)
                    return connectionManager;
                if (MediaServer == null)
                    return null;
                connectionManager = MediaServer.GetService("urn:upnp-org:serviceId:ConnectionManager");
                return connectionManager;
            }
        }
        #endregion Eigenschaften (UPNP)
    }
}