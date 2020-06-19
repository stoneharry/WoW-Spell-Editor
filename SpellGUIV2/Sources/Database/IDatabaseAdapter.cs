using System.Data;

namespace SpellEditor.Sources.Database
{
    public interface IDatabaseAdapter
    {
        bool Updating { get; set; }

        DataTable Query(string query);
        void CommitChanges(string query, DataTable dataTable);
        void Execute(string p);
        void CreateAllTablesFromBindings();
        string EscapeString(string str);
        string GetTableCreateString(Binding.Binding binding);
        object QuerySingleValue(string query);
    }
}
