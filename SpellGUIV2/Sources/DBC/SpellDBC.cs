using System;
using System.Threading.Tasks;
using System.Data;
using SpellEditor.Sources.Database;

namespace SpellEditor.Sources.DBC
{
    class SpellDBC : AbstractDBC
    {
        public Task ImportToSql(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc UpdateProgress, string bindingName, ImportExportType _type)
        {
            return ImportTo(adapter, UpdateProgress, "ID", bindingName, _type);
        }

        public static DataRow GetRecordById(uint id, MainWindow mainWindows)
        {
            DataRowCollection Result = mainWindows.GetDBAdapter().Query(string.Format("SELECT * FROM `spell` WHERE `ID` = '{0}'", id)).Rows;
            if (Result != null && Result.Count == 1)
                return Result[0];
            return null;
        }

        public Task Export(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, ImportExportType _type)
        {
            return ExportTo(adapter, updateProgress, "ID", "Spell", _type);
        }
    }
}
