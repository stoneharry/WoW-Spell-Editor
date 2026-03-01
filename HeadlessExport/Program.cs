using MySql.Data.MySqlClient;
using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.SpellStringTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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

        static void Main(string[] args)
        {
            var adapters = new List<IDatabaseAdapter>();
            try
            {
                Console.WriteLine("Loading config...");
                Config.Init();
                Config.connectionType = Config.ConnectionType.MySQL;

                SpawnAdapters(ref adapters, 10);

                var descriptions = new List<KeyValuePair<string, string>>();
                var adapter = adapters[1];

                WriteLine("Getting all spell data...");
                using (var query = adapter.Query("SELECT * FROM spell"))
                {
                    var rows = query.AsEnumerable();
                    var totalRows = rows.Count();
                    int progress = 0;
                    foreach (var row in rows)
                    {
                        if (++progress % 100 == 0)
                        {
                            int percent = (progress / totalRows) * 100;
                            WriteLine($"====== Progress: ===== {percent}%");
                            //controller.SetProgress(percent);
                        }
                        var id = row["id"].ToString();
                        var desc = row["spelldescription0"].ToString();
                        if (desc.Trim().Length == 0)
                            continue;
                        try
                        {
                            descriptions.Add(new KeyValuePair<string, string>(id, SpellStringParser.ParseString(desc, row, adapter)));
                        }
                        catch (Exception ex)
                        {
                            WriteLine(ex.ToString() + "\n" + ex.StackTrace);
                        }
                    }
                }
                var sb = new StringBuilder();
                var count = 0;
                sb.Append($"REPLACE INTO new_world.item_spell_gem_desc VALUES ");
                foreach (var pair in descriptions)
                {

                    sb.Append($"({pair.Key}, \"{MySqlHelper.EscapeString(pair.Value)}\"), ");
                    if (++count % 500 == 0)
                    {
                        sb = sb.Remove(sb.Length - 2, 2);
                        try
                        {
                            adapter.Execute(sb.ToString());
                            sb = new StringBuilder();
                            sb.Append($"REPLACE INTO new_world.item_spell_gem_desc VALUES ");
                        }
                        catch (Exception ex)
                        {
                            WriteLine("Failed on query:\n" + sb.ToString());
                            WriteLine(ex.ToString());
                            sb = new StringBuilder();
                            sb.Append($"REPLACE INTO new_world.item_spell_gem_desc VALUES ");
                        }
                    }
                }
                if (sb.Length > 0)
                {
                    sb = sb.Remove(sb.Length - 2, 2);
                    try
                    {
                        adapter.Execute(sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        WriteLine("Failed on query:\n" + sb.ToString());
                        WriteLine(ex.ToString());
                        sb = new StringBuilder();
                        sb.Append($"REPLACE INTO new_world.item_spell_gem_desc VALUES ");
                    }
                }
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

        private static void SpawnAdapters(ref List<IDatabaseAdapter> adapters, int numConnections)
        {
            var tasks = new List<Task<IDatabaseAdapter>>();
            int numBindings = BindingManager.GetInstance().GetAllBindings().Length;
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
    }
}
