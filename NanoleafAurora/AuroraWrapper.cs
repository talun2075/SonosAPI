using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;


namespace NanoleafAurora
{
    public static class AuroraWrapper
    {
        #region ClassVariables

        private static  Timer keepAliveTimer;
        private static List<AuroraKnowingDevices> _knowingAuroras;
        #endregion ClassVariables
        #region Private Methods
        /// <summary>
        /// Discover Nanoleafs in Local Network
        /// </summary>
        /// <returns>List of Founded auroras in Network</returns>
        private static async Task<List<AuroraSearchResults>> FindAuroras()
        {
            try
            {
                List<AuroraSearchResults> lasr = new List<AuroraSearchResults>();
                IReadOnlyList<IZeroconfHost> results =await ZeroconfResolver.ResolveAsync("_nanoleafapi._tcp.local.").ConfigureAwait(true);

                if (results.Count == 0) return lasr;
                foreach (IZeroconfHost host in results)
                {
                    AuroraSearchResults asr = new AuroraSearchResults(host.IPAddress,
                        host.Services.First().Value.Properties.First().First().Value, host.Services.First().Value.Port);
                    lasr.Add(asr);
                }
                return lasr;
            }
            catch (Exception ex)
            {
                ErrorMessage = "error in private FindAurora Method: " + ex.Message;
                return null;
            }
        }
        /// <summary>
        /// KeepAlaive Call Method from Timer
        /// </summary>
        /// <param name="state"></param>
        private static async void KeepAlive(object state)
        {
            await Discovery();
        }
        /// <summary>
        /// Start SEarch for Auroras in NEtwork
        /// Build List of Auroras include New and knowed Devices
        /// </summary>
        /// <returns>List of All Auroras</returns>
        private static async Task<List<Aurora>> Discovery()
        {
            try
            {
                List<Aurora> listAurora = new List<Aurora>();
                //Start to Search
                List<AuroraSearchResults> lasr = await FindAuroras();
                if (lasr.Count > 0)
                {

                    foreach (AuroraSearchResults asrResults in lasr)
                    {
                        AuroraKnowingDevices akd = _knowingAuroras.First(x => x.MacAdress == asrResults.MACAdress);
                        if (akd != null)
                        {
                            Aurora a = new Aurora(akd.AuthToken, asrResults.IP,akd.DeviceName, asrResults.Port);
                            if (string.IsNullOrEmpty(a.ErrorMessage))
                            {
                                listAurora.Add(a);
                            }
                        }
                        else
                        {
                            //Aurora a = new Aurora("new", asrResults.IP, "New", asrResults.Port);
                            //listAurora.Add(a); //todo: wieder aktivieren
                        }
                    }



                }
                AurorasList = listAurora;
                // Avoid multiple state changes and consolidate them
                if (keepAliveTimer != null)
                    keepAliveTimer.Dispose();
                keepAliveTimer = new Timer(KeepAlive, null, TimeSpan.FromSeconds(60), TimeSpan.FromMilliseconds(-1));
                return listAurora;
            }
            catch (Exception ex)
            {
                ErrorMessage = "error in private Discovery Method: " + ex.Message;
                return null;
            }

        }
        #endregion Private Methods
        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="KnowingAuroras"></param>
        /// <returns></returns>
        public static async Task<List<Aurora>> InitAuroraWrapper(List<AuroraKnowingDevices> KnowingAuroras)
        {
            _knowingAuroras = KnowingAuroras;
            await Discovery();
            return AurorasList;
        }

        public static Aurora GetAurorabySerial(string serial)
        {
            if (AurorasList == null || AurorasList.Count == 0) return null;
            try
            {
                foreach (Aurora aurora in AurorasList)
                {
                    if (!aurora.NewAurora && String.IsNullOrEmpty(aurora.ErrorMessage))
                    {
                        if(aurora.SerialNo == serial)
                            return aurora;
                    }
                        
                }
                return null;
            }
            catch (Exception ex)
            {
                var k = ex.Message;
                return null;
            }
        }

        public static List<Aurora> StaticListWithoutDiscovery(List<AuroraKnowingDevices> KnowingAuroras)
        {
            List<Aurora> la = new List<Aurora>();
            foreach (AuroraKnowingDevices akd in KnowingAuroras)
            {
                string i = "192.168.0.102";
                if (akd.DeviceName == "Wohnzimmer")
                {
                    i = "192.168.0.166";
                }
                Aurora a = new Aurora(akd.AuthToken,i,akd.DeviceName);  

                la.Add(a);
            }

            return la;
        }
        #endregion Public Methods

        #region Propertys
        public static List<Aurora> AurorasList { get; private set; }
        public static String ErrorMessage { get; private set; }
        #endregion Propertys
        //todo: Gruppen An und aus
        //todo: Gruppen Scenarios
        //todo: KeepAlive über die SonosConsole Starten.
    }




}
