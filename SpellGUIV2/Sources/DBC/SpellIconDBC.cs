using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SpellEditor.Sources.DBC
{
    class SpellIconDBC
    {
        // Begin Window
        private MainWindow main;
        private DBAdapter adapter;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public Icon_DBC_Map body;
        // End DBCs

        // Begin Other
        private static bool loadedAllIcons = false;
        // End Other

        public SpellIconDBC(MainWindow window, DBAdapter adapter)
        {
            this.main = window;
            this.adapter = adapter;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32();
            }
        }

        public Task LoadImages(double margin)
        {
            return (new TaskFactory()).StartNew(() =>
            {
                if (!File.Exists("DBC/SpellIcon.dbc"))
                {
                    main.HandleErrorMessage("SpellIcon.dbc was not found!");

                    return;
                }

                FileStream fileStream;
                try
                {
                    fileStream = new FileStream("DBC/SpellIcon.dbc", FileMode.Open);
                }
                catch (IOException)
                {
                    return;
                }
                int count = Marshal.SizeOf(typeof(DBC_Header));
                byte[] readBuffer = new byte[count];
                BinaryReader reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
                handle.Free();

                body.records = new Icon_DBC_Record[header.RecordCount];

                for (UInt32 i = 0; i < header.RecordCount; ++i)
                {
                    count = Marshal.SizeOf(typeof(Icon_DBC_Record));
                    readBuffer = new byte[count];
                    reader = new BinaryReader(fileStream);
                    readBuffer = reader.ReadBytes(count);
                    handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                    body.records[i] = (Icon_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Icon_DBC_Record));
                    handle.Free();
                }

                body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

                reader.Close();
                fileStream.Close();

                UpdateMainWindowIcons(margin);
            });
        }

        public async void UpdateMainWindowIcons(double margin)
        {
			if (adapter == null || main.selectedID == 0) { // adapter.query below caused unhandled exception with main.selectedID as 0.
                return;
            }

            DataRow res;
            try
            {
				res = adapter.query(string.Format("SELECT `SpellIconID`,`ActiveIconID` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0];
            }
            catch (Exception)
            {
                return;
            }
            UInt32 iconInt = UInt32.Parse(res[0].ToString());
            UInt32 iconActiveInt = UInt32.Parse(res[1].ToString());
            UInt32 selectedRecord = UInt32.MaxValue;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                if (body.records[i].ID == iconInt)
                {
                    selectedRecord = i;

                    break;
                }

                if (body.records[i].ID == iconActiveInt)
                {
                    selectedRecord = i;

                    break;
                }
            }

            string icon = "";

            int offset = 0;

            try
            {
                if (selectedRecord == UInt32.MaxValue) { throw new Exception("The icon for this spell does not exist in the SpellIcon.dbc"); }

                offset = (int)body.records[selectedRecord].Name;

                while (body.StringBlock[offset] != '\0') { icon += body.StringBlock[offset++]; }

                if (!File.Exists(icon + ".blp")) { throw new Exception("File could not be found: " + "Icons\\" + icon + ".blp"); }
            }

            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);

                return;
            }

            FileStream fileStream = new FileStream(icon + ".blp", FileMode.Open);

            SereniaBLPLib.BlpFile image;

            image = new SereniaBLPLib.BlpFile(fileStream);

            Bitmap bit = image.getBitmap(0);

            await Task.Factory.StartNew(() =>
            {
                main.CurrentIcon.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bit.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
            }, CancellationToken.None, TaskCreationOptions.None, main.UIScheduler);

            image.close();
            fileStream.Close();

            if (!loadedAllIcons)
            {
                loadedAllIcons = true;

                int currentOffset = 1;

                string[] icons = body.StringBlock.Split('\0');

                int iconIndex = 0;
                int columnsUsed = icons.Length / 11;
                int rowsToDo = columnsUsed / 2;

                for (int j = -rowsToDo; j <= rowsToDo; ++j)
                {
                    for (int i = -5; i < 6; ++i)
                    {
                        ++iconIndex;
                        if (iconIndex >= icons.Length - 1) { break; }
                        int this_icons_offset = currentOffset;

                        currentOffset += icons[iconIndex].Length + 1;

                        if (!File.Exists(icons[iconIndex] + ".blp"))
                        {
                            Console.WriteLine("Warning: Icon not found: " + icons[iconIndex] + ".blp");

                            continue;
                        }

                        fileStream = new FileStream(icons[iconIndex] + ".blp", FileMode.Open);
                        image = new SereniaBLPLib.BlpFile(fileStream);
                        bit = image.getBitmap(0);

                        await Task.Factory.StartNew(() =>
                        {
                            System.Windows.Controls.Image temp = new System.Windows.Controls.Image();

                            temp.Width = 32;
                            temp.Height = 32;
                            temp.Margin = new System.Windows.Thickness(margin, 0, 0, 0);
                            temp.VerticalAlignment = VerticalAlignment.Top;
                            temp.HorizontalAlignment = HorizontalAlignment.Left;
                            temp.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bit.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                            temp.Name = "Index_" + this_icons_offset;
                            temp.MouseDown += this.ImageDown;

                            main.IconGrid.Children.Add(temp);
                        }, CancellationToken.None, TaskCreationOptions.None, main.UIScheduler);

                        image.close();
                        fileStream.Close();
                    }
                }
            }
        }

        public void ImageDown(object sender, EventArgs e)
        {
            main.NewIcon.Source = ((System.Windows.Controls.Image)sender).Source;

            System.Windows.Controls.Image temp = (System.Windows.Controls.Image)sender;

            UInt32 offset = UInt32.Parse(temp.Name.Substring(6));
            UInt32 ID = 0;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                if (body.records[i].Name == offset)
                {
                    ID = body.records[i].ID;

                    break;
                }
            }

            main.newIconID = ID;
        }

        public string getIconPath(int iconId)
        {
            string icon = "";
            int offset = 0;   
            UInt32 selectedRecord = UInt32.MaxValue;
            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                if (body.records[i].ID == iconId)
                {
                    selectedRecord = i;
                    break;
                }
            }      
            try
            {
                if (selectedRecord == UInt32.MaxValue) {
                    // Raising the exception is causing lag when a lot of spells do not exist, so just load nothing
                    return "";
                    //throw new Exception("The icon trying to be loaded does not exist in the SpellIcon.dbc");
                }
                offset = (int)body.records[selectedRecord].Name;
                while (body.StringBlock[offset] != '\0')
                {
                    icon += body.StringBlock[offset++];
                }
                if (!File.Exists(icon + ".blp"))
                {
                    throw new Exception("File could not be found: " + "Icons\\" + icon + ".blp");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                icon = "";
            }
            return icon;
        }

        public struct Icon_DBC_Map
        {
            public Icon_DBC_Record[] records;
            public string StringBlock;
        };

        public struct Icon_DBC_Record
        {
            public UInt32 ID;
            public UInt32 Name;
        };
    };
}
