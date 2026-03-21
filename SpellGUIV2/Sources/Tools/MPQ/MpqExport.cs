using StormLibSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpellEditor.Sources.Tools.MPQ
{
    class MpqExport
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private static void EnsureStormLibAvailable()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var arch = Environment.Is64BitProcess ? "x64" : "x86";
            var rootDll = Path.Combine(baseDir, "stormlib.dll");
            var outputArchDll = Path.Combine(baseDir, arch, "StormLib.dll");
            var repoArchDll = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "stormlibsharp", "lib", arch, "rel", "StormLib.dll"));

            if (!File.Exists(rootDll))
            {
                if (File.Exists(outputArchDll))
                {
                    File.Copy(outputArchDll, rootDll, true);
                }
                else if (File.Exists(repoArchDll))
                {
                    File.Copy(repoArchDll, rootDll, true);
                }
            }

            var loadResult = LoadLibrary(rootDll);

            if (loadResult == IntPtr.Zero)
            {
                throw new DllNotFoundException($"StormLib native DLL could not be loaded from {rootDll}");
            }
        }

        public void CreateMpqFromDbcFileList(string archiveName, List<string> exportList)
        {
            EnsureStormLibAvailable();
            var archivePath = "Export\\" + archiveName;
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }
            using (var archive = MpqArchive.CreateNew(archivePath, MpqArchiveVersion.Version1))
            {
                exportList.ForEach((dbcFile) =>
                {
                    var pathToAdd = "DBFilesClient\\" + dbcFile.Substring(dbcFile.IndexOf('\\') + 1);
                    archive.AddFileFromDiskWithCompression(dbcFile, pathToAdd, MpqCompressionTypeFlags.MPQ_COMPRESSION_ZLIB);
                });
            }
        }
    }
}
