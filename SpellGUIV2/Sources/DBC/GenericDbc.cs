namespace SpellEditor.Sources.DBC
{
    public class GenericDbc : AbstractDBC
    {
        public GenericDbc(string path)
        {
            ReadDBCFile(path);
        }
    };
}