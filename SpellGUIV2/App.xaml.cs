using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SpellEditor
{
    public partial class App : Application
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
