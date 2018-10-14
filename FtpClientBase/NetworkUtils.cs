using System;
using System.Net;
using System.Net.Sockets;

namespace FtpClientBase
{
    public static class NetworkUtils
    {
        public static IPAddress GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IsLanIp(ip))
                {
                    return ip;
                }
            }

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static bool IsLanIp(IPAddress ipAddress)
        {
            var addressBytes = ipAddress.GetAddressBytes();
            return addressBytes[0] == 10 ||
                   (addressBytes[0] == 172 && addressBytes[1] >= 16 && addressBytes[1] <= 31) ||
                   (addressBytes[0] == 192 && addressBytes[1] == 168);
        }
    }
}