using System;
using System.Data;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Database
{
    public interface IDatabaseAdapter : IDisposable
    {
        bool Updating { get; set; }

        DataTable Query(string query);
        void CommitChanges(string query, DataTable dataTable);
        void Execute(string p);
        void CreateAllTablesFromBindings();
        string EscapeString(string str);
        string GetTableCreateString(Binding.Binding binding);
        object QuerySingleValue(string query);

        Task<DataTable> QueryAsync(string query) => Task.Run(() => Query(query));
        Task<object> QuerySingleValueAsync(string query) => Task.Run(() => QuerySingleValue(query));
        Task ExecuteAsync(string p) => Task.Run(() => Execute(p));
    }
}
