using System;
using System.Web.Http;
using ExternalDevices;

namespace SonosAPI.Controllers
{
    /// <summary>
    /// Schnittstelle/API für die Nanoleaf Aurora
    /// </summary>
    public class NanoleafController : ApiController
    {
        /// <summary>
        /// Get Data
        /// </summary>
        /// <returns>Nanoleaf Object</returns>
        [HttpGet]
        public NanoLeafJson Get()
        {
            if (Nanoleaf.Initialisieren())
            {
                return Nanoleaf.NLJ;
            }
            
            return new NanoLeafJson() {Name = "ERROR"};
        }
        /// <summary>
        /// Set Scenario
        /// </summary>
        /// <param name="id">Name of Scenario</param>
        /// <returns></returns>
        [HttpGet]
        public string SetSelectedScenario(string id)
        {
            if (Nanoleaf.Scenarios.Contains(id) && Nanoleaf.SelectedScenario != id)
            {
                Nanoleaf.SelectedScenario = id;
            }
            return Nanoleaf.SelectedScenario;
        }
        /// <summary>
        /// Set Powerstate
        /// </summary>
        /// <param name="id">true/false</param>
        /// <returns></returns>
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
        /// <summary>
        /// Brightness /Helligkeit
        /// </summary>
        /// <param name="id">Number between min and max</param>
        /// <returns>Brightness</returns>
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
        /// <summary>
        /// Setzen eins zufälligen Scenarios
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public String SetRandomScenario(int id)
        {
            return Nanoleaf.SetRandomScenario();
        }

        [HttpGet]
        public Boolean SetHue(int id)
        {
            if (id < Nanoleaf.NLJ.State.Hue.Min || id > Nanoleaf.NLJ.State.Hue.Max) return false;
            Nanoleaf.Hue = id;
            return true;
        }
    }
}
