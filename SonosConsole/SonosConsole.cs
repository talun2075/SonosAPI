using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Timers;
using PacketDotNet;
using SharpPcap;
using Timer = System.Timers.Timer;
using System.ServiceProcess;

namespace SonosConsole
{
    /// <summary>
    /// Consolenanwendung, die auf Dashbuttons reagiert und den Sonosdienst am Leben halten soll.
    /// 
    /// </summary>
    class SonosConsole
    {
        private const int ReadTimeoutMilliseconds = 1000;
        private static readonly TimeSpan DuplicateIgnoreInterval = new TimeSpan(0, 0, 10);
        private static readonly Dictionary<string, DateTime> DiclastEventTime = new Dictionary<string, DateTime>();
        private static readonly List<PhysicalAddress> dashlist = new List<PhysicalAddress>();
        private const string initialSonosUrl = "http://192.168.0.6";
        private const string goodReturnValue = "ok";
        private const int DefaultInterfaceIndex = 0;
        private static Boolean firstrun = true;
        private static DateTime LastIISReset = DateTime.Now;
        private static int timeoutMilliseconds = 30000;
        static void Main(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            //Liste der DashButtons definieren.MAC ohne Doppelpunkte
            dashlist.Add(PhysicalAddress.Parse("AC63BEC64066"));//Seriennummer 213544
            dashlist.Add(PhysicalAddress.Parse("AC63BE1A3548"));//Seriennummer 359509
            dashlist.Add(PhysicalAddress.Parse("AC63BE9D0BE7"));//Seriennummer 277109
            dashlist.Add(PhysicalAddress.Parse("AC63BE3AC1CF"));//Seriennummer 832769
            dashlist.Add(PhysicalAddress.Parse("AC63BE0EA91A"));//Seriennummer 823671
            dashlist.Add(PhysicalAddress.Parse("50F5DA5B814D"));//Seriennummer 455294
            
            foreach (var macstring in dashlist)
            {
                DiclastEventTime.Add(macstring.ToString(), DateTime.Now.AddSeconds(-100));
            }
            CaptureDeviceList devices = CaptureDeviceList.Instance;

            if (devices.Count < 1)
            {
                Console.WriteLine("Keine Netzwerkkarte gefunden.");
                return;
            }
            //Test für Timer einbauen.
            Timer holdOnLive = new Timer();
            holdOnLive.Elapsed += HoldOnLive;
            holdOnLive.Interval = 3600000;
            holdOnLive.Enabled = true;
            //Initial das Web initialisieren.
            HoldOnLive(null, null);

            ICaptureDevice device = devices[DefaultInterfaceIndex];
            device.OnPacketArrival += device_OnPacketArrival;
            device.Open(DeviceMode.Promiscuous, ReadTimeoutMilliseconds);
            device.StartCapture();
            Console.WriteLine("-- Es wird auf Dashbuttons gelauscht...");
            Console.ReadLine();
            device.StopCapture();
            device.Close();


        }
        /// <summary>
        /// Soll den Server am Leben erhalten.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="args"></param>
        private static void HoldOnLive(object t, ElapsedEventArgs args)
        {
            //Timer alle 24 Stunden für IIS Reset
            try
            {
                var lastIisResetHours = (DateTime.Now - LastIISReset).TotalHours;
                //CallWebInterface2("http://localhost:50066/", "html",false);
                if (lastIisResetHours > 18)
                {
                    Console.WriteLine(DateTime.Now + " IISReset wird durchgeführt und 15 Sekunden gewartet.");
                    LastIISReset = DateTime.Now;
                    System.Diagnostics.Process.Start(@"C:\Windows\System32\iisreset.exe");
                    Thread.Sleep(60000);
                    firstrun = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Beim IISReset ist folgender Fehler aufgetreten:");
                Console.WriteLine(ex.Message);
            }
            try
            {
                Console.WriteLine(DateTime.Now + " IE Wird gestartet und "+ (firstrun ? "60" : "30")+" Sekunden gewartet.");
                // Start ieexplorer.exe and go to SonosUrl.
                System.Diagnostics.Process.Start(@"C:\Program Files\Internet Explorer\iexplore.exe", initialSonosUrl);
                // Waite 10 seconds.
                Thread.Sleep(firstrun ? 60000 : timeoutMilliseconds);
                // Get all IEXPLORE processes.
                System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcessesByName("IEXPLORE");
                foreach (System.Diagnostics.Process proc in procs)
                {
                    // Look for Sonos title.
                    if (proc.MainWindowTitle.IndexOf("Sonos", StringComparison.Ordinal) > -1)
                        proc.Kill(); // Close it down.
                    //Others ist a Problem
                    if (proc.MainWindowTitle.IndexOf("Die Seite kann nicht angezeigt", StringComparison.Ordinal) > -1)
                    {
                        //Hier die Dienste Prüfen.
                        ServiceController service = new ServiceController("W3SVC");
                        try
                        {
                            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine("Beim Starten von WWW-Publishingdienst ist folgender Fehler aufgetreten:");
                            Console.WriteLine(ex.Message);
                        }
                    }
                    if (proc.MainWindowTitle.IndexOf("Service Unavailable", StringComparison.Ordinal) > -1)
                    {
                        //AppPoll nicht gestartet
                        System.Diagnostics.Process.Start(@"C:\Windows\System32\inetsrv\appcmd", "start apppool /apppool.name:musik");
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Beim Laden und schließen des IE´s ist folgender Fehler aufgetreten");
                Console.WriteLine(ex.Message);
            }
            // Console.WriteLine(DateTime.Now + " HoldonLive läd die Seite erfolgreich:" + CallWebInterface2(initialSonosUrl, "Coordinator", false));
        }

        /// <summary>
        /// Event, welches verarbeitet wird, wenn Pakete ankommen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            while (packet != null)
            {
                var arpPacket = packet as ARPPacket;
                if (arpPacket != null)
                {
                    HandleArpPacket(arpPacket);
                    break;
                }
                packet = packet.PayloadPacket;
            }
        }
        /// <summary>
        /// Verarbeitet die Liste der Dashbuttons
        /// </summary>
        /// <param name="arpPacket"></param>
        private static void HandleArpPacket(ARPPacket arpPacket)
        {
            //Prüfen, ob das Package ein Dashbutton ist. 
            if (dashlist.Contains(arpPacket.SenderHardwareAddress))
            {
                // Dash seems to send two ARP packets per button press, one second apart.
                // Dash device is active (blinking) for about 10 seconds after button press,
                // and doesn't allow another press for another 25 seconds.
                // 36 seconds after the initial push, the device sends the same two ARP packets.
                var mactostring = arpPacket.SenderHardwareAddress.ToString();
                switch (mactostring)
                {
                    //Dashbutton 1
                    case "AC63BEC64066":
                        var now = DateTime.Now;
                        if (now - DuplicateIgnoreInterval > DiclastEventTime["AC63BEC64066"])
                        {
                            //Prüfen, ob der Button zweimal Zeitnah (<45 Sekunden) geklickt wurde.
                            var id = 0;
                            var dicLastclick = DiclastEventTime["AC63BEC64066"];
                            var sincelastclick = (now - DiclastEventTime["AC63BEC64066"]).TotalSeconds;
                            if (sincelastclick < 45)
                            {
                                id = 1;
                                Console.WriteLine("Gästezimmer wurde nochmal unter 45 Sekunden gedrückt");
                            }
                            else
                            {
                                Console.WriteLine("Gästezimmer wurde gedrückt");
                            }
                            DiclastEventTime["AC63BEC64066"] = now;
                            try
                            {
                                string call = initialSonosUrl+ "/Sonos/dash/dash1/" + id;
                                CallWebInterface2(call, goodReturnValue);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Gästezimmer wurde eine Exception ausgelöst");
                                Console.WriteLine("Now:" + now);
                                Console.WriteLine("Dictionary:" + dicLastclick);
                                Console.WriteLine("LastClick:" + sincelastclick);
                                Console.WriteLine("Exception:" + ex.Message);
                            }
                        }
                        break;
                    case "AC63BE1A3548":
                    case "AC63BE3AC1CF":
                        var nowAC63BE1A3548 = DateTime.Now;
                        if (nowAC63BE1A3548 - DuplicateIgnoreInterval > DiclastEventTime["AC63BE1A3548"])
                        {
                            Console.WriteLine("Musik Erdgeschoss");
                            DiclastEventTime["AC63BE1A3548"] = nowAC63BE1A3548;
                            try
                            {
                                CallWebInterface2(initialSonosUrl+"/Sonos/dash/dash2/0", goodReturnValue);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Erdgeschoss wurde eine Exception ausgelöst");
                                Console.WriteLine("Exception:" + ex.Message);
                            }
                        }
                        break;
                    case "AC63BE9D0BE7":
                        var nowAC63BE9D0BE7 = DateTime.Now;
                        if (nowAC63BE9D0BE7 - DuplicateIgnoreInterval > DiclastEventTime["AC63BE9D0BE7"])
                        {
                            Console.WriteLine("Kinderzimmer");
                            DiclastEventTime["AC63BE9D0BE7"] = nowAC63BE9D0BE7;
                            try
                            {
                                CallWebInterface2(initialSonosUrl+"/Sonos/dash/dash3/0", goodReturnValue);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Kinderzimmer wurde eine Exception ausgelöst");
                                Console.WriteLine("Exception:" + ex.Message);
                            }
                        }

                        break;
                    case "AC63BE0EA91A":
                        var nowAC63BE0EA91A = DateTime.Now;
                        if (nowAC63BE0EA91A - DuplicateIgnoreInterval > DiclastEventTime["AC63BE0EA91A"])
                        {
                            Console.WriteLine("Küche");
                            DiclastEventTime["AC63BE0EA91A"] = nowAC63BE0EA91A;
                            try
                            {
                                CallWebInterface2(initialSonosUrl+"/Sonos/dash/dash4/0", goodReturnValue);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Küche wurde eine Exception ausgelöst");
                                Console.WriteLine("Exception:" + ex.Message);
                            }
                        }
                        break;
                    case "50F5DA5B814D":
                        var now50F5DA5B814D = DateTime.Now;
                        if (now50F5DA5B814D - DuplicateIgnoreInterval > DiclastEventTime["50F5DA5B814D"])
                        {
                            Console.WriteLine("Mine Spezial");
                            DiclastEventTime["50F5DA5B814D"] = now50F5DA5B814D;
                            try
                            {
                                CallWebInterface2(initialSonosUrl + "/Sonos/dash/dash5/0", goodReturnValue);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Mine Spezial wurde eine Exception ausgelöst");
                                Console.WriteLine("Exception:" + ex.Message);
                            }
                        }
                        break;
                    default:
                        Console.WriteLine("Kein Eintrag für den Dashbutton gefunden");
                        return;
                }
            }
        }

        /// <summary>
        /// Ruft die Übergebene Url auf
        /// </summary>
        /// <param name="Url">Ziel URL</param>
        /// <param name="webGoodReturn">Parameter der erwartet wird, wenn der Aufruf ok ist, ansonsten wird es 5 Mal versucht</param>
        /// <param name="writeReturn">Schreibt den Return in die Console</param>
        /// <returns>Erfolgreich ausgeführt?</returns>
        private static void CallWebInterface2(string Url, string webGoodReturn, bool writeReturn = true)
        {
            try
            {

                Boolean okrequest = false;
                Int32 count = 0;
                while (!okrequest)
                {

                    WebClient wc = new WebClient();
                    String webReturnValue = wc.DownloadString(Url);
                    if (writeReturn)
                    {
                        Console.WriteLine("Return vom Server: " + webReturnValue);
                    }
                    if (webReturnValue.Contains("Exception"))
                    {
                        Console.WriteLine("Es gab einen Return mit einer Exception:");
                        Console.WriteLine(webReturnValue);
                        Console.WriteLine("Das Web wird neu initialisiert");
                        HoldOnLive(null, null);
                    }
                    if (webReturnValue.Contains(webGoodReturn) || count > 5)
                    {
                        okrequest = true;
                        if (count > 5)
                        {
                            Console.WriteLine("Request Abbruch, weil Loob");
                        }
                    }
                    count++;
                    wc.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Beim Aufruf von CallWebInterface2(" + Url + ") ist folgender Fehler aufgetreten:");
                Console.WriteLine(ex.Message);
            }

        }
    }
}
