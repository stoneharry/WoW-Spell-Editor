using NLog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpellEditor.Sources.BLP
{
    class BlpManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // For garbage collection of Bitmap handles
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        private static BlpManager _Instance = new BlpManager();
        private ConcurrentDictionary<string, ImageSource> _ImageMap = new ConcurrentDictionary<string, ImageSource>();

        private BlpManager()
        {
        }

        public static BlpManager GetInstance()
        {
            return _Instance;
        }

        public ImageSource GetImageSourceFromBlpPath(string filePath)
        {
            if (_ImageMap.TryGetValue(filePath, out ImageSource source))
            {
                return source;
            }
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    using (var blpImage = new SereniaBLPLib.BlpFile(fileStream))
                    {
                        using (var bit = blpImage.getBitmap(0))
                        {
                            var handle = bit.GetHbitmap();
                            try
                            {
                                source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    handle, IntPtr.Zero, Int32Rect.Empty,
                                    BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                                // Freeze so that it can be accessed on any thread
                                source.Freeze();
                                _ImageMap.TryAdd(filePath, source);
                                return source;
                            }
                            finally
                            {
                                DeleteObject(handle);
                            }
                        }
                    }
                }
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 32 || (ioEx.HResult & 0xFFFF) == 33)
            {
                // lets not add files in the map that are currently used because these free up next time and loads properly.
                // this is a hack fix for this issue and cba looking for the real one.
                // its probably due to some multithreading conflict and tries to use them at the same time.
                Logger.Info($"[BlpManager] FILE IN USE: {filePath}");
            }
            catch (Exception e)
            {
                // Logging full exception is quite costly here
                Logger.Info($"[BlpManager] WARNING Unable to load image: {filePath} - {e.Message}");
                // Making the choice here to not try to load the resource again until the program is restarted
                _ImageMap.TryAdd(filePath, null);
            }
            return null;
        }
    }
}
