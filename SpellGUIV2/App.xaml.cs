using System;
using System.Reflection;
using System.Windows;
using NLog;

namespace SpellEditor
{
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = new AssemblyName(args.Name).Name + ".dll";
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libraries", dllName);
            if (System.IO.File.Exists(path))
                return Assembly.LoadFrom(path);
            return null;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Logger.Info("######################################################");
            Logger.Info($"Stopped WoW Spell Editor - {DateTime.Now.ToString()}");
            Logger.Info("######################################################");
            Console.Out.Flush();
        }
    }
}
