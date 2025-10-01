using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameBox.Utils
{
    public static class NetworkUtils
    {
        // Mapping of IP last octets to fruit codes (1-255)
        private static readonly Dictionary<int, string> IpToFruit = new()
        {
            { 1, "Apple" },
            { 2, "Blackberry" },
            { 3, "Carrot" },
            { 4, "Date" },
            { 5, "Eggplant" },
            { 6, "Fig" },
            { 7, "Grape" },
            { 8, "Honeydew" },
            { 9, "IceCream" },
            { 10, "Jackfruit" },
            { 11, "Kiwi" },
            { 12, "Lemon" },
            { 13, "Mango" },
            { 14, "Nectarine" },
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
            { 26, "Zucchini" },
            { 27, "Apricot" },
            { 28, "Banana" },
            { 29, "Cherry" },
            { 30, "Dragonfruit" },
            { 31, "Elderberry" },
            { 32, "Feijoa" },
            { 33, "Guava" },
            { 34, "Huckleberry" },
            { 35, "Imbe" },
            { 36, "Jujube" },
            { 37, "Kumquat" },
            { 38, "Lime" },
            { 39, "Mulberry" },
            { 40, "Nutmeg" },
            { 41, "Olive" },
            { 42, "Papaya" },
            { 43, "Quinoa" },
            { 44, "Rhubarb" },
            { 45, "Starfruit" },
            { 46, "Tangerine" },
            { 47, "Umami" },
            { 48, "VanillaBean" },
            { 49, "Walnut" },
            { 50, "Xigua" },
            { 51, "Yuzu" },
            { 52, "Zest" },
            { 53, "Acai" },
            { 54, "Blueberry" },
            { 55, "Cranberry" },
            { 56, "Durian" },
            { 57, "Endive" },
            { 58, "Fennel" },
            { 59, "Grapefruit" },
            { 60, "Hazelnut" },
            { 61, "IcePlant" },
            { 62, "Jalapeno" },
            { 63, "Kale" },
            { 64, "Lychee" },
            { 65, "Melon" },
            { 66, "Navel" },
            { 67, "Onion" },
            { 68, "Pear" },
            { 69, "Quandong" },
            { 70, "Radish" },
            { 71, "Satsuma" },
            { 72, "Tamarind" },
            { 73, "Ube" },
            { 74, "VinegarFruit" },
            { 75, "Wasabi" },
            { 76, "Xoconostle" },
            { 77, "Yarrow" },
            { 78, "Ziti" },
            { 79, "Almond" },
            { 80, "Basil" },
            { 81, "Celery" },
            { 82, "Dill" },
            { 83, "Escarole" },
            { 84, "Frisee" },
            { 85, "Garlic" },
            { 86, "Horseradish" },
            { 87, "Iceberg" },
            { 88, "Jicama" },
            { 89, "Kohlrabi" },
            { 90, "Leek" },
            { 91, "Mushroom" },
            { 92, "Nori" },
            { 93, "Oregano" },
            { 94, "Parsley" },
            { 95, "QuailEgg" },
            { 96, "Rosemary" },
            { 97, "Sage" },
            { 98, "Thyme" },
            { 99, "Umeboshi" },
            { 100, "Vinegar" },
            { 101, "WaterChestnut" },
            { 102, "Xacuti" },
            { 103, "Yuca" },
            { 104, "Zatar" },
            { 105, "Arugula" },
            { 106, "Beet" },
            { 107, "Cabbage" },
            { 108, "Daikon" },
            { 109, "Edamame" },
            { 110, "Fava" },
            { 111, "Ginger" },
            { 112, "Habanero" },
            { 113, "ItalianIce" },
            { 114, "Jaboticaba" },
            { 115, "Kabocha" },
            { 116, "Lavender" },
            { 117, "Mandarin" },
            { 118, "Napa" },
            { 119, "Okra" },
            { 120, "Pumpkin" },
            { 121, "Quinault" },
            { 122, "Radicchio" },
            { 123, "Shallot" },
            { 124, "Turnip" },
            { 125, "Upland" },
            { 126, "Vidalia" },
            { 127, "Wakame" },
            { 128, "Xanthan" },
            { 129, "YellowPepper" },
            { 130, "Zaatar" },
            { 131, "Asparagus" },
            { 132, "Broccoli" },
            { 133, "Cauliflower" },
            { 134, "Doughnut" },
            { 135, "Eclair" },
            { 136, "Flan" },
            { 137, "Gelato" },
            { 138, "Honey" },
            { 139, "IcePop" },
            { 140, "Jello" },
            { 141, "KitKat" },
            { 142, "Licorice" },
            { 143, "Macaron" },
            { 144, "Nougat" },
            { 145, "Oreo" },
            { 146, "Pie" },
            { 147, "Quiche" },
            { 148, "RollCake" },
            { 149, "Sundae" },
            { 150, "Tart" },
            { 151, "UpsideCake" },
            { 152, "VanillaCake" },
            { 153, "Waffle" },
            { 154, "XmasCookie" },
            { 155, "Yulelog" },
            { 156, "Zabaione" },
            { 157, "Anise" },
            { 158, "Brioche" },
            { 159, "Croissant" },
            { 160, "Donut" },
            { 161, "Espresso" },
            { 162, "Fudge" },
            { 163, "Gingerbread" },
            { 164, "Halvah" },
            { 165, "IrishCream" },
            { 166, "JavaChip" },
            { 167, "KrispyKreme" },
            { 168, "Latte" },
            { 169, "Mocha" },
            { 170, "Nutella" },
            { 171, "OatmealCookie" },
            { 172, "Praline" },
            { 173, "QueenCake" },
            { 174, "Rum" },
            { 175, "Souffle" },
            { 176, "Tiramisu" },
            { 177, "Urchin" },
            { 178, "VelvetCake" },
            { 179, "Whiskey" },
            { 180, "Xylitol" },
            { 181, "YogurtPie" },
            { 182, "Zeppole" },
            { 183, "Amaretto" },
            { 184, "Biscotti" },
            { 185, "Cannoli" },
            { 186, "Dacquoise" },
            { 187, "Eggnog" },
            { 188, "Fritter" },
            { 189, "Ganache" },
            { 190, "Honeycomb" },
            { 191, "IcingSugar" },
            { 192, "JamTart" },
            { 193, "Kuchen" },
            { 194, "Ladyfinger" },
            { 195, "Marzipan" },
            { 196, "Napoleon" },
            { 197, "Opera" },
            { 198, "Panettone" },
            { 199, "QueensPudding" },
            { 200, "RumBaba" },
            { 201, "Strudel" },
            { 202, "Truffle" },
            { 203, "UpsideDown" },
            { 204, "ViennaFinger" },
            { 205, "Whoopie" },
            { 206, "XmasLog" },
            { 207, "YeastCake" },
            { 208, "Zwieback" },
            { 209, "Affogato" },
            { 210, "Brownie" },
            { 211, "Caramel" },
            { 212, "Danish" },
            { 213, "Empanada" },
            { 214, "Flambé" },
            { 215, "Galette" },
            { 216, "Hamantash" },
            { 217, "IcePick" },
            { 218, "Jalebi" },
            { 219, "Knafeh" },
            { 220, "Lebkuchen" },
            { 221, "Meringue" },
            { 222, "Nanaimo" },
            { 223, "Oatcake" },
            { 224, "Panforte" },
            { 225, "Quesito" },
            { 226, "Rugelach" },
            { 227, "Shortbread" },
            { 228, "Tortoni" },
            { 229, "Ukoy" },
            { 230, "VanillaSlice" },
            { 231, "WalnutCake" },
            { 232, "Xuixo" },
            { 233, "YoYo" },
            { 234, "Zuppa" },
            { 235, "AngelCake" },
            { 236, "BakedAlaska" },
            { 237, "Cobbler" },
            { 238, "Dumpling" },
            { 239, "ElephantEar" },
            { 240, "Fruitcake" },
            { 241, "Gâteau" },
            { 242, "Honeybun" },
            { 243, "IceCreamCake" },
            { 244, "Jelly" },
            { 245, "Kolache" },
            { 246, "LemonBar" },
            { 247, "MilkCake" },
            { 248, "NutRoll" },
            { 249, "OrangeCake" },
            { 250, "PecanPie" },
            { 251, "Qottab" },
            { 252, "RedVelvet" },
            { 253, "SnowCake" },
            { 254, "Trifle" },
            { 255, "UncleSam" }
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
                    // Now we support the full range 1-255
                    if (lastOctet >= 1 && lastOctet <= 255)
                    {
                        return IpToFruit.GetValueOrDefault(lastOctet, $"Unknown{lastOctet}");
                    }
                    return $"Unknown{lastOctet}";
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
