using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using SpellEditor.Sources.Config;
using System.Data;
using System.Windows;

namespace SpellEditor.Sources.Tools.SpellClassMaskParser
{
	public class SpellClassMaskParser
	{
		public SpellClassMaskParser(MainWindow window)
		{
			classMaskStore = new ArrayList[18, 3, 32];

			DataTable dt = window.GetDBAdapter().query(string.Format("SELECT id,SpellFamilyName,SpellFamilyFlags,SpellFamilyFlags1,SpellFamilyFlags2 FROM {0}",window.GetConfig().Table));

			foreach (DataRow dr in dt.Rows)
			{
				uint id = uint.Parse(dr[0].ToString());
				uint SpellFamilyName = uint.Parse(dr[1].ToString());
				uint[] SpellFamilyFlag = { uint.Parse(dr[2].ToString()), uint.Parse(dr[3].ToString()), uint.Parse(dr[4].ToString()) };

				if (SpellFamilyName == 0)
					continue;

				for (uint effectIndex = 0; effectIndex<3; effectIndex++)
				{
					if (SpellFamilyFlag[effectIndex] != 0)
					{
						for (uint i = 0;i<32;i++)
						{
							uint Mask = (uint)Math.Pow(2, i);

							if ((SpellFamilyFlag[effectIndex] & Mask) != 0)
							{
								classMaskStore[SpellFamilyName, effectIndex, Mask].Add(id);
							}
						}
					}
				}
			}
		}

		//classMaskStore[spellFamily,Effect,Mask] = spellList
		public ArrayList[,,] classMaskStore;

		public ArrayList GetSpellList(uint familyName, uint effectIndex, uint Mask)
		{
			return (ArrayList)classMaskStore.GetValue(familyName, effectIndex, Mask);
		}
	}

}
