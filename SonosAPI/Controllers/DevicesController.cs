using System;
using System.Collections.Generic;
using System.Web.Http;
using SonosAPI.Classes;
using SonosUPNP;

namespace SonosAPI.Controllers
{
    public class DevicesController : ApiController
    {
        #region Klassenvariablen

        /// <summary>
        /// Alles was von den XFile Informationen gelöscht werden muß;
        /// </summary>
        public static string RemoveFromUri { get; set; } = SonosConstants.xfilecifs;
        #endregion KlassenVariablen
        #region Public Methoden
        /// <summary>
        /// Initialisierung des ganzen
        /// </summary>
        /// <returns></returns>
        public string Get()
        {
            try
            {
                SonosHelper.Initialisierung();
                return "Ready";
            }
            catch (Exception x)
            {
                SonosHelper.ServerErrorsAdd("DeviceGetError", x);
                return x.Message;
            }
        }
        /// <summary>
        /// Gibt eine Liste mit allen Zonen aus
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IList<SonosZone> GetCoordinator(int id)
        {
            if (SonosHelper.Sonos == null || SonosHelper.Sonos.Players.Count == 0)
            {
                SonosHelper.Initialisierung();
            }
            SonosHelper.RemoveCoordinatorFromZonePlayerList();
            if (SonosHelper.Sonos == null) return null;
            return SonosHelper.Sonos.Zones;
        }
        #endregion Public Methoden
    }
}
