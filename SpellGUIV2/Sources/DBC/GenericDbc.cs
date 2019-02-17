namespace SpellEditor.Sources.DBC
{
    class GenericDbc : AbstractDBC
    {
        public GenericDbc(string path)
        {
            ReadDBCFile(path);
        }
    };
}