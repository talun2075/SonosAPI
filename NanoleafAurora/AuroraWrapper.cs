﻿using System;
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
        private static List<String> _groupScenarios; 
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
                        AuroraKnowingDevices akd = _knowingAuroras.FirstOrDefault(x => x.MacAdress == asrResults.MACAdress);
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
                            Aurora a = new Aurora("new", asrResults.IP, "New", asrResults.Port);
                            listAurora.Add(a);
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
        public static async Task<List<Aurora>> InitAuroraWrapper(List<AuroraKnowingDevices> KnowingAuroras = null)
        {
            _knowingAuroras = KnowingAuroras ?? ListAuroraKnowingDeviceses;
            await Discovery();
            return AurorasList;
        }
        /// <summary>
        /// Get Aurora Object by Serial
        /// </summary>
        /// <param name="serial">Serial String of a Knowing Aurora</param>
        /// <returns>Aurora</returns>
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
                ErrorMessage = ex.Message;
                return null;
            }
        }
        /// <summary>
        /// Init /KeepAlive without Retunr and Async
        /// </summary>
        /// <param name="KnowingAuroras"></param>
        public static void KeepAliveWithoutAsync(List<AuroraKnowingDevices> KnowingAuroras = null)
        {
            _knowingAuroras = KnowingAuroras ?? ListAuroraKnowingDeviceses;
#pragma warning disable CS4014 // Da dieser Aufruf nicht abgewartet wird, wird die Ausführung der aktuellen Methode fortgesetzt, bevor der Aufruf abgeschlossen ist
            Discovery();
#pragma warning restore CS4014 // Da dieser Aufruf nicht abgewartet wird, wird die Ausführung der aktuellen Methode fortgesetzt, bevor der Aufruf abgeschlossen ist
        }
        /// <summary>
        /// Change Powerstate for all Auroras
        /// </summary>
        /// <param name="_poweron"></param>
        /// <returns></returns>
        public static Boolean GroupPowerOn(Boolean _poweron)
        {
            if (AurorasList == null || AurorasList.Count == 0) return true;
            try
            {
                foreach (Aurora aurora in AurorasList)
                {
                    if (!aurora.NewAurora && aurora.PowerOn != _poweron)
                    {
                        aurora.PowerOn = _poweron;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Merge double Scenarios of all Auroras
        /// </summary>
        /// <returns></returns>
        public static List<String> GetGroupScenarios()
        {
            if (_groupScenarios == null || _groupScenarios.Count == 0)
            {
                _groupScenarios = new List<string>();
                List<String> tempgs = new List<string>();
                if (AurorasList == null || AurorasList.Count == 0) return _groupScenarios;

                foreach (Aurora aurora in AurorasList)
                {
                    tempgs = tempgs.Count == 0 ? aurora.Scenarios : tempgs.Intersect(aurora.Scenarios).ToList();
                }
                if (tempgs.Count > 0) _groupScenarios = tempgs;
            }
            return _groupScenarios;
            
        }
        /// <summary>
        /// Set Group Scenarios
        /// </summary>
        /// <param name="scenario"></param>
        /// <returns></returns>
        public static String SetGroupScenarios(string scenario)
        {
            if (AurorasList == null || AurorasList.Count == 0) return "No Auroras Found";
            try
            {
                foreach (Aurora aurora in AurorasList)
                {
                    if (aurora.Scenarios.Contains(scenario))
                    {
                        aurora.SelectedScenario = scenario;
                    }
                }
                return "Done";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        #endregion Public Methods
        /// <summary>
        /// List of Knowing / Discovered Auroras
        /// </summary>
        #region Propertys
        public static List<Aurora> AurorasList { get; private set; }
        /// <summary>
        /// ErrorMesages on Discovery
        /// </summary>
        public static String ErrorMessage { get; private set; }
        /// <summary>
        /// List of Knowing Aurora Devices
        /// </summary>
#if DEBUG
        private static List<AuroraKnowingDevices> ListAuroraKnowingDeviceses { get; } = new List<AuroraKnowingDevices>()
        {
            new AuroraKnowingDevices("C8:EF:29:5C:91:24", "oUsABosEpyi5phDPQItyINzRK545sAae", "Wohnzimmer"),
            new AuroraKnowingDevices("94:9F:5B:E9:5F:A8", "OVVRrA5NoPUFjbH4a7dDgW4KmJp5njRB", "Esszimmer")

        };
#else
        private static List<AuroraKnowingDevices> ListAuroraKnowingDeviceses { get; } = new List<AuroraKnowingDevices>()
        {
            new AuroraKnowingDevices("C8:EF:29:5C:91:24", "JH9eV0l9Zxkqe8ZSDB0FBMfLb2xamZG3", "Wohnzimmer"),
            new AuroraKnowingDevices("94:9F:5B:E9:5F:A8", "68AeTERgauJl02Fhbnel3Eh64UNY2pX9", "Esszimmer")

        };
#endif
        #endregion Propertys
    }




}