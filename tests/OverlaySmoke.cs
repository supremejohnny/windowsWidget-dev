using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

class OverlaySmoke {
  static void Check(bool value,string message) { if(!value)throw new Exception(message); }
  static void Pump(int milliseconds) {
    int until=Environment.TickCount+milliseconds;
    while(Environment.TickCount<until) { Application.DoEvents();Thread.Sleep(10); }
  }
  [STAThread] static void Main() {
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
      target.Location=new Point(540,340);Pump(100);
      Win.GetWindowRect(target.Handle,out r);
      Check(pet.Left==r.Left+36-260 && pet.Top==r.Top+4-52,"tracks x and y");
      other.Text="Window Pet smoke occluder";other.StartPosition=FormStartPosition.Manual;
      other.Location=new Point(600,350);other.Show();Win.SetForegroundWindow(other.Handle);Pump(150);
      Check(!pet.Visible,"hides behind another foreground window");
      pet.Close();
    }
    Console.WriteLine("Overlay smoke test passed");
  }
}
