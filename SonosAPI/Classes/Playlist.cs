using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SonosUPNP;

namespace SonosAPI.Classes
{
    /// <summary>
    /// Sonos CurrentPlaylist eines Players
    /// </summary>
    public class Playlist
    {
        public uint NumberReturned { get; set; }
        public int TotalMatches { get; set; }
        public List<PlaylistItem> PlayListItems { get; } = new List<PlaylistItem>();
        public void FillPlaylist(SonosPlayer pl)
        {
            IList<PlaylistItem> list = new List<PlaylistItem>();
            try
            {
                var xml = pl.GetPlaylistWithTotalNumbers(NumberReturned, 0);
                if (xml[1] != null)
                {
                    TotalMatches = Convert.ToInt16(xml[1]);
                    list = ParseSonosXML(xml[0]);
                }
            }
            catch (Exception ex)
            {
                SonosHelper.ServerErrorsAdd("Playlist:FillPlaylist:Block1", ex);
            }

            //Eintrag in der Liste vorhanden
            if (TotalMatches == 0 &&  list.Count == 0)
            {
                PlayListItems.Add(new PlaylistItem() {Album = "Leer",Artist = "Leer", Title = "Leer"});
                return;
            }
            try
            {
                if (list.Count > 0)
                {
                    PlayListItems.AddRange(list);
                    NumberReturned = Convert.ToUInt16(PlayListItems.Count);
                }

                if (PlayListItems.Count < TotalMatches)
                {
                    FillPlaylist(pl);
                }
            }
            catch (Exception ex)
            {
                SonosHelper.ServerErrorsAdd("Playlist:FillPlaylist:Block2", ex);
            }
        }
        public static IList<PlaylistItem> ParseSonosXML(string xmlString)
        {
            var xml = XElement.Parse(xmlString);
            XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
            XNamespace dc = "http://purl.org/dc/elements/1.1/";
            XNamespace upnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";
            //XNamespace r = "urn:schemas-rinconnetworks-com:metadata-1-0/";

            var items = xml.Elements(ns + "item");
            var list = new List<PlaylistItem>();

            foreach (var item in items)
            {
                var track = new PlaylistItem
                {
                    Uri = (string) item.Element(ns + "res"),
                    AlbumArtURI = (string) item.Element(upnp + "albumArtURI"),
                    Album = (string) item.Element(upnp + "album"),
                    Artist = (string) item.Element(dc + "creator"),
                    Title = (string) item.Element(dc + "title")
                };
                list.Add(track);

            }
            return list;
        }
    }


    public class PlaylistItem
    {
        public string Title { get; set; }
        public string Uri { get; set; }
        public string Album { get; set; }
        public string Artist { get; set; }
        public string AlbumArtURI { get; set; }

    }
}