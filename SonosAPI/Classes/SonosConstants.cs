﻿namespace SonosAPI.Classes
{
    public static class SonosConstants
    {
        private const int relativerVolWert = 10;
        public const string KücheName = "Küche";
        public const int KücheVolume = relativerVolWert + 4;
        public const string EsszimmerName = "Esszimmer";
        public const int EsszimmerVolume = relativerVolWert + 11;
        public const string WohnzimmerName = "Wohnzimmer";
        public const int WohnzimmerVolume = relativerVolWert + 54;
        public const string GästezimmerName = "Gästezimmer";
        public const int GästezimmerVolume = 1;
        public const string SchlafzimmerName = "Schlafzimmer";
        public const int SchlafzimmerVolume = 9;
        public const string IanzimmerName = "Ians Zimmer";
        public const int IanzimmerVolume = 7;
        public const string ArbeitszimmerName = "Arbeitszimmer";
        public const int ArbeitzimmerVolume = 10;
        /// <summary>
        /// Wird genommen, wenn man dem Player sagen möchte, dass er eine Playlist von einem Player (sich selber eingeschlossen) abspielen soll.
        /// </summary>
        public const string xrinconqueue = "x-rincon-queue:";
        /// <summary>
        /// Verweise vom Sonos auf das Filesystem. 
        /// </summary>
        public const string xfilecifs = "x-file-cifs:";
        /// <summary>
        /// Wird benutzt um Bei Kommunikation zwischen Client und Server Dummy WErte zu übergeben
        /// </summary>
        public const string empty = "leer";
        /// <summary>
        /// Ist ein Stream von einem Player z.B: bei Audio Eingang
        /// </summary>
        public const string xrinconstream = "x-rincon-stream:";
        /// <summary>
        /// Ist ein Stream von externer Quelle wie z.B. Radio
        /// </summary>
        public const string xsonosapistream = "x-sonosapi-stream";

        public const string xsonoshttp = "x-sonos-http";
        public const string AudioEingang = "Audio Eingang";
        /// <summary>
        /// Fürs Browsen der Favoriten
        /// </summary>
        public const string FV2 = "FV:2";
        /// <summary>
        /// Fürs Browsen von Sonos Playlists
        /// </summary>
        public const string SQ = "SQ:";
        /// <summary>
        /// Wird genutzt um einen Player einen anderen zuzuführen.
        /// </summary>
        public const string xrincon = "x-rincon:";
        /// <summary>
        /// Fürs Browsen nach Genres
        /// </summary>
        public const string aGenre = "A:GENRE";
        /// <summary>
        /// Fürs Browsen nach Interpreten
        /// </summary>
        public const string aAlbumArtist = "A:ALBUMARTIST";

        public const string MarantzUrl = "http://192.168.0.4";
    }
    /// <summary>
    /// Constants for Messagequeue
    /// </summary>
    public static class SonosCheckChangesConstants
    {
        /// <summary>
        /// Check Volume
        /// </summary>
        public const string Volume = "Volume";
        /// <summary>
        /// Check to SinglePlayer
        /// </summary>
        public const string SinglePlayer = "SinglePlayer";
        /// <summary>
        /// Is added to a Zone
        /// </summary>
        public const string AddToZone = "AddToZone";
        /// <summary>
        /// Is Playing
        /// </summary>
        public const string Playing = "Playing";
        /// <summary>
        /// Check the Powerstate of the Marantz AVR
        /// </summary>
        public const string MarantzPower = "Marantzpower";

        public const string NanoleafSelectedScenario = "NanoleafSelectedScenario";

    }
}