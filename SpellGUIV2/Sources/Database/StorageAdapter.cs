using SpellEditor.Sources.DBC;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Database
{
    public interface IStorageAdapter
    {
        Task Export(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName, DBCBodyToSerialize spellBody);
        Task Export(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName);
        Task Import(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName);
    }
}
