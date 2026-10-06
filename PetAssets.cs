using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

// Built-in assets travel with the executable; no extraction or asset directory.
static class PetAssets {
  public static Bitmap LoadAmiya() {
    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowPet.amiya_test1.png")) {
      if(stream==null)throw new InvalidOperationException("Built-in asset amiya_test1 is missing. Rebuild using build.ps1.");
      using(var loaded=Image.FromStream(stream)) {
        var image=new Bitmap(320,296,PixelFormat.Format32bppPArgb);
        try {
          using(var graphics=Graphics.FromImage(image)) {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(loaded,new Rectangle(0,0,320,296));
          }
          return image;
        } catch { image.Dispose();throw; }
      }
    }
  }
  public static string Notices() {
    var assembly=Assembly.GetExecutingAssembly();
    using(var stream=assembly.GetManifestResourceStream("WindowPet.NOTICES.txt"))
    using(var reader=new StreamReader(stream))return reader.ReadToEnd();
  }
}
