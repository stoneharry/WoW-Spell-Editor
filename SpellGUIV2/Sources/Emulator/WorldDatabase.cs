using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Emulator
{
    /// <summary>
    /// Optional connection to a 3.3.5 emulator world database, separate from IDatabaseAdapter.
    /// The schema differences between the supported cores live on Emulator.
    /// </summary>
    public class WorldDatabase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _connectionString;
        private readonly Dictionary<uint, RankChain> _rankChains = new Dictionary<uint, RankChain>();

        public Emulator Emulator { get; }
        public string DatabaseName { get; }
        public string Host { get; }
        public string Port { get; }

        public WorldDatabase(Emulator emulator, string host, string port, string user, string pass, string database)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("World database host is not configured");
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("World database name is not configured");

            Emulator = emulator ?? Emulators.Default;
            Host = host;
            Port = string.IsNullOrWhiteSpace(port) ? "3306" : port;
            DatabaseName = database;

            _connectionString = $"server={host};port={Port};uid={user};pwd=\"{pass}\";database={database};" +
                "Charset=utf8mb4;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=10;Connection Lifetime=300;Default Command Timeout=60;";
        }

        public static WorldDatabase FromConfig() => new WorldDatabase(
            Emulators.Current,
            Config.Config.EmulatorHost,
            Config.Config.EmulatorPort,
            Config.Config.EmulatorUser,
            Config.Config.EmulatorPass,
            Config.Config.EmulatorDatabase);

        public static bool IsConfigured => Config.Config.EmulatorEnabled
            && Config.Config.EmulatorHost.Length > 0
            && Config.Config.EmulatorDatabase.Length > 0;

        private MySqlConnection OpenConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public void TestConnection() => OpenConnection().Dispose();

        public DataTable Query(string sql)
        {
            Logger.Trace(sql);
            using (var connection = OpenConnection())
            using (var adapter = new MySqlDataAdapter(sql, connection))
            using (var dataSet = new DataSet())
            {
                adapter.Fill(dataSet);
                return dataSet.Tables[0];
            }
        }

        public object QuerySingleValue(string sql)
        {
            Logger.Trace(sql);
            using (var connection = OpenConnection())
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
        }

        public int Execute(string sql)
        {
            Logger.Trace(sql);
            using (var connection = OpenConnection())
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>One transaction so a partially applied save is not possible.</summary>
        public int ExecuteTransaction(IReadOnlyList<string> statements)
        {
            if (statements == null || statements.Count == 0)
                return 0;

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var affected = 0;
                    foreach (var statement in statements)
                    {
                        Logger.Trace(statement);
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = statement;
                            affected += cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    return affected;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public List<string> FindMissingTables(IEnumerable<string> tableNames)
        {
            var wanted = tableNames.ToList();
            if (wanted.Count == 0)
                return wanted;

            var inClause = string.Join(", ", wanted.Select(name => $"'{Escape(name)}'"));
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = Query("SELECT TABLE_NAME FROM information_schema.TABLES " +
                $"WHERE TABLE_SCHEMA = '{Escape(DatabaseName)}' AND TABLE_NAME IN ({inClause})");
            foreach (DataRow row in result.Rows)
                found.Add(row[0].ToString());

            return wanted.Where(name => !found.Contains(name)).ToList();
        }

        /// <summary>Cached until a write could have changed it, every tab asks for the same spell.</summary>
        public RankChain GetRankChain(uint spellId)
        {
            lock (_rankChains)
            {
                if (_rankChains.TryGetValue(spellId, out var cached))
                    return cached;
            }

            var chain = RankChain.Load(this, spellId);
            lock (_rankChains)
                _rankChains[spellId] = chain;
            return chain;
        }

        public void ClearRankChains()
        {
            lock (_rankChains)
                _rankChains.Clear();
        }

        public Task<DataTable> QueryAsync(string sql) => Task.Run(() => Query(sql));

        public Task<int> ExecuteTransactionAsync(IReadOnlyList<string> statements) => Task.Run(() => ExecuteTransaction(statements));

        public static string Escape(string value)
        {
            if (value == null)
                return string.Empty;
            // Backslashes first, or the ones added for the quotes get escaped again
            return value.Replace("\\", "\\\\").Replace("'", "''");
        }
    }
}
