using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace SpellEditor.Sources.Config
{
    public class Config
    {
        public enum ConnectionType
        {
            SQLite,
            MySQL
        }

        public bool isInit = false;
        public bool needInitMysql = false;
        XDocument xml = new XDocument();

        public string Host = "127.0.0.1";
        public string User = "root";
        public string Pass = "12345";
        public string Port = "3306";
        public string Database = "SpellEditor";

        public string Language = "enUS";

        public ConnectionType connectionType = ConnectionType.SQLite;

        public void CreateXmlFile()
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

        public Config()
        {
            if (!File.Exists("config.xml"))
                CreateXmlFile();

            xml = XDocument.Load("config.xml");
        }

        public void Init()
        {
            if (!File.Exists("config.xml"))
                CreateXmlFile();

            ReadConfigFile();
        }

        public void ReadConfigFile()
        {
            Host = GetConfigValue("Mysql/host");
            User = GetConfigValue("Mysql/username");
            Pass = GetConfigValue("Mysql/password");
            Port = GetConfigValue("Mysql/port");
            Database = GetConfigValue("Mysql/database");

            Language = GetConfigValue("language");

            if (Host == "" || User == "" || Pass == "" || Port == "" || Database == "")
                needInitMysql = true;
        }

        public void UpdateConfigValue(string key, string value)
        {
            var node = GetXmlNode(key);
            if (node != null)
            {
                node.SetValue(value);
                Save();
            }
            else
                CreateConfigValue(key, value);

            ReadConfigFile();
        }

        public XElement GetXmlNode(string key)
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

        public void CreateConfigValue(string key, string value)
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

        public bool HasKey(string key)
        {
            return GetXmlNode(key) == null ? false : true;
        }

        public string GetConfigValue(string key)
        {
            var node = GetXmlNode(key);
            return node == null ? "" : node.Value;
        }

        public void Save()
        {
            xml.Save("config.xml");
        }
    }
}
