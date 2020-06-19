using NLog;
using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            Logger.Info("Creating creature_template queries...");
            //generateTemplateQueries();

            Logger.Info("Creating creature queries...");
            //generateSpawnQueries();

            Logger.Info("Done.");
        }

        private void PopulateNewEntries(MutableGenericDbc modelData, MutableGenericDbc displayInfo, List<string> files)
        {
            uint entry = DbStartingId;
            files.ForEach((file) =>
            {
                // Hardcoded for wotlk 3.3.5
                var entryData = new Dictionary<string, object>();
                entryData.Add("id", entry++);
                entryData.Add("flags", 0);
                entryData.Add("modelPath", GetPathRelativeToSpellDir(file));
                entryData.Add("alternateModel", 0);
                entryData.Add("sizeClass", 1);
                entryData.Add("modelScale", 1);
                entryData.Add("bloodId", 0);
                entryData.Add("footprint", 0);
                entryData.Add("footprintTextureLength", 0);
                entryData.Add("footprintTextureWidth", 0);
                entryData.Add("footprintParticleScale", 0);
                entryData.Add("foleyMaterialId", 0);
                entryData.Add("footstepShakeSize", 0);
                entryData.Add("deathThudShakeSize", 0);
                entryData.Add("soundData", 0);
                entryData.Add("collisionWidth", 0);
                entryData.Add("collisionHeight", 0);
                entryData.Add("mountHeight", 0);
                entryData.Add("geoBoxMin1", 0);
                entryData.Add("geoBoxMin2", 0);
                entryData.Add("geoBoxMin3", 0);
                entryData.Add("geoBoxMax1", 0);
                entryData.Add("geoBoxMax2", 0);
                entryData.Add("geoBoxMax3", 0);
                entryData.Add("worldEffectScale", 1);
                entryData.Add("attachedEffectScale", 0);
                entryData.Add("unk5", 0);
                entryData.Add("unk6", 0);
                modelData.AddRecord(entryData);
            });

            entry = DbStartingId;
            files.ForEach((file) =>
            {
                // Hardcoded for wotlk 3.3.5
                var entryData = new Dictionary<string, object>();
                entryData.Add("ID", entry);
                entryData.Add("ModelID", entry);
                entryData.Add("SoundID", 0);
                entryData.Add("ExtendedDisplayInfoID", 0);
                entryData.Add("CreatureModelScale", 1);
                entryData.Add("CreatureModelAlpha", 255);
                entryData.Add("TextureVariation_1", 0);
                entryData.Add("TextureVariation_2", 0);
                entryData.Add("TextureVariation_3", 0);
                entryData.Add("PortraitTextureName", 0);
                entryData.Add("BloodLevel", 0);
                entryData.Add("BloodID", 0);
                entryData.Add("NPCSoundID", 0);
                entryData.Add("ParticleColorID", 0);
                entryData.Add("CreatureGeosetData", 0);
                entryData.Add("ObjectEffectPackageID", 0);
                displayInfo.AddRecord(entryData);
                ++entry;
            });
        }

        private string GetPathRelativeToSpellDir(string path)
        {
            string fileName = path.Substring(path.LastIndexOf('\\'));
            string fullName = path;
            int index = fullName.IndexOf("SPELLS");
            int lastIndex = fullName.LastIndexOf('\\');
            fullName = fullName.Substring(index, lastIndex - index);
            return fullName + "\\" + fileName;
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
