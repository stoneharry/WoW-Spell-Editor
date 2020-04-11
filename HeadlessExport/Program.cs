using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeadlessExport
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Loading config...");
                Config.Init();
                Console.WriteLine("Creating MySQL connection from config.xml...");
                var adapter = new MySQL();

                Console.WriteLine("Exporting all DBC files...");
                var bindingManager = BindingManager.GetInstance();
                foreach (var binding in bindingManager.GetAllBindings())
                {
                    Console.WriteLine($"Exporting { binding.Name }...");
                    var dbc = new HeadlessDbc();
                    var task = dbc.ExportToDbc(adapter, SetProgress, binding.Fields[0].Name, binding.Name);
                    while (!task.IsCompleted)
                    {
                        Thread.Sleep(500);
                    }
                }

                Console.WriteLine("Done.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Build failed: {e.GetType() }: { e.Message }\n{ e }");
            }
        }

        public static void SetProgress(double value)
        {
            Console.WriteLine($"{ Convert.ToInt32(value * 100D) }%");
        }
    }
}
