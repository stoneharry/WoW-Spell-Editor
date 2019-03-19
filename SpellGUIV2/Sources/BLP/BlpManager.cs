using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpellEditor.Sources.BLP
{
    class BlpManager
    {
        // For GC collection of Bitmap handles
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        private static BlpManager _Instance = new BlpManager();
        private Dictionary<string, ImageSource> _ImageMap = new Dictionary<string, ImageSource>();

        private BlpManager()
        {
        }

        public static BlpManager GetInstance()
        {
            return _Instance;
        }

        public ImageSource GetImageSourceFromBlpPath(string filePath)
        {
            if (_ImageMap.ContainsKey(filePath))
            {
                return _ImageMap[filePath];
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
                                var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    handle, IntPtr.Zero, Int32Rect.Empty,
                                    BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                                _ImageMap.Add(filePath, source);
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
            catch (Exception e)
            {
                // Logging full exception is quite costly here
                Console.WriteLine($"[BlpManager] WARNING Unable to load image: {filePath} - {e.Message}");
                // Making the choice here to not try to load the resource again until the program is restarted
                _ImageMap.Add(filePath, null);
            }
            return null;
        }
    }

    class BlpReference
    {
        public ImageSource Source;
    }
}
