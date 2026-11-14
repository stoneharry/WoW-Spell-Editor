using System;
using System.Data;

namespace SpellEditor.Sources.Database
{
    public interface IDatabaseAdapter : IDisposable
    {
        bool Updating { get; set; }

        DataTable Query(string query);
        // Streams rows to the callback without buffering them into a DataTable
        void QueryReader(string query, Action<IDataReader> read);
        void CommitChanges(string query, DataTable dataTable);
        void Execute(string p);
        void CreateAllTablesFromBindings();
        string EscapeString(string str);
        string GetTableCreateString(Binding.Binding binding);
        object QuerySingleValue(string query);
    }
}
