using GrimBuilder2.Core.Services;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Pfim;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Helpers;

sealed class TexImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language) =>
        value is string { } path ? GetImageSource(path) : null;

    static readonly Dictionary<string, WriteableBitmap> bitmapCache = [];
    private static WriteableBitmap GetImageSource(string path)
    {
        if (bitmapCache.TryGetValue(path, out var cachedBitmap)) return cachedBitmap;

        var texData = App.GetService<ArzParserService>().GetFileData(path[(path.IndexOf('/') + 1)..]).data;
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

        var bitmap = new WriteableBitmap(dds.Width, dds.Height);
        using var bitmapSourceWriterStream = bitmap.PixelBuffer.AsStream();
        bitmapSourceWriterStream.Write(ddsPixels);

        return bitmapCache[path] = bitmap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
