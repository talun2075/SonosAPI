﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace ExternalDevices
{
    /// <summary>
    /// Klasse,die den Marantz AVR in ein Objekt hält
    /// </summary>
    public static class Marantz
    {
        #region InternalVariables
        private static String mUrl;
        private static String mXMLPath = "/goform/formMainZone_MainZoneXml.xml";
        private static String mInputPath = "MainZone/index.put.asp";
        private static MarantzInputs _selectedMarantzInputs;
        private static Boolean _PowerOn;
        private static String _volume;
        #endregion InternalVariables
        /// <summary>
        /// Initialisieren des Marantz
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Boolean Initialisieren(String url)
        {
            try
            {
                if (String.IsNullOrEmpty(url)) return false;
                mUrl = url;
                if (url.StartsWith("http://"))
                    url = url.Replace("http://", "");
                if(!Regex.IsMatch(url, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$")) return false;
                XmlDocument myXmlDocument = new XmlDocument();
                myXmlDocument.Load(mUrl + mXMLPath); //Load NOT LoadXml
                XmlNode powerstateNode = myXmlDocument.SelectSingleNode("descendant::Power");
                XmlNode inputFuncSelectNode = myXmlDocument.SelectSingleNode("descendant::InputFuncSelect");
                XmlNode volumeStateNode = myXmlDocument.SelectSingleNode("descendant::MasterVolume");
                if (powerstateNode == null || inputFuncSelectNode == null || volumeStateNode == null) return false;
                _PowerOn = powerstateNode.InnerText == "ON";
                _volume = volumeStateNode.InnerText;
                try
                {
                    _selectedMarantzInputs = (MarantzInputs)Enum.Parse(typeof(MarantzInputs), inputFuncSelectNode.InnerText);
                }
                catch
                {
                    if (inputFuncSelectNode.InnerText.ToLower().Contains("tv"))
                    {
                        _selectedMarantzInputs = MarantzInputs.TV;
                    }
                    else
                    {
                        _selectedMarantzInputs = MarantzInputs.Sonos;
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        private static void MarantzInput(string inp)
        {
                WebRequest webRequest = WebRequest.Create(mUrl+"/"+mInputPath);
                webRequest.Method = "POST";
                string postData = inp;
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                // Set the ContentType property of the WebRequest.
                webRequest.ContentType = "application/x-www-form-urlencoded";
                // Set the ContentLength property of the WebRequest.
                webRequest.ContentLength = byteArray.Length;
                // Get the request stream.
                Stream dataStream = webRequest.GetRequestStream();
                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);
                // Close the Stream object.
                dataStream.Close();
                // Get the response.
                WebResponse response = webRequest.GetResponse();
                response.Dispose();
                dataStream.Dispose();
        }
        /// <summary>
        /// Lautstärke Setzen (Format "-30.0")
        /// </summary>
        public static string Volume { get { return _volume; }
            set
            {
                if (value == _volume) return;
                if (!_volume.StartsWith("-") || (!_volume.EndsWith(".0") && !_volume.EndsWith(".5"))) return;
                _volume = value;
                MarantzInput("cmd0=PutMasterVolumeSet%2F"+_volume);
            } }
        /// <summary>
        /// Marantz Ein und Ausschalten
        /// </summary>
        public static Boolean PowerOn { get {return _PowerOn;}
            set
            {
                if (value == _PowerOn) return;
                _PowerOn = value;
                if (_PowerOn)
                {
                    MarantzInput("cmd0=PutZone_OnOff%2FON&cmd1=aspMainZone_WebUpdateStatus%2F");
                }
                else
                {
                    MarantzInput("cmd0=PutZone_OnOff%2FOFF&cmd1=aspMainZone_WebUpdateStatus%2F");
                }
            }
            
        }
        /// <summary>
        /// Ausgewählte Quelle Setzen
        /// </summary>
        public static MarantzInputs SelectedInput
        {
            get
            {
                return _selectedMarantzInputs;
            }
            set
            {
                if (value == _selectedMarantzInputs) return;
                switch (value)
                {
                    case MarantzInputs.Sonos:
                        MarantzInput("cmd0=PutZone_InputFunction%2FCD&cmd1=aspMainZone_WebUpdateStatus%2F");
                        break;
                    case MarantzInputs.Film:
                        MarantzInput("cmd0=PutZone_InputFunction%2FBD&cmd1=aspMainZone_WebUpdateStatus%2F");
                        break;
                    case MarantzInputs.PS3:
                        MarantzInput("cmd0=PutZone_InputFunction%2FGAME&cmd1=aspMainZone_WebUpdateStatus%2F");
                        break;
                    case MarantzInputs.TV:
                        MarantzInput("cmd0=PutZone_InputFunction%2FTV&cmd1=aspMainZone_WebUpdateStatus%2F");
                        break;
                }
                _selectedMarantzInputs = value;

            }
        }
    }

}
/// <summary>
/// Enum mit den möglichen Selektierbaren Quellen
/// </summary>
public enum MarantzInputs
{
    Sonos,
    Film,
    PS3,
    PS4,
    TV
}