using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace HeadlessExport
{
    class Program
    {
        static ConcurrentDictionary<int, string> _TaskNameLookup;
        static ConcurrentDictionary<int, ReportProgress> _TaskProgressLookup;

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
                _TaskNameLookup = new ConcurrentDictionary<int, string>();
                _TaskProgressLookup = new ConcurrentDictionary<int, ReportProgress>();
                foreach (var binding in bindingManager.GetAllBindings())
                {
                    Console.WriteLine($"Exporting { binding.Name }...");
                    var task = new HeadlessDbc()
                        .ExportToDbc(adapter, SetProgress, binding.Fields[0].Name, binding.Name);
                    _TaskNameLookup.TryAdd(task.Id, binding.Name);
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
            int reportValue = Convert.ToInt32(value * 100D);
            int id = Task.CurrentId.GetValueOrDefault(0);
            var name = _TaskNameLookup.Keys.Contains(id) ? _TaskNameLookup[id] : string.Empty;
            if (_TaskProgressLookup.TryGetValue(id, out var savedProgress))
            {
                int state = savedProgress.State;
                if (reportValue > savedProgress.Progress)
                {
                    LogProgress(name, id, reportValue, state);
                }
                else if (reportValue < savedProgress.Progress)
                {
                    ++state;
                    LogProgress(name, id, reportValue, state);
                }
                _TaskProgressLookup.TryUpdate(id, new ReportProgress(reportValue, state), savedProgress);
            }
            else
            {
                _TaskProgressLookup.TryAdd(id, new ReportProgress(reportValue, 0));
            }
        }

        public static void LogProgress(string name, int id, int reportValue, int state)
        {
            var stateStr = state == 0 ? "Export" : "Write";
            var nameStr = name.Length > 0 ? name : $"[{id}]";
            Console.WriteLine($" [{nameStr}] {stateStr}: {reportValue}%");
        }

        class ReportProgress
        {
            public readonly int Progress;
            public readonly int State;

            public ReportProgress(int progress, int state)
            {
                Progress = progress;
                State = state;
            }
        }
    }
}
