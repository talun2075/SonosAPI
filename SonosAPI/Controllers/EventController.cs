using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SonosUPNP;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Newtonsoft.Json;

namespace SonosAPI.Controllers
{
    public class EventController : ApiController
    {
        
        static readonly List<StreamWriter> DisconnectedClients = new List<StreamWriter>();
        //private static readonly ConcurrentQueue<StreamWriter> _streammessage = new ConcurrentQueue<StreamWriter>();
        private static readonly List<StreamWriter> _streammessage = new List<StreamWriter>();
        
        public HttpResponseMessage Get(HttpRequestMessage request)
        {
            HttpResponseMessage response = request.CreateResponse();
            response.Content = new PushStreamContent(OnStreamAvailable, "text/event-stream");
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = new TimeSpan(0, 0, 0, 1)
            };
            return response;
        }

        public static void EventTopologieChange(object state)
        {
            foreach (var data in _streammessage)
            {

                try
                {
                    data.WriteLine("data:ZoneChange\n");
                    data.Flush();
                }
                catch
                {
                    lock (DisconnectedClients)
                    {
                        DisconnectedClients.Add(data);
                    }
                }
            }
            if (DisconnectedClients.Count == 0) return;
            lock (DisconnectedClients) { 
                foreach (StreamWriter disconnectedClient in DisconnectedClients)
                {
                    _streammessage.Remove(disconnectedClient);
                    disconnectedClient.Close();
                    disconnectedClient.Dispose();
                }
            DisconnectedClients.Clear();
        }
    }
        public static void EventPlayerChange(SonosZone pl)
        {
            if (pl == null || pl.Coordinator.CurrentState.TransportState == PlayerStatus.TRANSITIONING || _streammessage == null) return;
            foreach (var data in _streammessage.ToArray())
            {
                try
                {
                    data.WriteLine("data:" + JsonConvert.SerializeObject(pl) + data.NewLine);
                    data.Flush();
                    data.WriteLine("data:" + JsonConvert.SerializeObject(pl) + data.NewLine);
                    data.Flush();
                }
                catch
                {
                    lock (DisconnectedClients)
                    {
                        DisconnectedClients.Add(data);
                    }
                }
            }
            if (DisconnectedClients.Count == 0) return;
                lock (DisconnectedClients)
                {
                    foreach (StreamWriter disconnectedClient in DisconnectedClients)
                    {
                        _streammessage.Remove(disconnectedClient);
                        disconnectedClient.Close();
                        disconnectedClient.Dispose();
                    }
                    DisconnectedClients.Clear();
                }
            
        }
        public static void OnStreamAvailable(Stream stream, HttpContent headers, TransportContext context)
        {
            StreamWriter streamwriter = new StreamWriter(stream);
            if (!_streammessage.Contains(streamwriter))
            {
                _streammessage.Add(streamwriter);
            }
            //_streammessage.Enqueue(streamwriter);
        }

    }
}
