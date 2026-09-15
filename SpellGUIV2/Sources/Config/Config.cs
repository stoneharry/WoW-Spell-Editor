using NLog;
using SpellEditor.Sources.VersionControl;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace SpellEditor.Sources.Config
{
    public static class Config
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public enum ConnectionType
        {
            SQLite,
            MySQL
        }

        public static bool IsInit = false;
        public static bool NeedInitMysql = false;
        private static XDocument xml = new XDocument();
        
        public static string Host
        {
            get { return GetConfigValue("MySQL/Host"); }
            set
            {
                UpdateConfigValue("MySQL/Host", value);
                Save();
            }
        }
        public static string User
        {
            get { return GetConfigValue("MySQL/Username"); }
            set
            {
                UpdateConfigValue("MySQL/Username", value);
                Save();
            }
        }
        public static string Pass
        {
            get { return GetConfigValue("MySQL/Password"); }
            set
            {
                UpdateConfigValue("MySQL/Password", value);
                Save();
            }
        }
        public static string Port
        {
            get { return GetConfigValue("MySQL/Port"); }
            set
            {
                UpdateConfigValue("MySQL/Port", value);
                Save();
            }
        }
        public static string Database
        {
            get { return GetConfigValue("MySQL/Database"); }
            set
            {
                UpdateConfigValue("MySQL/Database", value);
                Save();
            }
        }
        public static string BindingsDirectory
        {
            get { return GetConfigValue("BindingsDirectory"); }
            set
            {
                UpdateConfigValue("BindingsDirectory", value);
                Save();
            }
        }
        public static string DbcDirectory
        {
            get { return GetConfigValue("DbcDirectory"); }
            set
            {
                UpdateConfigValue("DbcDirectory", value);
                Save();
            }
        }
        public static string Language
        {
            get { return GetConfigValue("Language"); }
            set
            {
                UpdateConfigValue("Language", value);
                Save();
            }
        }
        public static string WoWVersion
        {
            get { return GetConfigValue("WoWVersion"); }
            set
            {
                UpdateConfigValue("WoWVersion", value);
                Save();
            }
        }
        public static string SQLiteFilename
        {
            get { return GetConfigValue("SQLite/DatabaseFileName"); }
            set
            {
                UpdateConfigValue("SQLite/DatabaseFileName", value);
                Save();
            }
        }
        public static bool DynamicMiscValueFields
        {
            get
            {
                var str = GetConfigValue("DynamicMiscValueFields");
                return "true".Equals(str) || str == string.Empty; ;
            }
            set
            {
                UpdateConfigValue("DynamicMiscValueFields", value ? "true" : "false");
                Save();
            }
        }

        // Off by default. When on, everything that used to be read up front is read up front
        // again: the optional DBCs and the full spell family class mask index.
        public static bool CacheAllOnLoad
        {
            get { return "true".Equals(GetConfigValue("CacheAllOnLoad")); }
            set
            {
                UpdateConfigValue("CacheAllOnLoad", value ? "true" : "false");
                Save();
            }
        }

        // On by default. Selecting a spell refetches its row and drops the cached data that
        // belongs to it, so the editor always shows what is currently in the database.
        public static bool RefreshCacheOnSpellSelection
        {
            get
            {
                var str = GetConfigValue("RefreshCacheOnSpellSelection");
                return "true".Equals(str) || str == string.Empty;
            }
            set
            {
                UpdateConfigValue("RefreshCacheOnSpellSelection", value ? "true" : "false");
                Save();
            }
        }

        public static string DefaultMpqName
        {
            get { return GetConfigValue("DefaultMpqName"); }
            set
            {
                UpdateConfigValue("DefaultMpqName", value);
                Save();
            }
        }

        public static string SqlMapperDirectory
        {
            get { return GetConfigValue("SqlMapperDirectory"); }
            set
            {
                UpdateConfigValue("SqlMapperDirectory", value);
                Save();
            }
        }

        // Optional emulator world database, off by default and separate to the DBC one
        public static bool EmulatorEnabled
        {
            get { return "true".Equals(GetConfigValue("Emulator/Enabled")); }
            set
            {
                UpdateConfigValue("Emulator/Enabled", value ? "true" : "false");
                Save();
            }
        }
        // Which core the world database belongs to, see Sources/Emulator/Emulator.cs
        public static string EmulatorType
        {
            get { return GetConfigValue("Emulator/Type"); }
            set
            {
                UpdateConfigValue("Emulator/Type", value);
                Save();
            }
        }
        public static string EmulatorHost
        {
            get { return GetConfigValue("Emulator/Host"); }
            set
            {
                UpdateConfigValue("Emulator/Host", value);
                Save();
            }
        }
        public static string EmulatorUser
        {
            get { return GetConfigValue("Emulator/Username"); }
            set
            {
                UpdateConfigValue("Emulator/Username", value);
                Save();
            }
        }
        public static string EmulatorPass
        {
            get { return GetConfigValue("Emulator/Password"); }
            set
            {
                UpdateConfigValue("Emulator/Password", value);
                Save();
            }
        }
        public static string EmulatorPort
        {
            get { return GetConfigValue("Emulator/Port"); }
            set
            {
                UpdateConfigValue("Emulator/Port", value);
                Save();
            }
        }
        public static string EmulatorDatabase
        {
            get { return GetConfigValue("Emulator/Database"); }
            set
            {
                UpdateConfigValue("Emulator/Database", value);
                Save();
            }
        }

        public static ConnectionType connectionType = ConnectionType.SQLite;

        private static void CreateXmlFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode node = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "");
            xmlDoc.AppendChild(node);
            XmlNode root = xmlDoc.CreateElement("SpellEditor");
            xmlDoc.AppendChild(root);

            try
            {
                xmlDoc.Save("config.xml");
            }
            catch (Exception e)
            {
                Logger.Info(e.Message);
            }
        }

        public static void Init()
        {
            if (!File.Exists("config.xml"))
                CreateXmlFile();

            xml = XDocument.Load("config.xml");
            if (xml?.Root == null)
                CreateXmlFile();

            ReadConfigFile();
        }

        private static void ReadConfigFile()
        {
            // Password intentionally not checked for length, empty password is allowed
            if (Host.Length == 0 || User.Length == 0 || Port.Length == 0 || Database.Length == 0)
            {
                NeedInitMysql = true;
            }

            if (SQLiteFilename.Length == 0)
            {
                SQLiteFilename = "SpellEditor";
            }

            if (Language.Length == 0)
            {
                Language = "enUS";
            }

            if (BindingsDirectory.Length == 0 || DbcDirectory.Length == 0)
            {
                BindingsDirectory = Environment.CurrentDirectory + "\\Bindings_335_wotlk";
                DbcDirectory = Environment.CurrentDirectory + "\\DBC_335_wotlk";
            }

            if (WoWVersion.Length == 0)
            {
                WoWVersion = WoWVersionManager.GetInstance().LookupVersion(WoWVersionManager.DefaultVersionString).Version;
            }

            if (SqlMapperDirectory.Length == 0)
            {
                SqlMapperDirectory = Environment.CurrentDirectory + "\\SqlMap\\AzerothCore";
            }

            // Defaults so the emulator fields are not empty, it stays disabled until turned on
            if (EmulatorType.Length == 0)
            {
                EmulatorType = Emulator.Emulators.Default.Id;
            }
            var emulator = Emulator.Emulators.Lookup(EmulatorType);
            if (EmulatorHost.Length == 0)
            {
                EmulatorHost = "127.0.0.1";
            }
            if (EmulatorPort.Length == 0)
            {
                EmulatorPort = "3306";
            }
            if (EmulatorUser.Length == 0)
            {
                EmulatorUser = emulator.DefaultUser;
            }
            if (EmulatorDatabase.Length == 0)
            {
                EmulatorDatabase = emulator.DefaultDatabase;
            }
        }

        private static void UpdateConfigValue(string key, string value)
        {
            var node = GetXmlNode(key);
            if (node != null)
            {
                node.SetValue(value);
                Save();
            }
            else
                CreateConfigValue(key, value);
        }

        private static XElement GetXmlNode(string key)
        {
            string[] nodes = key.Split('/');
            XElement xElement = xml.Root;
            foreach (var node in nodes)
            {
                if (xElement == null)
                    return null;

                xElement = xElement.Element(node);
            }

            return xElement;
        }

        private static void CreateConfigValue(string key, string value)
        {
            string[] nodes = key.Split('/');

            XElement UpperElement = xml.Root;
            XElement xElement;
            for (int i = 0; i < nodes.Length; i++)
            {
                xElement = UpperElement.Element(nodes[i]);

                if (xElement == null)
                {
                    xElement = new XElement(nodes[i]);
                    UpperElement.Add(xElement);
                }
                UpperElement = xElement;

                if (i == nodes.Length - 1)
                    xElement.SetValue(value);
            }
            Save();
        }

        private static bool HasKey(string key)
        {
            return GetXmlNode(key) != null;
        }

        private static string GetConfigValue(string key)
        {
            var node = GetXmlNode(key);
            return node == null ? "" : node.Value;
        }

        private static void Save()
        {
            xml.Save("config.xml");
        }
    }
}
