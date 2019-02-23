using System;
using System.Runtime.Remoting.Messaging;
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

        public void WriteConfigFile(string host, string user, string pass, string port, string database)
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
                writer.WriteElementString("language", Language);
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
                    WriteConfigFile(Host, User, Pass, Port, Language);
            }
            catch (Exception e)
            {
                throw new Exception("ERROR: config.xml is corrupt - please delete it and run the program again.\n" + e.Message);
            }
        }
    }
}
