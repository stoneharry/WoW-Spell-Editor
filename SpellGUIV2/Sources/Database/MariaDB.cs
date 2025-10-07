using System;
using System.Data;
using System.Threading;
using NLog;
using MySqlConnector;

namespace SpellEditor.Sources.Database
{
    public class MariaDB : AbstractAdapter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly MySqlConnection _connection;
        private Timer _heartbeat;

        public MariaDB(bool initialiseDatabase)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = Config.Config.Host,
                UserID = Config.Config.User,
                Password = Config.Config.Pass,
                Port = uint.Parse(Config.Config.Port),
                CharacterSet = "utf8mb4"
            };

            _connection = new MySqlConnection(builder.ConnectionString);
            _connection.Open();

            if (initialiseDatabase)
            {
                // Create DB if not exists and use
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS `{0}`; USE `{0}`;", Config.Config.Database);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                // Use DB
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = string.Format("USE `{0}`;", Config.Config.Database);
                    cmd.ExecuteNonQuery();
                }
            }

            // Heartbeat keeps the connection alive, otherwise it can be killed by remote for inactivity
            // Object reference needs to be held to prevent garbage collection.
            _heartbeat = CreateKeepAliveTimer(TimeSpan.FromMinutes(2));
        }

        public override void Dispose()
        {
            _heartbeat?.Dispose();
            _heartbeat = null;
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                try
                {
                    _connection.Close();
                }
                catch (Exception exception)
                {
                    Logger.Error(exception);
                }
                finally
                {
                    _connection.Dispose();
                }
            }
        }

        public override void CreateAllTablesFromBindings()
        {
            lock (_syncLock)
            {
                foreach (var createStatement in GetAllTableCreateStrings())
                {
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandText = createStatement;
                        Logger.Trace(cmd.CommandText);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public override DataTable Query(string query)
        {
            Logger.Trace(query);
            lock (_syncLock)
            {
                using (var adapter = new MySqlDataAdapter(query, _connection))
                {
                    using (var dataSet = new DataSet())
                    {
                        adapter.SelectCommand.CommandTimeout = 0;
                        adapter.Fill(dataSet);
                        return dataSet.Tables[0];
                    }
                }
            }
        }

        public override object QuerySingleValue(string query)
        {
            Logger.Trace(query);
            lock (_syncLock)
            {
                using (var adapter = new MySqlDataAdapter(query, _connection))
                {
                    using (var dataSet = new DataSet())
                    {
                        adapter.SelectCommand.CommandTimeout = 0;
                        adapter.Fill(dataSet);
                        var table = dataSet.Tables[0];
                        return table.Rows.Count > 0 ? table.Rows[0][0] : null;
                    }
                }
            }
        }

        public override void CommitChanges(string query, DataTable dataTable)
        {
            if (Updating)
                return;

            Logger.Trace(query);
            lock (_syncLock)
            {
                using (var adapter = new MySqlDataAdapter())
                {
                    using (var mcb = new MySqlCommandBuilder(adapter))
                    {
                        mcb.ConflictOption = ConflictOption.OverwriteChanges;
                        adapter.SelectCommand = new MySqlCommand(query, _connection);
                        adapter.Update(dataTable);
                        dataTable.AcceptChanges();
                    }
                }
            }
        }

        public override void Execute(string p)
        {
            if (Updating)
                return;

            Logger.Trace(p);
            lock (_syncLock)
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = p;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override string EscapeString(string keyWord)
        {
            keyWord = keyWord.Replace("'", "''");
            keyWord = keyWord.Replace("\\", "\\\\");
            return keyWord;
        }

        private Timer CreateKeepAliveTimer(TimeSpan interval)
        {
            return new Timer(
                (e) => Execute("SELECT 1"),
                null,
                interval,
                interval);
        }
    }
}
