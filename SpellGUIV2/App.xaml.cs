using System;
using System.Windows;

namespace SpellEditor
{
    public partial class App
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Console.WriteLine("######################################################");
            Console.WriteLine($"Stopped WoW Spell Editor - {DateTime.Now.ToString()}");
            Console.WriteLine("######################################################");
            Console.Out.Flush();
        }
    }
}
