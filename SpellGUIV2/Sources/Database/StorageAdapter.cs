using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Database
{
    public interface IStorageAdapter
    {
        Task Export(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string idKey, string bindingName);

        Task Import(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string idKey, string bindingName);
    }
}
