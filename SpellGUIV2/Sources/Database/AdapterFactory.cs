using SpellEditor.Sources.Database;
using System;

namespace SpellEditor.Sources.Database
{
    public class AdapterFactory
    {
        private static AdapterFactory instance = new AdapterFactory();

        public static AdapterFactory Instance { get { return instance; } }

        private AdapterFactory()
        {
            // NOOP
        }

        public IDatabaseAdapter GetAdapter(bool initialiseDatabase)
        {
            switch (Config.Config.connectionType)
            {
                case Config.Config.ConnectionType.MySQL:
                    return new MySQL(initialiseDatabase);
                case Config.Config.ConnectionType.SQLite:
                    return new SQLite();
                case Config.Config.ConnectionType.MariaDB:
                    return new MariaDB(initialiseDatabase);
                default:
                    throw new Exception("Unknown config connection type, valid types: MySQL, MariaDB, SQLite");
            }
        }

    }
}
