using GrimBuilder2.Core.Services;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Pfim;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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

    private static WriteableBitmap GetImageSource(string path)
    {
        var texData = App.GetService<ArzParserService>().GetFileData(path[(path.IndexOf('/') + 1)..]).data;
        using var texReaderStream = new MemoryStream(texData);

        var frameLength = 0;
        texReaderStream.Position = 8;
        texReaderStream.ReadExactly(MemoryMarshal.Cast<int, byte>(MemoryMarshal.CreateSpan(ref frameLength, 1)));

        var ddsBuffer = new byte[frameLength];
        texReaderStream.ReadExactly(ddsBuffer);
        ddsBuffer[3] = 0x20;

        var dds = Dds.Create(ddsBuffer, new());

        var bitmap = new WriteableBitmap(dds.Width, dds.Height);
        using var bitmapSourceWriterStream = bitmap.PixelBuffer.AsStream();
        bitmapSourceWriterStream.Write(dds.Data);

        return bitmap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
