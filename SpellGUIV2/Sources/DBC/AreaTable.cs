using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpellEditor.Sources.DBC
{
	class AreaTable : AbstractDBC
    {
        public Dictionary<uint, AreaTableLookup> Lookups;

        public AreaTable(MainWindow window)
        {
            try
            {
                ReadDBCFile<AreaTable_DBC_Record>("DBC/AreaTable.dbc");

                Lookups = new Dictionary<uint, AreaTableLookup>();

                for (uint i = 0; i < Header.RecordCount; ++i) 
                {
                    var record = Body.RecordMaps[i];

                    uint offset = ((uint[]) record["Name"])[window.GetLanguage()];
                    if (offset == 0)
                        continue;
				    AreaTableLookup temp;
                    temp.ID = (uint) record["ID"];
				    temp.AreaName = Reader.LookupStringOffset(offset);
				    Lookups.Add(temp.ID, temp);
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

		public struct AreaTableLookup
        {
            public uint ID;
            public string AreaName;
        };

        [Serializable]
        public struct AreaTable_DBC_Record
        {
// These fields are used through reflection, disable warning
#pragma warning disable 0649
#pragma warning disable 0169
            public uint ID;
			public uint Map;
			public uint zone;
			public uint exploreFlag;
			public uint Flags;
			public uint SoundPreferences;
			public uint SoundPreferencesUnderwater;
			public uint SoundAmbience;
			public uint ZoneMusic;
			public uint zoneIntroMusic;
			public uint area_level;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public uint[] Name;
			public uint NameFlag;
			public uint FactionGroup;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public uint[] LiquidType;
			public float MinElevation;
			public float AmbientMultiplier;
			public uint Light;
#pragma warning restore 0649
#pragma warning restore 0169
        };
    };
}
