﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace ExternalDevices
{
    /// <summary>
    /// Class to Communicate with a Nanoleaf Aurora
    /// </summary>
    public static class Nanoleaf
    {
        private static String _tokenAuth;
        private static String _ip;
        private static Int16 _port;
        private const string _Statepath = "/state";
        private const string _Apipath = "/api/v1/";
        #region PublicMethods
        /// <summary>
        /// Init the aurora
        /// </summary>
        /// <param name="token">User Token</param>
        /// <param name="ip">IP of the Aurora</param>
        /// <param name="port">Port (Default 16021)</param>
        /// <returns>Was the Init Successfull</returns>
        public static Boolean Initialisieren(string token= "JH9eV0l9Zxkqe8ZSDB0FBMfLb2xamZG3", string ip="192.168.0.166", Int16 port=16021)
        {
            if (String.IsNullOrEmpty(ip) || String.IsNullOrEmpty(token)) return false;
            if (ip.StartsWith("http://"))
                ip = ip.Replace("http://", "");
            if (!Regex.IsMatch(ip, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$")) return false;
            if (port == 0) return false;
            _tokenAuth = token;
            _ip = ip;
            _port = port;
            return token == "new" || GetNanoLeafInformations();
        }
        /// <summary>
        /// Use to Get a New Token
        /// Push the On Button for 5 till 7 Seconds on the Aurora and then Call this Method. You have 30 Seconds time.
        /// </summary>
        /// <returns>New Token</returns>
        public static String NewUser(string ip, Int16 port=16021)
        {
            if (Initialisieren("new", ip, port))
            {
                return ConnectToNanoleaf(NanoleafRequest.POST, "new");
            }
            return "Es ist ein Fehler beim Anlegen des neuen Users entstanden!";
        }

        public static String SetRandomScenario()
        {
            CheckIniState();
            Random rng = new Random();
            int k = rng.Next(NLJ.Effects.Scenarios.Count);
            SelectedScenario = NLJ.Effects.Scenarios[k];
            return NLJ.Effects.Scenarios[k];
        }
        #endregion PublicMethods

        #region PublicProperties
        /// <summary>
        /// GET/SET the Powerstate
        /// </summary>
        public static Boolean PowerOn
        {
            get
            {
                return NLJ.State.Powerstate.Value;
            }
            set
            {
                if (value != NLJ.State.Powerstate.Value)
                {
                    NLJ.State.Powerstate.Value = value;
                    ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath, "{\"on\":"+ NLJ.State.Powerstate.Value.ToString().ToLower() + "}");
                }
            }
        }
        /// <summary>
        /// Selected Scenario
        /// </summary>
        public static String SelectedScenario
        {
            get { return NLJ.Effects.Selected; }
            set
            {
                if (NLJ.Effects.Scenarios.Contains(value))
                {
                    NLJ.Effects.Selected = value;
                    string jsontemp = "{\"select\":\"" + value + "\"}";
                    ConnectToNanoleaf(NanoleafRequest.PUT, "/effects", jsontemp);
                    GetNanoLeafInformations();
                }
            }
        }
        /// <summary>
        /// All Knowing Scenarios
        /// </summary>
        public static List<String> Scenarios => NLJ.Effects.Scenarios;
        /// <summary>
        /// Helligkeit
        /// </summary>
        public static int Brightness
        {
            get
            {
                return NLJ.State.Brightness.Value; 
            }
            set
            {
                if (value > NLJ.State.Brightness.Max || value < NLJ.State.Brightness.Min) return;
                NLJ.State.Brightness.Value = value;
                ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath, "{\"brightness\":" + NLJ.State.Brightness.Value + "}");

            }
        }
        /// <summary>
        /// Farbton (Hue)
        /// </summary>
        public static int Hue
        {
            get
            {
                return NLJ.State.Hue.Value;
            }
            set
            {
                if (value > NLJ.State.Hue.Max || value < NLJ.State.Hue.Min) return;
                NLJ.State.Hue.Value = value;
                ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath, "{\"hue\":" + NLJ.State.Hue.Value + "}");
            }
        }
        /// <summary>
        /// Saturation
        /// </summary>
        public static int Saturation
        {
            get
            {
                return NLJ.State.Saturation.Value;
            }
            set
            {
                if (value > NLJ.State.Saturation.Max || value < NLJ.State.Saturation.Min) return;
                NLJ.State.Saturation.Value = value;
                ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath, "{\"sat\":" + NLJ.State.Saturation.Value + "}");
            }
        }
        /// <summary>
        /// ColorTemperature
        /// </summary>
        public static int ColorTemperature
        {
            get
            {
                return NLJ.State.ColorTemperature.Value;
            }
            set
            {
                if (value > NLJ.State.ColorTemperature.Max || value < NLJ.State.ColorTemperature.Min) return;
                NLJ.State.ColorTemperature.Value = value;
                ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath, "{\"ct\":" + NLJ.State.Saturation.Value + "}");
            }
        }
        #endregion PublicProperties

        #region PrivateMethods
        /// <summary>
        /// Check The Ini state and Initilaized if not set
        /// </summary>
        private static void CheckIniState()
        {
                Initialisieren();
        }

        /// <summary>
        /// This Object is generated by the Json Informations of the Nanoleaf
        /// </summary>
        public static NanoLeafJson NLJ { get; private set; }
        /// <summary>
        /// Connect to the Naoleaf
        /// </summary>
        /// <param name="nr">RequestType</param>
        /// <param name="call">Call need to get State of Something like PowerOn (Path)</param>
        /// <param name="value">Value to set on PUT or Post</param>
        /// <returns></returns>
        private static String ConnectToNanoleaf(NanoleafRequest nr, string call, string value = "")
        {
            try
            {
                string retval;
                Uri urlstate;
                if (!string.IsNullOrEmpty(call))
                {
                    if (call == "new")
                    {
                        urlstate = new Uri("http://" + _ip + ":" + _port + _Apipath +  call);
                    }
                    else
                    {
                        if (call == "INIT")
                            call = String.Empty;
                        urlstate = new Uri("http://" + _ip + ":" + _port + _Apipath + _tokenAuth + call);
                    }
                }
                else
                {
                    return "Error no Call";
                }

                WebRequest webRequest = WebRequest.Create(urlstate);
                switch (nr)
                {
                    case NanoleafRequest.GET:
                        webRequest.Method = "GET";
                        break;
                    case NanoleafRequest.POST:
                        webRequest.Method = "POST";
                        break;
                    case NanoleafRequest.PUT:
                        webRequest.Method = "PUT";
                        break;
                }
                if (nr != NanoleafRequest.GET)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(value);
                    // Set the ContentType property of the WebRequest.
                    webRequest.ContentType = "application/json";
                    // Set the ContentLength property of the WebRequest.
                    webRequest.ContentLength = byteArray.Length;
                    // Get the request stream.
                    var dataStream = webRequest.GetRequestStream();
                    // Write the data to the request stream.
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    // Close the Stream object.
                    dataStream.Close();
                    // Get the response.
                    dataStream.Dispose();
                }
                var response = (HttpWebResponse)webRequest.GetResponse();
                // ReSharper disable once AssignNullToNotNullAttribute
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    retval = reader.ReadToEnd();
                }
                response.Dispose();
                return retval;
            }
            catch (Exception ex)
            {
                return ex.Message;

            }
        }
        /// <summary>
        /// Inernal Class to get Changes from Nanoleaf
        /// On Each Change its to call, that the Changes Knowing
        /// </summary>
        /// <returns></returns>
        private static Boolean GetNanoLeafInformations()
        {
            string json;
            try
            {
                json = ConnectToNanoleaf(NanoleafRequest.GET, "INIT");
            }
            catch
            {
                return false;
            }
            try
            {
                if (String.IsNullOrEmpty(json)) return false;
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    // Deserialization from JSON  
                    DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(NanoLeafJson));
                    NLJ = (NanoLeafJson)deserializer.ReadObject(ms);
                    NLJ.State.ColorTemperature.Max = 6500;
                    NLJ.State.ColorTemperature.Min = 1200;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        #endregion PrivateMethods
    }

    enum NanoleafRequest
    {
        GET,
        PUT,
        POST,
    }
    #region NanoleafJsonTranslate
    [DataContract]
    public class NanoLeafJson
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "serialNo")]
        public string SerialNo { get; set; }
        [DataMember(Name = "manufacturer")]
        public string Manufacturer { get; set; }
        [DataMember(Name = "firmwareVersion")]
        public string FirmwareVersion { get; set; }
        [DataMember(Name = "model")]
        public string Model { get; set; }
        [DataMember(Name = "state")]
        public NanoleafJsonState State { get; set; }
        [DataMember(Name = "effects")]
        public NanoleafJsonEffects Effects { get; set; }
        [DataMember(Name = "panelLayout")]
        public NanoleafJsonPanelLayout PanelLayout { get; set; }
    }
    /// <summary>
    /// Abstract State from Json
    /// </summary>
    [DataContract]
    public class NanoleafJsonState
    {
        [DataMember(Name = "on")]
        public NanoLeafJsonValue Powerstate { get; set; }
        [DataMember(Name = "colorMode")]
        public string ColorMode { get; set; }
        [DataMember(Name = "brightness")]
        public NanoleafJsonVMM Brightness { get; set; }
        [DataMember(Name = "hue")]
        public NanoleafJsonVMM Hue { get; set; }
        [DataMember(Name = "sat")]
        public NanoleafJsonVMM Saturation { get; set; }
        [DataMember(Name = "ct")]
        public NanoleafJsonVMM ColorTemperature { get; set; }
    }
    /// <summary>
    /// Abstract Value/Max/Min Object from Json
    /// </summary>
    [DataContract]
    public class NanoleafJsonVMM
    {
        [DataMember(Name = "value")]
        public int Value { get; set; }
        [DataMember(Name = "max")]
        public int Max { get; set; }
        [DataMember(Name = "min")]
        public int Min { get; set; }
    }
    [DataContract]
    public class NanoleafJsonEffects
    {
        [DataMember(Name = "select")]
        public string Selected { get; set; }
        [DataMember(Name = "effectsList")]
        public List<String> Scenarios { get; set; }
    }
    [DataContract]
    public class NanoleafJsonPanelLayout
    {
        [DataMember(Name = "globalOrientation")]
        public NanoleafJsonVMM GlobalOrientation { get; set; }
        [DataMember(Name = "layout")]
        public NanoleafJsonPanelLayoutLayout Layout { get; set; }
    }
    [DataContract]
    public class NanoleafJsonPanelLayoutLayout
    {
        [DataMember(Name = "layoutData")]
        public string LayoutData { get; set; }
    }
    [DataContract]
    public class NanoLeafJsonValue
    {
        [DataMember(Name = "value")]
        public Boolean Value { get; set; }
    }
    #endregion NanoleafJsonTranslate
}
