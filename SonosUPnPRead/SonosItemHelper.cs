using System;
using System.Collections.Generic;
using System.Linq;
using SonosUPNP;

namespace SonosUPnP
{
    public static class SonosItemHelper
    {

        private const string audio = "Audio Eingang";
        private const string radio = "Radio";
        private const string service = "Dienst";
        private const string xsonosapi = "x-sonosapi";
        private const string xsonosapiradio = xsonosapi + "-radio";
        private const string xsonosapistream = xsonosapi + "-stream";
        private const string xsonosapihlsstatic = xsonosapi + "-hls-static";
        private static readonly List<string> _bekannteStreamingPfade = new List<string> { "x-rincon-stream:RINCON", "x-rincon-mp3radio", xsonosapiradio, xsonosapihlsstatic, xsonosapistream, "x-sonos-http", "x-sonosprog-http", "aac:" };
        /// <summary>
        /// Prüft ob ein Item ein Streaming Item (Stream, Dienst wie Amazon) ist
        /// </summary>
        /// <param name="si">Zu bearbeitendes SonosItems</param>
        /// <param name="pl">Player um Prüfungen vorzunehmen.</param>
        /// <returns>Bearbeitetes SonosItem</returns>
        public static SonosItem CheckItemForStreaming(SonosItem si, SonosPlayer pl)
        {
            if (pl == null) return si;
            try
            {
                if (CheckItemForStreamingUriCheck(si.Uri))
                {
                    si.Stream = true;
                    if (si.Uri.StartsWith("x-rincon-stream:RINCON"))
                    {
                        //Eingang eines Players
                        si.StreamContent = audio;
                    }
                    else
                    {
                        if (si.StreamContent == audio)
                        {
                            si.StreamContent = String.Empty;
                        }
                    }
                    if (si.Uri.StartsWith(xsonosapistream) || si.Uri.StartsWith(xsonosapiradio) ||
                        si.Uri.StartsWith("aac:") || si.Uri.StartsWith("x-rincon-mp3radio"))
                    {
                        //Radio
                        si = GetStreamRadioStuff(si, pl);
                    }
                    if (si.Uri.StartsWith("x-sonos-http:") || si.Uri.StartsWith(xsonosapihlsstatic))
                    {
                        //HTTP Dienst wie Amazon
                        si.StreamContent = service;
                        //test
                        var minfo = pl.GetMediaInfoURIMeta();
                        if (minfo[0].StartsWith(xsonosapiradio))
                        {
                            si.ClassType = "object.item.audioItem.audioBroadcast";
                        }
                    }
                    if (si.Uri.StartsWith("x-sonosprog-http:song") || si.Uri.StartsWith("x-sonos-http:song"))
                    {
                        //HTTP Dienst Apple
                        //prüfen ob Apple Radio
                        List<string> k = pl.GetMediaInfoURIMeta();
                        if (k != null && k.Count > 1 && k[0].StartsWith(xsonosapiradio))
                        {
                            si.StreamContent = radio;
                        }
                        else
                        {
                            si.StreamContent = "Apple";
                        }

                    }
                }
                else
                {
                    if (si.StreamContent == audio)
                    {
                        si.StreamContent = String.Empty;
                        si.Stream = false;
                    }
                }
            }
            catch (Exception ex)
            {
                pl.ServerErrorsAdd("SonosItemHelper:CheckItemForStreaming ", ex);
            }
            return si;
        }
        /// <summary>
        /// Prüft ob es sich bei der uri um einen Streamingpfad handelt.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Boolean CheckItemForStreamingUriCheck(string uri)
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
        /// <summary>
        /// Baut Cover, Titel und Artist fpr Radio Sender auf.
        /// </summary>
        /// <param name="si"></param>
        /// <param name="pl"></param>
        /// <returns></returns>
        private static SonosItem GetStreamRadioStuff(SonosItem si, SonosPlayer pl)
        {
            try
            {
                try
                {

                    if (si.Title.StartsWith(xsonosapi) || si.Title == "Playlist" || !CheckRadioTitle(si.Title))
                    {
                        si.Title = String.Empty;
                    }
                    if (CheckRadioTitle(si.StreamContent))
                    {
                        si.Title = si.StreamContent;
                    }
                    si.StreamContent = radio;
                }
                catch (Exception ex)
                {
                    pl.ServerErrorsAdd("SonosItemHelper:GetStreamRadioStuff:Block1 ", ex);
                }
                
                List<string> k = new List<string>();
                try
                {
                    k = pl.GetMediaInfoURIMeta();
                    if (k.Count != 0)
                    {
                        si.AlbumArtURI = "/getaa?s=1&u=" + k[0];
                    }
                }
                catch (Exception ex)
                {
                    pl.ServerErrorsAdd("SonosItemHelper:GetStreamRadioStuff:Block2:CoverArt ", ex);
                }
                try
                {
                    if (k.Count > 0 && k.ElementAtOrDefault(2) != null && !string.IsNullOrEmpty(k[2]))
                    {
                        SonosItem streaminfo = SonosItem.ParseSingleItem(pl.GetAktSongInfo().TrackMetaData);
                        var x = SonosItem.ParseSingleItem(k[2]);
                        si.Artist = x.Title;
                        if (CheckRadioTitle(streaminfo.StreamContent))
                        {
                            si.Title = streaminfo.StreamContent.Contains("|")
                                ? streaminfo.StreamContent.Split('|')[0]
                                : streaminfo.StreamContent;
                        }
                    }
                }
                catch (Exception ex)
                {
                    pl.ServerErrorsAdd("SonosItemHelper:GetStreamRadioStuff:Block3:Anzahl K:"+k.Count, ex);
                }
            }
            catch (Exception ex)
            {
                pl.ServerErrorsAdd("SonosItemHelper:GetStreamRadioStuff ", ex);
            }
            return si;
        }
        /// <summary>
        /// Prüft den Streamcontent auf bekannte Lückenfüller, die nicht angezeigt werden sollen. 
        /// </summary>
        /// <param name="org"></param>
        /// <returns></returns>
        private static Boolean CheckRadioTitle(string org)
        {
            return (!string.IsNullOrEmpty(org) && !org.StartsWith(xsonosapi) && !org.Contains("-live-mp3") && !org.StartsWith("ADBREAK_") && !org.StartsWith("ZPSTR_CONNECTING"));

        }
    }
}