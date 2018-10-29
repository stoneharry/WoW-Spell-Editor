using SereniaBLPLib;
using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SpellEditor.Sources.DBC
{
    class SpellIconDBC : AbstractDBC
    {
        private MainWindow main;
        private DBAdapter adapter;

        private static bool loadedAllIcons = false;
        private double? iconSize = null;
        private Thickness? iconMargin = null;

        public List<Icon_DBC_Lookup> Lookups = new List<Icon_DBC_Lookup>();

        public SpellIconDBC(MainWindow window, DBAdapter adapter)
        {
            main = window;
            this.adapter = adapter;
        }

        public Task LoadImages(double margin)
        {
            return (new TaskFactory()).StartNew(() =>
            {
                try
                {
                    ReadDBCFile<Icon_DBC_Record>("DBC/SpellIcon.dbc");

                    for (uint i = 0; i < Header.RecordCount; ++i)
                    {
                        var record = Body.RecordMaps[i];
                        uint offset = (uint)record["Name"];
                        if (offset == 0)
                            continue;
                        string name = reader.LookupStringOffset(offset);
                        uint id = (uint)record["ID"];

                        Icon_DBC_Lookup lookup;
                        lookup.ID = id;
                        lookup.Offset = offset;
                        lookup.Name = name;
                        Lookups.Add(lookup);
                    }
                    reader.CleanStringsMap();
                    // In this DBC we don't actually need to keep the DBC data now that
                    // we have extracted the lookup tables. Nulling it out may help with
                    // memory consumption.
                    reader = null;
                    Body = null;

                    UpdateMainWindowIcons(margin);
                }
                catch (Exception ex)
                {
                    main.HandleErrorMessage(ex.Message);
                    return;
                }
            });
        }

        public void updateIconSize(double newSize, Thickness margin)
        {
            iconSize = newSize;
            iconMargin = margin;
        }

        public async void UpdateMainWindowIcons(double margin)
        {
            // adapter.query below caused unhandled exception with main.selectedID as 0.
            if (adapter == null || main.selectedID == 0)
                return;

            DataRow res = adapter.query(string.Format("SELECT `SpellIconID`,`ActiveIconID` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0];
            uint iconInt = uint.Parse(res[0].ToString());
            uint iconActiveInt = uint.Parse(res[1].ToString());
            int selectedRecord = int.MaxValue;

            for (int i = 0; i < Header.RecordCount - 1; ++i)
            {
                uint id = Lookups[i].ID;
                if (id == iconInt || id == iconActiveInt)
                {
                    selectedRecord = i;
                    break;
                }
            }

            string icon = "";
            uint offset = 0;

            // Load selected record icon file
            try
            {
                if (selectedRecord == int.MaxValue)
                {
                    throw new Exception("The icon for this spell does not exist in the SpellIcon.dbc");
                }
                offset = Lookups[selectedRecord].Offset;
                icon = Lookups[selectedRecord].Name;
                if (!File.Exists(icon + ".blp"))
                {
                    throw new Exception("File could not be found: " + "Icons\\" + icon + ".blp");
                }
            }
            catch (Exception ex)
            {
                main.Dispatcher.Invoke(new Action(() => main.HandleErrorMessage(ex.Message)));
                return;
            }

            // Update currently selected icon
            using (FileStream fileStream = new FileStream(icon + ".blp", FileMode.Open))
            {
                using (BlpFile image = new BlpFile(fileStream))
                {
                    Bitmap bit = image.getBitmap(0);
                    await Task.Factory.StartNew(() =>
                    {
                        main.CurrentIcon.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bit.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                    }, CancellationToken.None, TaskCreationOptions.None, main.UIScheduler);
                }
            }

            // Load all icons available if have not already
            if (!loadedAllIcons)
            {
                loadedAllIcons = true;

                List<Icon_DBC_Lookup> lookups = Lookups.ToList();
                foreach (var entry in lookups)
                {
                    var path = entry.Name;
                    if (!File.Exists(path + ".blp"))
                    {
                        Console.WriteLine("Warning: Icon not found: " + path + ".blp");
                        continue;
                    }
                    bool loaded = false;
                    Bitmap bit = null;
                    try
                    {
                        using (FileStream fileStream = new FileStream(path + ".blp", FileMode.Open))
                        {
                            using (BlpFile image = new BlpFile(fileStream))
                            {
                                bit = image.getBitmap(0);
                                loaded = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error loading image, unsupported BLP format: {path}.blp\n{e.Message}\n{e}");
                    }
                    if (!loaded)
                        continue;

                    await Task.Factory.StartNew(() =>
                    {
                        System.Windows.Controls.Image temp = new System.Windows.Controls.Image();

                        temp.Width = iconSize == null ? 32 : iconSize.Value;
                        temp.Height = iconSize == null ? 32 : iconSize.Value;
                        temp.Margin = iconMargin == null ? new System.Windows.Thickness(margin, 0, 0, 0) : iconMargin.Value;
                        temp.VerticalAlignment = VerticalAlignment.Top;
                        temp.HorizontalAlignment = HorizontalAlignment.Left;
                        temp.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bit.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                        temp.Name = "Index_" + entry.Offset;
                        temp.ToolTip = path;
                        temp.MouseDown += ImageDown;

                        main.IconGrid.Children.Add(temp);
                    }, CancellationToken.None, TaskCreationOptions.None, main.UIScheduler);
                }
            }
        }

        public void ImageDown(object sender, EventArgs e)
        {
            main.NewIcon.Source = ((System.Windows.Controls.Image)sender).Source;
            System.Windows.Controls.Image temp = (System.Windows.Controls.Image)sender;

            uint offset = uint.Parse(temp.Name.Substring(6));
            uint ID = 0;

            for (int i = 0; i < Header.RecordCount; ++i)
            {
                if (Lookups[i].Offset == offset)
                {
                    ID = Lookups[i].ID;
                    break;
                }
            }
            main.newIconID = ID;
        }

        public string getIconPath(int iconId)
        {
            Icon_DBC_Lookup selectedRecord;
            selectedRecord.ID = int.MaxValue;
            selectedRecord.Name = "";
            for (int i = 0; i < Header.RecordCount; ++i)
            {
                if (Lookups[i].ID == iconId)
                {
                    selectedRecord = Lookups[i];
                    break;
                }
            }      
            try
            {
                if (selectedRecord.ID == int.MaxValue) {
                    // Raising the exception is causing lag when a lot of spells do not exist, so just load nothing
                    return "";
                    //throw new Exception("The icon trying to be loaded does not exist in the SpellIcon.dbc");
                }
                string icon = selectedRecord.Name;
                if (!File.Exists(icon + ".blp"))
                {
                    throw new Exception("File could not be found: " + "Icons\\" + icon + ".blp");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return selectedRecord.Name;
        }

        public struct Icon_DBC_Lookup
        {
            public uint ID;
            public uint Offset;
            public string Name;
        }

        public struct Icon_DBC_Record
        {
// These fields are used through reflection, disable warning
#pragma warning disable 0649
#pragma warning disable 0169
            public uint ID;
            public uint Name;
#pragma warning restore 0649
#pragma warning restore 0169
        };
    };
}
