using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Database
{
    public class SqlFileStorage : IStorageAdapter
    {
        public Task Export(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string idKey, string bindingName)
        {
            return Task.Run(() =>
            {
                using (var newAdapter = AdapterFactory.Instance.GetAdapter(false))
                {
                    adapter.ExportTableToSql(bindingName, "Export", Task.CurrentId, updateProgress);
                }
            });
        }

        public Task Import(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string idKey, string bindingName)
        {
            throw new NotImplementedException();
        }
    }
}
