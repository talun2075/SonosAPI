using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using OSTL.UPnP;
using SonosUPnP;

namespace SonosUPNP
{
    public class SonosDiscovery
    {
        private IList<SonosZone> zones = new List<SonosZone>();
        private IList<SonosPlayer> players = new List<SonosPlayer>();
        private UPnPSmartControlPoint ControlPoint { get; set; }
        private IDictionary<string, UPnPDevice> playerDevices = new Dictionary<string, UPnPDevice>();
        private Timer stateChangedTimer;

        /// <summary>
        /// Initialisierung und Suchen der Sonosgeräte
        /// </summary>
        public virtual void StartScan()
        {
            IsReseted = false;
            ControlPoint = new UPnPSmartControlPoint(OnDeviceAdded, OnServiceAdded, "urn:schemas-upnp-org:device:ZonePlayer:0");
        }
        public virtual void Reset()
        {
            ControlPoint.Devices.Clear();
            IsReseted = true;
        }
        /// <summary>
        /// Wurde ein Reset durchgeführt und muss neu gescannt werden?
        /// </summary>
        public Boolean IsReseted { get; private set; }
        /// <summary>
        /// Alle Geräte als Liste
        /// </summary>
        public IList<SonosPlayer> Players
        {
            get { return players; }
            set { players = value; }
        }
        /// <summary>
        /// Alle Zonen als Liste
        /// </summary>
        public IList<SonosZone> Zones
        {
            get { return zones; }
            set { zones = value; }
        }
        public int PlayerDevices
        {
            get { return playerDevices.Count; }
        }
        public event Action TopologyChanged;

        private void OnServiceAdded(UPnPSmartControlPoint sender, UPnPService service)
        {
        }

        /// <summary>
        /// Wenn ein Gerät gefunden wird, wird dieses den 
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="device"></param>
        private void OnDeviceAdded(UPnPSmartControlPoint cp, UPnPDevice device)
        {
            //Console.WriteLine("found player " + device);
            //players.Add(new SonosPlayer(device));
            // we need to save these for future reference
            lock (playerDevices)
            {
                playerDevices[device.UniqueDeviceName] = device;
            }

            // okay, we will try and notify the players that they have been found now.
            var player = players.FirstOrDefault(p => p.UUID == device.UniqueDeviceName);
            if (player != null)
            {
                player.SetDevice(device);
            }

            // Subscribe to events
            var topologyService = device.GetService("urn:upnp-org:serviceId:ZoneGroupTopology");
            topologyService.Subscribe(600, (service, subscribeok) =>
                {
                    if (!subscribeok) return;

                    var stateVariable = service.GetStateVariableObject("ZoneGroupState");
                    stateVariable.OnModified += OnZoneGroupStateChanged;
                });
        }
        /// <summary>
        /// Eventing, wird benutzt um Änderungen an der Zone zu ermitteln
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="newvalue"></param>
        private void OnZoneGroupStateChanged(UPnPStateVariable sender, object newvalue)
        {
            //Console.WriteLine(sender.Value);

            // Avoid multiple state changes and consolidate them
            if (stateChangedTimer != null)
                stateChangedTimer.Dispose();
            stateChangedTimer = new Timer(state => HandleZoneXML(sender.Value.ToString()), null, TimeSpan.FromMilliseconds(1000),
                                          TimeSpan.FromMilliseconds(-1));
        }
        /// <summary>
        /// Ermittelt die Zonen und die Player.
        /// </summary>
        /// <param name="xml"></param>
        private void HandleZoneXML(string xml)
        {
            var doc = XElement.Parse(xml);
            lock (zones)
            {
                /* Test nicht alle Zonen zu löschen sondern entsprechend zu ersetzen.
                List<String> abgearbeiteteZone = new List<string>(); //Liste mir den UUID der abgearbeiteten XML  Liste
                List<SonosZone> zuloeschendezone = new List<SonosZone>(); //Zonen, die gelöscht werden müssen, weil nicht mehr vorhanden. 
                //todo: Wie sollen zukünftig die Zone.Player gefüllt werden? Löschen oder Aktualisieren
                foreach (var zoneXML in doc.Descendants("ZoneGroup"))
                {
                    
                    var zone = new SonosZone((string)zoneXML.Attribute("Coordinator"));//Zone aus der XML
                    var pllist = zoneXML.Descendants("ZoneGroupMember").Where(x => x.Attribute("Invisible") == null).ToList();//Alle Player aus der XML Liste als XML
                    var playerlist = new List<SonosPlayer>(); //Alle Player aus der XML Liste als Playerliste
                    var playerlisttodelete = new List<SonosPlayer>(); //Player, die aus der vorhadenen Zone gelöscht werden sollen
                    var aktzone = Zones.FirstOrDefault(x => x.CoordinatorUUID == zone.CoordinatorUUID); //Liest aus ob es die XML Zone schon Gibt
                    //Zonen,die neu sind in liste, damit später gelöscht werden kann.
                    abgearbeiteteZone.Add(zone.CoordinatorUUID); 
                    if (aktzone != null)
                    {
                        //Playerliste durchlaufen.
                        if (pllist.Any())
                        {
                            foreach (var playerXml in pllist)
                            {
                                var player = new SonosPlayer
                                {
                                    Name = (string) playerXml.Attribute("ZoneName"),
                                    UUID = (string) playerXml.Attribute("UUID"),
                                    ControlPoint = ControlPoint,
                                    CurrentState = new PlayerState()
                                };
                                playerlist.Add(player);
                            }
                            //Listen fertig nun vergleichen
                            foreach (SonosPlayer splist in aktzone.Players)
                            {
                                var founded = playerlist.Find(x => x.UUID == splist.UUID);
                                if (founded != null)
                                {
                                    //player ist in der liste
                                    playerlist.Remove(founded);
                                }
                                else
                                {
                                    //playerlöschen, die nicht mehr benötigt werden.
                                    playerlisttodelete.Add(splist);
                                }
                            }
                            //cordinator löschen
                            var cofound= playerlist.Find(x => x.UUID == aktzone.CoordinatorUUID);
                            if (cofound != null)
                                playerlist.Remove(cofound);
                            //nun zufügen
                            foreach (SonosPlayer spadd in playerlist)
                            {
                                aktzone.AddPlayer(spadd);
                            }
                            //und löschen
                            foreach (SonosPlayer spdelete in playerlisttodelete)
                            {
                                aktzone.Players.Remove(spdelete);
                            }
                        }

                    }
                    else
                    {
                        //Zone gibt es nicht somit erstellen
                        CreateZone(zoneXML);
                        //Diese Player müssen woanders gelöscht werden.
                    }
                }
                //prüfen ob es mehr als vorhandene Zones in der liste gibt.
                foreach (SonosZone sz in zones)
                {
                    if (!abgearbeiteteZone.Contains(sz.CoordinatorUUID))
                    {
                        zuloeschendezone.Add(sz);
                    }
                }
                if (zuloeschendezone.Any())
                {
                    foreach (SonosZone szd in zuloeschendezone)
                    {
                        Zones.Remove(szd);
                    }
                }

                Test nicht alle Zonen zu löschen sondern entsprechend zu ersetzen.*/

                //zones.Clear();
                var zlist = new List<SonosZone>();
                foreach (var zoneXML in doc.Descendants("ZoneGroup"))
                {
                    CreateZone(zoneXML,zlist);
                }
                Zones = zlist;
            }

            lock (players)
            {
                players.Clear();
                lock (zones)
                {
                    players = zones.SelectMany(z => z.Players).ToList();
                    var coplayer = zones.Select(z => z.Coordinator).ToList();
                    foreach (SonosPlayer cop in coplayer)
                    {
                        if(!players.Contains(cop))
                            players.Add(cop);
                    }
                }
            }
            if (TopologyChanged != null)
                TopologyChanged.Invoke();
        }

        /// <summary>
        /// Generiert die Zonen sowie die Player in diesen.
        /// </summary>
        /// <param name="zoneXml"></param>
        /// <param name="sz">List of SonosZones</param>
        private void CreateZone(XElement zoneXml, List<SonosZone> sz)
        {
            var list = zoneXml.Descendants("ZoneGroupMember").Where(x => x.Attribute("Invisible") == null).ToList();
            if (list.Count > 0)
            {
                var internalzone = new SonosZone((string)zoneXml.Attribute("Coordinator"));

                foreach (var playerXml in list)
                {
                    var player = new SonosPlayer
                                     {
                                         Name = (string)playerXml.Attribute("ZoneName"),
                                         UUID = (string)playerXml.Attribute("UUID"),
                                         DeviceLocation = new Uri((string)playerXml.Attribute("Location")),
                                         ControlPoint = ControlPoint,
                                         CurrentState = new PlayerState()
                                     };
                    if (player.UUID == internalzone.CoordinatorUUID)
                    {
                        internalzone.Coordinator = player;
                    }
                    else
                    {
                        internalzone.AddPlayer(player);
                        Players.Add(player);
                    }

                    // This can happen before or after the topology event...
                    if (playerDevices.ContainsKey(player.UUID))
                    {
                        player.SetDevice(playerDevices[player.UUID]);
                    }
                    else
                    {
                        ControlPoint.ForceDeviceAddition(player.DeviceLocation);
                    }
                }
                if(!sz.Contains(internalzone))
                sz.Add(internalzone);
                //Zones.Add(zone);
            }
        }
    }
}