using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameBox.Utils
{
    public static class NetworkUtils
    {
        // Mapping of IP last octets (1-255) to unique, easy-to-type words
        private static readonly string[] OctetWords = new string[]
        {
            // Fruits
            "Apple", "Apricot", "Avocado", "Banana", "Berry", "Blackberry", "Blueberry", "Cherry", "Coconut", "Date",
            "Fig", "Grape", "Guava", "Kiwi", "Lemon", "Lime", "Mango", "Melon", "Nectarine", "Orange",
            "Papaya", "Peach", "Pear", "Pineapple", "Plum", "Raspberry", "Strawberry", "Tangerine", "Watermelon", "Olive",
            // Vegetables
            "Carrot", "Celery", "Corn", "Cucumber", "Eggplant", "Garlic", "Lettuce", "Onion", "Pea", "Pepper",
            "Potato", "Pumpkin", "Radish", "Spinach", "Squash", "Tomato", "Turnip", "Yam", "Zucchini", "Bean",
            // Nuts/Seeds
            "Almond", "Cashew", "Hazelnut", "Peanut", "Pecan", "Pistachio", "Walnut", "Chia", "Flax", "Sesame",
            // Animals (easy spelling)
            "Cat", "Dog", "Duck", "Fox", "Goat", "Horse", "Lion", "Mouse", "Pig", "Rabbit",
            "Sheep", "Tiger", "Wolf", "Bear", "Cow", "Deer", "Frog", "Hawk", "Mole", "Owl",
            "Rat", "Seal", "Swan", "Whale", "Bat", "Bee", "Crab", "Crow", "Fish", "Goose",
            "Lamb", "Mule", "Panda", "Shark", "Snail", "Snake", "Spider", "Squirrel", "Wasp", "Ant",
            // Colors (easy)
            "Red", "Blue", "Green", "Yellow", "Purple", "Pink", "Brown", "Black", "White", "Gray",
            "Orange", "Gold", "Silver", "Tan", "Cyan", "Ivory", "Navy", "Teal", "Violet", "Magenta",
            // Foods (easy, common)
            "Bread", "Butter", "Cake", "Candy", "Cheese", "Chips", "Cookie", "Cream", "Egg", "Ham",
            "Honey", "Jam", "Juice", "Milk", "Muffin", "Oat", "Oil", "Pie", "Rice", "Salt",
            "Soup", "Sugar", "Toast", "Waffle", "Yogurt", "Bagel", "Bacon", "Donut", "Fruit", "Salad",
            // Herbs/Spices (easy)
            "Basil", "Chive", "Cilantro", "Dill", "Mint", "Parsley", "Rosemary", "Sage", "Thyme", "Peppercorn",
            // More Fruits/Vegetables
            "Acai", "Cantaloupe", "Jackfruit", "Kale", "Lime", "Mandarin", "Mulberry", "Passion", "Persimmon", "Quince",
            "Starfruit", "Ugli", "Jicama", "Kumquat", "Lychee", "Rutabaga", "Shallot", "Sweetpea", "Endive", "Fennel",
            // More Animals
            "Hawk", "Jay", "Lynx", "Moose", "Orca", "Otter", "Robin", "Vole", "Vulture", "Yak",
            // Short objects (easy to type)
            "Ball", "Book", "Brush", "Cup", "Key", "Lamp", "Leaf", "Pen", "Rock", "Rope",
            "Sand", "Sock", "Star", "Stick", "Stone", "Tape", "Tool", "Toy", "Twig", "Wheel",
            // Even more foods/objects
            "Beanie", "Bowl", "Fork", "Knife", "Plate", "Spoon", "Tray", "Drum", "Flag", "Glove",
            "Hat", "Ring", "Sock", "Tile", "Tube", "Vase", "Zip", "Can", "Jar", "Pad",
            // Short adjectives/colors
            "Bright", "Calm", "Clear", "Cool", "Dim", "Dull", "Faint", "Grand", "Mild", "Rich",
            "Sharp", "Shy", "Slick", "Slim", "Soft", "Solid", "Tame", "Wild", "Wise", "Young",
            // More animals
            "Goose", "Jay", "Lynx", "Moose", "Orca", "Otter", "Robin", "Vole", "Vulture", "Yak",
            // More colors/objects
            "Aqua", "Azure", "Berry", "Blush", "Cherry", "Coral", "Denim", "Flame", "Ivory", "Lemon",
            "Mint", "Moss", "Peach", "Plum", "Ruby", "Sage", "Sky", "Snow", "Stone", "Sun",
            // Extra simple foods/objects
            "Ice", "Jam", "Nut", "Oat", "Oil", "Tea", "Vanilla", "Wheat", "Yolk", "Zest",
            // Fill to 255 with repeat or easy randoms
            "Fern", "Pine", "Lily", "Rose", "Tulip", "Daisy", "Cedar", "Palm", "Elm", "Oak",
            "Beech", "Maple", "Willow", "Spruce", "Ash"
        };

        private static readonly Dictionary<int, string> IpToWord = new();
        private static readonly Dictionary<string, int> WordToIp = new();

        static NetworkUtils()
        {
            // Initialize mapping
            for (int i = 0; i < OctetWords.Length; i++)
            {
                IpToWord[i + 1] = OctetWords[i];
                WordToIp[OctetWords[i].ToLower()] = i + 1;
            }
        }

        /// <summary>
        /// Gets the local IP address of the machine.
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
        /// Converts an IP address to a word code.
        /// </summary>
        public static string IpToWordCode(string ipAddress)
        {
            try
            {
                var parts = ipAddress.Split('.');
                if (parts.Length == 4 && int.TryParse(parts[3], out int lastOctet))
                {
                    if (lastOctet >= 1 && lastOctet <= OctetWords.Length)
                        return IpToWord.GetValueOrDefault(lastOctet, $"Code{lastOctet}");
                    else
                        return $"Code{lastOctet}";
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Converts a word code back to the last octet of an IP.
        /// </summary>
        public static int WordCodeToLastOctet(string wordCode)
        {
            return WordToIp.GetValueOrDefault(wordCode.ToLower(), -1);
        }

        /// <summary>
        /// Gets the network base (first three octets) of local IP.
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
        /// Constructs full IP from word code.
        /// </summary>
        public static string WordCodeToIp(string wordCode)
        {
            var lastOctet = WordCodeToLastOctet(wordCode);
            if (lastOctet == -1) return string.Empty;
            var networkBase = GetNetworkBase();
            return $"{networkBase}.{lastOctet}";
        }

        /// <summary>
        /// Checks if an IP address is reachable.
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
        /// Gets a random available port.
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
