﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using NanoleafAurora;
using SonosAPI.Classes;

namespace SonosAPI.Controllers
{
    /// <summary>
    /// Schnittstelle/API für die Nanoleaf Aurora
    /// </summary>
    public class NanoleafController : ApiController
    {
        #region ClassVariables

        //public static readonly List<AuroraKnowingDevices> listAuroraKnowingDeviceses = new List<AuroraKnowingDevices>()
        //{
        //   new AuroraKnowingDevices("C8:EF:29:5C:91:24", "JH9eV0l9Zxkqe8ZSDB0FBMfLb2xamZG3","Wohnzimmer"),
        //   new AuroraKnowingDevices("94:9F:5B:E9:5F:A8", "68AeTERgauJl02Fhbnel3Eh64UNY2pX9","Esszimmer")
        //};
        #endregion ClassVariables
        /// <summary>
        /// Get Data
        /// </summary>
        /// <returns>Nanoleaf Object</returns>
        [HttpGet]
        public async Task<List<Aurora>> Get()
        {
            //var ls = AuroraWrapper.StaticListWithoutDiscovery(listAuroraKnowingDeviceses);
            //if (ls.Count > 0) return ls;
            AuroraWrapper.KeepAlive = false;
            if (AuroraWrapper.AurorasList == null || AuroraWrapper.AurorasList.Count == 0)
            {
              return await AuroraWrapper.InitAuroraWrapper(null, false);
            }
            AuroraWrapper.errorEventHandler +=SonosHelper.TraceLogAuroraEvent;
            return AuroraWrapper.AurorasList;
        }

        /// <summary>
        /// Set Scenario
        /// </summary>
        /// <param name="id">Name of Scenario</param>
        /// <param name="v"></param>
        /// <returns></returns>
        [HttpGet]
        public string SetSelectedScenario(string id, string v)
        {
            try
            {
                Aurora a = AuroraWrapper.GetAurorabySerial(id);
                if (a.Scenarios.Contains(v) && a.SelectedScenario != v)
                {
                    a.SelectedScenario = v;
                }
                return a.SelectedScenario;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Set Powerstate
        /// </summary>
        /// <param name="id">true/false</param>
        /// <param name="v">Value of Powerstate</param>
        /// <returns></returns>
        [HttpGet]
        public Boolean SetPowerState(string id, string v)
        {
            Boolean po;
            Aurora a = AuroraWrapper.GetAurorabySerial(id);
            Boolean.TryParse(v, out po);
            if (a == null) return false;
            if (string.IsNullOrEmpty(v)) return a.PowerOn;
            if (a.PowerOn != po)
                {
                    a.PowerOn = po;
                }
            return a.PowerOn;
        }
        [HttpGet]
        public Boolean SetGroupPowerState(string id)
        {
            Boolean po;
            Boolean.TryParse(id, out po);
            return AuroraWrapper.GroupPowerOn(po);
        }
        /// <summary>
        /// Brightness /Helligkeit
        /// </summary>
        /// <param name="id">Number between min and max</param>
        /// <param name="v">Value of Brightness</param>
        /// <returns>Brightness</returns>
        [HttpGet]
        public int SetBrightness(string id, int v)
        {
            Aurora a = AuroraWrapper.GetAurorabySerial(id);
            if (v > a.NLJ.State.Brightness.Max || v < a.NLJ.State.Brightness.Min) return 0;
            if (a.NLJ.State.Brightness.Value != v)
            {
                a.Brightness = v;
            }
            return a.Brightness;
        }
        [HttpGet]
        public int SetSaturation(string id, int v)
        {
            Aurora a = AuroraWrapper.GetAurorabySerial(id);
            if (v > a.NLJ.State.Saturation.Max || v < a.NLJ.State.Saturation.Min) return 0;
            if (a.NLJ.State.Saturation.Value != v)
            {
                a.Saturation = v;
            }
            return a.Saturation;
        }
        [HttpGet]
        public int SetColorTemperature(string id, int v)
        {
            Aurora a = AuroraWrapper.GetAurorabySerial(id);
            if (v > a.NLJ.State.ColorTemperature.Max || v < a.NLJ.State.ColorTemperature.Min) return 0;
            if (a.NLJ.State.ColorTemperature.Value != v)
            {
                a.ColorTemperature = v;
            }
            return a.ColorTemperature;
        }
        /// <summary>
        /// Setzen eins zufälligen Scenarios
        /// </summary>
        /// <param name="serial">Serial of the Aurora</param>
        /// <returns></returns>
        [HttpGet]
        public String SetRandomScenario(string serial)
        {
            Aurora a = AuroraWrapper.GetAurorabySerial(serial);
            return a.SetRandomScenario();
        }
        /// <summary>
        /// Ermitteln der Gruppenscenarien
        /// </summary>
        /// <param name="id">Dummy</param>
        /// <returns></returns>
        [HttpGet]
        public List<String> GetGroupScenario(string id)
        {

            return AuroraWrapper.GetGroupScenarios();
        }
        /// <summary>
        /// Setzen der Gruppen Scenarien
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public String SetGroupScenario(string id)
        {
            return AuroraWrapper.SetGroupScenarios(id);

        }
        [HttpGet]
        public Boolean SetHue(string id, int v)
        {
            Aurora a = AuroraWrapper.GetAurorabySerial(id);
            if (v < a.NLJ.State.Hue.Min || v > a.NLJ.State.Hue.Max) return false;
            a.Hue = v;
            return true;
        }
        /// <summary>
        /// Registriert einen neuen User bei allen gefundenen Aurroas.
        /// Funktioniert nur, wenn auch bei der Aurora 5-7 Sekunden geklickt wurde. 
        /// </summary>
        /// <param name="id">Dummy</param>
        /// <returns></returns>
        [HttpGet]
        public List<String> RegisterNewUser(string id)
        {
            List<String> newUser = new List<string>();
            if (AuroraWrapper.AurorasList == null || AuroraWrapper.AurorasList.Count == 0) return newUser;
            foreach (Aurora aurora in AuroraWrapper.AurorasList)
            {
                try
                {
                    String n = "Name:" + aurora.Name + " Token:" + aurora.NewUser();
                    if (!String.IsNullOrEmpty(n))
                        newUser.Add(n);
                }
                catch(Exception exception)
                {
                    newUser.Add(aurora.Name+" Exception:"+exception.Message);
                }
            }
            return newUser;

        }
        /// <summary>
        /// Umbenennen von Scenarien
        /// </summary>
        /// <param name="id">Serial der Auroras</param>
        /// <param name="v">Altes Scenario @ Neues Scenario Beispiel old@new</param>
        /// <returns>True wenn es geklappt hat.</returns>
        [HttpGet]
        public Boolean RenameScenario(string id, string v)
        {
            Aurora a = AuroraWrapper.GetAurorabySerial(id);
            if (a == null || !v.Contains("@")) return false;
            var sp = v.Split('@');
            if (string.IsNullOrEmpty(sp[0]) || string.IsNullOrEmpty(sp[1])) return false;
            return a.RenameScenario(sp[0], sp[1]);
        }
    }
}
