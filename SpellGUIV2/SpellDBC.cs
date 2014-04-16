using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    public class SpellDBC
    {
        long FileSize;

        SpellDBC_Header header;
        SpellDBC_Body body;

        public bool loadDBCFile(string fileName)
        {
            try
            {
                FileStream fs = new FileStream(fileName, FileMode.Open);
                FileSize = fs.Length;

                int count = Marshal.SizeOf(typeof(SpellDBC_Header));
                byte[] readBuffer = new byte[count];
                BinaryReader reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
                handle.Free();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

            return true;
        }
    }
    struct SpellDBC_Header
    {
        UInt32 magic;
        UInt32 record_count;
        UInt32 field_count;
        UInt32 record_size;
        UInt32 string_block_size;
    };

    struct SpellDBC_Body
    {
        SpellDBC_Header header;
        SpellDBC_Record[] records;
        char[] string_block;
    };

    struct SpellDBC_Record
    {

    };
}
