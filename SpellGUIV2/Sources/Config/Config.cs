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
        public static bool RenderImagesInView
        {
            get
            {
                var str = GetConfigValue("RenderImagesInView");
                return "true".Equals(str);
            }
            set
            {
                UpdateConfigValue("RenderImagesInView", value ? "true" : "false");
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
