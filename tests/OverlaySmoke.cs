using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

class OverlaySmoke {
  [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
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
      var pet=new PetForm(target.Handle,0,100);
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
      Check(VisibleAbove(target.Handle)==pet.Handle && VisibleAbove(pet.Handle)==pet.AnchorHandle,
        "stowed rope stays with target z-order");
      SendMessage(pet.AnchorHandle,0x0201,IntPtr.Zero,new IntPtr(10|(10<<16)));
      SendMessage(pet.AnchorHandle,0x0202,IntPtr.Zero,new IntPtr(10|(10<<16)));
      Pump(400);
      Check(!pet.IsStowed,"second anchor click restores pet");
      pet.Close();
    }
    Console.WriteLine("Overlay smoke test passed");
  }
}
