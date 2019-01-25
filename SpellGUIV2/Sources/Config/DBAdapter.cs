using System.Data;

namespace SpellEditor.Sources.Config
{
    public interface DBAdapter
    {
        string Table { get; set; }
        bool Updating { get; set; }

        DataTable query(string query);
        void CommitChanges(string query, DataTable dataTable);
        void Execute(string p);
        string EscapeString(string str);
        string GetTableCreateString();
    }
}
