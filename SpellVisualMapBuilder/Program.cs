using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SpellEditor.Sources.DBC;
using MySql.Data.MySqlClient;

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
        private static Int32[] MapTopLeft = { 138, 138 };
        private static Int32[] MapBottomRight = { -138, -138 };
        private static Int32 MapZ = -144;
        private static Int32 CellSize = 7;
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
                CreatureDisplayInfo displayInfo = new CreatureDisplayInfo();
                Print("Loaded {0} display ID's from CreatureDisplayInfo.", displayInfo.header.RecordCount);

                Print("Creating {0} new entries in both DBC's...", paths.Count);
                populateNewEntries(modelData, displayInfo);

                Print("Creating Export\\CreatureModelData.dbc...");
                modelData.SaveDBCFile();

                Print("Creating Export\\CreatureDisplayInfo.dbc...");
                displayInfo.SaveDBCFile();

                Print("Creating creature_template queries...");
                generateTemplateQueries();

                Print("Creating creature queries... || NOT YET IMPLEMENTED");
                generateSpawnQueries();

                Print("Program finished successfully.");
            }
            catch (Exception e)
            {
                Print("ERROR: " + e.Message);
            }
        }

        private static void generateSpawnQueries()
        {
            StringBuilder str = new StringBuilder();
            uint newSize = (uint)paths.Count + DBCStartingEntry;
            String[] stringPaths = paths.ToArray();
            uint creatureEntry = CTStartingEntry;

            int currX = MapTopLeft[0];
            int currY = MapTopLeft[1];
            int limitX = MapBottomRight[0];
            int limitY = MapBottomRight[1];
            for (uint i = DBCStartingEntry; i < newSize; ++i)
            {
                str.Append("insert into `creature` (`id`, `map`, `zoneId`, `areaId`, `spawnMask`, `phaseMask`, `modelid`, `equipment_id`," +
                    " `position_x`, `position_y`, `position_z`, `orientation`, `spawntimesecs`, `spawndist`, `currentwaypoint`, `curhealth`," +
                    " `curmana`, `MovementType`, `npcflag`, `unit_flags`, `dynamicflags`, `VerifiedBuild`)");
                str.Append(String.Format("values('{0}','{1}','{2}','0','1','1','0','0','{3}','{4}','{5}','0','30','0','0','5','0','0','0','0','0','0');\n",
                    creatureEntry++, 13, 0, currX, currY, MapZ)); // id, map, zone, x, y, z
                currX -= CellSize;
                if (currX <= limitX)
                {
                    currX = MapTopLeft[0];
                    currY -= CellSize;
                    if (currY <= limitY)
                        throw new Exception("Spawned outside of defined grid.");
                }
            }
            File.WriteAllText("Export/Creature.sql", str.ToString(), UTF8Encoding.GetEncoding(0));
        }

        private static void generateTemplateQueries()
        {
            StringBuilder str = new StringBuilder();
            uint newSize = (uint)paths.Count + DBCStartingEntry;
            String[] stringPaths = paths.ToArray();
            uint creatureEntry = CTStartingEntry;
            for (uint i = DBCStartingEntry; i < newSize; ++i)
            {
                str.Append("INSERT INTO `creature_template` VALUES (");
                str.Append(String.Format("'{0}', '0','0','0','0','0', '{1}', '0','0','0', \"{2}\",", creatureEntry++, i,
                    MySqlHelper.EscapeString(stringPaths[i - DBCStartingEntry])));
                str.Append("'','','0','1','1','0','35','0','1','1.14286','1','0','0','1500','2000','1','1','1','0','0','0','0'"
                    + ",'0','0','0','0','6','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0',"
                    + "'0','','0','3','1','4','1','1','1','1','0','0','0','0','0','0','0','0','0','0','0','','0');\n");
            }
            File.WriteAllText("Export/CreatureTemplate.sql", str.ToString(), UTF8Encoding.GetEncoding(0));
        }

        private static void populateNewEntries(CreatureModelData modelData, CreatureDisplayInfo displayInfo)
        {
            // CreatureModelData
            int index = (int)modelData.header.RecordCount;
            int newSize = index + paths.Count;
            Array.Resize(ref modelData.body.records, newSize);
            Array.Resize(ref modelData.body.pathStrings, newSize);
            modelData.header.RecordCount = (uint)newSize;
            String[] stringPaths = paths.ToArray();
            uint entry = DBCStartingEntry;
            for (int i = index; i < newSize; ++i)
            {
                CreatureModelData.DBC_Record newRecord = new CreatureModelData.DBC_Record();
                newRecord.ID = entry++;
                newRecord.Flags = 0;
                newRecord.AlternateModel = 1;
                newRecord.sizeClass = 1;
                newRecord.modelScale = 1;
                newRecord.BloodLevel = 0;
                newRecord.Footprint = 0;
                newRecord.footprintTextureLength = 0;
                newRecord.footprintTextureWidth = 0;
                newRecord.footstepShakeSize = 0;
                newRecord.footprintParticleScale = 0;
                newRecord.foleyMaterialId = 0;
                newRecord.deathThudShakeSize = 0;
                newRecord.SoundData = 0;
                newRecord.CollisionHeight = 0;
                newRecord.CollisionWidth = 0;
                newRecord.mountHeight = 0;
                newRecord.geoBoxMaxF1 = 0;
                newRecord.geoBoxMinF1 = 0;
                newRecord.geoBoxMaxF2 = 0;
                newRecord.geoBoxMinF2 = 0;
                newRecord.geoBoxMaxF3 = 0;
                newRecord.geoBoxMinF3 = 0;
                newRecord.worldEffectScale = 1;
                newRecord.Unknown6 = 0;
                newRecord.Unknown5 = 0;
                newRecord.attachedEffectScale = 0;
                modelData.body.pathStrings[i] = stringPaths[i - index];
                modelData.body.records[i] = newRecord;
            }
            // CreatureDisplayInfo
            index = (int)displayInfo.header.RecordCount;
            newSize = index + paths.Count;
            Array.Resize(ref displayInfo.body.records, newSize);
            displayInfo.header.RecordCount = (uint)newSize;
            entry = DBCStartingEntry;
            for (int i = index; i < newSize; ++i)
            {
                CreatureDisplayInfo.DBC_Record newRecord = new CreatureDisplayInfo.DBC_Record();
                newRecord.Model = entry;
                newRecord.ID = entry++;
                newRecord.blood = 0;
                newRecord.bloodLevel = 0;
                newRecord.creatureGeosetData = 0;
                newRecord.ExtraDisplayInfo = 0;
                newRecord.NPCSounds = 0;
                newRecord.objectEffectPackageID = 0;
                newRecord.Opacity = 255;
                newRecord.Particles = 0;
                newRecord.portraitTextureName = 0;
                newRecord.Scale = 1;
                newRecord.Skin1 = 0;
                newRecord.Skin2 = 0;
                newRecord.Skin3 = 0;
                newRecord.Sound = 0;
                displayInfo.body.records[i] = newRecord;
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
