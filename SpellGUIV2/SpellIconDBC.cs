using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    class SpellIconDBC
    {
        public SpellDBC_Header header;
        public IconDBC_Map body;

        private MainWindow main;
        private SpellDBC spell;

        public SpellIconDBC(MainWindow window, SpellDBC theSpellDBC)
        {
            main = window;
            spell = theSpellDBC;

            if (!File.Exists("SpellIcon.dbc"))
                throw new Exception("SpellIcon.dbc was not found!");

            FileStream fs = new FileStream("SpellIcon.dbc", FileMode.Open);
            // Read header
            int count = Marshal.SizeOf(typeof(SpellDBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fs);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
            handle.Free();

            body.records = new IconDBC_Record[header.record_count];
            // Read body
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                count = Marshal.SizeOf(typeof(IconDBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (IconDBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(IconDBC_Record));
                handle.Free();
            }

            string StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.string_block_size));
            body.lookup = new Dictionary<UInt32, string>();

            string temp = "";
            UInt32 lastString = 0;
            for (UInt32 i = 0; i < header.string_block_size; ++i)
            {
                char t = StringBlock[(int)i];
                if (t == '\0')
                {
                    body.lookup.Add(lastString, temp);
                    lastString += (uint)temp.Length + 1;
                    temp = "";
                }
                else
                {
                    temp += t;
                }
            }

            updateMainWindowIcons();
        }

        private void updateMainWindowIcons()
        {

        }

        public struct IconDBC_Map
        {
            public IconDBC_Record[] records;
            public Dictionary<UInt32, string> lookup;
        }

        public struct IconDBC_Record
        {
            public UInt32 ID;
            public UInt32 name;
        }

        
        /*
            SereniaBLPLib.BlpFile exampleBLP;

            FileStream file = new FileStream("C:\\Users\\Harry_\\Desktop\\Interface\\Icons\\Ability_Ambush.blp", FileMode.Open);
            exampleBLP = new SereniaBLPLib.BlpFile(file);

            Bitmap bit = exampleBLP.getBitmap(0);
            TestImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
               bit.GetHbitmap(),
               IntPtr.Zero,
               System.Windows.Int32Rect.Empty,
               BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
         */
    }
}
