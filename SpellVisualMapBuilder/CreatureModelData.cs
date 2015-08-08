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
    class CreatureModelData
    {
        // Begin DBCs
        public DBC_Header header;
        public DBC_Map body;
        // End DBCs

        public CreatureModelData()
        {
            if (!File.Exists("Import\\CreatureModelData.dbc"))
                throw new Exception("Import\\CreatureModelData.dbc does not exist.");

            FileStream fileStream = new FileStream("Import\\CreatureModelData.dbc", FileMode.Open);
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

            body.pathStrings = new String[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].ModelPath;

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != '\0') { toAdd += body.StringBlock[offset++]; }

                body.pathStrings[i] = toAdd;
            }
        }

        public void SaveDBCFile()
        {
            UInt32 stringBlockOffset = 1;

            Dictionary<int, UInt32> offsetStorage = new Dictionary<int, UInt32>();
            Dictionary<UInt32, string> reverseStorage = new Dictionary<UInt32, string>();

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                if (body.pathStrings[i].Length == 0)
                    body.records[i].ModelPath = 0;
                else
                {
                    int key = body.pathStrings[i].GetHashCode();

                    if (offsetStorage.ContainsKey(key))
                        body.records[i].ModelPath = offsetStorage[key];
                    else
                    {
                        body.records[i].ModelPath = stringBlockOffset;
                        stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.pathStrings[i]) + 1;
                        offsetStorage.Add(key, body.records[i].ModelPath);
                        reverseStorage.Add(body.records[i].ModelPath, body.pathStrings[i]);
                    }
                }
            }

            header.StringBlockSize = (int)stringBlockOffset;

            String path = "Export/CreatureModelData.dbc";

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
                File.Delete(path);
            FileStream fileStream = new FileStream(path, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(fileStream);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] buffer = new byte[count];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Marshal.StructureToPtr(header, handle.AddrOfPinnedObject(), true);
            writer.Write(buffer, 0, count);
            handle.Free();

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(DBC_Record));
                buffer = new byte[count];
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(body.records[i], handle.AddrOfPinnedObject(), true);
                writer.Write(buffer, 0, count);
                handle.Free();
            }

            UInt32[] offsetsStored = offsetStorage.Values.ToArray<UInt32>();

            writer.Write(Encoding.UTF8.GetBytes("\0"));

            for (int i = 0; i < offsetsStored.Length; ++i)
                writer.Write(Encoding.UTF8.GetBytes(reverseStorage[offsetsStored[i]] + "\0"));

            writer.Close();
            fileStream.Close();
        }

        public struct DBC_Map
        {
            public DBC_Record[] records;
            public String[] pathStrings;
            public string StringBlock;
        };

        public struct DBC_Record
        {
            public UInt32 ID;
            public UInt32 Flags;
            public UInt32 ModelPath;
            // This is always 0. It would be used, if something was in here. Its pushed into:
            //   M2Scene::AddNewModel(GetM2Cache(), Modelpath, AlternateModel, 0)
            public UInt32 AlternateModel;
            public UInt32 sizeClass;
            public float modelScale;
            public UInt32 BloodLevel;
            public UInt32 Footprint;
            public float footprintTextureLength;
            public float footprintTextureWidth;
            public float footprintParticleScale;
            public UInt32 foleyMaterialId;
            public UInt32 footstepShakeSize;
            public UInt32 deathThudShakeSize;
            public UInt32 SoundData;
            public float CollisionWidth;
            public float CollisionHeight;
            public float mountHeight;
            public float geoBoxMinF1; // Made up of 3 floats, Vec3F
            public float geoBoxMinF2;
            public float geoBoxMinF3;
            public float geoBoxMaxF1; // Made up of 3 floats, Vec3F
            public float geoBoxMaxF2;
            public float geoBoxMaxF3;
            public float worldEffectScale;
            public float attachedEffectScale;
            public float Unknown5;
            public float Unknown6;
        };
    };
}
