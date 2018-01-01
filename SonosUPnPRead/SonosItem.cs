using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SonosUPNP
{
    /// <summary>
    /// Informationen über ein einzeles Lied oder eine Playlist
    /// </summary>
	public class SonosItem
	{
        public string Uri { get; set; }
        public string MetaData { get; set; }
        public string AlbumArtURI { get; set; } = String.Empty;
        public string Artist { get; set; }
        public string Album { get; set; } = String.Empty;
        public string Title { get; set; }
        public string Description { get; set; }
        public string ContainerID { get; set; }
        public string ParentID { get; set; }
        public string ItemID { get; set; }
        public MP3File.MP3File MP3 { get; set; }
        public bool Stream { get; set; }
        public string StreamContent { get; set; }
        public string ClassType { get; set; }

        /// <summary>
        /// Konstruktor mit Leerem SonosItem
        /// </summary>
        public SonosItem()
        {
        }
        /// <summary>
        /// Konstruktor mit komplettem SonosItem
        /// </summary>
        /// <param name="_tr"></param>
        public SonosItem(SonosItem _tr)
        {
            Uri = _tr.Uri;
            MetaData = _tr.MetaData;
            Title = _tr.Title;
            AlbumArtURI = _tr.AlbumArtURI;
            Artist = _tr.Artist;
            Album = _tr.Album;
            Description = _tr.Description;
            ContainerID = _tr.ContainerID;
            ParentID = _tr.ParentID;
            ItemID = _tr.ItemID;
            Stream = _tr.Stream;
            StreamContent = _tr.StreamContent;
            ClassType = _tr.ClassType;
        }
        /// <summary>
        /// Liefert eine Liste mit SonosItems zurück. (Tracks oder Playlisten)
        /// </summary>
        /// <param name="xmlString"></param>
        /// <returns></returns>
		public static IList<SonosItem> Parse(string xmlString)
		{
			var xml = XElement.Parse(xmlString);
			XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
			XNamespace dc = "http://purl.org/dc/elements/1.1/";
			XNamespace upnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";
			XNamespace r = "urn:schemas-rinconnetworks-com:metadata-1-0/";

			var items = xml.Elements(ns + "item");
			var list = new List<SonosItem>();

			foreach (var item in items)
			{
			    var track = new SonosItem
			    {
			        Uri = (string) item.Element(ns + "res"),
			        MetaData = (string) item.Element(r + "resMD"),
			        StreamContent = (string) item.Element(r + "streamContent"),
			        AlbumArtURI = (string) item.Element(upnp + "albumArtURI"),
                    ClassType = (string)item.Element(upnp + "class"),
                    Album = (string) item.Element(upnp + "album"),
			        Artist = (string) item.Element(dc + "creator"),
			        Title = (string) item.Element(dc + "title"),
			        Description = (string) item.Element(r + "description"),
			        ParentID = item.FirstAttribute.NextAttribute.Value,
			        ItemID = item.FirstAttribute.Value
			    };
			    if (string.IsNullOrEmpty(track.MetaData))
                {
                    //Wenn die Metadata nicht befüllt sind, werden diese selber gebaut
                    string meta = "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\""+track.ItemID+"\" parentID=\""+track.ParentID+"\" restricted=\"true\"><dc:title>"+track.Title+"</dc:title><upnp:class>object.item.audioItem.musicTrack</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">RINCON_AssociatedZPUDN</desc></item></DIDL-Lite>";
                    track.MetaData = meta;
                }


                list.Add(track);
					         
			}
            //Playlisten sind nicht item sondern Container
            if (list.Count == 0)
            {
                items = xml.Elements(ns + "container");
                foreach (var item in items)
                {
                    var track = new SonosItem();
                    track.Uri = (string)item.Element(ns + "res");
                    track.Title = (string)item.Element(dc + "title");
                    track.AlbumArtURI = (string)item.Element(upnp + "albumArtURI");
                    track.ClassType = (string) item.Element(upnp + "class");
                    track.ContainerID = item.FirstAttribute.Value;
                    track.ParentID = item.FirstAttribute.NextAttribute.Value;
                    list.Add(track);
                }
                if (list.Count == 1)
                {
                    //wenn nur ein Eintrag, dann ist die übergebene variable auch der Metadata eintrag
                    list[0].MetaData = xmlString;
                }
            }
			return list;
		}
        /// <summary>
        /// Ermittelt aus dem DIDL XML ein SonosItem
        /// </summary>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static SonosItem ParseSingleItem(string xmlString)
        {
            if (!string.IsNullOrEmpty(xmlString))
            {
                var xml = XElement.Parse(xmlString);
                XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
                XNamespace dc = "http://purl.org/dc/elements/1.1/";
                XNamespace upnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";
                XNamespace r = "urn:schemas-rinconnetworks-com:metadata-1-0/";

                var items = xml.Elements(ns + "item");

                var list = new List<SonosItem>();

                foreach (var item in items)
                {
                    var track = new SonosItem();
                    track.Uri = (string)item.Element(ns + "res");
                    track.MetaData = (string)item.Element(r + "resMD");
                    var taau = (string) item.Element(upnp + "albumArtURI");
                    track.AlbumArtURI = String.IsNullOrEmpty(taau) ?String.Empty : taau;
                    track.ClassType = (string)item.Element(upnp + "class");
                    var tal = (string) item.Element(upnp + "album");
                    track.Album = String.IsNullOrEmpty(tal) ? String.Empty : tal;
                    var tar = (string)item.Element(dc + "creator");
                    track.Artist = String.IsNullOrEmpty(tar) ? String.Empty : tar; 
                    //Title | Wenn Streamcontent vorhanden, dann wird radio abgespielt und der Titel ist falsch. 
                    track.StreamContent = (string)item.Element(r + "streamContent");
                    string tti = (string)item.Element(dc + "title");
                    track.Title = String.IsNullOrEmpty(tti) ? String.Empty : tti;
                    track.Description = (string)item.Element(r + "description");
                    if (track.ItemID == null || track.ParentID == null)
                    {
                        foreach (XAttribute itemAttri in item.Attributes())
                        {
                            if (track.ItemID == null)
                            {
                                if (itemAttri.Name == "id")
                                {
                                    track.ItemID = itemAttri.Value;
                                }
                            }
                            if (track.ParentID == null)
                            {
                                if (itemAttri.Name == "parentID")
                                {
                                    track.ParentID = itemAttri.Value;
                                }
                            }
                            if (track.ItemID != null && track.ParentID != null)
                            {
                                break;
                            }

                        }
                    }
                    list.Add(new SonosItem(track));

                }

                return list[0];
            }
             return new SonosItem();
        }
	}
}