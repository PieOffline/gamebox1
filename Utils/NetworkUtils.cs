using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameBox.Utils
{
    public static class NetworkUtils
    {
        // Mapping of IP last octets to fruit codes
        private static readonly Dictionary<int, string> IpToFruit = new()
        {
            { 1, "Apple" },
            { 2, "Blackberry" },
            { 3, "Carrot" },
            { 4, "Date" },
            { 5, "Egg" },
            { 6, "Fig" },
            { 7, "Grape" },
            { 8, "Honey" },
            { 9, "Ice" },
            { 10, "Jam" },
            { 11, "Kiwi" },
            { 12, "Lemon" },
            { 13, "Mango" },
            { 14, "Nut" },
            { 15, "Orange" },
            { 16, "Peach" },
            { 17, "Quince" },
            { 18, "Raspberry" },
            { 19, "Strawberry" },
            { 20, "Tomato" },
            { 21, "Ugli" },
            { 22, "Vanilla" },
            { 23, "Watermelon" },
            { 24, "Ximenia" },
            { 25, "Yam" },
            { 26, "Zucchini" }
        };

        private static readonly Dictionary<string, int> FruitToIp = new();

        static NetworkUtils()
        {
            // Initialize reverse mapping
            foreach (var kvp in IpToFruit)
            {
                FruitToIp[kvp.Value.ToLower()] = kvp.Key;
            }
        }

        /// <summary>
        /// Gets the local IP address of the machine
        /// </summary>
        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// Converts an IP address to a fruit code
        /// </summary>
        public static string IpToFruitCode(string ipAddress)
        {
            try
            {
                var parts = ipAddress.Split('.');
                if (parts.Length == 4 && int.TryParse(parts[3], out int lastOctet))
                {
                    // For IP addresses beyond our mapping, generate a deterministic fruit name
                    if (lastOctet > 26)
                    {
                        var index = (lastOctet % 26) + 1;
                        return IpToFruit.GetValueOrDefault(index, $"Fruit{lastOctet}");
                    }
                    return IpToFruit.GetValueOrDefault(lastOctet, $"Unknown{lastOctet}");
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Converts a fruit code back to the last octet of an IP
        /// </summary>
        public static int FruitCodeToLastOctet(string fruitCode)
        {
            return FruitToIp.GetValueOrDefault(fruitCode.ToLower(), -1);
        }

        /// <summary>
        /// Gets the network base (first three octets) of local IP
        /// </summary>
        public static string GetNetworkBase()
        {
            var localIp = GetLocalIPAddress();
            var parts = localIp.Split('.');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            return "192.168.0";
        }

        /// <summary>
        /// Constructs full IP from fruit code
        /// </summary>
        public static string FruitCodeToIp(string fruitCode)
        {
            var lastOctet = FruitCodeToLastOctet(fruitCode);
            if (lastOctet == -1) return string.Empty;

            var networkBase = GetNetworkBase();
            return $"{networkBase}.{lastOctet}";
        }

        /// <summary>
        /// Checks if an IP address is reachable
        /// </summary>
        public static bool IsIpReachable(string ipAddress, int timeoutMs = 1000)
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send(ipAddress, timeoutMs);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a random available port
        /// </summary>
        public static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
