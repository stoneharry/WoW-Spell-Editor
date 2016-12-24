using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using SpellEditor.Sources.Controls;
using SpellEditor.Sources.Config;

namespace SpellEditor.Sources.DBC
{
	class ItemSubClass
	{
		// Begin Window
        private MainWindow main;
        private DBAdapter adapter;
        // End Window

        // Begin DBCs
        public DBC_Header header;
		public ItemSubClass_DBC_Map body;
        // End DBCs

		public ItemSubClass(MainWindow window, DBAdapter adapter)
        {
            this.main = window;
            this.adapter = adapter;

			if (!File.Exists("DBC/ItemSubClass.dbc"))
            {
				main.HandleErrorMessage("ItemSubClass.dbc was not found!");

                return;
            }

			FileStream fileStream = new FileStream("DBC/ItemSubClass.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new ItemSubClass_DBC_Record[header.RecordCount];

			for (UInt32 i = 0; i < header.RecordCount; ++i)
			{
				body.records[i].Class = new UInt32();
				body.records[i].subClass = new UInt32();
				body.records[i].prerequisiteProficiency = new UInt32();
				body.records[i].postrequisiteProficiency = new UInt32();
				body.records[i].flags = new UInt32();
				body.records[i].displayFlags = new UInt32();
				body.records[i].weaponParrySeq = new UInt32();
				body.records[i].weaponReadySeq = new UInt32();
				body.records[i].weaponAttackSeq = new UInt32();
				body.records[i].WeaponSwingSize = new UInt32();
				body.records[i].displayName = new UInt32[16];
				body.records[i].displayNameFlag = new UInt32();
				body.records[i].verboseName = new UInt32[16];
				body.records[i].verboseNameFlag = new UInt32();
			}

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
				count = Marshal.SizeOf(typeof(ItemSubClass_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
				body.records[i] = (ItemSubClass_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(ItemSubClass_DBC_Record));
                handle.Free();
            }

            body.StringBlock = reader.ReadBytes(header.StringBlockSize);

            reader.Close();
            fileStream.Close();

			body.lookup = new ItemSubClassLookup[29, 32];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].displayName[window.GetLanguage()];

                if (offset == 0) { continue; }

				if ((int)body.records[i].verboseName[window.GetLanguage()] != 0)
					offset = (int)body.records[i].verboseName[window.GetLanguage()];

                int returnValue = offset;

				System.Collections.ArrayList al = new System.Collections.ArrayList(); 

                while (body.StringBlock[offset] != 0) { al.Add(body.StringBlock[offset++]); }

				byte[] toAdd = new byte[al.Count];
				int n = 0;
				foreach (byte o in al) { toAdd[n++] = o; }

				ItemSubClassLookup temp;

				temp.ID = body.records[i].subClass;
				temp.Name = Encoding.UTF8.GetString(toAdd);
				body.lookup[(int)body.records[i].Class, (int)body.records[i].subClass] = temp;
            }
        }

		public struct ItemSubClass_DBC_Map
        {
			public ItemSubClass_DBC_Record[] records;
			public ItemSubClassLookup[,] lookup;
            public byte[] StringBlock;
        };

		public struct ItemSubClassLookup
        {
            public UInt32 ID;
            public string Name;
        };

        public struct ItemSubClass_DBC_Record
        {
            public UInt32 Class;
			public UInt32 subClass;
			public UInt32 prerequisiteProficiency;
			public UInt32 postrequisiteProficiency;
			public UInt32 flags;
			public UInt32 displayFlags;
			public UInt32 weaponParrySeq;
			public UInt32 weaponReadySeq;
			public UInt32 weaponAttackSeq;
			public UInt32 WeaponSwingSize;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public UInt32[] displayName;
			public UInt32 displayNameFlag;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public UInt32[] verboseName;
			public UInt32 verboseNameFlag;
        };
    
	}
}
