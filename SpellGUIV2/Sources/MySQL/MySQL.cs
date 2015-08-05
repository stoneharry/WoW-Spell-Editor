using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellEditor.Sources.Config;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace SpellEditor.Sources.MySQL
{
    class MySQL
    {
        private Config.Config config;
        private MySqlConnection conn;

        public MySQL(Config.Config config)
        {
            this.config = config;

            String connectionString = "server={0};port={1};uid={2};pwd={3};";
            connectionString = String.Format(connectionString,
                config.Host, config.Port, config.User, config.Pass);

            conn = new MySqlConnection();
            conn.ConnectionString = connectionString;
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = String.Format("CREATE DATABASE IF NOT EXISTS `{0}`;", config.Database);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
            cmd = conn.CreateCommand();
            cmd.CommandText = String.Format("USE `{0}`;", config.Database);
            cmd.ExecuteNonQuery();
            cmd.Dispose();

            cmd = conn.CreateCommand();
            cmd.CommandText = String.Format(getTableCreateString(), config.Table);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        public DataTable query(String query)
        {
            var adapter = new MySqlDataAdapter(query, conn);
            DataSet DS = new DataSet();

            adapter.Fill(DS);

            return DS.Tables[0];
        }

        ~MySQL()
        {
            if (conn != null)
                conn.Close();
        }

        private String getTableCreateString()
        {
            StringBuilder str = new StringBuilder();
            str.Append(@"CREATE TABLE `{0}` (");

            var structure = new SpellEditor.Sources.DBC.Spell_DBC_Record();
            var fields = structure.GetType().GetFields();
            foreach (var f in fields)
            {
                switch (Type.GetTypeCode(f.FieldType))
                {
                    case TypeCode.UInt32:
                        str.Append(String.Format(@"`{0}` int(10) unsigned NOT NULL, ", f.Name));
                        break;
                    case TypeCode.Int32:
                        str.Append(String.Format(@"`{0}` int(11) NOT NULL, ", f.Name));
                        break;
                    default:
                        throw new Exception("ERROR: Unhandled type: " + f.FieldType + " on field: " + f.Name);

                }
                Console.WriteLine(f.FieldType + " | " + f.Name);
            }

            str.Append(@"PRIMARY KEY (`entry`)) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=FIXED;");
            
            return str.ToString();
        }
    }
}
