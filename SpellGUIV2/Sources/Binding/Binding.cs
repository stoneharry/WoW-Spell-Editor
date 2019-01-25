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
    }
}
