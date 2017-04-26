using System;
using System.Web.Http;
using ExternalDevices;

namespace SonosAPI.Controllers
{
    public class NanoleafController : ApiController
    {
        [HttpGet]
        public NanoLeafJson Get()
        {
            if (Nanoleaf.Initialisieren())
            {
                return Nanoleaf.NLJ;
            }
            return new NanoLeafJson() {Name = "ERROR"};
        }

        [HttpGet]
        public string SetSelectedScenario(string id)
        {
            if (Nanoleaf.Scenarios.Contains(id) && Nanoleaf.SelectedScenario != id)
            {
                Nanoleaf.SelectedScenario = id;
            }
            return Nanoleaf.SelectedScenario;
        }

        [HttpGet]
        public Boolean SetPowerState(string id)
        {
            if (string.IsNullOrEmpty(id)) return Nanoleaf.PowerOn;
            Boolean tid;
            if (Boolean.TryParse(id, out tid))
            {
                if (Nanoleaf.PowerOn != tid)
                {
                    Nanoleaf.PowerOn = tid;
                }
            }
            return Nanoleaf.PowerOn;
        }

        [HttpGet]
        public int SetBrightness(int id)
        {
            if (id > Nanoleaf.NLJ.State.Brightness.Max || id < Nanoleaf.NLJ.State.Brightness.Min) return 0;
            if (Nanoleaf.NLJ.State.Brightness.Value != id)
            {
                Nanoleaf.Brightness = id;
            }
            return Nanoleaf.Brightness;
        }
    }
}
