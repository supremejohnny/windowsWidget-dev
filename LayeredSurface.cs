using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// One reusable premultiplied-alpha DIB. No per-frame bitmap or native handle allocation.
sealed class LayeredSurface : IDisposable {
  [StructLayout(LayoutKind.Sequential)] struct BitmapInfo {
    public uint Size; public int Width,Height; public ushort Planes,BitCount;
    public uint Compression,ImageSize; public int XPels,YPels; public uint Used,Important;
    public uint Color;
  }
  [StructLayout(LayoutKind.Sequential,Pack=1)] struct Blend {
    public byte Operation,Flags,Alpha,Format;
  }
  [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
  [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
  [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
  [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dst,ref Point position,ref Size size,IntPtr src,ref Point origin,uint key,ref Blend blend,uint flags);
  IntPtr dc,dib,oldBitmap;
  Bitmap bitmap;
  Graphics graphics;
  readonly Size size;
  public LayeredSurface(int width,int height) {
    size=new Size(width,height);
    dc=CreateCompatibleDC(IntPtr.Zero);
    var info=new BitmapInfo {Size=40,Width=width,Height=-height,Planes=1,BitCount=32};
    IntPtr bits;
    dib=CreateDIBSection(dc,ref info,0,out bits,IntPtr.Zero,0);
    if(dc==IntPtr.Zero||dib==IntPtr.Zero) {Dispose();throw new InvalidOperationException("Could not create alpha drawing surface.");}
    oldBitmap=SelectObject(dc,dib);
    bitmap=new Bitmap(width,height,width*4,PixelFormat.Format32bppPArgb,bits);
    graphics=Graphics.FromImage(bitmap);
    graphics.SmoothingMode=SmoothingMode.AntiAlias;
    graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
  }
  public void Present(IntPtr hwnd,int x,int y,Action<Graphics> paint) {
    graphics.Clear(Color.Transparent);
    paint(graphics);
    graphics.Flush();
    Point position=new Point(x,y),origin=Point.Empty;
    Size frame=size;
    var blend=new Blend {Alpha=255,Format=1};
    if(!UpdateLayeredWindow(hwnd,IntPtr.Zero,ref position,ref frame,dc,ref origin,0,ref blend,2))
      throw new Win32Exception(Marshal.GetLastWin32Error());
  }
  public void Dispose() {
    if(graphics!=null){graphics.Dispose();graphics=null;}
    if(bitmap!=null){bitmap.Dispose();bitmap=null;}
    if(oldBitmap!=IntPtr.Zero&&dc!=IntPtr.Zero){SelectObject(dc,oldBitmap);oldBitmap=IntPtr.Zero;}
    if(dib!=IntPtr.Zero){DeleteObject(dib);dib=IntPtr.Zero;}
    if(dc!=IntPtr.Zero){DeleteDC(dc);dc=IntPtr.Zero;}
  }
}
