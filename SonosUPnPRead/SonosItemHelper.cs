using System;
using System.Collections.Generic;
using SonosUPNP;

namespace SonosUPnP
{
    public static class SonosItemHelper
    {
        private static readonly List<string> _bekannteStreamingPfade = new List<string> { "x-rincon-stream:RINCON", "x-rincon-mp3radio", "x-sonosapi-radio", "x-sonosapi-hls-static", "x-sonosapi-stream", "x-sonos-http", "x-sonosprog-http", "aac:"};

        /// <summary>
        /// Prüft ob ein Item ein Streaming Item (Stream, Dienst wie Amazon) ist
        /// </summary>
        /// <param name="si">Zu bearbeitendes SonosItems</param>
        /// <param name="pl">Player um Prüfungen vorzunehmen.</param>
        /// <returns>Bearbeitetes SonosItem</returns>
        public static SonosItem CheckItemForStreaming(SonosItem si, SonosPlayer pl)
        {
            if (CheckItemForStreamingUriCheck(si.Uri))
            {
                si.Stream = true;
                if (si.Uri.StartsWith("x-rincon-stream:RINCON"))
                {
                    //Eingang eines Players
                    si.StreamContent = "Audio Eingang";
                }
                else
                {
                    if (si.StreamContent == "Audio Eingang")
                    {
                        si.StreamContent = String.Empty;
                    }
                }
                if (si.Uri.StartsWith("x-sonosapi-stream") || si.Uri.StartsWith("x-sonosapi-radio:") || si.Uri.StartsWith("aac:"))
                {
                    if (!string.IsNullOrEmpty(si.StreamContent))
                    {
                        si.Title = si.StreamContent;
                    }
                    si.StreamContent = "Radio";
                    List<string> k = pl.GetMediaInfoURIMeta();
                    si.AlbumArtURI = "/getaa?s=1&u=" + k[0];
                }
                if (si.Uri.StartsWith("x-sonos-http:") || si.Uri.StartsWith("x-sonosapi-hls-static"))
                {
                    //HTTP Dienst wie Amazon
                    si.StreamContent = "Dienst";
                }
                //Amazon
                if (si.Uri.StartsWith("x-sonosapi-hls-static"))
                {
                    //HTTP Dienst Amazon
                    si.StreamContent = "Dienst";
                }

                if (si.Uri.StartsWith("x-sonosprog-http:song") || si.Uri.StartsWith("x-sonos-http:song"))
                {
                    //HTTP Dienst Apple
                    //prüfen ob Apple Radio
                    List<string> k = pl.GetMediaInfoURIMeta();
                    if (k != null && k.Count > 1 && k[0].StartsWith("x-sonosapi-radio:"))
                    {
                        si.StreamContent = "Radio";
                    }
                    else
                    {
                        si.StreamContent = "Apple";
                    }

                }
            }
            else
            {
                if (si.StreamContent == "Audio Eingang")
                {
                    si.StreamContent = String.Empty;
                    si.Stream = false;
                }
            }
            return si;
        }

        private static Boolean CheckItemForStreamingUriCheck(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return false;

            foreach (string s in _bekannteStreamingPfade)
            {
                if (uri.Contains(s))
                    return true;
            }

            return false;
        }


    }
}