using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using SonosUPNP;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace SonosAPI.Controllers
{
    //public class EventController : ApiController
    //{
    //    //Eventing wie dieses wird nicht vom IIS 7 suportet.
    //    static readonly ConcurrentQueue<StreamWriter> ConnectedClients = new ConcurrentQueue<StreamWriter>();

    //    public EventController()
    //    {

    //    }
    //    //private static readonly Lazy<Timer> _timer = new Lazy<Timer>(() => new Timer(EventTopologieChange, null, 0, 10000));
    //    private static readonly ConcurrentQueue<StreamWriter> _streammessage = new ConcurrentQueue<StreamWriter>();

    //    public HttpResponseMessage Get(HttpRequestMessage request)
    //    {
    //        //Timer t = _timer.Value;
    //        HttpResponseMessage response = request.CreateResponse();
    //        response.Content = new PushStreamContent(OnStreamAvailable, "text/event-stream");
    //        return response;
    //    }
    //    public static void EventTopologieChange(object state)
    //    {
    //        foreach (var data in _streammessage)
    //        {

    //            data.WriteLine("data:" + DevicesController.Topologiechanged + "\n");
    //            data.Flush();
    //        }
    //    }
    //    public static void EventPlayerChange(SonosPlayer pl)
    //    {
    //        foreach (var data in _streammessage)
    //        {
    //            //Dictionary<String, String> stch = new Dictionary<String, String>();
    //            //stch.Add(pl.UUID, pl.CurrentState.LastStateChange.ToString());
    //            String[] k = new string[2];
    //            k[0] = pl.UUID;
    //            k[1] = pl.CurrentState.LastStateChange.Ticks.ToString();
    //            data.WriteLine("data:" + JsonConvert.SerializeObject(k) + "\n");
    //            data.Flush();
    //        }
    //    }
    //    public static void OnStreamAvailable(Stream stream, HttpContent headers, TransportContext context)
    //    {
    //        StreamWriter streamwriter = new StreamWriter(stream);
    //        //if (!_streammessage.Contains(streamwriter))
    //        //{
    //        //    _streammessage.Enqueue(streamwriter);
    //        //}
    //        _streammessage.Enqueue(streamwriter);
    //    }

    //}
}
