using NLog;
using SpellEditor.Sources.Binding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpellEditor.Sources.DBC
{
    class MutableGenericDbc : AbstractDBC
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public List<Dictionary<string, object>> MemoryData = new List<Dictionary<string, object>>();

        public MutableGenericDbc(string path)
        {
            ReadDBCFile(path);
            Body.RecordMaps.ToList().ForEach(MemoryData.Add);
            // Try to let GC collect this to save memory
            Body.RecordMaps = null;
        }

        public void AddRecord(Dictionary<string, object> entryData) => MemoryData.Add(entryData);

        public bool RemoveRecord(Dictionary<string, object> entryData) => MemoryData.Remove(entryData);

        public void SaveToFile(string bindingName)
        {
            var binding = BindingManager.GetInstance().FindBinding(bindingName);
            // Convert all string offsets to real strings
            binding.Fields.Where((field) => field.Type == BindingType.STRING_OFFSET).ToList().ForEach((field) =>
            {
                MemoryData.ForEach((record) =>
                {
                    var value = record[field.Name].ToString();
                    if (uint.TryParse(value, out var offset))
                    {
                        record[field.Name] = Reader.LookupStringOffset(offset);
                    }
                });
            });
            Header.RecordCount = (uint)MemoryData.Count;
            Logger.Debug("Saving to DBC file: " + bindingName);
            var newBody = new DBCBodyToSerialize
            {
                Records = MemoryData
            };
            Header.StringBlockSize = newBody.GenerateStringOffsetsMap(binding);
            SaveDbcFile(UpdateProgress, newBody, binding);
        }

        public static void UpdateProgress(double value)
        {
            Logger.Debug($"{ Convert.ToInt32(value * 100D) }%");
        }
    }
}
