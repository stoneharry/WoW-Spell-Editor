using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SpellEditor.Sources.DBC;

namespace SpellVisualMapBuilder
{
    // Public use of a DBC Header file
    public struct DBC_Header
    {
        public UInt32 Magic;
        public UInt32 RecordCount;
        public UInt32 FieldCount;
        public UInt32 RecordSize;
        public Int32 StringBlockSize;
    };

    class Program
    {
        private static UInt32 DBCStartingEntry = 15000;
        private static UInt32 CTStartingEntry = 100000;
        private static UInt32 MapEntry = 13;
        private static HashSet<String> paths = new HashSet<String>();

        public static void Print(String message, params object[] args)
        {
            if (args == null)
                Console.WriteLine(message);
            else
                Console.WriteLine(String.Format(message, args));
        }

        static void Main(string[] args)
        {
            try
            {
                Print("This program will use ID's {0}+ for CreatureDisplayInfo and CreatureModelInfo.", DBCStartingEntry);
                Print("This program will use creature ID's {0}+ for creature_template.", CTStartingEntry);
                Print("This program will spawn the creatures on map {0}.", MapEntry);

                Print("Loading Import\\SPELLS folder...");
                if (!Directory.Exists("Import/SPELLS"))
                    throw new Exception("Spells folder does not exist!");

                DirectoryInfo dir = new DirectoryInfo("Import/SPELLS");
                loadAllSpells(dir);

                Print("Loaded {0} spells. Loading Import\\CreatureModelData.dbc...", paths.Count);
                CreatureModelData modelData = new CreatureModelData();
                Print("Loaded {0} models from CreatureModelData.", modelData.header.RecordCount);

                Print("Loading Input\\CreatureDisplayInfo.dbc...");

                Print("Creating Export\\CreatureModelData.dbc...");

                Print("Creating Export\\CreatureDisplayInfo.dbc...");

                Print("Creating creature_template queries...");

                Print("Creating creature queries... || NOT YET IMPLEMENTED");

                Print("Program finished successfully.");
            }
            catch (Exception e)
            {
                Print("ERROR: " + e.Message);
            }
        }

        private static void loadAllSpells(DirectoryInfo dir)
        {
            foreach (DirectoryInfo subDir in dir.GetDirectories())
                loadAllSpells(subDir);
            foreach (FileInfo file in dir.GetFiles())
            {
                String extension = file.Extension.ToLower();
                if (extension.Equals(".m2"))
                {
                    String name = file.Name.Substring(0, file.Name.IndexOf('.'));
                    String FullName = file.FullName;
                    int index = FullName.IndexOf("SPELLS");
                    int lastIndex = FullName.LastIndexOf('\\');
                    FullName = FullName.Substring(index, lastIndex - index);
                    FullName = FullName + "\\" + name + ".mdx";
                    paths.Add(FullName);
                }
            }
        }
    }
}
