using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class AreaGroup
    {
        // Begin Window
        private MainWindow main;
		private DBAdapter adapter;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public AreaGroup_DBC_Map body;
        // End DBCs

        public AreaGroup(MainWindow window, DBAdapter adapter)
        {
            this.main = window;
            this.adapter = adapter;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].AreaID = new UInt32[6];
                body.records[i].NextGroup = new UInt32();
            }

            if (!File.Exists("DBC/AreaGroup.dbc"))
            {
                main.HandleErrorMessage("AreaGroup.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/AreaGroup.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new AreaGroup_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(AreaGroup_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (AreaGroup_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(AreaGroup_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<AreaGroupLookup>();

            int boxIndex = 1;

            main.AreaGroup.Items.Add(0);

            AreaGroupLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                AreaGroupLookup temp;

				AreaGroup_DBC_Record agTemp = body.records[i];

				System.Collections.ArrayList al = new System.Collections.ArrayList();

				do
				{
					foreach (UInt32 val in agTemp.AreaID)
						if (val!=0) 
							al.Add(val);

					agTemp = FindAreaGroup(agTemp.NextGroup);

				} while (agTemp.NextGroup != 0);

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;
				Label areaGroupLab = new Label();

				areaGroupLab.Content = id.ToString() + "\t\t";

				String areaList_str = "";
				foreach (UInt32 val in al)
				{
					areaList_str += "AreaId:";
					areaList_str += val;
					areaList_str += "\t\t";
					areaList_str += window.GetAreaTableName(val);
					areaList_str += "\n";
				}

				areaGroupLab.ToolTip = areaList_str;

				main.AreaGroup.Items.Add(areaGroupLab);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

		public AreaGroup_DBC_Record FindAreaGroup(UInt32 fId)
		{
			foreach (AreaGroup_DBC_Record o in body.records) 
			{
				if (o.ID == fId)
					return o;
			}
			return new AreaGroup_DBC_Record();
		}

        public void UpdateAreaGroupSelection()
        {
            uint ID = UInt32.Parse(adapter.query(string.Format("SELECT `AreaGroupID` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());

            if (ID == 0)
            {
                main.AreaGroup.threadSafeIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.AreaGroup.threadSafeIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct AreaGroup_DBC_Map
    {
        public AreaGroup_DBC_Record[] records;
        public List<AreaGroupLookup> lookup;
    };

    public struct AreaGroupLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct AreaGroup_DBC_Record
    {
        public UInt32 ID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public UInt32[] AreaID;
        public UInt32 NextGroup;
    };
}
