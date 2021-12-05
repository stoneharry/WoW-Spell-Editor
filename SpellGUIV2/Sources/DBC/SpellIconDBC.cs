using NLog;
using SpellEditor.Sources.BLP;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpellEditor.Sources.DBC
{
    class SpellIconDBC : AbstractDBC
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private MainWindow main;
        private IDatabaseAdapter adapter;

        private double? iconSize = null;
        private Thickness? iconMargin = null;

        public List<Icon_DBC_Lookup> Lookups = new List<Icon_DBC_Lookup>();

        public SpellIconDBC(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellIcon.dbc");
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

            var container = adapter.Query(string.Format("SELECT `SpellIconID`,`ActiveIconID` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID));
            if (container == null || container.Rows.Count == 0)
            {
                return;
            }
            var res = container.Rows[0];
            uint iconInt = uint.Parse(res[0].ToString());
            uint iconActiveInt = uint.Parse(res[1].ToString());
            // Update currently selected icon, we don't currently handle ActiveIconID
            main.CurrentIcon.Source = BlpManager.GetInstance().GetImageSourceFromBlpPath(GetIconPath(iconInt) + ".blp");
            // Load all icons available if we have not already
            if (main.IconGrid.Children.Count == 0)
            {
                var watch = new Stopwatch();
                watch.Start();
                LoadAllIcons(margin);
                watch.Stop();
                Logger.Info($"Loaded all icons as UI elements in {watch.ElapsedMilliseconds}ms");
            }
        }

        public void LoadAllIcons(double margin)
        {
            var pathsToAdd = new List<Icon_DBC_Lookup>();
            foreach (var entry in Lookups)
            {
                var path = entry.Name + ".blp";
                if (File.Exists(path))
                {
                    pathsToAdd.Add(entry);
                }
            }
            var imagesPool = new List<Image>(pathsToAdd.Count);
            for (int i = 0; i < pathsToAdd.Count; ++i)
            {
                imagesPool.Add(new Image());
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
                image.ToolTip = entry.ID + " - " + entry.Name + ".blp";
                image.MouseDown += ImageDown;
            }
            foreach (var image in imagesPool)
            {
                main.IconGrid.Children.Add(image);
            }
            if (Config.Config.RenderImagesInView)
            {
                main.IconScrollViewer.ScrollChanged += IconScrollViewer_ScrollChanged;
            }
            else
            {
                imagesPool.ForEach((image) => image.IsVisibleChanged += IsImageVisibleChanged);
            }
        }

        private async void IconScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer sv = (ScrollViewer)sender;
            Rect svViewportBounds = new Rect(sv.HorizontalOffset, sv.VerticalOffset, sv.ViewportWidth, sv.ViewportHeight);

            for (int i = 0; i < main.IconGrid.Children.Count; ++i)
            {
                var container = main.IconGrid.Children[i] as FrameworkElement;
                if (container != null)
                {
                    var offset = VisualTreeHelper.GetOffset(container);
                    var bounds = new Rect(offset.X, offset.Y, container.ActualWidth, container.ActualHeight);

                    var image = container as Image;
                    var source = image.Source;
                    if (svViewportBounds.IntersectsWith(bounds))
                    {
                        if (source == null)
                        {
                            var path = image.ToolTip.ToString().Substring(image.ToolTip.ToString().IndexOf('-') + 2);
                            await Task.Factory.StartNew(() => source = BlpManager.GetInstance().GetImageSourceFromBlpPath(path));
                        }
                    }
                    else
                    {
                        source = null;
                    }
                    image.Source = source;
                }
            }
        }

        private async void IsImageVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var image = sender as Image;
            var source = image.Source;
            if (source == null && (bool)e.NewValue)
            {
                var path = image.ToolTip.ToString().Substring(image.ToolTip.ToString().IndexOf('-') + 2);
                await Task.Factory.StartNew(() => source = BlpManager.GetInstance().GetImageSourceFromBlpPath(path));
            }
            image.Source = source;
        }

        public void ImageDown(object sender, EventArgs e)
        {
            var image = sender as Image;
            main.NewIcon.Source = image.Source;

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
                Logger.Info(ex.Message);
                Logger.Info(ex.StackTrace);
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
