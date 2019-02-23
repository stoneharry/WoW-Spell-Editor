using System;
using System.IO;
using System.Xml;

namespace SpellEditor.Sources.Config
{
    public class Config
    {
        public enum ConnectionType
        {
            SQLite,
            MySQL
        }

        public string Host = "127.0.0.1";
        public string User = "root";
        public string Pass = "12345";
        public string Port = "3306";
        public string Database = "SpellEditor";
        public string Language = "enUS";

        public ConnectionType connectionType = ConnectionType.SQLite;

        public void WriteConfigFile() => WriteConfigFile(Host, User, Pass, Port, Database, Language);
        public void WriteConfigFile(string host, string user, string pass, string port, string database, string language)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            using (XmlWriter writer = XmlWriter.Create("config.xml", settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("MySQL");
                writer.WriteElementString("host", host);
                writer.WriteElementString("username", user);
                writer.WriteElementString("password", pass);
                writer.WriteElementString("port", port);
                writer.WriteElementString("database", database);
                writer.WriteElementString("language", language);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public void ReadConfigFile()
        {
            try
            {
                bool hasError = false;
                using (XmlReader reader = XmlReader.Create("config.xml"))
                {
                    if (reader.ReadToFollowing("host"))
                        Host = reader.ReadElementContentAsString();
                    else
                        hasError = true;
                    if (reader.ReadToFollowing("username"))
                        User = reader.ReadElementContentAsString();
                    else
                        hasError = true;
                    if (reader.ReadToFollowing("password"))
                        Pass = reader.ReadElementContentAsString();
                    else
                        hasError = true;
                    if (reader.ReadToFollowing("port"))
                        Port = reader.ReadElementContentAsString();
                    else
                        hasError = true;
                    if (reader.ReadToFollowing("database"))
                        Database = reader.ReadElementContentAsString();
                    else
                        hasError = true;
                    if (reader.ReadToFollowing("language"))
                        Language = reader.ReadElementContentAsString();
                    else
                        hasError = true;
                }
                if (hasError)
                    WriteConfigFile(Host, User, Pass, Port, Database, Language);
            }
            catch (Exception e)
            {
                throw new Exception("ERROR: config.xml is corrupt - please delete it and run the program again.\n" + e.Message);
            }
        }

        public void UpdateConfigValue(string key, string value)
        {
            if (!File.Exists("config.xml"))
                return;
            var xml = new XmlDocument();
            xml.Load("config.xml");
            var node = xml.SelectSingleNode("MySQL/" + key);
            if (node == null)
                return;
            node.InnerText = value;
            xml.Save("config.xml");

            ReadConfigFile();
        }

        public string GetConfigValue(string key)
        {
            var xml = new XmlDocument();
            if (!File.Exists("config.xml"))
                return "";
            xml.Load("config.xml");
            var node = xml.SelectSingleNode("MySQL/" + key);
            return node == null ? "" : node.InnerText;
        }
    }
}
