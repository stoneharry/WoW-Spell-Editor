using MySql.Data.MySqlClient;
using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.SpellStringTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeadlessExport
{
    class Program
    {
        private static readonly BlockingCollection<string> _logQueue = new BlockingCollection<string>();

        static Program()
        {
            var thread = new Thread(() => { while (true) Console.WriteLine(_logQueue.Take()); })
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static void WriteLine(string value) => _logQueue.Add(value);

        static void Main(string[] args)
        {
            IDatabaseAdapter adapter = null;
            try
            {
                Console.WriteLine("Loading config...");
                Config.Init();
                Config.connectionType = Config.ConnectionType.MySQL;

                var watch = new Stopwatch();
                watch.Start();

                adapter = AdapterFactory.Instance.GetAdapter(false);
                WriteLine($"Connected in {Math.Round(watch.Elapsed.TotalSeconds, 2)}s");

                Task.WaitAll(DBCManager.GetInstance().LoadRequiredDbcs().ToArray());

                // Load every spell row once — eliminates all per-spell DB queries
                WriteLine("Loading all spell rows into memory...");
                watch.Restart();
                var allSpells = adapter.Query("SELECT * FROM spell");
                var resultCache = new ConcurrentDictionary<uint, DataTable>();
                var cachingAdapter = new CachingDatabaseAdapter(adapter, allSpells, resultCache);
                var spellRows = allSpells.AsEnumerable();
                WriteLine($"Loaded {allSpells.Rows.Count} rows in {Math.Round(watch.Elapsed.TotalSeconds, 2)}s");

                // Parse descriptions in parallel — CPU-bound after caching
                watch.Restart();
                var parser = new SpellStringParser();
                var descriptions = new ConcurrentBag<KeyValuePair<string, string>>();
                int totalRows = allSpells.Rows.Count;
                int progress = 0;

                Parallel.ForEach(
                    spellRows,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    row =>
                    {
                        var current = Interlocked.Increment(ref progress);
                        if (current % 1000 == 0)
                            WriteLine($"Progress: {current * 100 / totalRows}%");

                        var desc = row["spelldescription0"].ToString();
                        if (desc.Trim().Length == 0) return;

                        var id = row["id"].ToString();
                        try
                        {
                            descriptions.Add(new KeyValuePair<string, string>(
                                id, parser.ParseString(desc, row, cachingAdapter)));
                        }
                        catch (Exception ex)
                        {
                            WriteLine($"[{id}] {ex.Message}");
                        }
                    });

                WriteLine($"Parsed {descriptions.Count} descriptions in {Math.Round(watch.Elapsed.TotalSeconds, 2)}s");

                // Write results in batches
                watch.Restart();
                const string insertPrefix = "REPLACE INTO new_world.item_spell_gem_desc VALUES ";
                var sb = new StringBuilder(insertPrefix);
                int count = 0;
                foreach (var pair in descriptions)
                {
                    sb.Append($"({pair.Key}, \"{MySqlHelper.EscapeString(pair.Value)}\"), ");
                    if (++count % 500 == 0)
                    {
                        sb.Remove(sb.Length - 2, 2);
                        ExecuteBatch(adapter, sb.ToString());
                        sb.Clear();
                        sb.Append(insertPrefix);
                    }
                }
                if (sb.Length > insertPrefix.Length)
                {
                    sb.Remove(sb.Length - 2, 2);
                    ExecuteBatch(adapter, sb.ToString());
                }

                WriteLine($"Wrote {count} rows in {Math.Round(watch.Elapsed.TotalSeconds, 2)}s");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Build failed: {e.GetType()}: {e.Message}\n{e}");
            }
            finally
            {
                adapter?.Dispose();
            }
        }

        private static void ExecuteBatch(IDatabaseAdapter adapter, string sql)
        {
            try
            {
                adapter.Execute(sql);
            }
            catch (Exception ex)
            {
                WriteLine($"Batch write failed: {ex.Message}");
            }
        }
    }
}
