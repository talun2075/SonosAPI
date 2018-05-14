using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SonosAPI.Classes
{
    /// <summary>
    /// Klasse, die Sonos Streaming Elemente Abspeichert und verwaltet
    /// </summary>
    public static class SonosStreamRating
    {
        #region Klassenvariablen
        /// <summary>
        /// XML Serialisierer für die Serialisierung
        /// </summary>
        static private readonly XmlSerializer _xmls = new XmlSerializer(typeof(List<MP3File.MP3File>));
        private static readonly string _savepath = SonosHelper.LoggingPfad+ @"\StreamRating\";
        private static readonly string _savepathfile = _savepath+"SavedStream.xml";

        #endregion Klassenvariablen

        /// <summary>
        /// Läd evtl. vorhandene Items
        /// </summary>
        /// <returns></returns>
        public static Boolean LoadRatedItems()
        {
            try
            {
                if (File.Exists(_savepathfile))
                {
                    StreamReader reader = new StreamReader(_savepathfile);
                    RatedListItems = (List<MP3File.MP3File>) _xmls.Deserialize(reader);
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
        /// <summary>
        /// Fügt ein Item hinzu oder ersetzt das vorhandene
        /// </summary>
        /// <param name="mf"></param>
        /// <returns></returns>
        public static Boolean AddItem(MP3File.MP3File mf)
        {
            try
            {
                MP3File.MP3File ent = RatedListItems.Find(x => x.Pfad == mf.Pfad);
                if (ent == null)
                {
                    //nicht gefunden daher zufügen
                    RatedListItems.Add(mf);
                }
                else
                {
                    RatedListItems.Remove(ent);
                    RatedListItems.Add(mf);
                }
                WriteData();
            }
            catch
            {
                return false;
            }

            return false;
        }
        /// <summary>
        /// Schreibt die Informationen nieder
        /// </summary>
        private static void WriteData()
        {
            try
            {
                if (File.Exists(_savepath))
                {
                    File.Delete(_savepath);
                }
                Directory.CreateDirectory(_savepath);
                StreamWriter textWriter = new StreamWriter(_savepathfile);
                _xmls.Serialize(textWriter, RatedListItems);
                textWriter.Close();
                textWriter.Dispose();
            }
            catch (Exception ex)
            {
                SonosHelper.TraceLog("SonosStreamRating","Writedata:Exception:"+ex.Message);
            }
        }
        #region Eigenschaften
        /// <summary>
        /// Liste aller Items
        /// </summary>
        public static List<MP3File.MP3File> RatedListItems { get; private set; } = new List<MP3File.MP3File>();
        #endregion Eigenschaften
    }
}
 