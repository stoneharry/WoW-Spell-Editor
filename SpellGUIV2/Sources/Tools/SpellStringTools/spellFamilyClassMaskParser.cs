using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
        // classMaskStore[MaskIndex,MaskSlot] = spellList, one of these per spell family.
        // Families are read from the database the first time something asks about them
        // rather than indexing the whole spell table up front.
        private readonly Dictionary<uint, ArrayList[,]> _FamilyStore = new Dictionary<uint, ArrayList[,]>();
        private readonly IDatabaseAdapter _Adapter;
        private bool _AllFamiliesCached;

        public SpellFamilyClassMaskParser(IDatabaseAdapter adapter)
        {
            _Adapter = adapter;
        }

        // The old startup behaviour, one pass over the whole spell table. Only used when
        // the cache everything on load option is turned on.
        public void CacheAllFamilies()
        {
            var isWotlkOrGreater = WoWVersionManager.IsWotlkOrGreaterSelected;
            var query = isWotlkOrGreater ?
                "SELECT id,SpellFamilyName,SpellFamilyFlags,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell" :
                "SELECT id,SpellFamilyName,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell";

            using (DataTable dt = _Adapter?.Query(query))
            {
                if (dt == null)
                    return;

                lock (_FamilyStore)
                {
                    _FamilyStore.Clear();
                    foreach (DataRow dr in dt.Rows)
                    {
                        uint spellFamilyName = uint.Parse(dr[1].ToString());
                        if (spellFamilyName == 0)
                            continue;

                        if (!_FamilyStore.TryGetValue(spellFamilyName, out var store))
                        {
                            store = new ArrayList[3, 32];
                            _FamilyStore[spellFamilyName] = store;
                        }
                        StoreSpellFlags(store, dr, 2, isWotlkOrGreater);
                    }
                    _AllFamiliesCached = true;
                }
            }
        }

        public void InvalidateFamily(uint familyName)
        {
            lock (_FamilyStore)
            {
                _FamilyStore.Remove(familyName);
                _AllFamiliesCached = false;
            }
        }

        public void InvalidateAll()
        {
            lock (_FamilyStore)
            {
                _FamilyStore.Clear();
                _AllFamiliesCached = false;
            }
        }

        public ArrayList GetSpellList(uint familyName, uint MaskIndex, uint MaskSlot)
        {
            if (MaskIndex > 2 || MaskSlot > 31)
                return null;
            var store = GetFamilyStore(familyName);
            return store?[MaskIndex, MaskSlot];
        }

        private ArrayList[,] GetFamilyStore(uint familyName)
        {
            if (familyName == 0)
                return null;

            lock (_FamilyStore)
            {
                if (_FamilyStore.TryGetValue(familyName, out var cached))
                    return cached;
                if (_AllFamiliesCached)
                    return null;
            }

            var isWotlkOrGreater = WoWVersionManager.IsWotlkOrGreaterSelected;
            var query = isWotlkOrGreater ?
                $"SELECT id,SpellFamilyFlags,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell WHERE SpellFamilyName = {familyName}" :
                $"SELECT id,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell WHERE SpellFamilyName = {familyName}";

            var store = new ArrayList[3, 32];
            using (DataTable dt = _Adapter?.Query(query))
            {
                if (dt == null)
                    return null;

                foreach (DataRow dr in dt.Rows)
                    StoreSpellFlags(store, dr, 1, isWotlkOrGreater);
            }

            lock (_FamilyStore)
            {
                _FamilyStore[familyName] = store;
            }
            return store;
        }

        // Row layout is id, then the family flag columns starting at flagsColumn
        private static void StoreSpellFlags(ArrayList[,] store, DataRow row, int flagsColumn, bool isWotlkOrGreater)
        {
            uint id = uint.Parse(row[0].ToString());
            var maskCount = isWotlkOrGreater ? 3 : 2;
            for (uint maskIndex = 0; maskIndex < maskCount; maskIndex++)
            {
                uint flags = uint.Parse(row[flagsColumn + (int)maskIndex].ToString());
                if (flags == 0)
                    continue;

                for (uint i = 0; i < 32; i++)
                {
                    if ((flags & (1u << (int)i)) == 0)
                        continue;

                    if (store[maskIndex, i] == null)
                        store[maskIndex, i] = new ArrayList();

                    store[maskIndex, i].Add(id);
                }
            }
        }

        public void UpdateAllEffectFamiliesLists(MainWindow window, uint familyName, IDatabaseAdapter adapter)
        {
            UpdateMainWindowEffectFamiliesList(window, familyName, adapter, 0);
            UpdateMainWindowEffectFamiliesList(window, familyName, adapter, 1);
            UpdateMainWindowEffectFamiliesList(window, familyName, adapter, 2);
        }

        // reimplementation of UpdateSpellEffectTargetList
        // update spells lists in popup window listbox
        public void UpdateEffectTargetSpellsList(SpellFamiliesWindow window, uint familyName, IDatabaseAdapter adapter, bool filter_duplicates)
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

            List<string> unique_spell_names = new List<string>(); // to check for duplicates
            var newItems = new List<string>();
            foreach (DataRow row in adapter.Query(query).Rows)
            {
                string spell_name = row[1].ToString();

                if (filter_duplicates && unique_spell_names.Contains(spell_name))
                    continue;

                newItems.Add($"{row[0]} - {row[1]}");

                if (filter_duplicates)
                    unique_spell_names.Add(spell_name);
            }
            // update spell list listbox
            window.EffectTargetSpellsList.ItemsSource = newItems;
        }

        // Spells that use this family in their effects (mostly talents, item sets)
        // WOTLK only, as earlier versions don't have the class mask fields.
        public void UpdateEffectModifiersSpellsList(SpellFamiliesWindow window, uint familyName, IDatabaseAdapter adapter, bool filter_duplicates)
        {
            Debug.Assert(WoWVersionManager.IsWotlkOrGreaterSelected);

            string query = string.Format(@"SELECT id, SpellName0 FROM spell WHERE 
                SpellFamilyName = {0}
                AND 
                (
                    (
                        (spell.Effect1 > 0 AND (spell.EffectSpellClassMaskA1 & {1}) > 0) OR
                        (spell.Effect2 > 0 AND (spell.EffectSpellClassMaskB1 & {1}) > 0) OR
                        (spell.Effect3 > 0 AND (spell.EffectSpellClassMaskC1 & {1}) > 0)
                    )
                    OR
                    (
                        (spell.Effect1 > 0 AND (spell.EffectSpellClassMaskA2 & {2}) > 0) OR
                        (spell.Effect2 > 0 AND (spell.EffectSpellClassMaskB2 & {2}) > 0) OR
                        (spell.Effect3 > 0 AND (spell.EffectSpellClassMaskC2 & {2}) > 0)
                    )
                    OR
                    (
                        (spell.Effect1 > 0 AND (spell.EffectSpellClassMaskA3 & {3}) > 0) OR
                        (spell.Effect2 > 0 AND (spell.EffectSpellClassMaskB3 & {3}) > 0) OR
                        (spell.Effect3 > 0 AND (spell.EffectSpellClassMaskC3 & {3}) > 0)
                    )
                );",
                familyName,
                window._active_families_values[0],
                window._active_families_values[1],
                window._active_families_values[2]);

            List<string> unique_spell_names = new List<string>(); // to check for duplicates
            var newItems = new List<string>();
            foreach (DataRow row in adapter.Query(query).Rows)
            {
                string spell_name = row[1].ToString();

                if (filter_duplicates && unique_spell_names.Contains(spell_name))
                    continue;

                newItems.Add($"{row[0]} - {row[1]}");

                if (filter_duplicates)
                    unique_spell_names.Add(spell_name);
            }
            // update spell list listbox
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
                        content = $"Fam{category}: 0x{mask:X8} (bit {i})";

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
                        content = $"Fam{category}: 0x{mask:X8} (bit {i})";

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
