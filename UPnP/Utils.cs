using System;
using System.Net;
using System.Net.Sockets;

namespace OSTL.UPnP
{
    public class Utils
    {
        public static IPAddress UpnpMulticastV4Addr = IPAddress.Parse("239.255.255.250");
        public static IPAddress UpnpMulticastV6Addr1 = IPAddress.Parse("FF05::C"); // Site local
        public static IPAddress UpnpMulticastV6Addr2 = IPAddress.Parse("FF02::C"); // Link local
        public static IPEndPoint UpnpMulticastV4EndPoint = new IPEndPoint(UpnpMulticastV4Addr, 1900);
        public static IPEndPoint UpnpMulticastV6EndPoint1 = new IPEndPoint(UpnpMulticastV6Addr1, 1900);
        public static IPEndPoint UpnpMulticastV6EndPoint2 = new IPEndPoint(UpnpMulticastV6Addr2, 1900);

        public static string GetMulticastAddr(IPAddress addr)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork) return "239.255.255.250";
            if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (addr.IsIPv6LinkLocal) return "FF02::C";
                return "FF05::C";
            }
            return "";
        }

        public static string GetMulticastAddrBraket(IPAddress addr)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork) return "239.255.255.250";
            if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (addr.IsIPv6LinkLocal) return "[FF02::C]";
                return "[FF05::C]";
            }
            return "";
        }

        public static string GetMulticastAddrBraketPort(IPAddress addr)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork) return "239.255.255.250:1900";
            if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (addr.IsIPv6LinkLocal) return "[FF02::C]:1900";
                return "[FF05::C]:1900";
            }
            return "";
        }

        private static bool MonoDetected;
        private static bool MonoActive;
        public static bool IsMono()
        {
            if (MonoDetected) return MonoActive;
            MonoActive = (Type.GetType("Mono.Runtime") != null);
            MonoDetected = true;
            return MonoActive;
        }
    }
}
