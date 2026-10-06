using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

// Load the real release assembly from a directory containing only its EXE.
// Exercise the built-in image and notices in both 32-bit and 64-bit CLR hosts.
static class PortableSmoke {
  static void Check(bool condition,string message) { if(!condition)throw new Exception(message); }
  static string Digest(Stream stream) {
    using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
  }
  static void Main(string[] args) {
    try {
      string exe=Path.GetFullPath(args[0]);
      Check(!Directory.Exists(Path.Combine(Path.GetDirectoryName(exe),"assets")),"Test directory must have no assets");
      var assembly=Assembly.LoadFrom(exe);
      Check(assembly.GetName().Version.ToString()=="0.0.1.0","Assembly version");
      PortableExecutableKinds kind;ImageFileMachine machine;
      assembly.ManifestModule.GetPEKind(out kind,out machine);
      Check((kind&PortableExecutableKinds.ILOnly)!=0 && (kind&PortableExecutableKinds.Required32Bit)==0,"AnyCPU IL-only assembly");
      using(var stream=assembly.GetManifestResourceStream("WindowPet.amiya_test1.png"))
      using(var source=File.OpenRead(args[1])) {
        Check(stream!=null,"Embedded image exists");Check(Digest(stream)==Digest(source),"Embedded source bytes match original");
      }
      var assets=assembly.GetType("PetAssets",true);
      string notices=(string)assets.GetMethod("Notices").Invoke(null,null);
      Check(notices.Contains("James Wrightson") && notices.Contains("MIT License"),"Embedded upstream license");
      using(var bitmap=(Bitmap)assets.GetMethod("LoadAmiya").Invoke(null,null)) {
        Check(bitmap.Width==320 && bitmap.Height==296,"Image decode works without external files");
        var type=assembly.GetType("PetBodyPhysics",true);
        var body=type.GetMethod("FromImage").Invoke(null,new object[]{bitmap,160.0,148.0});
        double x=(double)type.GetField("CenterX").GetValue(body);
        double y=(double)type.GetField("CenterY").GetValue(body);
        double moment=(double)type.GetField("MomentPerMass").GetValue(body);
        // Baseline from the actual high-quality 320x296 raster used by PetForm.
        Check(Math.Abs(x-(-.47458))<.02 && Math.Abs(y-85.41807)<.02 && Math.Abs(moment-9527.96215)<1,"Approved asset mass distribution unchanged");
        Console.WriteLine("Portable image COM ({0:F3}, {1:F3}), moment {2:F2}",x,y,moment);
      }
      Console.WriteLine("Portable smoke passed: {0}-bit CLR, {1}, {2}",IntPtr.Size*8,kind,machine);
    } catch(Exception error) { Console.Error.WriteLine(error);Environment.ExitCode=1; }
  }
}
