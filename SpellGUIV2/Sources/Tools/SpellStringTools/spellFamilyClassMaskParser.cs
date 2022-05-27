using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Windows.Controls;
using SpellEditor.Sources.Controls;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.VersionControl;

namespace SpellEditor.Sources.Tools.SpellFamilyClassMaskStoreParser
{
    public class SpellFamilyClassMaskParser
    {
        public SpellFamilyClassMaskParser(MainWindow window)
        {
            SpellFamilyClassMaskStore = new ArrayList[100, 3, 32]; // 18 -> 100 : I'm testing if we can create new spellfamilies just

            var isWotlkOrGreater = WoWVersionManager.IsWotlkOrGreaterSelected;
            var query = isWotlkOrGreater ?
                "SELECT id,SpellFamilyName,SpellFamilyFlags,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell" :
                "SELECT id,SpellFamilyName,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell";
            DataTable dt = window?.GetDBAdapter()?.Query(query);
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
                            uint Mask = (uint)Math.Pow(2, i);

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

        //classMaskStore[spellFamily,MaskIndex,MaskSlot] = spellList
        public ArrayList[,,] SpellFamilyClassMaskStore;

        public ArrayList GetSpellList(uint familyName, uint MaskIndex, uint MaskSlot)
        {
            return (ArrayList)SpellFamilyClassMaskStore.GetValue(familyName, MaskIndex, MaskSlot);
        }

        public void UpdateSpellFamilyClassMask(MainWindow window, uint familyName, bool isWotlkOrGreater, IDatabaseAdapter adapter, List<uint> masks)
        {
            UpdateSpellFamilyClassMask(window, window.SpellMask11, familyName, 0);
            UpdateSpellFamilyClassMask(window, window.SpellMask21, familyName, 1);
            UpdateSpellFamilyClassMask(window, window.SpellMask31, familyName, 2);
            UpdateSpellFamilyClassMask(window, window.SpellMask12, familyName, 0);
            UpdateSpellFamilyClassMask(window, window.SpellMask22, familyName, 1);
            UpdateSpellFamilyClassMask(window, window.SpellMask32, familyName, 2);
            if (isWotlkOrGreater)
            {
                UpdateSpellFamilyClassMask(window, window.SpellMask13, familyName, 0);
                UpdateSpellFamilyClassMask(window, window.SpellMask23, familyName, 1);
                UpdateSpellFamilyClassMask(window, window.SpellMask33, familyName, 2);
            }

            if (masks != null)
            {
                UpdateSpellEffectMasksSelected(window, familyName, adapter, masks);
            }
        }

        public void UpdateSpellEffectMasksSelected(MainWindow window, uint familyName, IDatabaseAdapter adapter, List<uint> masks)
        {
            UpdateSpellEffectTargetList(window.EffectTargetSpellsList1, 0, familyName, adapter, masks);
            UpdateSpellEffectTargetList(window.EffectTargetSpellsList2, 1, familyName, adapter, masks);
            UpdateSpellEffectTargetList(window.EffectTargetSpellsList3, 2, familyName, adapter, masks);
        }

        private void UpdateSpellFamilyClassMask(MainWindow window, ThreadSafeComboBox spellMaskComboBox, uint familyName, uint maskSlot)
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

        private void UpdateSpellEffectTargetList(ListBox list, int effectIndex, uint familyName, IDatabaseAdapter adapter, List<uint> masks)
        {
            list.Items.Clear();

            uint mask1 = masks[0 + (effectIndex * 3)];
            uint mask2 = masks[1 + (effectIndex * 3)];
            uint mask3 = masks[2 + (effectIndex * 3)];

            string query = string.Format(@"SELECT id, SpellName0 FROM spell WHERE 
                SpellFamilyName = {0} AND 
                (
                    (SpellFamilyFlags & {1}) > 0 OR
                    (SpellFamilyFlags1 & {2}) > 0 OR
                    (SpellFamilyFlags2 & {3}) > 0
                );",
                familyName,
                mask1,
                mask2,
                mask3);

            foreach (DataRow row in adapter.Query(query).Rows)
            {
                list.Items.Add($"{row[0]} - {row[1]}");
            }
        }
    }

}
