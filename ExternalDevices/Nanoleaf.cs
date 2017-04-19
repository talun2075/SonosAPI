using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ExternalDevices
{
    public static class Nanoleaf
    {
        private static Boolean _powerStatus;
        private static String _tokenAuth;
        private static String _ip;
        private static Int16 _port;
        private const string _Statepath = "/state";
        private const string _Apipath = "/api/beta/";

        public static Boolean Initialisieren(string token, string ip, Int16 port)
        {
            if (String.IsNullOrEmpty(ip) || String.IsNullOrEmpty(token)) return false;
            if (ip.StartsWith("http://"))
                ip = ip.Replace("http://", "");
            if (!Regex.IsMatch(ip, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$")) return false;
            if (port == 0) return false;
            _tokenAuth = token;
            _ip = ip;
            _port = port;
            string pw = ConnectToNanoleaf(NanoleafRequest.GET, "on");
            if (pw.Contains("true"))
            {
                _powerStatus = true;
            }
            return true;
        }
        /// <summary>
        /// GET/SET the Powerstate
        /// </summary>
        public static Boolean PowerOn
        {
            get
            {
                return _powerStatus;
            }
            set
            {
                if (value != _powerStatus)
                {
                    ConnectToNanoleaf(NanoleafRequest.PUT, null, "{\"on\":" + value.ToString().ToLower() + "}");
                    _powerStatus = value;
                }
            }
        }
        /// <summary>
        /// Connect to the Naoleaf
        /// </summary>
        /// <param name="nr">RequestType</param>
        /// <param name="call">Call need to get State of Something like PowerOn (Path)</param>
        /// <param name="value">Value to set on PUT or Post</param>
        /// <returns></returns>
        private static String ConnectToNanoleaf(NanoleafRequest nr,string call,string value="")
        {
            try
            {
                string retval;
                Uri urlstate;
                if (!string.IsNullOrEmpty(call))
                {
                    urlstate = new Uri("http://" + _ip + ":" + _port + _Apipath + _tokenAuth + _Statepath + "/" + call);
                }
                else
                {
                    urlstate = new Uri("http://" + _ip + ":" + _port + _Apipath + _tokenAuth + _Statepath);
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
                    string postData = value;
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
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
                var response = (HttpWebResponse) webRequest.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK) return "Error: " + response.StatusCode;
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
    }


    enum NanoleafRequest
    {
        GET,
        PUT,
        POST,
    }
}
