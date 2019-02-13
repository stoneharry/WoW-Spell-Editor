using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.IO;

namespace SpellEditor.Sources.Binding
{
    public class Binding
    {
        public readonly BindingEntry[] Fields;
        public readonly string Name;

        public Binding(string fileName, List<BindingEntry> bindingEntryList)
        {
            Name = Path.GetFileNameWithoutExtension(fileName);
            Fields = bindingEntryList.ToArray();
        }

        /**
         * Calculates and returns the size of the structure represented by this binding.
         */
        public int CalcRecordSize()
        {
            int size = 0;
            foreach (var field in Fields)
            {
                switch (field.Type)
                {
                    case BindingType.INT:
                        {
                            size += sizeof(int);
                            break;
                        }
                    case BindingType.STRING_OFFSET:
                    case BindingType.UINT:
                        {
                            size += sizeof(uint);
                            break;
                        }
                    case BindingType.FLOAT:
                        {
                            size += sizeof(float);
                            break;
                        }
                    case BindingType.DOUBLE:
                        {
                            size += sizeof(double);
                            break;
                        }
                }
            }
            return size;
        }

        /**
         * Calculates and returns the number of fields the structure represented by this binding contains.
         */
        public uint CalcFieldCount()
        {
            if (Fields == null)
                return 0;
            return (uint) Fields.Length;
        }


        /**
         * Returns the number of rows in the database for the given binding/table name.
         * 
         * Returns -1 if an exception is raised, most likely because the table does not exist.
         * 
         * A better way to query for the table existing would be to query the performance_schema,
         * but this requires more permissions for a MySQL user.
         */
        public int GetNumRowsInTable(IDatabaseAdapter adapter)
        {
            try
            {
                var table = adapter.Query("SELECT COUNT(*) FROM " + Name);
                if (table.Rows.Count == 1)
                    return int.Parse(table.Rows[0][0].ToString());
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("WARNING: ImportExportWindow triggered: " + e.Message);
                return -1;
            }
        }
    }
}
