using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var output = args.Length == 1 ? args[0] : throw new ArgumentException("Destination ICO path required.");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var sizes = new[] { 16, 20, 24, 32, 48, 64, 128, 256 };
        var images = sizes.Select(RenderPng).ToList();
        using var stream = File.Create(output);
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)images.Count);
        var offset = 6 + images.Count * 16;
        for (var i = 0; i < images.Count; i++)
        {
            var size = sizes[i]; writer.Write((byte)(size == 256 ? 0 : size)); writer.Write((byte)(size == 256 ? 0 : size)); writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32); writer.Write(images[i].Length); writer.Write(offset); offset += images[i].Length;
        }
        foreach (var image in images) writer.Write(image);
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(output)!, "ClipboardPro-512.png"), RenderPng(512));
    }

    private static byte[] RenderPng(int size)
    {
        var visual = new DrawingVisual();
        using (var c = visual.RenderOpen())
        {
            c.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(169, 155, 255)), null, new Rect(27, 17, 70, 70), 22, 22);
            c.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(119, 91, 250)), null, new Rect(34, 15, 74, 76), 26, 26);
            var outer = Geometry.Parse("M83,46.5 C77.8,42.1 71.3,40 64.3,40.7 C50.7,42 40.1,53.5 40.1,67.2 C40.1,81.8 51.9,93.6 66.5,93.6 C72.6,93.6 78.3,91.5 82.7,88 L74.2,79.5 C70.9,81.9 66.8,83.3 62.3,83.3 C52.9,83.3 45.3,75.7 45.3,66.3 C45.3,56.9 52.9,49.3 62.3,49.3 C66.8,49.3 71,51.1 74.1,54.1 Z");
            c.DrawGeometry(new SolidColorBrush(Color.FromRgb(23, 23, 28)), null, outer);
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var png = new MemoryStream(); encoder.Save(png); return png.ToArray();
    }
}
