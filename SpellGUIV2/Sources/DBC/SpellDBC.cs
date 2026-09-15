using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using SpellEditor.Sources.Database;

namespace SpellEditor.Sources.DBC
{
    class SpellDBC : AbstractDBC
    {
        // Persistent for the lifetime of the app. Rows are only refetched when a spell is
        // selected, refreshed or saved.
        private static readonly Dictionary<uint, DataRow> RecordCache = new Dictionary<uint, DataRow>();

        public Task ImportToSql(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc UpdateProgress, string bindingName, ImportExportType _type)
        {
            return ImportTo(adapter, UpdateProgress, "ID", bindingName, _type);
        }

        public static void InvalidateRecord(uint id)
        {
            lock (RecordCache)
                RecordCache.Remove(id);
        }

        public static void SeedRecordCache(DataTable table)
        {
            if (table == null)
                return;
            lock (RecordCache)
            {
                foreach (DataRow row in table.Rows)
                {
                    if (uint.TryParse(row["ID"].ToString(), out var id))
                        RecordCache[id] = row;
                }
            }
        }

        public static bool IsCached(uint id)
        {
            lock (RecordCache)
                return RecordCache.ContainsKey(id);
        }

        public static DataRow GetCachedRecord(uint id)
        {
            lock (RecordCache)
                return RecordCache.TryGetValue(id, out var cached) ? cached : null;
        }

        // The cached row on its own table, the spell load path works on a table of one row
        public static DataTable GetCachedTable(uint id)
        {
            var row = GetCachedRecord(id);
            if (row == null || row.Table == null)
                return null;
            var table = row.Table.Clone();
            table.ImportRow(row);
            return table;
        }

        public static DataRow GetRecordById(uint id, MainWindow mainWindows)
        {
            lock (RecordCache)
            {
                if (RecordCache.TryGetValue(id, out var cached))
                    return cached;
            }
            DataRowCollection Result = mainWindows.GetDBAdapter().Query(string.Format("SELECT * FROM `spell` WHERE `ID` = '{0}'", id)).Rows;
            var row = Result != null && Result.Count == 1 ? Result[0] : null;
            if (row != null)
            {
                lock (RecordCache)
                    RecordCache[id] = row;
            }
            return row;
        }

        public Task Export(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, ImportExportType _type)
        {
            return ExportTo(adapter, updateProgress, "ID", "Spell", _type);
        }
    }
}
