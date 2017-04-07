/*   
Copyright 2006 - 2010 Intel Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using OpenSource.Utilities;

namespace OSTL.UPnP
{
    /// <summary>
    /// This is a convenience class to facilitate HTTP communication. This class
    /// handles the creation and sending of requests with an <seealso cref="OSTL.UPnP.HTTPSession"/>
    /// object. HTTPRequest also allows you to pipeline requests, whether or not the server supports it, 
    /// because that functionality is abstracted away, so even if pipelining is not supported,
    /// this object will serialize the requests, and open seperate sessions.
    /// </summary>
    public sealed class HTTPRequest
    {
        private bool ReceivedFirstResponse;

        public bool IdleTimeout = true;
        private static readonly LifeTimeMonitor KeepAliveTimer = new LifeTimeMonitor();
        LifeTimeMonitor.LifeTimeHandler KeepAliveHandler;

        public static bool PIPELINE = true;
        private bool _PIPELINE = true;

        private readonly Hashtable NotPipelinedTable = Hashtable.Synchronized(new Hashtable());

        public delegate void HeaderHandler(HTTPRequest sender, HTTPSession WebSession, HTTPMessage header, Stream StreamObj, object Tag);
        private IPEndPoint _Source;
        public IPEndPoint Source
        {
            get
            {
                return (_Source);
            }
        }
        private class StateData
        {
            public readonly object Tag;
            public readonly HeaderHandler HeaderCB;
            public readonly HTTPMessage Request;
            public readonly IPEndPoint Dest;

            public StateData(HTTPMessage req, IPEndPoint d, object Tag, HeaderHandler HeaderCB)
            {
                Dest = d;
                Request = req;
                this.Tag = Tag;
                this.HeaderCB = HeaderCB;
            }
        }

        public delegate void InactiveClosedHandler(HTTPRequest sender);
        public event InactiveClosedHandler OnInactiveClosed;

        public delegate void RequestHandler(HTTPRequest sender, HTTPMessage Response, object Tag);
        /// <summary>
        /// Fired when a the response is received
        /// </summary>
        public event RequestHandler OnResponse;
        /// <summary>
        /// Fired when anything is sent/received on the socket
        /// </summary>
        public event RequestHandler OnSniffPacket;

        public delegate void SniffHandler(HTTPRequest sender, byte[] buffer, int offset, int count);
        /// <summary>
        /// Fired when anything is sent/received on the socket
        /// </summary>
        public event SniffHandler OnSniff;

        private HTTPSession s;
        private readonly Queue TagQueue = new Queue();


        /// <summary>
        /// Instantiates a new Request Object
        /// </summary>
        public HTTPRequest()
        {
            KeepAliveHandler = KeepAliveSink;
            KeepAliveTimer.OnExpired += KeepAliveHandler;
            InstanceTracker.Add(this);
            _PIPELINE = PIPELINE;
        }


        private void KeepAliveSink(LifeTimeMonitor sender, object obj)
        {
            if (IdleTimeout == false || (int)obj != GetHashCode()) return;
            ForceCloseSession();
            if (OnInactiveClosed != null) OnInactiveClosed(this);
        }
        public IPEndPoint ProxySetting = null;

        /// <summary>
        /// Terminates and disposes this object
        /// </summary>
        public void Dispose()
        {
            lock (TagQueue)
            {
                HTTPSession x = s;
                if (x != null)
                    x.Close();
                s = null;
                TagQueue.Clear();
            }
        }

        public void SetSniffHandlers()
        {
            if (s != null)
            {
                s.OnSniff += SniffSink;
                s.OnSniffPacket += SniffPacketSink;
            }
        }
        public void ReleaseSniffHandlers()
        {
            if (s != null)
            {
                s.OnSniff -= SniffSink;
                s.OnSniffPacket -= SniffPacketSink;
            }
        }

        internal void ForceCloseSession()
        {
            try
            {
                s.Close();
            }
            catch (Exception ex)
            {
                EventLogger.Log(ex);
            }
        }

        /// <summary>
        /// Pipelines a request packet
        /// </summary>
        /// <param name="dest">Destination IPEndPoint</param>
        /// <param name="MSG">HTTPMessage Packet</param>
        /// <param name="Tag">State Data</param>
        public void PipelineRequest(IPEndPoint dest, HTTPMessage MSG, object Tag)
        {
            ContinueRequest(dest, "", Tag, MSG);
        }

        /// <summary>
        /// Pipelines a Uri request
        /// </summary>
        /// <param name="Resource">Uri to GET</param>
        /// <param name="Tag">State Data</param>
        public void PipelineRequest(Uri Resource, object Tag)
        {
            object[] Args = { Resource, Tag };

            string IP = Resource.Host;
            if (Resource.HostNameType == UriHostNameType.Dns)
            {
                Dns.BeginGetHostEntry(IP, GetHostByNameSink, Args);
            }
            else
            {
                ContinueRequest(
                    new IPEndPoint(IPAddress.Parse(Resource.Host), Resource.Port),
                    HTTPMessage.UnEscapeString(Resource.PathAndQuery),
                    Tag,
                    null);

            }
        }
        private void GetHostByNameSink(IAsyncResult result)
        {
            IPHostEntry e;
            try
            {
                e = Dns.EndGetHostEntry(result);
            }
            catch (Exception ex)
            {
                // Could not resolve?
                EventLogger.Log(ex);
                return;
            }

            object[] Args = (object[])result.AsyncState;
            Uri Resource = (Uri)Args[0];
            object Tag = Args[1];

            ContinueRequest(
                new IPEndPoint(e.AddressList[0], Resource.Port),
                HTTPMessage.UnEscapeString(Resource.PathAndQuery),
                Tag,
                null);
        }

        private string RemoveIPv6Scope(string addr)
        {
            int i = addr.IndexOf('%');
            if (i >= 0) addr = addr.Substring(0, i);
            return addr;
        }

        private void ContinueRequest(IPEndPoint dest, string PQ, object Tag, HTTPMessage MSG)
        {
            HTTPMessage r;
            if (MSG == null)
            {
                r = new HTTPMessage();
                r.Directive = "GET";
                r.DirectiveObj = PQ;
                if (dest.AddressFamily == AddressFamily.InterNetwork) r.AddTag("Host", dest.ToString());
                if (dest.AddressFamily == AddressFamily.InterNetworkV6) r.AddTag("Host", "[" + RemoveIPv6Scope(dest.ToString()) + "]");
            }
            else
            {
                r = MSG;
            }

            lock (TagQueue)
            {
                IdleTimeout = false;
                KeepAliveTimer.Remove(GetHashCode());

                if ((PIPELINE == false && _PIPELINE == false) || (_PIPELINE == false))
                {
                    HTTPRequest TR = new HTTPRequest();
                    TR.ProxySetting = ProxySetting;
                    TR._PIPELINE = true;
                    if (OnSniff != null) TR.OnSniff += NonPipelinedSniffSink;
                    if (OnSniffPacket != null) TR.OnSniffPacket += NonPipelinedSniffPacketSink;
                    TR.OnResponse += NonPipelinedResponseSink;
                    NotPipelinedTable[TR] = TR;
                    TR.PipelineRequest(dest, r, Tag);
                    return;
                }

                TagQueue.Enqueue(new StateData(r, dest, Tag, null));

                IPAddress localif = IPAddress.Any;
                if (dest.AddressFamily == AddressFamily.InterNetworkV6) localif = IPAddress.IPv6Any;

                if (s == null)
                {
                    ReceivedFirstResponse = false;
                    if (ProxySetting != null)
                    {
                        s = new HTTPSession(new IPEndPoint(localif, 0),
                            ProxySetting,
                            CreateSink,
                            CreateFailedSink,
                            null);
                    }
                    else
                    {
                        s = new HTTPSession(new IPEndPoint(localif, 0),
                            dest,
                            CreateSink,
                            CreateFailedSink,
                            null);
                    }
                }
                else
                {
                    if (s.IsConnected && ReceivedFirstResponse)
                    {
                        try
                        {
                            if (ProxySetting == null)
                            {
                                s.Send(r);
                            }
                            else
                            {
                                HTTPMessage pr = (HTTPMessage)r.Clone();
                                pr.DirectiveObj = "http://" + dest + pr.DirectiveObj;
                                pr.Version = "1.0";
                                s.Send(pr);
                            }
                        }
                        catch (Exception ex)
                        {
                            EventLogger.Log(ex);
                        }
                    }
                }
            }
        }

        private void NonPipelinedSniffSink(HTTPRequest sender, byte[] buffer, int offset, int count)
        {
            if (OnSniff != null)
                OnSniff(this, buffer, offset, count);
        }
        private void NonPipelinedSniffPacketSink(HTTPRequest sender, HTTPMessage Response, object Tag)
        {
            if (OnSniffPacket != null)
                OnSniffPacket(this, Response, Tag);
        }

        private void NonPipelinedResponseSink(HTTPRequest sender, HTTPMessage Response, object Tag)
        {
            //			OpenSource.Utilities.EventLogger.Log(sender.s,System.Diagnostics.EventLogEntryType.Information,"TryingToDispose");
            _Source = sender.Source;
            NotPipelinedTable.Remove(sender);
            sender.Dispose();
            if (OnResponse != null)
                OnResponse(this, Response, Tag);
        }
        private void HeaderSink(HTTPSession sender, HTTPMessage header, Stream TheStream)
        {
            _Source = sender.Source;
            StateData sd;
            if (TheStream != null)
            {
                // This is the result of post-headers in a chunked document
                sd = (StateData)sender.StateObject;
                object Tag = sd.Tag;
                if (sd.HeaderCB != null) sd.HeaderCB(this, sender, header, TheStream, Tag);
                sender.StateObject = null;
                KeepAliveTimer.Add(GetHashCode(), 10);
            }
            else
            {
                lock (TagQueue)
                {
                    sd = (StateData)TagQueue.Dequeue();
                }
                sender.StateObject = sd;
                object Tag = sd.Tag;
                if (sd.HeaderCB != null)
                {
                    sd.HeaderCB(this, sender, header, null, Tag);
                    if (sender.UserStream != null && !sender.IsChunked)
                    {
                        // If I don't set this to null, this holds a strong reference, resulting in
                        // possible memory leaks
                        sender.StateObject = null;
                    }
                }
            }
        }
        private void ReceiveSink(HTTPSession sender, HTTPMessage msg)
        {
            StateData sd = (StateData)sender.StateObject;
            object Tag = sd.Tag;

            if (msg.Version == "1.0" || msg.Version == "0.9")
            {
                sender.Close();
            }
            else
            {
                if (msg.GetTag("Connection").ToUpper() == "CLOSE")
                {
                    sender.Close();
                }
            }


            if (OnResponse != null) OnResponse(this, msg, Tag);
            // If I don't set this to null, this holds a strong reference, resulting in
            // possible memory leaks
            sender.StateObject = null;
            lock (TagQueue)
            {
                if (TagQueue.Count == 0)
                {
                    IdleTimeout = true;
                    KeepAliveTimer.Add(GetHashCode(), 10);
                }
            }
        }

        private void SniffSink(byte[] buffer, int offset, int count)
        {
            if (OnSniff != null)
            {
                OnSniff(this, buffer, offset, count);
            }
        }
        private void SniffPacketSink(HTTPSession sender, HTTPMessage MSG)
        {
            if (OnSniffPacket != null)
            {
                if (sender.StateObject == null)
                {
                    OnSniffPacket(this, MSG, null);
                    return;
                }
                StateData sd = (StateData)sender.StateObject;
                object Tag = sd.Tag;

                OnSniffPacket(this, MSG, Tag);
            }
        }

        private void StreamDoneSink(HTTPSession sender, Stream StreamObject)
        {
            //2ToDo: Place callback from StateData here, to notify Stream is Done
        }

        private void RequestAnsweredSink(HTTPSession ss)
        {
            lock (TagQueue)
            {
                if (!ReceivedFirstResponse)
                {
                    ReceivedFirstResponse = true;
                    IEnumerator en = TagQueue.GetEnumerator();
                    while (en.MoveNext())
                    {
                        StateData sd = (StateData)en.Current;
                        try
                        {
                            if (ProxySetting == null)
                            {
                                ss.Send(sd.Request);
                            }
                            else
                            {
                                HTTPMessage pr = (HTTPMessage)sd.Request.Clone();
                                pr.DirectiveObj = "http://" + sd.Dest + pr.DirectiveObj;
                                pr.Version = "1.0";
                                ss.Send(pr);
                            }
                        }
                        catch (Exception ex)
                        {
                            EventLogger.Log(ex);
                        }
                    }
                }
            }
        }

        private void CreateSink(HTTPSession ss)
        {
            lock (TagQueue)
            {
                ss.OnHeader += HeaderSink;
                ss.OnReceive += ReceiveSink;
                ss.OnClosed += CloseSink;
                ss.OnStreamDone += StreamDoneSink;
                ss.OnRequestAnswered += RequestAnsweredSink;

                if (OnSniff != null) ss.OnSniff += SniffSink;
                if (OnSniffPacket != null) ss.OnSniffPacket += SniffPacketSink;

                StateData sd = (StateData)TagQueue.Peek();

                try
                {
                    if (ProxySetting == null)
                    {
                        ss.Send(sd.Request);
                    }
                    else
                    {
                        HTTPMessage pr = (HTTPMessage)sd.Request.Clone();
                        pr.DirectiveObj = "http://" + sd.Dest + pr.DirectiveObj;
                        pr.Version = "1.0";
                        ss.Send(pr);
                    }
                }
                catch (Exception exc)
                {
                    EventLogger.Log(exc);
                }
            }
        }

        private void CloseSink(HTTPSession ss)
        {
            bool err = false;
            string erraddr = "";

            ss.CancelAllEvents();
            lock (TagQueue)
            {
                KeepAliveTimer.Remove(GetHashCode());

                if (TagQueue.Count > 0)
                {
                    EventLogger.Log(this, EventLogEntryType.Information, "Switching Pipeline Modes [" + ss.GetHashCode() + "]");
                    _PIPELINE = false;
                    if (!ReceivedFirstResponse)
                    {
                        erraddr = ((StateData)TagQueue.Peek()).Dest.ToString();
                    }
                }

                if (!ReceivedFirstResponse)
                {
                    EventLogger.Log(this, EventLogEntryType.Error, "Server[" + erraddr + "] closed socket without answering");
                    err = true;
                }

                while (TagQueue.Count > 0)
                {
                    StateData sd = (StateData)TagQueue.Dequeue();
                    if (!err)
                    {
                        HTTPRequest TR = new HTTPRequest();
                        TR.ProxySetting = ProxySetting;
                        TR._PIPELINE = true;
                        if (OnSniff != null) TR.OnSniff += NonPipelinedSniffSink;
                        if (OnSniffPacket != null) TR.OnSniffPacket += NonPipelinedSniffPacketSink;
                        TR.OnResponse += NonPipelinedResponseSink;
                        NotPipelinedTable[TR] = TR;
                        TR.PipelineRequest(sd.Dest, sd.Request, sd.Tag);
                    }
                    else
                    {
                        if (OnResponse != null) OnResponse(this, null, sd.Tag);

                    }
                }
                s = null;
            }
        }

        private void CreateFailedSink(HTTPSession ss)
        {
            lock (TagQueue)
            {
                while (TagQueue.Count > 0)
                {
                    StateData sd = (StateData)TagQueue.Dequeue();
                    EventLogger.Log(this, EventLogEntryType.Error, "Connection Attempt to [" + sd.Dest + "] Refused/Failed");

                    object Tag = sd.Tag;
                    if (sd.HeaderCB != null)
                    {
                        sd.HeaderCB(this, ss, null, null, Tag);
                    }
                    else
                    {
                        if (OnResponse != null) OnResponse(this, null, Tag);
                    }
                }
                s = null;
            }
        }
    }
}
