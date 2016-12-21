using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using SpellEditor.Sources.Controls;
using System.Windows.Threading;


namespace SpellEditor.Sources.DBC
{
	class AreaTable
	{
		  // Begin Window
        private MainWindow main;
		private SQLite.SQLite Sqlite;
        // End Window

        // Begin DBCs
        public DBC_Header header;
		public AreaTable_DBC_Map body;
        // End DBCs

        public AreaTable(MainWindow window, SQLite.SQLite SqliteConn)
        {
            main = window;
			Sqlite = SqliteConn;

			if (!File.Exists("DBC/AreaTable.dbc"))
            {
				main.HandleErrorMessage("AreaTable.dbc was not found!");

                return;
            }

			FileStream fileStream = new FileStream("DBC/AreaTable.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new AreaTable_DBC_Record[header.RecordCount];

			for (UInt32 i = 0; i < header.RecordCount; ++i)
			{
				body.records[i].ID = new UInt32();
				body.records[i].Map = new UInt32();
				body.records[i].zone = new UInt32();
				body.records[i].exploreFlag = new UInt32();
				body.records[i].Flags = new UInt32();
				body.records[i].SoundPreferences = new UInt32();
				body.records[i].SoundPreferencesUnderwater = new UInt32();
				body.records[i].SoundAmbience = new UInt32();
				body.records[i].ZoneMusic = new UInt32();
				body.records[i].zoneIntroMusic = new UInt32();
				body.records[i].area_level = new UInt32();
				body.records[i].LiquidType = new UInt32[4];
				body.records[i].MinElevation = new float();
				body.records[i].AmbientMultiplier = new float();
				body.records[i].Light = new UInt32();
				body.records[i].Name = new UInt32[16];
				body.records[i].NameFlag = new UInt32();
				body.records[i].FactionGroup = new UInt32();
			}

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
				count = Marshal.SizeOf(typeof(AreaTable_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
				body.records[i] = (AreaTable_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(AreaTable_DBC_Record));
                handle.Free();
            }

            body.StringBlock = reader.ReadBytes(header.StringBlockSize);

            reader.Close();
            fileStream.Close();

			body.lookup = new Dictionary<uint, AreaTableLookup>();

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[window.GetLanguage()];

                if (offset == 0) { continue; }

                int returnValue = offset;

				System.Collections.ArrayList al = new System.Collections.ArrayList(); 

                while (body.StringBlock[offset] != 0) { al.Add(body.StringBlock[offset++]); }

				byte[] toAdd = new byte[al.Count];
				int n = 0;
				foreach (byte o in al) { toAdd[n++] = o; }

				AreaTableLookup temp;

                temp.ID = (int)body.records[i].ID;
				temp.AreaName = Encoding.UTF8.GetString(toAdd); ;

				body.lookup.Add(body.records[i].ID, temp);
            }
        }

		public struct AreaTable_DBC_Map
        {
			public AreaTable_DBC_Record[] records;
			public Dictionary<UInt32, AreaTableLookup> lookup;
            public byte[] StringBlock;
        };

		public struct AreaTableLookup
        {
            public int ID;
            public string AreaName;
        };

        public struct AreaTable_DBC_Record
        {
            public UInt32 ID;
			public UInt32 Map;
			public UInt32 zone;
			public UInt32 exploreFlag;
			public UInt32 Flags;
			public UInt32 SoundPreferences;
			public UInt32 SoundPreferencesUnderwater;
			public UInt32 SoundAmbience;
			public UInt32 ZoneMusic;
			public UInt32 zoneIntroMusic;
			public UInt32 area_level;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public UInt32[] Name;
			public UInt32 NameFlag;
			public UInt32 FactionGroup;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public UInt32[] LiquidType;
			public float MinElevation;
			public float AmbientMultiplier;
			public UInt32 Light;
        };
    };
}
