using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
static class CollageFactory {
    internal static string Get(string folder,List<string> paths)
    {
        if(paths.Count==0)return null;
        string signature=String.Join("\n",paths.ToArray());

        byte[] hash; using(var sha=System.Security.Cryptography.SHA256.Create())hash=sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signature));
        var cache=Path.Combine(folder,"GroupIcons"); Directory.CreateDirectory(cache);
        string file=Path.Combine(cache,BitConverter.ToString(hash).Replace("-","").Substring(0,20)+".ico");
        var icons=new List<Icon>();
        try {
            foreach(var path in paths.GetRange(0,Math.Min(4,paths.Count)))icons.Add(Icon.ExtractAssociatedIcon(path)??(Icon)SystemIcons.Application.Clone());
            int[] sizes={16,24,32,48,64,128,256}; var chunks=new List<byte[]>();
            foreach(int size in sizes) {
                using(var bitmap=new Bitmap(size,size))using(var g=Graphics.FromImage(bitmap)) {
                    g.Clear(Color.Transparent); g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    for(int i=0;i<icons.Count;i++) {
                        Rectangle rect;
                        if(icons.Count==1)rect=new Rectangle(0,0,size,size);
                        else if(icons.Count==2) { int side=(int)(size*0.72); int inset=size-side; rect=new Rectangle(i==0?0:inset,i==0?0:inset,side,side); }
                        else { int side=size/2; rect=new Rectangle((i%2)*side,(i/2)*side,side,side); }
                        using(var image=icons[i].ToBitmap())g.DrawImage(image,rect);
                    }
                    if(paths.Count>4)using(var brush=new SolidBrush(Color.FromArgb(230,55,35,105)))using(var font=new Font("Segoe UI",Math.Max(5,size*0.19f),FontStyle.Bold,GraphicsUnit.Pixel)) {
                        var rect=new Rectangle(size/2,size/2,size-size/2,size-size/2);g.FillRectangle(brush,rect);
                        using(var format=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center })g.DrawString("+"+(paths.Count-3),font,Brushes.White,rect,format);
                    }
                    using(var stream=new MemoryStream()) { bitmap.Save(stream,System.Drawing.Imaging.ImageFormat.Png); chunks.Add(stream.ToArray()); }
                }
            }
            // A membership-specific filename avoids stale icons in Explorer's cache.
            if(!File.Exists(file))using(var writer=new BinaryWriter(File.Create(file))) {
                writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
                for(int i=0;i<sizes.Length;i++) { writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(chunks[i].Length);writer.Write(offset);offset+=chunks[i].Length; }
                foreach(var chunk in chunks)writer.Write(chunk);
            }
            return file;
        } finally { foreach(var icon in icons)icon.Dispose(); }
    }
}

