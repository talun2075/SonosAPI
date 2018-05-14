using System;

namespace NanoleafAurora
{
    /// <summary>
    /// Class for your Knowing Devices.
    /// </summary>
    public class AuroraKnowingDevices
    {
        public AuroraKnowingDevices() { }

        public AuroraKnowingDevices(String _MacAdress, String _AuthToken, String _DeviceName)
        {
            MacAdress = _MacAdress;
            AuthToken = _AuthToken;
            DeviceName = _DeviceName;
        }
        public AuroraKnowingDevices(String _MacAdress, String _AuthToken, String _DeviceName, String IP)
        {
            MacAdress = _MacAdress;
            AuthToken = _AuthToken;
            DeviceName = _DeviceName;
            KnowingIP = IP;
        }
        public String MacAdress { get; set; }
        public String AuthToken { get; set; }
        public String DeviceName { get; set; }
        public String KnowingIP { get; set; }
    }
}
