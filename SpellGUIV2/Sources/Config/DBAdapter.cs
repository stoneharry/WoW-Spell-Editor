using System.Data;

namespace SpellEditor.Sources.Config
{
    public interface DBAdapter
    {
        string Table { get; set; }
        bool Updating { get; set; }

        DataTable query(string query);
        void commitChanges(string query, DataTable dataTable);
        void execute(string p);
        string getTableCreateString();
    }
}
