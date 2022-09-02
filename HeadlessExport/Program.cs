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
        static Dictionary<int, string> _TaskNameLookup;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Loading config...");
                Config.Init();

                Console.WriteLine("Creating MySQL connection from config.xml...");
                var adapter = new MySQL();

                Console.WriteLine("Reading all bindings...");
                var bindingManager = BindingManager.GetInstance();
                Console.WriteLine($"Got { bindingManager.GetAllBindings().Count() } bindings to export");

                Console.WriteLine("Exporting all DBC files...");
                var taskList = new List<Task>();
                _TaskNameLookup = new Dictionary<int, string>();
                foreach (var binding in bindingManager.GetAllBindings())
                {
                    Console.WriteLine($"Exporting { binding.Name }...");
                    var task = new HeadlessDbc()
                        .ExportToDbc(adapter, SetProgress, binding.Fields[0].Name, binding.Name);
                    _TaskNameLookup.Add(task.Id, binding.Name);
                    taskList.Add(task);
                }
                Task.WaitAll(taskList.ToArray());
                Console.WriteLine($"Finished exporting { taskList.Count() } dbc files.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Build failed: {e.GetType() }: { e.Message }\n{ e }");
            }
        }

        public static void SetProgress(double value)
        {
            int id = Task.CurrentId.GetValueOrDefault(0);
            var name = _TaskNameLookup.Keys.Contains(id) ? _TaskNameLookup[id] : string.Empty;
            Console.WriteLine($" { (name.Length > 0 ? name : $"[{ id }]") }: { Convert.ToInt32(value * 100D) }%");
        }
    }
}
