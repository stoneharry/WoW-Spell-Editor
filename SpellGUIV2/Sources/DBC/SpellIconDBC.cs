using NLog;
using SpellEditor.Sources.BLP;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Windows.Controls.Image;

namespace SpellEditor.Sources.DBC
{
    class SpellIconDBC : AbstractDBC
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private MainWindow main;
        private IDatabaseAdapter adapter;

        private double? iconSize = null;
        private Thickness? iconMargin = null;

        private List<Image> _imagesPool;
        private DispatcherTimer _scrollDebounce;

        public List<Icon_DBC_Lookup> Lookups = new List<Icon_DBC_Lookup>();

        public SpellIconDBC(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellIcon.dbc");
        }

        public override void LoadGraphicUserInterface()
        {
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                uint offset = (uint)record["Name"];
                if (offset == 0)
                    continue;
                string name = LookupStringOffset(offset);
                uint id = (uint)record["ID"];

                Icon_DBC_Lookup lookup;
                lookup.ID = id;
                lookup.Offset = offset;
                lookup.Name = name;
                Lookups.Add(lookup);
            }

            // In this DBC we don't actually need to keep the DBC data now that
            // we have extracted the lookup tables. Nulling it out may help with
            // memory consumption.
            CleanStringsMap();
            CleanBody();
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
            if (adapter == null || main.selectedID == 0 || main.IconGrid.Children.Count > 0)
                return;

            // Convert to background worker here
            Task.Run(() =>
            {
                var container = adapter.Query(string.Format("SELECT `SpellIconID`,`ActiveIconID` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID));
                if (container == null || container.Rows.Count == 0)
                {
                    return;
                }
                var res = container.Rows[0];
                uint iconInt = uint.Parse(res[0].ToString());
                uint iconActiveInt = uint.Parse(res[1].ToString());
                // Update currently selected icon, we don't currently handle ActiveIconID
                main.Dispatcher?.BeginInvoke(DispatcherPriority.Normal, new Action(
                    () => main.CurrentIcon.Source = BlpManager.GetInstance().GetImageSourceFromBlpPath(GetIconPath(iconInt) + ".blp")));
            });

            // Load all icons available if we have not already
            var watch = new Stopwatch();
            watch.Start();
            LoadAllIcons(margin);
            watch.Stop();
            Logger.Info($"Loaded all icons as UI elements in {watch.ElapsedMilliseconds}ms");
        }

        public void LoadAllIcons(double margin)
        {
            var pathsToAdd = Lookups.Where(entry => File.Exists(entry.Name + ".blp")).ToList();
            var imagesPool = new List<Image>(pathsToAdd.Count);
            foreach (var entry in pathsToAdd)
            {
                var image = new Image
                {
                    Width = iconSize == null ? 32 : iconSize.Value,
                    Height = iconSize == null ? 32 : iconSize.Value,
                    Margin = iconMargin == null ? new Thickness(margin, 0, 0, 0) : iconMargin.Value,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Name = "Index_" + entry.Offset,
                    ToolTip = entry.ID + " - " + entry.Name + ".blp"
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                image.MouseDown += ImageDown;
                imagesPool.Add(image);
            }
            pathsToAdd = null;
            _imagesPool = imagesPool;

            // Add all children in a single pass so WPF coalesces into one layout pass
            foreach (var image in imagesPool)
                main.IconGrid.Children.Add(image);

            var renderInView = Config.Config.RenderImagesInView;
            if (renderInView)
            {
                var sv = main.IconScrollViewer;
                _scrollDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
                _scrollDebounce.Tick += (s, e) =>
                {
                    _scrollDebounce.Stop();
                    UpdateVisibleIcons(sv);
                };
                sv.ScrollChanged += IconScrollViewer_ScrollChanged;
                // Defer initial load until layout has completed
                main.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    new Action(() => UpdateVisibleIcons(sv)));
            }
            else
            {
                // Extract paths on the UI thread before handing off to background
                var imagePaths = imagesPool.Select(img =>
                {
                    var tip = img.ToolTip.ToString();
                    return (img, tip.Substring(tip.IndexOf('-') + 2));
                }).ToList();

                // Load in parallel; post each assignment as fire-and-forget so background
                // threads don't block waiting for the UI thread between images
                Task.Run(() => Parallel.ForEach(
                    imagePaths,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    pair =>
                    {
                        var source = BlpManager.GetInstance().GetImageSourceFromBlpPath(pair.Item2);
                        main.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                            new Action(() => pair.Item1.Source = source));
                    }));
            }
        }

        private void IconScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _scrollDebounce.Stop();
            _scrollDebounce.Start();
        }

        private void UpdateVisibleIcons(ScrollViewer sv)
        {
            if (_imagesPool == null || _imagesPool.Count == 0)
                return;

            double size = iconSize ?? 32;
            double leftMargin = iconMargin?.Left ?? 4;
            double itemWidth = size + leftMargin;

            // Calculate which rows are in view using scroll position and item size
            int columns = Math.Max(1, (int)(sv.ViewportWidth / itemWidth));
            int firstRow = Math.Max(0, (int)(sv.VerticalOffset / size) - 1);
            int lastRow = (int)((sv.VerticalOffset + sv.ViewportHeight) / size) + 1;

            for (int i = 0; i < _imagesPool.Count; i++)
            {
                var image = _imagesPool[i];
                bool inView = (i / columns) >= firstRow && (i / columns) <= lastRow;

                if (inView && image.Source == null)
                {
                    var capturedImage = image;
                    var path = image.ToolTip.ToString();
                    path = path.Substring(path.IndexOf('-') + 2);
                    Task.Run(() =>
                    {
                        var source = BlpManager.GetInstance().GetImageSourceFromBlpPath(path);
                        main.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                            new Action(() => capturedImage.Source = source));
                    });
                }
                else if (!inView)
                {
                    image.Source = null;
                }
            }
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
