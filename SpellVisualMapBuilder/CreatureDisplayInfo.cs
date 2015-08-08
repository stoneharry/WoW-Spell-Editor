using SpellVisualMapBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class CreatureDisplayInfo
    {
        // Begin DBCs
        public DBC_Header header;
        public DBC_Map body;
        // End DBCs

        public CreatureDisplayInfo()
        {
            if (!File.Exists("Import\\CreatureDisplayInfo.dbc"))
                throw new Exception("Import\\CreatureDisplayInfo.dbc does not exist.");

            FileStream fileStream = new FileStream("Import\\CreatureDisplayInfo.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            count = Marshal.SizeOf(typeof(DBC_Record));
            if (header.RecordSize != count)
                throw new Exception("This DBC version is not supported! It is not 3.3.5a.");

            body.records = new DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();
        }

        public struct DBC_Map
        {
            public DBC_Record[] records;
            public String StringBlock;
        };

        public struct DBC_Record
        {
            public UInt32 ID;
            public UInt32 Model;
            public UInt32 Sound;
            public UInt32 ExtraDisplayInfo;
            public float Scale;
            public UInt32 Opacity;
            public UInt32 Skin1;
            public UInt32 Skin2;
            public UInt32 Skin3;
            public UInt32 portraitTextureName;
            public UInt32 bloodLevel;
            public UInt32 blood;
            public UInt32 NPCSounds;
            public UInt32 Particles;
            public UInt32 creatureGeosetData;
            public UInt32 objectEffectPackageID;
        };
    };
}
