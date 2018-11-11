using MySql.Data.MySqlClient;
using SpellEditor.Sources.Config;
using System;
using System.Data;
using System.Reflection;
using System.Text;

namespace SpellEditor.Sources.MySQL
{
    class MySQL : DBAdapter
    {
        private Config.Config config;
        private MySqlConnection conn;
        public string Table
        {
            get;
            set;
        }
        public bool Updating
        {
            get;
            set;
        }

        public MySQL(Config.Config config)
        {
            this.config = config;
            this.Table = config.Table;

            string connectionString = "server={0};port={1};uid={2};pwd={3};Charset=utf8;";
                connectionString = string.Format(connectionString,
                config.Host, config.Port, config.User, config.Pass);

            conn = new MySqlConnection();
            conn.ConnectionString = connectionString;
            conn.Open();
            // Create DB
            var cmd = conn.CreateCommand();
            cmd.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS `{0}`;", config.Database);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
            cmd = conn.CreateCommand();
            cmd.CommandText = string.Format("USE `{0}`;", config.Database);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
            // Create Table
            cmd = conn.CreateCommand();
            cmd.CommandText = string.Format(GetTableCreateString(), config.Table);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        ~MySQL()
        {
            if (conn != null)
                conn.Close();
        }

        private readonly object syncLock = new object();

        public DataTable query(string query)
        {
            lock (syncLock)
            {
                var adapter = new MySqlDataAdapter(query, conn);
                DataSet DS = new DataSet();
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(DS);
                return DS.Tables[0];
            }
        }

        public void CommitChanges(string query, DataTable dataTable)
        {
            if (Updating)
                return;
            lock (syncLock)
            {
                var adapter = new MySqlDataAdapter();
                var mcb = new MySqlCommandBuilder(adapter);
                mcb.ConflictOption = ConflictOption.OverwriteChanges;
                adapter.SelectCommand = new MySqlCommand(query, conn);
                adapter.Update(dataTable);
                dataTable.AcceptChanges();
            }
        }

        public void Execute(string p)
        {
            if (Updating)
                return;
            //lock (syncLock)
            //{
            var cmd = conn.CreateCommand();
                cmd.CommandText = p;
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            //}
        }

        public string GetTableCreateString()
        {
            StringBuilder str = new StringBuilder();
            str.Append(@"CREATE TABLE IF NOT EXISTS `{0}` (");

            var structure = new SpellEditor.Sources.DBC.Spell_DBC_Record();
            var fields = structure.GetType().GetFields();
            foreach (var f in fields)
            {
                switch (Type.GetTypeCode(f.FieldType))
                {
                    case TypeCode.UInt32:
                        {
                            str.Append(string.Format(@"`{0}` int(10) unsigned NOT NULL DEFAULT '0', ", f.Name));
                            break;
                        }
                    case TypeCode.Int32:
                        str.Append(string.Format(@"`{0}` int(11) NOT NULL DEFAULT '0', ", f.Name));
                        break;
                    case TypeCode.Single:
                        str.Append(string.Format(@"`{0}` FLOAT NOT NULL DEFAULT '0', ", f.Name));
                        break;
                    case TypeCode.Object:
                        {
                            var attr = f.GetCustomAttribute<SpellEditor.Sources.DBC.HandleField>();
                            if (attr != null)
                            {
                                if (attr.Method == 1)
                                {
                                    for (int i = 0; i < attr.Count; ++i)
                                        str.Append(string.Format(@"`{0}{1}` TEXT CHARACTER SET utf8, ", f.Name, i));
                                    break;
                                }
                                else if (attr.Method == 2)
                                {
                                    for (int i = 0; i < attr.Count; ++i)
                                        str.Append(string.Format(@"`{0}{1}` int(10) unsigned NOT NULL DEFAULT '0', ", f.Name, i));
                                    break;
                                }
                            }
                            goto default;
                        }
                    default:
                        throw new Exception("ERROR: Unhandled type: " + f.FieldType + " on field: " + f.Name);

                }
            }

            str.Append(@"PRIMARY KEY (`ID`)) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;");
            
            return str.ToString();
        }
    }
}
