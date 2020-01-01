using System;
using System.Threading.Tasks;
using System.Data;
using SpellEditor.Sources.Database;

namespace SpellEditor.Sources.DBC
{
    class SpellDBC : AbstractDBC
    {
        public bool LoadDBCFile(MainWindow window)
        {
            try
            {
                ReadDBCFile(Config.Config.DbcDirectory + "\\Spell.dbc");
            }
            catch (Exception ex)
            {
                window.HandleErrorMessage(ex.Message);
                return false;
            }
            return true;
        }

        public Task ImportToSql(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc UpdateProgress, string bindingName)
        {
            return ImportToSql(adapter, UpdateProgress, "ID", bindingName);
        }

        public static DataRow GetRecordById(uint id, MainWindow mainWindows)
        {
            DataRowCollection Result = mainWindows.GetDBAdapter().Query(string.Format("SELECT * FROM `spell` WHERE `ID` = '{0}'", id)).Rows;
            if (Result != null && Result.Count == 1)
                return Result[0];
            return null;
        }

        public Task Export(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress)
        {
            return ExportToDbc(adapter, updateProgress, "ID", "Spell");
        }
    }
}
