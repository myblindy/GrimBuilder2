using GrimBuilder2.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Pfim;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

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

    static readonly Dictionary<(string path, bool grayed), WriteableBitmap> bitmapCache = [];
    private static async void OnImageChanged(Image image, string? path, bool grayed)
    {
        if (string.IsNullOrWhiteSpace(path))
            image.Source = null;
        else
        {
            if (bitmapCache.TryGetValue((path, grayed), out var bitmap))
                image.Source = bitmap;
            else
            {
                var (pixelData, width, height) = await Task.Run(async () =>
                {
                    var arz = App.GetService<ArzParserService>();
                    await arz.EnsureLoadedAsync().ConfigureAwait(false);

                    var texData = arz.GetFileData(path[(path.IndexOf('/') + 1)..]).data;
                    using var texReaderStream = new MemoryStream(texData);

                    var frameLength = 0;
                    texReaderStream.Position = 8;
                    texReaderStream.ReadExactly(MemoryMarshal.Cast<int, byte>(MemoryMarshal.CreateSpan(ref frameLength, 1)));

                    var ddsBuffer = new byte[frameLength];
                    texReaderStream.ReadExactly(ddsBuffer);
                    ddsBuffer[3] = 0x20;

                    var dds = Dds.Create(ddsBuffer, new());

                    var ddsPixels = dds.Data;
                    if (dds.BytesPerPixel == 3)
                    {
                        // RGB -> RGBA
                        var buffer = new byte[dds.Height * dds.Width * 4];

                        unsafe
                        {
                            fixed (byte* _src = ddsPixels)
                            fixed (byte* _dst = buffer)
                            {
                                var src = _src;
                                var dst = _dst;
                                var dstEnd = dst + dds.Height * dds.Width * 4;
                                for (; dst < dstEnd - 4; src += 3, dst += 4)
                                {
                                    // over copy a while integer, then overwrite the alpha byte with 0xFF
                                    *(int*)dst = *(int*)src;
                                    dst[3] = 0xFF;
                                }

                                // handle the last pixel (didn't have enough garbage data in the source to over copy it)
                                dst[0] = src[0];
                                dst[1] = src[1];
                                dst[2] = src[2];
                                dst[3] = 0xFF;
                            }
                        }
                        ddsPixels = buffer;
                    }
                    else if (dds.BytesPerPixel != 4)
                        throw new NotImplementedException();

                    if (grayed)
                    {
                        // RGBA -> Grayscale
                        unsafe
                        {
                            fixed (byte* _pixels = ddsPixels)
                            {
                                var pixels = _pixels;
                                var pixelsEnd = pixels + ddsPixels.Length;
                                for (; pixels < pixelsEnd; pixels += 4)
                                {
                                    var grayValue = (byte)(pixels[0] * 0.299 + pixels[1] * 0.587 + pixels[2] * 0.114);
                                    pixels[0] = pixels[1] = pixels[2] = grayValue;
                                }
                            }
                        }
                    }

                    return (ddsPixels, dds.Width, dds.Height);
                });

                bitmap = new WriteableBitmap(width, height);
                using var bitmapSourceWriterStream = bitmap.PixelBuffer.AsStream();
                bitmapSourceWriterStream.Write(pixelData);
                image.Source = bitmapCache[(path, grayed)] = bitmap;
            }
        }
    }
}
