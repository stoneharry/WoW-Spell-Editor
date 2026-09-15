using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StormLibSharp.Native
{
    internal static class NativeLibraryResolver
    {
        [ModuleInitializer]
        internal static void Install()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeLibraryResolver).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!libraryName.StartsWith("stormlib", StringComparison.OrdinalIgnoreCase))
                return IntPtr.Zero;

            foreach (var candidate in GetCandidatePaths())
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
                    return handle;
            }
            return IntPtr.Zero;
        }

        private static string[] GetCandidatePaths()
        {
            var baseDir = AppContext.BaseDirectory;
            var rid = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => "win-x64"
            };
            return new[]
            {
                Path.Combine(baseDir, "runtimes", rid, "native", "StormLib.dll"),
                Path.Combine(baseDir, "StormLib.dll")
            };
        }
    }
}
