using SpellEditor.Sources.BLP;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellIconDBC : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        private double? iconSize = null;
        private Thickness? iconMargin = null;

        public List<Icon_DBC_Lookup> Lookups = new List<Icon_DBC_Lookup>();

        public SpellIconDBC(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/SpellIcon.dbc");
                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    uint offset = (uint)record["Name"];
                    if (offset == 0)
                        continue;
                    string name = Reader.LookupStringOffset(offset);
                    uint id = (uint)record["ID"];

                    Icon_DBC_Lookup lookup;
                    lookup.ID = id;
                    lookup.Offset = offset;
                    lookup.Name = name;
                    Lookups.Add(lookup);
                }
                Reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                Reader = null;
                Body = null;
            }
            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);
                return;
            }
        }

        public void LoadImages(double margin)
        {
            UpdateMainWindowIcons(margin);
        }

        public void updateIconSize(double newSize, Thickness margin)
        {
            iconSize = newSize;
            iconMargin = margin;
        }

        private class Worker : BackgroundWorker
        {
            public IDatabaseAdapter _adapter;

            public Worker(IDatabaseAdapter _adapter)
            {
                this._adapter = _adapter;
            }
        }

        public void UpdateMainWindowIcons(double margin)
        {
            // adapter.query below caused unhandled exception with main.selectedID as 0.
            if (adapter == null || main.selectedID == 0)
                return;
            
            // Convert to background worker here

            DataRow res = adapter.Query(string.Format("SELECT `SpellIconID`,`ActiveIconID` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0];
            uint iconInt = uint.Parse(res[0].ToString());
            uint iconActiveInt = uint.Parse(res[1].ToString());
            // Update currently selected icon, we don't currently use ActiveIconID
            System.Windows.Controls.Image temp = new System.Windows.Controls.Image();
            temp.Width = iconSize == null ? 32 : iconSize.Value;
            temp.Height = iconSize == null ? 32 : iconSize.Value;
            temp.Margin = iconMargin == null ? new Thickness(margin, 0, 0, 0) : iconMargin.Value;
            temp.VerticalAlignment = VerticalAlignment.Top;
            temp.HorizontalAlignment = HorizontalAlignment.Left;
            temp.Source = BlpManager.GetInstance().GetImageSourceFromBlpPath(GetIconPath(iconInt) + ".blp");
            temp.Name = "CurrentSpellIcon";
            // Code smells here on hacky positioning and updating the icon
            temp.Margin = new Thickness(103, 38, 0, 0);
            main.CurrentIconGrid.Children.Clear();
            main.CurrentIconGrid.Children.Add(temp);
            
            // Load all icons available if have not already
            if (main.IconGrid.Children.Count == 0)
            {
                var watch = new Stopwatch();
                watch.Start();
                LoadAllIcons(margin);
                watch.Stop();
                Console.WriteLine($"Loaded all icons as UI elements in {watch.ElapsedMilliseconds}ms");
            }
        }

        public void LoadAllIcons(double margin)
        {
            List<Icon_DBC_Lookup> lookups = Lookups.ToList();
            var pathsToAdd = new List<Icon_DBC_Lookup>();
            var imagesPool = new List<Image>();
            foreach (var entry in lookups)
            {
                var path = entry.Name + ".blp";
                if (File.Exists(path))
                {
                    pathsToAdd.Add(entry);
                    imagesPool.Add(new Image());
                }
            }
            for (int i = 0; i < pathsToAdd.Count; ++i)
            {
                var entry = pathsToAdd[i];
                var image = imagesPool[i];
                image.Width = iconSize == null ? 32 : iconSize.Value;
                image.Height = iconSize == null ? 32 : iconSize.Value;
                image.Margin = iconMargin == null ? new Thickness(margin, 0, 0, 0) : iconMargin.Value;
                image.VerticalAlignment = VerticalAlignment.Top;
                image.HorizontalAlignment = HorizontalAlignment.Left;
                image.Name = "Index_" + entry.Offset;
                image.ToolTip = entry.Name + ".blp";
                image.IsVisibleChanged += IsImageVisibleChanged;
                image.MouseDown += ImageDown;
            }
            foreach (var image in imagesPool)
            {
                main.IconGrid.Children.Add(image);
            }
        }

        private async void IsImageVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var image = sender as Image;
            var source = image.Source;
            if ((bool)e.NewValue)
            {
                var path = image.ToolTip.ToString();
                await Task.Factory.StartNew(() =>
                {
                    source = BlpManager.GetInstance().GetImageSourceFromBlpPath(path);
                });
            }
            else
            {
                source = null;
            }
            image.Source = source;
        }

        public void ImageDown(object sender, EventArgs e)
        {
            var image = (Image)sender;
            var temp = new Image();

            temp.Width = iconSize == null ? 32 : iconSize.Value;
            temp.Height = iconSize == null ? 32 : iconSize.Value;
            temp.Margin = iconMargin == null ? new Thickness(16, 0, 0, 0) : iconMargin.Value;
            temp.VerticalAlignment = VerticalAlignment.Top;
            temp.HorizontalAlignment = HorizontalAlignment.Left;
            temp.Source = image.Source;
            temp.Name = "NewSpellIcon";

            // Code smells here on hacky positioning and updating the icon
            temp.Margin = new Thickness(285, 38, 0, 0);
            main.NewIconGrid.Children.Clear();
            main.NewIconGrid.Children.Add(temp);

            uint offset = uint.Parse(image.Name.Substring(6));
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

        public string GetIconPath(uint iconId)
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
    };
}
