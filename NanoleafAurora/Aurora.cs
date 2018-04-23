using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace NanoleafAurora
{
    //Todo: Call Request All (Doc 3.4)
    /// <summary>
    /// Class to Communicate with a Nanoleaf Aurora
    /// </summary>
    [DataContract]
    public class Aurora
    {
        private readonly int _port;
        private const string _Statepath = "/state";
        private const string _Apipath = "/api/v1/";

        #region PublicMethods

        /// <summary>
        /// Init the aurora
        /// </summary>
        /// <param name="token">User Token type "New" for new User</param>
        /// <param name="_ip">IP of the Aurora</param>
        /// <param name="_Name"></param>
        /// <param name="port">Port (Default 16021)</param>
        public Aurora(string token, string _ip, string _Name, int port = 16021)
        {
            try
            {
                if (String.IsNullOrEmpty(_ip) || String.IsNullOrEmpty(token))
                    throw new ArgumentNullException(nameof(_ip), "ip or token is Empty");
                if (_ip.StartsWith("http://"))
                    _ip = _ip.Replace("http://", "");
                if (!Regex.IsMatch(_ip, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$"))
                    throw new ArgumentOutOfRangeException(nameof(_ip), _ip, "This is not a IP");
                if (port == 0) throw new ArgumentOutOfRangeException(nameof(port), port, "Need Port grater then Zero");
                Token = token;
                Ip = _ip;
                _port = port;
                Name = _Name;
                if (Token.ToLower() != "new")
                {
                    GetNanoLeafInformations();
                }
                else
                {
                    NewAurora = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Use to Get a New Token
        /// Push the On Button for 5 till 7 Seconds on the Aurora and then Call this Method. You have 30 Seconds time.
        /// </summary>
        /// <returns>New Token</returns>
        public String NewUser()
        {
            return ConnectToNanoleaf(NanoleafRequest.POST, "new");
        }

        public String SetRandomScenario()
        {
            try
            {
                Random rng = new Random();
                int k = rng.Next(NLJ.Effects.Scenarios.Count);
                SelectedScenario = NLJ.Effects.Scenarios[k];
                return NLJ.Effects.Scenarios[k];
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                return String.Empty;
            }
        }

        /// <summary>
        /// Rename Scenario
        /// </summary>
        /// <param name="oldScenario">Name of Old Scenario. Must be in the EffectList (Scenarios)</param>
        /// <param name="newScenario">New Name</param>
        /// <returns>True if done or false on an Error</returns>
        public Boolean RenameScenario(string oldScenario, string newScenario)
        {
            if (!Scenarios.Contains(oldScenario) || string.IsNullOrEmpty(newScenario)) return false;
            try
            {
                string jsontemp = "{\"write\" : {\"command\" : \"rename\", \"animName\" : \"" + oldScenario +
                                  "\",\"newName\" : \"" + newScenario + "\"}}";
                ConnectToNanoleaf(NanoleafRequest.PUT, "/effects", jsontemp);
                GetNanoLeafInformations();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                return false;
            }
            return true;
        }

        #endregion PublicMethods

        #region PublicProperties

        /// <summary>
        /// GET/SET the Powerstate
        /// </summary>
        public Boolean PowerOn
        {
            get { return NLJ.State.Powerstate.Value; }
            set
            {
                try
                {
                    if (value != NLJ.State.Powerstate.Value)
                    {
                        NLJ.State.Powerstate.Value = value;
                        ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath,
                            "{\"on\":" + NLJ.State.Powerstate.Value.ToString().ToLower() + "}");
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Selected Scenario
        /// </summary>
        public String SelectedScenario
        {
            get { return NLJ.Effects.Selected; }
            set
            {
                try
                {
                    if (NLJ.Effects.Scenarios.Contains(value))
                    {
                        NLJ.Effects.Selected = value;
                        string jsontemp = "{\"select\":\"" + value + "\"}";
                        ConnectToNanoleaf(NanoleafRequest.PUT, "/effects", jsontemp);
                        GetNanoLeafInformations();
                    }
                    else
                    {
                        {
                            errorEventHandler?.Invoke("value not found", EventArgs.Empty);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// All Knowing Scenarios
        /// </summary>
        public List<String> Scenarios => NLJ.Effects.Scenarios;

        /// <summary>
        /// Helligkeit
        /// </summary>
        public int Brightness
        {
            get { return NLJ.State.Brightness.Value; }
            set
            {
                try
                {
                    if (value > NLJ.State.Brightness.Max || value < NLJ.State.Brightness.Min)
                    {
                        errorEventHandler?.Invoke("Brightness value out of Range", EventArgs.Empty);
                        return;
                    }
                    NLJ.State.Brightness.Value = value;
                    ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath,
                        "{\"brightness\":" + NLJ.State.Brightness.Value + "}");
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Farbton (Hue)
        /// </summary>
        public int Hue
        {
            get { return NLJ.State.Hue.Value; }
            set
            {
                try
                {
                    if (value > NLJ.State.Hue.Max || value < NLJ.State.Hue.Min)
                    {
                        errorEventHandler?.Invoke("Hue value out of Range", EventArgs.Empty);
                        return;
                    }

                    NLJ.State.Hue.Value = value;
                    ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath, "{\"hue\":" + NLJ.State.Hue.Value + "}");
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Saturation
        /// </summary>
        public int Saturation
        {
            get { return NLJ.State.Saturation.Value; }
            set
            {
                try
                {
                    if (value > NLJ.State.Saturation.Max || value < NLJ.State.Saturation.Min)
                    {
                        errorEventHandler?.Invoke("Saturation value out of Range", EventArgs.Empty);
                        return;
                    }
                    NLJ.State.Saturation.Value = value;
                    ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath, "{\"sat\":" + NLJ.State.Saturation.Value + "}");
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// ColorTemperature
        /// </summary>
        public int ColorTemperature
        {
            get { return NLJ.State.ColorTemperature.Value; }
            set
            {
                try
                {
                    if (value > NLJ.State.ColorTemperature.Max || value < NLJ.State.ColorTemperature.Min)
                    {
                        errorEventHandler?.Invoke("ColorTemperature value out of Range", EventArgs.Empty);
                        return;
                    }
                    NLJ.State.ColorTemperature.Value = value;
                    ConnectToNanoleaf(NanoleafRequest.PUT, _Statepath, "{\"ct\":" + NLJ.State.Saturation.Value + "}");
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// This Object is generated by the Json Informations of the Nanoleaf
        /// </summary>
        [DataMember]
        public NanoLeafJson NLJ { get; private set; }

        /// <summary>
        /// User Token
        /// </summary>
        [DataMember]
        public String Token { get; private set; }

        [DataMember]
        public Boolean NewAurora { get; private set; }

        /// <summary>
        /// IP of Aurora
        /// </summary>
        [DataMember]
        public String Ip { get; private set; }

        /// <summary>
        /// SerialNumber of Aurora
        /// </summary>
        [DataMember]
        public String SerialNo { get; private set; }

        [DataMember]
        public String ErrorMessage { get; set; }

        [DataMember]
        public String Name { get; set; }

        [DataMember]
        public String OpenAPIFirmwareVersion => "2.3.0";

        #endregion PublicProperties

        #region PrivateMethods

        /// <summary>
        /// Connect to the Naoleaf
        /// </summary>
        /// <param name="nr">RequestType</param>
        /// <param name="call">Call need to get State of Something like PowerOn (Path)</param>
        /// <param name="value">Value to set on PUT or Post</param>
        /// <returns></returns>
        private String ConnectToNanoleaf(NanoleafRequest nr, string call, string value = "")
        {
            HttpWebResponse response = null;
            try
            {
                string retval;
                Uri urlstate;
                if (!string.IsNullOrEmpty(call))
                {
                    if (call == "new")
                    {
                        urlstate = new Uri("http://" + Ip + ":" + _port + _Apipath + call);
                    }
                    else
                    {
                        if (call == "INIT")
                            call = String.Empty;
                        urlstate = new Uri("http://" + Ip + ":" + _port + _Apipath + Token + call);
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
                response = (HttpWebResponse) webRequest.GetResponse();
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
                if (response != null)
                {
                    response.Dispose();
                }
                ErrorMessage = ex.Message;
                errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
                return String.Empty;
            }
        }

        /// <summary>
        /// Inernal Class to get Changes from Nanoleaf
        /// On Each Change its to call, that the Changes Knowing
        /// </summary>
        /// <returns></returns>
        private void GetNanoLeafInformations()
        {

            var json = ConnectToNanoleaf(NanoleafRequest.GET, "INIT");

            if (String.IsNullOrEmpty(json))
            {
                return;
            }
            try
            {
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    // Deserialization from JSON  
                    DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof (NanoLeafJson));
                    NLJ = (NanoLeafJson) deserializer.ReadObject(ms);
                    SerialNo = NLJ.SerialNo;
                }
            }
            catch (Exception ex)
            {

                ErrorMessage = ex.Message;
                errorEventHandler?.Invoke(ex.Message, EventArgs.Empty);
            }
        }

        #endregion PrivateMethods

        #region Eventing

        public event EventHandler errorEventHandler;

        #endregion Eventing
    }

    internal enum NanoleafRequest
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

        [DataMember(Name = "rhythm")]
        public NanoLeafJsonRhythm Rhythm { get; set; }
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

    [DataContract]
    public class NanoLeafJsonRhythm
    {
        [DataMember(Name = "rhythmConnected")]
        public Boolean RhythmConnected { get; set; }

        [DataMember(Name = "rhythmActive")]
        public Boolean? RhythmActive { get; set; }

        [DataMember(Name = "rhythmId")]
        public String RhythmId { get; set; }

        [DataMember(Name = "hardwareVersion")]
        public String HardwareVersion { get; set; }

        [DataMember(Name = "firmwareVersion")]
        public String FirmwareVersion { get; set; }

        [DataMember(Name = "auxAvailable")]
        public Boolean? AuxAvailable { get; set; }

        [DataMember(Name = "rhythmMode")]
        public String RhythmMode { get; set; }

        [DataMember(Name = "rhythmPos")]
        public NanoLeafJsonRhythmPos rhythmPos { get; set; }
    }

    [DataContract]
    public class NanoLeafJsonRhythmPos
    {
        [DataMember(Name = "x")]
        public String x { get; set; }

        [DataMember(Name = "y")]
        public String y { get; set; }

        [DataMember(Name = "o")]
        public String o { get; set; }
    }
    #endregion NanoleafJsonTranslate
}