using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoleafAurora
{
    public class AuroraKnowingDevices
    {
        public AuroraKnowingDevices() { }

        public AuroraKnowingDevices(String _MacAdress, String _AuthToken, String _DeviceName)
        {
            MacAdress = _MacAdress;
            AuthToken = _AuthToken;
            DeviceName = _DeviceName;
        }

        public String MacAdress { get; set; }
        public String AuthToken { get; set; }
        public String DeviceName { get; set; }
    }
}
