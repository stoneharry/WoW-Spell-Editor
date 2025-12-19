using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Effects;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.Controls.SpellFamilyNames;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.VersionControl;

namespace SpellEditor.Sources.Tools.SpellFamilyClassMaskStoreParser
{
    public class SpellFamilyClassMaskParser
    {        
        //classMaskStore[spellFamily,MaskIndex,MaskSlot] = spellList
        public ArrayList[,,] SpellFamilyClassMaskStore;

        public SpellFamilyClassMaskParser(IDatabaseAdapter adapter)
        {
            SpellFamilyClassMaskStore = new ArrayList[100, 3, 32]; // 18 -> 100 : I'm testing if we can create new spellfamilies just

            var isWotlkOrGreater = WoWVersionManager.IsWotlkOrGreaterSelected;
            var query = isWotlkOrGreater ?
                "SELECT id,SpellFamilyName,SpellFamilyFlags,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell" :
                "SELECT id,SpellFamilyName,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell";

            using (DataTable dt = adapter?.Query(query))
            {
                if (dt == null)
                    return;

                foreach (DataRow dr in dt.Rows)
                {
                    uint id = uint.Parse(dr[0].ToString());
                    uint SpellFamilyName = uint.Parse(dr[1].ToString());
                    uint[] SpellFamilyFlag;
                    if (isWotlkOrGreater)
                    {
                        SpellFamilyFlag = new uint[] { uint.Parse(dr[2].ToString()), uint.Parse(dr[3].ToString()), uint.Parse(dr[4].ToString()) };
                    }
                    else
                    {
                        SpellFamilyFlag = new uint[] { uint.Parse(dr[2].ToString()), uint.Parse(dr[3].ToString()) };
                    }

                    if (SpellFamilyName == 0)
                        continue;

                    for (uint MaskIndex = 0; MaskIndex < (isWotlkOrGreater ? 3 : 2); MaskIndex++)
                    {
                        if (SpellFamilyFlag[MaskIndex] != 0)
                        {
                            for (uint i = 0; i < 32; i++)
                            {
                                uint Mask = 1u << (int)i;

                                if ((SpellFamilyFlag[MaskIndex] & Mask) != 0)
                                {
                                    if (SpellFamilyClassMaskStore[SpellFamilyName, MaskIndex, i] == null)
                                        SpellFamilyClassMaskStore[SpellFamilyName, MaskIndex, i] = new ArrayList();

                                    SpellFamilyClassMaskStore[SpellFamilyName, MaskIndex, i].Add(id);
                                }
                            }
                        }
                    }
                }
            }
        }


        public ArrayList GetSpellList(uint familyName, uint MaskIndex, uint MaskSlot)
        {
            return (ArrayList)SpellFamilyClassMaskStore.GetValue(familyName, MaskIndex, MaskSlot);
        }

        public void UpdateAllEffectFamiliesLists(MainWindow window, uint familyName, IDatabaseAdapter adapter)
        {
            UpdateMainWindowEffectFamiliesList(window, familyName, adapter, 0);
            UpdateMainWindowEffectFamiliesList(window, familyName, adapter, 1);
            UpdateMainWindowEffectFamiliesList(window, familyName, adapter, 2);
        }

        // reimplementation of UpdateSpellEffectTargetList
        // update spells lists in popup window listbox
        public void UpdateEffectTargetSpellsList(SpellFamiliesWindow window, uint familyName, IDatabaseAdapter adapter)
        {
            string query = string.Format(@"SELECT id, SpellName0 FROM spell WHERE 
                SpellFamilyName = {0} AND 
                (
                    (SpellFamilyFlags & {1}) > 0 OR
                    (SpellFamilyFlags1 & {2}) > 0 OR
                    (SpellFamilyFlags2 & {3}) > 0
                );",
                familyName,
                window._active_families_values[0],
                window._active_families_values[1],
                window._active_families_values[2]);

            // vanilla/tbc
            if (!WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                query = string.Format(@"SELECT id, SpellName0 FROM spell WHERE 
                SpellFamilyName = {0} AND 
                (
                    (SpellFamilyFlags1 & {1}) > 0 OR
                    (SpellFamilyFlags2 & {2}) > 0
                );",
                familyName,
                window._active_families_values[0],
                window._active_families_values[1]);
            }

            var newItems = new List<string>();
            foreach (DataRow row in adapter.Query(query).Rows)
            {
                newItems.Add($"{row[0]} - {row[1]}");
            }
            // update popup window update the window spell list listbox
            window.EffectTargetSpellsList.ItemsSource = newItems;
        }

        // update families listbox in mainwindow spell effects
        public void UpdateMainWindowEffectFamiliesList(MainWindow window, uint familyName, IDatabaseAdapter adapter, int effectIndex)
        {
            bool has_definition = SpellFamilyNames.familyFlagsNames.ContainsKey((int)familyName);
            Dictionary<int, string> definitions = new Dictionary<int, string>();

            if (has_definition)
                definitions = SpellFamilyNames.familyFlagsNames[(int)familyName];

            var newItems = new List<string>();
            uint[][] allfamilies = { window.familyFlagsA, window.familyFlagsB, window.familyFlagsC };
            for (int category = 0; category < 3; category++)
            {
                uint family = allfamilies[effectIndex][category];

                for (int i = 0; i < 32; i++)
                {
                    uint mask = 1u << i;

                    bool isSet = (family & mask) != 0;
                    if (!isSet)
                        continue;

                    int dict_index = (32 * category) + i + 1;
                    string content = "";
                    if (has_definition && definitions.ContainsKey(dict_index))
                    {
                        string data = definitions[dict_index];
                        if (!string.IsNullOrEmpty(data))
                        {
                            content += $"{dict_index} - ";
                            content += data;
                        }
                    }

                    bool bit_has_definition = !string.IsNullOrEmpty(content);
                    if (!bit_has_definition)
                        content = $"Family{category}: 0x{mask:X8} (bit {i})";

                    newItems.Add(content);
                }
            }

            if (effectIndex == 0)
                window.EffectSpellFamiliesList1.ItemsSource = newItems;
            else if (effectIndex == 1)
                window.EffectSpellFamiliesList2.ItemsSource = newItems;
            else if (effectIndex == 2)
                window.EffectSpellFamiliesList3.ItemsSource = newItems;

        }


        // update families listbox in mainwindow base
        // same thing as UpdateEffectTargetSpellsList() but for base instead of effect. Could merge both to one function.
        public void UpdateMainWindowBaseFamiliesList(MainWindow window, uint familyName, IDatabaseAdapter adapter)
        {
            bool has_definition = SpellFamilyNames.familyFlagsNames.ContainsKey((int)familyName);
            Dictionary<int, string> definitions = new Dictionary<int, string>();

            if (has_definition)
                definitions = SpellFamilyNames.familyFlagsNames[(int)familyName];

            // WOTLK has 3 fields in base families, tbc/vanilla only 2.
            int masks_count = WoWVersionManager.IsWotlkOrGreaterSelected ? 3 : 2;

            var newItems = new List<string>();
            for (int category = 0; category < masks_count; category++)
            {
                uint family = window.familyFlagsBase[category];

                for (int i = 0; i < 32; i++)
                {
                    uint mask = 1u << i;

                    bool isSet = (family & mask) != 0;
                    if (!isSet)
                        continue;

                    int dict_index = (32 * category) + i + 1;
                    string content = "";
                    if (has_definition && definitions.ContainsKey(dict_index))
                    {
                        string data = definitions[dict_index];
                        if (!string.IsNullOrEmpty(data))
                        {
                            content += $"{dict_index} - ";
                            content += data;
                        }
                    }

                    bool bit_has_definition = !string.IsNullOrEmpty(content);
                    if (!bit_has_definition)
                        content = $"Family{category}: 0x{mask:X8} (bit {i})";

                    newItems.Add(content);
                }
            }
            window.BaseSpellFamiliesList.ItemsSource = newItems;
        }

        // currently unused.
        // now done directly in window initialization CreateFamilyCheckboxes(), could move back to a dispatcher function again
        private void UpdateSpellFamilyClassMaskTooltips(MainWindow window, ThreadSafeComboBox spellMaskComboBox, uint familyName, uint maskSlot)
        {
            for (uint i = 0; i < 32; i++)
            {
                ThreadSafeCheckBox cb = (ThreadSafeCheckBox)spellMaskComboBox.Items.GetItemAt((int)i);
                ArrayList al = GetSpellList(familyName, maskSlot, i);
                string _tooltipStr = "";

                if (al != null && al.Count != 0)
                {
                    foreach (uint spellId in al)
                    {
                        _tooltipStr += spellId.ToString() + " - " + window.GetSpellNameById(spellId) + "\n";
                    }
                }
                cb.ToolTip = _tooltipStr;
            }
        }
    }

}
