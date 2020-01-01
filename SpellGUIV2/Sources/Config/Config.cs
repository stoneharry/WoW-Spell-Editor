using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace SpellEditor.Sources.Config
{
    public static class Config
    {
        public enum ConnectionType
        {
            SQLite,
            MySQL
        }

        public static bool isInit = false;
        public static bool needInitMysql = false;
        private static XDocument xml = new XDocument();

        public static string Host = "127.0.0.1";
        public static string User = "root";
        public static string Pass = "12345";
        public static string Port = "3306";
        public static string Database = "SpellEditor";
        public static string BindingsDirectory = "\\Bindings";
        public static string DbcDirectory = "\\DBC";

        public static string Language = "enUS";

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
                Console.WriteLine(e.Message);
            }
        }

        public static void Init()
        {
            if (!File.Exists("config.xml"))
                CreateXmlFile();

            xml = XDocument.Load("config.xml");
            if (xml == null || xml.Root == null)
                CreateXmlFile();

            ReadConfigFile();
        }

        private static void ReadConfigFile()
        {
            Host = GetConfigValue("Mysql/host");
            User = GetConfigValue("Mysql/username");
            Pass = GetConfigValue("Mysql/password");
            Port = GetConfigValue("Mysql/port");
            Database = GetConfigValue("Mysql/database");
            if (Host == "" || User == "" || Pass == "" || Port == "" || Database == "")
                needInitMysql = true;

            var lang = GetConfigValue("language");
            if (lang.Length == 0)
            {
                UpdateConfigValue("language", Language);
            }
            else
            {
                Language = lang;
            }

            var bindingsDir = GetConfigValue("BindingsDirectory");
            var dbcDir = GetConfigValue("DbcDirectory");
            if (bindingsDir.Length == 0 || dbcDir.Length == 0)
            {
                UpdateConfigValue("BindingsDirectory", BindingsDirectory);
                UpdateConfigValue("DbcDirectory", DbcDirectory);
            }
            else
            {
                BindingsDirectory = bindingsDir;
                DbcDirectory = dbcDir;
            }
        }

        public static void UpdateConfigValue(string key, string value)
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
            for (int i = 0; i < nodes.Length; i++)
            {
                if (xElement == null)
                    return null;

                xElement = xElement.Element(nodes[i]);
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

        public static bool HasKey(string key)
        {
            return GetXmlNode(key) == null ? false : true;
        }

        public static string GetConfigValue(string key)
        {
            var node = GetXmlNode(key);
            return node == null ? "" : node.Value;
        }

        public static void Save()
        {
            xml.Save("config.xml");
        }
    }
}
