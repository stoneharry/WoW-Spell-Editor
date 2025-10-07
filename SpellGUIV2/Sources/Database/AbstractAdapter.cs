using SpellEditor.Sources.Binding;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SpellEditor.Sources.Database
{
    public abstract class AbstractAdapter : IDatabaseAdapter, IDisposable
    {
        protected readonly object _syncLock = new object();
        public bool Updating { get; set; }

        public List<string> GetAllTableCreateStrings()
        {
            var list = new List<string>();
            foreach (var binding in BindingManager.GetInstance().GetAllBindings())
            {
                list.Add(string.Format(GetTableCreateString(binding), binding.Name.ToLower()));
            }
            return list;
        }

        public virtual string GetTableCreateString(Binding.Binding binding)
        {
            StringBuilder str = new StringBuilder();
            str.Append(@"CREATE TABLE IF NOT EXISTS `{0}` (");
            foreach (var field in binding.Fields)
            {
                switch (field.Type)
                {
                    case BindingType.UINT:
                        str.Append($@"`{field.Name}` int(10) unsigned NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.INT:
                        str.Append($@"`{field.Name}` int(11) NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.UINT8:
                        str.Append($@"`{field.Name}` tinyint unsigned NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.FLOAT:
                        str.Append($@"`{field.Name}` FLOAT NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.STRING_OFFSET:
                        str.Append($@"`{field.Name}` TEXT CHARACTER SET utf8, ");
                        break;
                    default:
                        throw new Exception($"ERROR: Unhandled type: {field.Type} on field: {field.Name} on binding: {binding.Name}");
                }
            }

            var idField = binding.Fields.FirstOrDefault(record => record.Name.ToLower().Equals("id"));
            if (idField != null && binding.OrderOutput)
                str.Append($"PRIMARY KEY (`{idField.Name}`)) ");
            else
            {
                str = str.Remove(str.Length - 2, 2);
                str = str.Append(") ");
            }
            str.Append("ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=COMPRESSED KEY_BLOCK_SIZE=8;");
            return str.ToString();
        }

        public virtual void CreateAllTablesFromBindings()
        {
            throw new NotImplementedException();
        }

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }


        public virtual void CommitChanges(string query, DataTable dataTable)
        {
            throw new NotImplementedException();
        }

        public virtual string EscapeString(string str)
        {
            throw new NotImplementedException();
        }

        public virtual void Execute(string p)
        {
            throw new NotImplementedException();
        }

        public virtual DataTable Query(string query)
        {
            throw new NotImplementedException();
        }

        public virtual object QuerySingleValue(string query)
        {
            throw new NotImplementedException();
        }
    }
}
