using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HeadlessExport
{
    class Program
    {
        static ConcurrentDictionary<int, string> _TaskNameLookup;
        static ConcurrentDictionary<int, ReportProgress> _TaskProgressLookup;
        static ConcurrentDictionary<int, HeadlessDbc> _HeadlessDbcLookup;

        private static BlockingCollection<string> m_Queue = new BlockingCollection<string>();

        static Program()
        {
            var thread = new Thread(
              () =>
              {
                  while (true) Console.WriteLine(m_Queue.Take());
              })
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static void WriteLine(string value)
        {
            m_Queue.Add(value);
        }

        static int PrioritiseSpellCompareBindings(Binding b1, Binding b2)
        {
            if (b1.ToString().Contains("Spell$"))
                return -1;
            if (b2.ToString().Contains("Spell$"))
                return 1;
            
            return string.Compare(b1.ToString(), b2.ToString());
        }

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
                var taskList = new List<Task<Stopwatch>>();
                _TaskNameLookup = new ConcurrentDictionary<int, string>();
                _TaskProgressLookup = new ConcurrentDictionary<int, ReportProgress>();
                _HeadlessDbcLookup = new ConcurrentDictionary<int, HeadlessDbc>();
                var exportWatch = new Stopwatch();
                exportWatch.Start();
                var bindings = bindingManager.GetAllBindings();
                Array.Sort(bindings, PrioritiseSpellCompareBindings);
                foreach (var binding in bindings)
                {
                    Console.WriteLine($"Exporting { binding.Name }...");
                    var dbc = new HeadlessDbc();
                    var task = dbc.TimedExportToDBC(adapter, binding.Fields[0].Name, binding.Name);
                    _TaskNameLookup.TryAdd(dbc.TaskId, binding.Name);
                    _TaskNameLookup.TryAdd(task.Id, binding.Name);
                    _HeadlessDbcLookup.TryAdd(dbc.TaskId, dbc);
                    taskList.Add(task);
                }
                Task.WaitAll(taskList.ToArray());
                exportWatch.Stop();
                taskList.Sort((x, y) => x.Result.ElapsedMilliseconds.CompareTo(y.Result.ElapsedMilliseconds));
                taskList.ForEach(task =>
                {
                    var bindingName = _TaskNameLookup.Keys.Contains(task.Id) ? _TaskNameLookup[task.Id] : task.Id.ToString();
                    Console.WriteLine($" - [{bindingName}]: {Math.Round(task.Result.Elapsed.TotalSeconds, 2)} seconds");
                });
                Console.WriteLine($"Finished exporting { taskList.Count() } dbc files in {Math.Round(exportWatch.Elapsed.TotalSeconds, 2)} seconds.");
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
            var dbc = _HeadlessDbcLookup.Keys.Contains(id) ? _HeadlessDbcLookup[id] : null;
            var elapsedStr = dbc != null ? $"{Math.Round(dbc.Timer.Elapsed.TotalSeconds, 2)}s, " : string.Empty;
            var stateStr = state == 0 ? "Export" : "Write";
            var nameStr = name.Length > 0 ? name : id.ToString();
            WriteLine($" [{nameStr}] {stateStr}: {elapsedStr}{reportValue}%");
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
