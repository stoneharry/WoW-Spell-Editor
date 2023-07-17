using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Database
{
    public class SqlFileStorage : IStorageAdapter
    {
        public Task Export(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string idKey, string bindingName)
        {
            return Task.Run(() =>
            {
                adapter.ExportTableToSql(bindingName, "Export", updateProgress);
            });
        }

        public Task Import(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string idKey, string bindingName)
        {
            throw new NotImplementedException();
        }
    }
}
