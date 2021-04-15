using MySql.Data.MySqlClient;
using NLog;
using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SpellEditor.Sources.Tools.VisualTools
{
    public class VisualMapBuilder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string LoadDirectory = "VisualMapBuilder\\SPELLS";
        public uint DbStartingId;
        public uint MapId;
        public int TopLeftX;
        public int TopLeftY;
        public int BottomRightX;
        public int BottomRightY;
        public int CoordZ;
        public int CellSize;
        public string CreatureQuery;
        public string CreatureDisplayInfoQuery;
        public string CreatureTemplateQuery;

        public VisualMapBuilder()
        {

        }

        public void RunBuild()
        {
            Logger.Info($"Loading all m2 files in \\{LoadDirectory} directory...");
            var files = AllM2FilesRecursive(LoadDirectory, new List<string>());
            Logger.Info($"Loaded {files.Count} m2 files.");

            Logger.Info($"Loading {Config.Config.BindingsDirectory}\\CreatureModelData.dbc...");
            var modelData = DBCManager.GetInstance().ReadLocalDbcForBinding("CreatureModelData");

            Logger.Info($"Loading {Config.Config.BindingsDirectory}\\CreatureDisplayInfo.dbc...");
            var displayInfo = DBCManager.GetInstance().ReadLocalDbcForBinding("CreatureDisplayInfo");

            Logger.Info($"Creating {files.Count} new entries in both DBC's...");
            PopulateNewEntries(modelData, displayInfo, files);

            Logger.Info("Creating Export\\CreatureModelData.dbc...");
            modelData.SaveToFile("CreatureModelData");

            Logger.Info("Creating Export\\CreatureDisplayInfo.dbc...");
            displayInfo.SaveToFile("CreatureDisplayInfo");

            Logger.Info("Creating queries...");
            SaveQueries(files);

            Logger.Info("Done.");
        }

        private void PopulateNewEntries(MutableGenericDbc modelData, MutableGenericDbc displayInfo, List<string> files)
        {
            uint entry = DbStartingId;
            files.ForEach((file) =>
            {
                // Hardcoded for wotlk 3.3.5
                modelData.AddRecord(new Dictionary<string, object>
                {
                    { "id", entry++ },
                    { "flags", 0 },
                    { "modelPath", GetPathRelativeToSpellDir(file) },
                    { "alternateModel", 0 },
                    { "sizeClass", 1 },
                    { "modelScale", 1 },
                    { "bloodId", 0 },
                    { "footprintTextureId", 0 },
                    { "footprintTextureLength", 0 },
                    { "footprintTextureWidth", 0 },
                    { "footprintParticleScale", 0 },
                    { "foleyMaterialId", 0 },
                    { "footstepShakeSize", 0 },
                    { "deathThudShakeSize", 0 },
                    { "soundData", 0 },
                    { "collisionWidth", 0 },
                    { "collisionHeight", 0 },
                    { "mountHeight", 0 },
                    { "geoBoxMinX", 0 },
                    { "geoBoxMinY", 0 },
                    { "geoBoxMinZ", 0 },
                    { "geoBoxMaxX", 0 },
                    { "geoBoxMaxY", 0 },
                    { "geoBoxMaxZ", 0 },
                    { "worldEffectScale", 1 },
                    { "attachedEffectScale", 0 },
                    { "MissileCollisionRadius", 0 },
                    { "MissileCollisionPush", 0 },
                    { "MissileCollisionRaise", 0 }
                });
            });

            entry = DbStartingId;
            files.ForEach((file) =>
            {
                // Hardcoded for wotlk 3.3.5
                displayInfo.AddRecord(new Dictionary<string, object>
                {
                    { "ID", entry },
                    { "ModelID", entry },
                    { "SoundID", 0 },
                    { "ExtendedDisplayInfoID", 0 },
                    { "CreatureModelScale", 1 },
                    { "CreatureModelAlpha", 255 },
                    { "TextureVariation_1", 0 },
                    { "TextureVariation_2", 0 },
                    { "TextureVariation_3", 0 },
                    { "PortraitTextureName", 0 },
                    { "BloodLevel", 0 },
                    { "BloodID", 0 },
                    { "NPCSoundID", 0 },
                    { "ParticleColorID", 0 },
                    { "CreatureGeosetData", 0 },
                    { "ObjectEffectPackageID", 0 }
                });
                ++entry;
            });
        }

        private void SaveQueries(List<string> files)
        {
            SaveSpawnQueries(files);
            SaveTemplateQueries(files);
        }

        private void SaveSpawnQueries(List<string> files)
        {
            var queries = new List<string>();
            var entry = DbStartingId;
            var currX = TopLeftX;
            var currY = TopLeftY;
            var limitX = BottomRightX;
            var limitY = BottomRightY;
            for (var i = 0; i < files.Count; ++i)
            {
                // id, map, zone, x, y, z, health
                queries.Add(string.Format(CreatureQuery, entry++, MapId, 0, currX, currY, CoordZ, 100));
                currX -= CellSize;
                if (currX <= limitX)
                {
                    currX = TopLeftX;
                    currY -= CellSize;
                    if (currY <= limitY)
                        throw new Exception("Spawned outside of defined grid.");
                }
            }
            File.WriteAllLines("Export/Creature.sql", queries, UTF8Encoding.GetEncoding(0));
        }

        private void SaveTemplateQueries(List<string> files)
        {
            var queries = new List<string>();
            var entry = DbStartingId;
            for (var i = 0; i < files.Count; ++i)
            {
                var name = files[i].Substring(files[i].IndexOf("SPELL"));
                queries.Add(string.Format(CreatureTemplateQuery, entry, entry, MySqlHelper.EscapeString(name)));
                queries.Add(string.Format(CreatureDisplayInfoQuery, entry));
                ++entry;
            }
            File.WriteAllLines("Export/CreatureTemplate.sql", queries, UTF8Encoding.GetEncoding(0));
        }

        private string GetPathRelativeToSpellDir(string path)
        {
            string fileName = path.Substring(path.LastIndexOf('\\'));
            string fullName = path;
            int index = fullName.IndexOf("SPELLS");
            int lastIndex = fullName.LastIndexOf('\\');
            fullName = fullName.Substring(index, lastIndex - index);
            return fullName + fileName;
        }

        private List<string> AllM2FilesRecursive(string directory, List<string> foundFiles)
        {
            // Load all sub directories recursively
            Directory.EnumerateDirectories(directory)
                .ToList()
                .ForEach((subDir) => foundFiles = AllM2FilesRecursive(subDir, foundFiles));
            // Load all files in current directory
            Directory.EnumerateFiles(directory)
                .ToList()
                .Where((file) => file.ToLower().EndsWith(".m2"))
                .ToList()
                .ForEach(foundFiles.Add);
            return foundFiles;
        }
    }
}
