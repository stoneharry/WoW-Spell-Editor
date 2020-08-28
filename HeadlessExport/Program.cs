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
                Console.WriteLine($"Got { bindingManager.GetAllBindings().Count() } bindings to export");
                var taskList = new List<Task>();
                foreach (var binding in bindingManager.GetAllBindings())
                {
                    Console.WriteLine($"Exporting { binding.Name }...");
                    var dbc = new HeadlessDbc();
                    taskList.Add(dbc.ExportToDbc(adapter, SetProgress, binding.Fields[0].Name, binding.Name));
                }
                while (taskList.Any(task => !task.IsCompleted))
                {
                    Thread.Sleep(500);
                }
                Console.WriteLine($"Finished exporting { taskList.Count() } dbc files.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Build failed: {e.GetType() }: { e.Message }\n{ e }");
            }
        }

        public static void SetProgress(double value)
        {
            Console.WriteLine($"[{ Task.CurrentId }]: { Convert.ToInt32(value * 100D) }%");
        }
    }
}
