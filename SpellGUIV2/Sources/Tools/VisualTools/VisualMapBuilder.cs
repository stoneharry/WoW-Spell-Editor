using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            //CreatureModelData modelData = new CreatureModelData();

            Logger.Info($"Loading {Config.Config.BindingsDirectory}\\CreatureDisplayInfo.dbc...");

            Logger.Info($"Creating {files.Count} new entries in both DBC's...");
            //PopulateNewEntries(modelData, displayInfo);

            Logger.Info("Creating Export\\CreatureModelData.dbc...");
            //modelData.SaveDBCFile();

            Logger.Info("Creating Export\\CreatureDisplayInfo.dbc...");
            //displayInfo.SaveDBCFile();

            Logger.Info("Creating creature_template queries...");
            //generateTemplateQueries();

            Logger.Info("Creating creature queries...");
            //generateSpawnQueries();

            Logger.Info("Done.");
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
