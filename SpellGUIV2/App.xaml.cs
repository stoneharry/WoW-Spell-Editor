using System;
using System.Windows;
using NLog;

namespace SpellEditor
{
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Required for OpenAI
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
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
