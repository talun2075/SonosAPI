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
            try
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
            catch (Exception ex)
            {
                //ignore
                return null;
            }
        }

        public static void EventTopologieChange(object state)
        {
            try
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
            catch (Exception ex)
            {
                //ignore
            }
        }
        
        public static void EventPlayerChange(SonosPlayer pl)
        {
            try
            {
                if (pl == null || pl.CurrentState.TransportState == PlayerStatus.TRANSITIONING || _streammessage == null)
                    return;
                foreach (var data in _streammessage.ToArray())
                {
                    try
                    {
                        var t = new RinconLastChangeItem
                        {
                            UUID = pl.UUID,
                            LastChange = pl.CurrentState.LastStateChange
                        };
                        data.WriteLine("data:" + JsonConvert.SerializeObject(t) + "\n\n");
                        data.Flush();
                        //data.WriteLine("data:" + JsonConvert.SerializeObject(t) + "\n\n");
                        //data.Flush();
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
            catch (Exception ex)
            {
                //ignore
            }
        }
        public static void OnStreamAvailable(Stream stream, HttpContent headers, TransportContext context)
        {
            try
            {
                StreamWriter streamwriter = new StreamWriter(stream);
                if (!_streammessage.Contains(streamwriter))
                {
                    _streammessage.Add(streamwriter);
                }
            }
            catch (Exception ex)
            {
                //ignore
            }
        }
    }

    public class RinconLastChangeItem
    {
        public String UUID { get; set; }
        public DateTime LastChange { get; set; }
    }
}
