using System;

namespace SonosUPNP
{
	/// <summary>
	/// H�lt die Informationen eines Songs
	/// </summary>
    public class PlayerInfo
	{
		public string TrackURI { get; set; }
		public int TrackIndex { get; set; }
		public string TrackMetaData { get; set; }
		public TimeSpan RelTime { get; set; }
		public TimeSpan TrackDuration { get; set; }
        /// <summary>
        /// Pr�ft ob die PlayerInfo ver�ndert wurde
        /// </summary>
        /// <returns></returns>
	    public Boolean isDefault()
	    {
	        return (string.IsNullOrEmpty(TrackURI) && TrackIndex == 0 && string.IsNullOrEmpty(TrackMetaData));
	    }
	}
}