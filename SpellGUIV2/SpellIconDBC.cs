using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

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

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.string_block_size));

            reader.Close();
            fs.Close();

            updateMainWindowIcons();
        }

        private void updateMainWindowIcons()
        {
            UInt32 iconInt = spell.body.records[main.selectedID].record.SpellIconID;
            UInt32 selectedRecord = UInt32.MaxValue;
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                if (body.records[i].ID == iconInt)
                {
                    selectedRecord = i;
                    break;
                }
            }

            int offset = (int)body.records[selectedRecord].name;
            string icon = "";
            while (body.StringBlock[offset] != '\0')
            {
                icon += body.StringBlock[offset++];
            }

            if (selectedRecord == UInt32.MaxValue)
                throw new Exception("The icon for this spell does not exist in the SpellIcon.dbc");

            if (!File.Exists(icon + ".blp"))
                throw new Exception("File could not be found: " + "Icons\\" + icon + ".blp");

            FileStream file = new FileStream(icon + ".blp", FileMode.Open);

            SereniaBLPLib.BlpFile image;
            image = new SereniaBLPLib.BlpFile(file);

            Bitmap bit = image.getBitmap(0);

            main.CurrentIcon.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
               bit.GetHbitmap(),
               IntPtr.Zero,
               System.Windows.Int32Rect.Empty,
               BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));

            for (int i = -5; i < 6; ++i)
            {
                for (int j = -1; j < 3; ++j)
                {
                    System.Windows.Controls.Image temp = new System.Windows.Controls.Image();
                    temp.Width = 64;
                    temp.Height = 64;
                    temp.Margin = new System.Windows.Thickness(i + (138 * i), j + (138 * j), 0, 0);
                    temp.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                       bit.GetHbitmap(),
                       IntPtr.Zero,
                       System.Windows.Int32Rect.Empty,
                       BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                    main.IconGrid.Children.Add(temp);
                }
            }
        }

        public struct IconDBC_Map
        {
            public IconDBC_Record[] records;
            public string StringBlock;
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
