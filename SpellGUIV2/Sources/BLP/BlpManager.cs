using NLog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpellEditor.Sources.BLP
{
    class BlpManager
    {
        private const int MaxDecodeDimension = 128;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static BlpManager _Instance = new BlpManager();

        private readonly ConcurrentDictionary<string, ImageSource> _ImageMap = new ConcurrentDictionary<string, ImageSource>();
        private readonly ConcurrentStack<PendingLoad> _PendingLoads = new ConcurrentStack<PendingLoad>();
        private readonly SemaphoreSlim _PendingSignal = new SemaphoreSlim(0);

        private BlpManager()
        {
            var workerCount = Math.Max(2, Math.Min(4, Environment.ProcessorCount - 1));
            for (var i = 0; i < workerCount; ++i)
            {
                var thread = new Thread(WorkerLoop)
                {
                    Name = "BlpLoader" + i,
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal
                };
                thread.Start();
            }
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
            source = Decode(filePath);
            _ImageMap.TryAdd(filePath, source);
            return source;
        }

        public void RequestImageSource(string filePath, Action<ImageSource> onLoaded)
        {
            if (_ImageMap.TryGetValue(filePath, out ImageSource source))
            {
                onLoaded(source);
                return;
            }
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _PendingLoads.Push(new PendingLoad(filePath, onLoaded, dispatcher));
            _PendingSignal.Release();
        }

        private void WorkerLoop()
        {
            while (true)
            {
                _PendingSignal.Wait();
                if (!_PendingLoads.TryPop(out PendingLoad load))
                {
                    continue;
                }
                var source = GetImageSourceFromBlpPath(load.FilePath);
                load.Dispatcher.BeginInvoke(DispatcherPriority.Background, load.OnLoaded, source);
            }
        }

        private ImageSource Decode(string filePath)
        {
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    32 * 1024, FileOptions.SequentialScan))
                {
                    using (var blpImage = new SereniaBLPLib.BlpFile(fileStream))
                    {
                        var level = PickMipMapLevel(blpImage);
                        var width = blpImage.GetMipMapWidth(level);
                        var height = blpImage.GetMipMapHeight(level);
                        var stride = width * 4;
                        var required = stride * height;
                        if (required <= 0)
                        {
                            return null;
                        }
                        var pixels = blpImage.getImageBytes(level);
                        if (pixels == null || pixels.Length < required)
                        {
                            return null;
                        }
                        SwapRedAndBlue(pixels, required);
                        var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null,
                            pixels, stride);
                        source.Freeze();
                        return source;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Info($"[BlpManager] WARNING Unable to load image: {filePath} - {e.Message}");
                return null;
            }
        }

        private static int PickMipMapLevel(SereniaBLPLib.BlpFile blpImage)
        {
            var count = blpImage.MipMapCount;
            var level = 0;
            while (level + 1 < count &&
                   blpImage.GetMipMapWidth(level + 1) >= MaxDecodeDimension &&
                   blpImage.GetMipMapHeight(level + 1) >= MaxDecodeDimension)
            {
                ++level;
            }
            return level;
        }

        private static void SwapRedAndBlue(byte[] pixels, int length)
        {
            for (var i = 0; i + 2 < length; i += 4)
            {
                var tmp = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = tmp;
            }
        }

        private struct PendingLoad
        {
            public readonly string FilePath;
            public readonly Action<ImageSource> OnLoaded;
            public readonly Dispatcher Dispatcher;

            public PendingLoad(string filePath, Action<ImageSource> onLoaded, Dispatcher dispatcher)
            {
                FilePath = filePath;
                OnLoaded = onLoaded;
                Dispatcher = dispatcher;
            }
        }
    }
}
