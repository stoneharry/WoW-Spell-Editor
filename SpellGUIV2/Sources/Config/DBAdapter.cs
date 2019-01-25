using System.Data;

namespace SpellEditor.Sources.Config
{
    public interface IDatabaseAdapter
    {
        string Table { get; set; }
        bool Updating { get; set; }

        DataTable Query(string query);
        void CommitChanges(string query, DataTable dataTable);
        void Execute(string p);
        string EscapeString(string str);
        string GetTableCreateString(Binding.Binding binding);
    }
}
