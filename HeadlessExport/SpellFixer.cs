using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace HeadlessExport
{
    public class SpellFixer
    {

        public void Run()
        {
            try
            {
                Console.WriteLine("Loading config...");
                Config.Init();
                Config.connectionType = Config.ConnectionType.MySQL;

                var adapter = new MySQL(false);
                using (var result = adapter.Query("SELECT id,SpellName0,SpellDescription0 FROM spell WHERE id >= 90000"))
                {
                    foreach (DataRow row in result.Rows)
                    {
                        FixSpell(row, ref adapter);
                    }
                }

                Console.WriteLine("All done.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Build failed: {e.GetType()}: {e.Message}\n{e}");
            }
        }

        private void FixSpell(DataRow row, ref MySQL adapter)
        {
            var desc = row["SpellDescription0"].ToString();

            if (GrammifyDescription(ref desc))
            {
                var id = (uint)row["id"];
                var name = row["SpellName0"].ToString();

                adapter.Execute($"UPDATE spell SET SpellDescription0 = \"{desc}\" WHERE id = {id}");

                Console.WriteLine($"Fixed spell {id} - {name}\n   {desc}\n");
            }
        }

        private bool GrammifyDescription(ref string desc)
        {
            if (desc.Length <= 1)
            {
                return false;
            }

            var updated = false;

            // Trim whitespace (only saves if further mods)
            desc = desc.TrimEnd(' ');

            // Handle new lines each
            if (desc.Contains("\n"))
            {
                var parts = desc.Split('\n');
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (part.StartsWith(" ") || part.Length <= 1)
                    {
                        continue;
                    }

                    
                    var newPart = part;
                    var trimmed = part.EndsWith("\r");

                    if (trimmed)
                    {
                        newPart = part.Substring(0, part.Length - 1);
                    }

                    updated = GrammifyDescription(ref newPart) || updated;

                    if (trimmed)
                    {
                        newPart += "\r";
                    }

                    desc = desc.Replace(part, newPart);
                }
            }
            // Handle single line
            else
            {
                if (!desc.EndsWith(".") &&
                    !desc.EndsWith("\r") &&
                    !desc.EndsWith("|R") &&
                    !desc.EndsWith("|r") &&
                    !desc.EndsWith("!") &&
                    !desc.EndsWith("?") &&
                    !desc.EndsWith("\n") &&
                    !desc.EndsWith(":"))
                {
                    if (desc.EndsWith(","))
                    {
                        desc.Trim(',');
                    }
                    desc += ".";
                    updated = true;
                }
            }

            return updated;
        }
    }
}
