using System;
using SonosUPNP;

namespace SonosUPnP
{
    /// <summary>
    /// Hält Informationen über das aktuelle Gerät und aktuellem Song
    /// </summary>
    public class PlayerState
    {
        /// <summary>
        /// Stellt dar, ob gerade eine Wiedergabe läuft oder ob Pausiert wird.
        /// </summary>
        public PlayerStatus TransportState { get; set; } = PlayerStatus.TRANSITIONING;
        /// <summary>
        /// Stellt dar, ob gerade eine Wiedergabe läuft oder ob Pausiert wird.
        /// </summary>
        public String TransportStateString { get; set; } = PlayerStatus.TRANSITIONING.ToString();
        /// <summary>
        /// Anzahl der Tracks in der aktuellen Wiedergabeliste
        /// </summary>
        public int NumberOfTracks { get; set; }
        /// <summary>
        /// Nummer des aktuellen Songs in der Wiedergabeliste (beginnend mit 1)
        /// </summary>
        public int CurrentTrackNumber { get; set; }
        /// <summary>
        /// Laufzeit des aktuellen Songs
        /// </summary>
        public TimeSpan CurrentTrackDuration { get; set; }
        /// <summary>
        /// Zeitpunkt, wann das letzte mal der Status aktualisiert wurde.(AVTransport)
        /// </summary>
        public DateTime LastStateChange { get; set; }
        /// <summary>
        /// aktuelle Wiedergabe Zeit des Tracks
        /// </summary>
        public TimeSpan RelTime { get; set; }
        /// <summary>
        /// Aktueller Song als SonosItem
        /// </summary>
        public SonosItem CurrentTrack { get; set; } = new SonosItem();
        /// <summary>
        /// Liefert den nächsten Track aus.
        /// </summary>
        public SonosItem NextTrack { get; set; }
        /// <summary>
        /// Aktuelle Wiedergabeart
        /// </summary>
        public String CurrentPlayMode { get; set; }
        /// <summary>
        /// Gibt an, ob Fade aktiviert ist.
        /// </summary>
        public Boolean CurrentCrossfadeMode { get; set; }
        /// <summary>
        /// Aktuelle Lautstärke
        /// </summary>
        public int Volume { get; set; }
        /// <summary>
        /// Hält als String die Infos, wie lange noch abgespielt wird, bis die Musik aus ist.
        /// </summary>
        public String RemainingSleepTimerDuration { get; set; } = "aus";
    }
}
