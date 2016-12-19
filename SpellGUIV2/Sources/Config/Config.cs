using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SpellEditor.Sources.Config
{
    class Config
    {
        public string Host = "127.0.0.1";
        public string User = "root";
        public string Pass = "12345";
        public string Port = "3306";
        public string Database = "SpellEditor";
        public string Table = "Spell";

        public void createFile(string h, string u, string p, string po, string db, string tb)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            using (XmlWriter writer = XmlWriter.Create("config.xml", settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("MySQL");

                writer.WriteElementString("host", h);
                writer.WriteElementString("username", u);
                writer.WriteElementString("password", p);
                writer.WriteElementString("port", po);
                writer.WriteElementString("database", db);
                writer.WriteElementString("table", tb);

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
        }

        public void loadFile()
        {
            try
            {
                using (XmlReader reader = XmlReader.Create("config.xml"))
                {
                    reader.ReadToFollowing("host");
                    Host = reader.ReadElementContentAsString();
                    reader.ReadToFollowing("username");
                    User = reader.ReadElementContentAsString();
                    reader.ReadToFollowing("password");
                    Pass = reader.ReadElementContentAsString();
                    reader.ReadToFollowing("port");
                    Port = reader.ReadElementContentAsString();
                    reader.ReadToFollowing("database");
                    Database = reader.ReadElementContentAsString();
                    reader.ReadToFollowing("table");
                    Table = reader.ReadElementContentAsString();
                    reader.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("config.xml is corrupt - please delete it and run the program again.\n" + e.Message);
            }
        }
    }
}
