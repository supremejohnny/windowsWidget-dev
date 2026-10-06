using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Drawing.Imaging;

class OverlaySmoke {
  [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
  [DllImport("user32.dll")] static extern uint GetGuiResources(IntPtr process,uint flags);
  static void CheckRender(PetForm pet,string name) {
    using(var bitmap=new Bitmap(520,420,PixelFormat.Format32bppPArgb)) {
      using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);pet.DrawScene(g);}
      Check(bitmap.GetPixel(260,52).A==255,"anchor center remains pinned and opaque");
      Check(bitmap.GetPixel(0,0).A==0,"canvas remains fully transparent");
      int partial=0;
      for(int x=245;x<275;x++)for(int y=43;y<76;y++) {
        int alpha=bitmap.GetPixel(x,y).A;if(alpha>0&&alpha<255)partial++;
      }
      Check(partial>10,"rope and anchor have fractional alpha edges");
      Directory.CreateDirectory(".test-output");bitmap.Save(".test-output/"+name+".png",ImageFormat.Png);
    }
  }
  static void Check(bool value,string message) { if(!value)throw new Exception(message); }
  static IntPtr VisibleAbove(IntPtr h) {
    for(int i=0;i<30;i++) { h=Win.GetWindow(h,3);if(h==IntPtr.Zero||Win.IsWindowVisible(h))return h; }
    return IntPtr.Zero;
  }
  static void Pump(int milliseconds) {
    int until=Environment.TickCount+milliseconds;
    while(Environment.TickCount<until) { Application.DoEvents();Thread.Sleep(10); }
  }
  [STAThread] static void Main() {
    try { Run(); }
    catch(Exception e) { Console.Error.WriteLine(e.GetType().Name+": "+e.Message);Console.Error.WriteLine(e.StackTrace);Environment.Exit(1); }
  }
  static void Run() {
    Win.SetProcessDPIAware();
    using(var target=new Form()) using(var other=new Form()) {
      target.Text="Window Pet smoke target";target.StartPosition=FormStartPosition.Manual;
      target.Location=new Point(500,300);target.Size=new Size(500,400);target.Show();
      var pet=new PetForm(target.Handle,0,100,0);
      Check(!pet.TopMost,"pet must stay in normal z-order");
      Win.Rect r;Win.GetWindowRect(target.Handle,out r);
      Check(pet.Left==r.Left+36-260 && pet.Top==r.Top+4-52,"position before Show");
      pet.Show();Win.SetForegroundWindow(target.Handle);Pump(150);
      Check(pet.Visible,"visible with target foreground");
      Check(VisibleAbove(target.Handle)==pet.Handle,"visual above target");
      Check(VisibleAbove(pet.Handle)==pet.HitHandle,"hit region above visual");
      Check(VisibleAbove(pet.HitHandle)==pet.AnchorHandle,"anchor input above pet input");
      var blank=new Point(target.Left+120,target.Top+100);
      Check((Win.GetWindowLong(pet.Handle,-20)&0x20)!=0,"visual layer must be click-through");
      Check(Win.WindowFromPoint(blank)!=pet.Handle && Win.WindowFromPoint(blank)!=pet.HitHandle,
        "blank overlay must not capture clicks");
      Check(pet.HitRegion.IsVisible(43,39),"pet body hit region");
      Check(!pet.HitRegion.IsVisible(0,0),"outside pet hit region");
      target.Location=new Point(540,340);Pump(100);
      CheckRender(pet,"rope-moving");
      Win.GetWindowRect(target.Handle,out r);
      Check(pet.Left==r.Left+36-260 && pet.Top==r.Top+4-52,"tracks x and y");
      other.Text="Window Pet smoke occluder";other.StartPosition=FormStartPosition.Manual;
      other.Location=new Point(600,350);other.Show();Win.SetForegroundWindow(other.Handle);Pump(150);
      Check(pet.Visible,"pet remains attached while another window is foreground");
      Check(VisibleAbove(target.Handle)==pet.Handle,"visual above target after occlusion");
      Check(VisibleAbove(pet.Handle)==pet.HitHandle,"hit region above visual after occlusion");
      Check(VisibleAbove(pet.HitHandle)==pet.AnchorHandle,"anchor above pet hit region");
      Check(VisibleAbove(pet.AnchorHandle)==other.Handle,"other window remains above pet");
      SendMessage(pet.AnchorHandle,0x0201,IntPtr.Zero,new IntPtr(10|(10<<16)));
      SendMessage(pet.AnchorHandle,0x0202,IntPtr.Zero,new IntPtr(10|(10<<16)));
      Pump(400);
      Check(pet.IsStowed,"anchor click stows pet");
      Check(!Win.IsWindowVisible(pet.HitHandle),"pet input hides when stowed");
      Check(Win.IsWindowVisible(pet.AnchorHandle),"anchor stays clickable");
      foreach(var node in pet.StowedPoints)Check(node.X==260 && node.Y>=52 && node.Y<=70,"single anchor with 18px static stub");
      CheckRender(pet,"rope-stowed");
      for(int i=0;i<10;i++)pet.RenderFrame();
      using(var process=Process.GetCurrentProcess()) {
        uint before=GetGuiResources(process.Handle,0);
        process.Refresh();long memory=process.PrivateMemorySize64;
        for(int i=0;i<300;i++)pet.RenderFrame();
        GC.Collect();GC.WaitForPendingFinalizers();
        uint after=GetGuiResources(process.Handle,0);process.Refresh();
        Check(after<=before+2,"repeated rendering must not leak GDI handles");
        Console.WriteLine("300 frames: GDI handles {0}->{1}; private memory delta {2:F2} MB",before,after,(process.PrivateMemorySize64-memory)/1048576.0);
      }
      int stepsAtRest=pet.PhysicsSteps;
      int eventsBefore=pet.LocationEvents;
      target.Location=new Point(560,360);Pump(30);
      Check(pet.LocationEvents>eventsBefore,"native target movement event received");
      Win.GetWindowRect(target.Handle,out r);
      Win.Rect anchorRect;Win.GetWindowRect(pet.AnchorHandle,out anchorRect);
      Check(pet.Left+260==r.Left+36 && pet.Top+52==r.Top+4,"stowed anchor follows before idle polling interval");
      Check(anchorRect.Left+10==r.Left+36 && anchorRect.Top+10==r.Top+4,"anchor hit area follows native movement");
      Check(pet.PhysicsSteps==stepsAtRest,"stowed mode does not simulate physics even while target moves");
      Check(VisibleAbove(target.Handle)==pet.Handle && VisibleAbove(pet.Handle)==pet.AnchorHandle,
        "stowed rope stays with target z-order");
      SendMessage(pet.AnchorHandle,0x0201,IntPtr.Zero,new IntPtr(10|(10<<16)));
      SendMessage(pet.AnchorHandle,0x0202,IntPtr.Zero,new IntPtr(10|(10<<16)));
      Pump(400);
      Check(!pet.IsStowed,"second anchor click restores pet");
      pet.Close();
      for(int corner=0;corner<4;corner++) {
        var placed=new PetForm(target.Handle,0,85,corner);
        Win.GetWindowRect(target.Handle,out r);
        int x=(corner==1||corner==3)?r.Right-36:r.Left+36;
        int y=(corner==2||corner==3)?r.Bottom-4:r.Top+4;
        Check(placed.Left==x-260 && placed.Top==y-52,"corner placement "+corner);
        placed.Show();Pump(30);placed.Close();
      }
    }
    Console.WriteLine("Overlay smoke test passed");
  }
}
