using System;
using System.Collections.Generic;
using System.Globalization;
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
        var folder=Path.GetDirectoryName(output)!;
        File.WriteAllBytes(Path.Combine(folder, "ClipboardPro-512.png"), RenderPng(512));
        File.WriteAllBytes(Path.Combine(folder, "InstallerWizard.bmp"), RenderInstallerImage(164,314));
        File.WriteAllBytes(Path.Combine(folder, "InstallerWizardSmall.bmp"), RenderInstallerImage(55,55));
    }

    private static byte[] RenderPng(int size)
    {
        var visual = new DrawingVisual();
        using (var c = visual.RenderOpen())
        {
            c.PushTransform(new ScaleTransform(size/128d,size/128d)); DrawMark(c); c.Pop();
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var png = new MemoryStream(); encoder.Save(png); return png.ToArray();
    }
    private static byte[] RenderInstallerImage(int width,int height)
    {
        var visual=new DrawingVisual(); using(var c=visual.RenderOpen())
        {
            c.DrawRectangle(new SolidColorBrush(Color.FromRgb(23,23,28)),null,new Rect(0,0,width,height));
            if(width<100) { c.PushTransform(new ScaleTransform(.38,.38)); c.PushTransform(new TranslateTransform(8,8)); DrawMark(c); c.Pop();c.Pop(); }
            else { c.PushTransform(new ScaleTransform(.94,.94)); c.PushTransform(new TranslateTransform(23,31)); DrawMark(c); c.Pop();c.Pop(); var text=new FormattedText("CLIPBOARD\nPRO",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI Variable"),16,new SolidColorBrush(Color.FromRgb(244,242,250)),1); c.DrawText(text,new Point(28,182)); var sub=new FormattedText("Siempre a mano.",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),9,new SolidColorBrush(Color.FromRgb(167,163,178)),1); c.DrawText(sub,new Point(28,230)); }
        }
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);var encoder=new BmpBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var buffer=new MemoryStream();encoder.Save(buffer);return buffer.ToArray();
    }
    private static void DrawMark(DrawingContext c)
    {
        c.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(178,166,255)),null,new Rect(13,10,93,93),29,29);
        c.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(124,92,255)),null,new Rect(22,22,94,94),29,29);
        var cut=Geometry.Parse("M91,50 C84,44 76,41 67,41 C48,41 33,56 33,75 C33,94 48,109 67,109 C76,109 84,106 91,100 L80,88 C76,91 72,93 67,93 C57,93 49,85 49,75 C49,65 57,57 67,57 C72,57 76,59 80,62 Z");
        c.DrawGeometry(new SolidColorBrush(Color.FromRgb(23,23,28)),null,cut);
    }
}
