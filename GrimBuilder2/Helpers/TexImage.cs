using GrimBuilder2.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Nito.AsyncEx;
using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Buffers.Binary;

using Image = Microsoft.UI.Xaml.Controls.Image;

namespace GrimBuilder2.Helpers;

static class TexImage
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached("Source",
        typeof(string), typeof(TexImage), new PropertyMetadata(null, (s, e) =>
            OnImageChanged((Image)s, (string)e.NewValue, GetGrayedOut((Image)s))));
    public static string? GetSource(Image item) => (string?)item.GetValue(SourceProperty);
    public static void SetSource(Image item, string? value) => item.SetValue(SourceProperty, value);

    public static readonly DependencyProperty GrayedOutProperty = DependencyProperty.RegisterAttached("GrayedOut",
        typeof(bool), typeof(TexImage), new PropertyMetadata(false, (s, e) =>
            OnImageChanged((Image)s, GetSource((Image)s), (bool)e.NewValue)));
    public static bool GetGrayedOut(Image item) => (bool)item.GetValue(GrayedOutProperty);
    public static void SetGrayedOut(Image item, bool value) => item.SetValue(GrayedOutProperty, value);

    struct BitmapCacheEntry
    {
        public ImageSource? ImageSource;
        public AsyncManualResetEvent? LoadedEvent;

        public BitmapCacheEntry() { }
    }
    static readonly Dictionary<(string path, bool grayed), BitmapCacheEntry> bitmapCache = [];
    static readonly Dictionary<string, AsyncManualResetEvent> bitmapPathLoading = [];
    private static async void OnImageChanged(Image image, string? path, bool grayed)
    {
        if (string.IsNullOrWhiteSpace(path))
            image.Source = null;
        else
        {
            // wait if we're loading the same path, to sequence grayed images
            if (bitmapPathLoading.TryGetValue(path, out var loadingEvent))
                await loadingEvent.WaitAsync();

            // if we already have the image in the cache, use it
            if (bitmapCache.TryGetValue((path, grayed), out var cacheEntry))
            {
                // if something else is loading the image, wait for it
                if (cacheEntry.ImageSource is null)
                    await cacheEntry.LoadedEvent!.WaitAsync();
                image.Source = cacheEntry.ImageSource;
            }
            else
            {
                // set up the loading events
                bitmapPathLoading.Add(path, new());
                bitmapCache.Add((path, grayed), new() { LoadedEvent = new() });

                // decode the tex data
                var imageStream = await Task.Run(async () =>
                {
                    var arz = App.GetService<ArzParserService>();
                    await arz.EnsureLoadedAsync().ConfigureAwait(false);

                    var texData = (await arz.GetFileDataAsync(path[(path.IndexOf('/') + 1)..]).ConfigureAwait(false)).data;

                    var frameLength = BinaryPrimitives.ReadInt32LittleEndian(texData.AsSpan(8..));
                    var ddsBuffer = texData[12..(12 + frameLength)];
                    ddsBuffer[3] = 0x20;

                    using var dds = Dds.Create(ddsBuffer, new());

                    using SixLabors.ImageSharp.Image imageData = dds.BytesPerPixel == 3
                        ? SixLabors.ImageSharp.Image.LoadPixelData<Bgr24>(dds.Data, dds.Width, dds.Height)
                        : SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(dds.Data, dds.Width, dds.Height);

                    if (grayed)
                        imageData.Mutate(x => x.Grayscale());

                    var msImageData = new MemoryStream(dds.Width * dds.Height * 3);
                    imageData.SaveAsPng(msImageData, new()
                    {
                        CompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel.Level1,
                        TransparentColorMode = SixLabors.ImageSharp.Formats.Png.PngTransparentColorMode.Clear
                    });
                    msImageData.Position = 0;
                    return msImageData;
                });

                // create the image source and bind it to the pixel data above
                var bitmap = new BitmapImage();
                image.Source = bitmap;
                _ = bitmap.SetSourceAsync(imageStream.AsRandomAccessStream());

                // replace the bitmap cache entry with the image source
                bitmapCache[(path, grayed)] = new() { ImageSource = bitmap };

                // signal that we're done loading the path
                bitmapPathLoading[path].Set();
                bitmapPathLoading.Remove(path);
            }
        }
    }
}
