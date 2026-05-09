using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace SpellEditor.Sources.Constants
{
    public static class SpellCategoyNames
    {

        public static Dictionary<uint, string> NamesMap = new Dictionary<uint, string>();

        public static void LoadCsvToMap()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(basePath, "SpellCategoryNames.csv");

            if (File.Exists(path))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    Delimiter = ","
                };

                using (var reader = new StreamReader(path))
                    using (var csv = new CsvReader(reader, config))
                    {
                        while (csv.Read())
                        {
                        var aa = csv.GetField<uint>(0);
                        var bb = csv.GetField<string>(1);

                        NamesMap[csv.GetField<uint>(0)] = csv.GetField<string>(1);
                        }
                    }
            }

        }
    }
}
