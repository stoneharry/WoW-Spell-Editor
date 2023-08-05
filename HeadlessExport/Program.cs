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
        static ConcurrentDictionary<int, int> _TaskProgressLookup;
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
            var adapters = new List<IDatabaseAdapter>();
            try
            {
                Console.WriteLine("Loading config...");
                Config.Init();
                Config.connectionType = Config.ConnectionType.MySQL;

                SpawnAdapters(ref adapters);
                var adapterIndex = 0;

                Console.WriteLine("Reading all bindings...");
                var bindingManager = BindingManager.GetInstance();
                Console.WriteLine($"Got {bindingManager.GetAllBindings().Count()} bindings to export");

                Console.WriteLine("Exporting all DBC files...");
                var taskList = new List<Task<Stopwatch>>();
                _TaskNameLookup = new ConcurrentDictionary<int, string>();
                _TaskProgressLookup = new ConcurrentDictionary<int, int>();
                _HeadlessDbcLookup = new ConcurrentDictionary<int, HeadlessDbc>();
                var exportWatch = new Stopwatch();
                exportWatch.Start();
                var bindings = bindingManager.GetAllBindings();
                Array.Sort(bindings, PrioritiseSpellCompareBindings);
                foreach (var binding in bindings)
                {
                    Console.WriteLine($"Exporting {binding.Name} using Adapter{adapterIndex + 1}...");
                    var adapter = adapters[adapterIndex++];
                    if (adapterIndex >= adapters.Count)
                        adapterIndex = 0;
                    var dbc = new HeadlessDbc();
                    var task = dbc.TimedExportToDBC(adapter, binding.Fields[0].Name, binding.Name, ImportExportType.DBC);
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
                Console.WriteLine($"Finished exporting {taskList.Count()} dbc files in {Math.Round(exportWatch.Elapsed.TotalSeconds, 2)} seconds.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Build failed: {e.GetType()}: {e.Message}\n{e}");
            }
            finally
            {
                adapters.ForEach((adapter) => adapter.Dispose());
            }
        }

        private static void SpawnAdapters(ref List<IDatabaseAdapter> adapters)
        {
            var tasks = new List<Task<IDatabaseAdapter>>();
            int numBindings = BindingManager.GetInstance().GetAllBindings().Length;
            int numConnections = Math.Max(numBindings >= 2 ? 2 : 1, numBindings / 10);
            WriteLine($"Spawning {numConnections} adapters...");
            var timer = new Stopwatch();
            timer.Start();
            for (var i = 0; i < numConnections; ++i)
            {
                tasks.Add(Task.Run(() =>
                {
                    var adapter = AdapterFactory.Instance.GetAdapter(false);
                    WriteLine($"Spawned Adapter{Task.CurrentId}");
                    return adapter;
                }));
            }
            Task.WaitAll(tasks.ToArray());
            foreach (var task in tasks)
            {
                adapters.Add(task.Result);
            }
            timer.Stop();
            WriteLine($"Spawned {numConnections} adapters in {Math.Round(timer.Elapsed.TotalSeconds, 2)} seconds.");
        }

        public static void SetProgress(double value, int taskId = 0)
        {
            int reportValue = Convert.ToInt32(value * 100D);
            int id = Task.CurrentId.GetValueOrDefault(0);
            var name = _TaskNameLookup.Keys.Contains(id) ? _TaskNameLookup[id] : string.Empty;
            if (_TaskProgressLookup.TryGetValue(id, out var savedProgress))
            {
                if (reportValue > (savedProgress + 5))
                {
                    if (_TaskProgressLookup.TryUpdate(id, reportValue, savedProgress))
                    {
                        LogProgress(name, id, reportValue);
                    }
                }
            }
            else
            {
                _TaskProgressLookup.TryAdd(id, reportValue);
            }
        }

        public static void LogProgress(string name, int id, int reportValue)
        {
            var dbc = _HeadlessDbcLookup.Keys.Contains(id) ? _HeadlessDbcLookup[id] : null;
            var elapsedStr = dbc != null ? $"{Math.Round(dbc.Timer.Elapsed.TotalSeconds, 2)}s, " : string.Empty;
            var nameStr = name.Length > 0 ? name : id.ToString();
            WriteLine($" [{nameStr}] Export: {elapsedStr}{reportValue}%");
        }
    }
}
