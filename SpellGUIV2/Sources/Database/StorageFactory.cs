using System;

namespace SpellEditor.Sources.Database
{
    public class StorageFactory
    {
        private static StorageFactory instance = new StorageFactory();

        public static StorageFactory Instance { get { return instance; } }

        private StorageFactory()
        {
            // NOOP
        }

        public IStorageAdapter GetStorageAdapter(ImportExportType type)
        {
            switch (type)
            {
                case ImportExportType.DBC:
                    return new DbcFileStorage();
                case ImportExportType.CSV:
                    return new CsvFileStorage();
                case ImportExportType.SQL:
                    return new SqlFileStorage();
                default:
                    throw new Exception("Unhandled ImportExport type: " + type.ToString());
            }
        }

    }
}
