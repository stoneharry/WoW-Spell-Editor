using System.Data;

namespace SpellEditor.Sources.Database
{
    public interface IDatabaseAdapter
    {
        bool Updating { get; set; }

        DataTable Query(string query);
        void CommitChanges(string query, DataTable dataTable);
        void Execute(string p);
        string EscapeString(string str);
        string GetTableCreateString(Binding.Binding binding);
    }
}
