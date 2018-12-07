using System;
using System.Runtime.InteropServices;
using SpellEditor.Sources.Config;

namespace SpellEditor.Sources.DBC
{
	class ItemSubClass : AbstractDBC
    {
        public ItemSubClassLookup[,] Lookups = new ItemSubClassLookup[29, 32];

        public ItemSubClass(MainWindow window, DBAdapter adapter)
        {
            try
            {
                ReadDBCFile<ItemSubClass_DBC_Record>("DBC/ItemSubClass.dbc");

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];

                    uint offset = ((uint[])record["displayName"])[window.GetLanguage()];
                    if (offset == 0)
                        continue;
                    ItemSubClassLookup temp;
                    temp.ID = (uint)record["subClass"];
                    temp.Name = Reader.LookupStringOffset(offset);
                    Lookups[(uint) record["Class"], temp.ID] = temp;
                }
                Reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                Reader = null;
                Body = null;
            }
            catch (Exception ex)
            {
                window.HandleErrorMessage(ex.Message);
                return;
            }
        }

		public struct ItemSubClassLookup
        {
            public uint ID;
            public string Name;
        };

        [Serializable]
        public struct ItemSubClass_DBC_Record
        {
// These fields are used through reflection, disable warning
#pragma warning disable 0649
#pragma warning disable 0169
            public uint Class;
			public uint subClass;
			public uint prerequisiteProficiency;
			public uint postrequisiteProficiency;
			public uint flags;
			public uint displayFlags;
			public uint weaponParrySeq;
			public uint weaponReadySeq;
			public uint weaponAttackSeq;
			public uint WeaponSwingSize;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public uint[] displayName;
			public uint displayNameFlag;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public uint[] verboseName;
			public uint verboseNameFlag;
#pragma warning restore 0649
#pragma warning restore 0169
        };
	}
}
