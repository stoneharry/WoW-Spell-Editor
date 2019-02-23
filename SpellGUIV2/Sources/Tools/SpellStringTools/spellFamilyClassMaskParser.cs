using System;
using System.Collections;
using System.Data;
using SpellEditor.Sources.Controls;

namespace SpellEditor.Sources.Tools.SpellFamilyClassMaskStoreParser
{
    public class SpellFamilyClassMaskParser
    {
        public SpellFamilyClassMaskParser(MainWindow window)
        {
            SpellFamilyClassMaskStore = new ArrayList[100, 3, 32]; // 18 -> 100 : I'm testing if we can create new spellfamilies just
                                                                    // by giving it some unique id for procces on our own spells

            DataTable dt = window.GetDBAdapter().Query("SELECT id,SpellFamilyName,SpellFamilyFlags,SpellFamilyFlags1,SpellFamilyFlags2 FROM spell");

            foreach (DataRow dr in dt.Rows)
            {
                uint id = uint.Parse(dr[0].ToString());
                uint SpellFamilyName = uint.Parse(dr[1].ToString());
                uint[] SpellFamilyFlag = { uint.Parse(dr[2].ToString()), uint.Parse(dr[3].ToString()), uint.Parse(dr[4].ToString()) };

                if (SpellFamilyName == 0)
                    continue;

                for (uint MaskIndex = 0; MaskIndex < 3; MaskIndex++)
                {
                    if (SpellFamilyFlag[MaskIndex] != 0)
                    {
                        for (uint i = 0;i<32;i++)
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

        public void UpdateSpellFamilyClassMask(MainWindow window, uint familyName)
        {
            _UpdateSpellFamilyClassMask(window, window.SpellMask11, familyName, 0);
            _UpdateSpellFamilyClassMask(window, window.SpellMask21, familyName, 1);
            _UpdateSpellFamilyClassMask(window, window.SpellMask31, familyName, 2);
            _UpdateSpellFamilyClassMask(window, window.SpellMask12, familyName, 0);
            _UpdateSpellFamilyClassMask(window, window.SpellMask22, familyName, 1);
            _UpdateSpellFamilyClassMask(window, window.SpellMask32, familyName, 2);
            _UpdateSpellFamilyClassMask(window, window.SpellMask13, familyName, 0);
            _UpdateSpellFamilyClassMask(window, window.SpellMask23, familyName, 1);
            _UpdateSpellFamilyClassMask(window, window.SpellMask33, familyName, 2);
        }

        private void _UpdateSpellFamilyClassMask(MainWindow window, ThreadSafeComboBox SpellMaskComboBox,uint familyName,uint MaskSlot)
        {
            for (uint i = 0; i < 32; i++)
            {
                ThreadSafeCheckBox cb = (ThreadSafeCheckBox)SpellMaskComboBox.Items.GetItemAt((int)i);
                ArrayList al = GetSpellList(familyName, MaskSlot, i);
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
