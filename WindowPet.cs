using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

static class Win {
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
  public delegate bool EnumProc(IntPtr h,IntPtr p);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc f,IntPtr p);
  [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
class Choice {
  public IntPtr Handle; public string Name;
  public override string ToString() { return Name; }
}
static class Program {
  [STAThread] static void Main() {
    Win.SetProcessDPIAware(); Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new SettingsForm());
  }
}
class SettingsForm : Form {
  ComboBox windows=new ComboBox(), pets=new ComboBox();
  NumericUpDown rope=new NumericUpDown();
  Button attach=new Button();
  Label status=new Label();
  PetForm overlay;
  public SettingsForm() {
    Text="Window Pet"; ClientSize=new Size(350,230); FormBorderStyle=FormBorderStyle.FixedDialog;
    MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
    Controls.Add(new Label{Text="Window",Location=new Point(16,18),AutoSize=true});
    windows.Location=new Point(16,40); windows.Size=new Size(258,25);
    windows.DropDownStyle=ComboBoxStyle.DropDownList; Controls.Add(windows);
    var refresh=new Button{Text="Refresh",Location=new Point(280,39),Size=new Size(58,27)};
    refresh.Click+=(s,e)=>RefreshWindows(); Controls.Add(refresh);
    Controls.Add(new Label{Text="Pet",Location=new Point(16,80),AutoSize=true});
    pets.Location=new Point(16,101); pets.Size=new Size(155,25);
    pets.DropDownStyle=ComboBoxStyle.DropDownList;
    pets.Items.AddRange(new object[]{"Lilac cat","Honey bear","Mint ghost"}); pets.SelectedIndex=0;
    Controls.Add(pets);
    Controls.Add(new Label{Text="Rope length (px)",Location=new Point(190,80),AutoSize=true});
    rope.Location=new Point(190,101); rope.Size=new Size(148,25);
    rope.Minimum=35; rope.Maximum=190; rope.Value=85; Controls.Add(rope);
    attach.Text="Attach"; attach.Location=new Point(16,146); attach.Size=new Size(322,32);
    attach.Click+=AttachClick; Controls.Add(attach);
    status.Location=new Point(16,190); status.Size=new Size(322,30);
    status.Text="Choose a window, pet, and rope length."; Controls.Add(status);
    RefreshWindows();
  }
  void RefreshWindows() {
    IntPtr previous=windows.SelectedItem is Choice ? ((Choice)windows.SelectedItem).Handle : IntPtr.Zero;
    windows.Items.Clear();
    Win.EnumWindows((h,p)=>{
      if (!Win.IsWindowVisible(h)||Win.IsIconic(h)) return true;
      var title=new StringBuilder(256); Win.GetWindowText(h,title,title.Capacity);
      if (title.Length==0) return true;
      uint pid; Win.GetWindowThreadProcessId(h,out pid);
      if (pid==(uint)Process.GetCurrentProcess().Id) return true;
      string process;
      try { process=Process.GetProcessById((int)pid).ProcessName; } catch { return true; }
      windows.Items.Add(new Choice{Handle=h,Name=title+"  ("+process+")"});
      return true;
    },IntPtr.Zero);
    for(int i=0;i<windows.Items.Count;i++)
      if(((Choice)windows.Items[i]).Handle==previous) { windows.SelectedIndex=i; break; }
    if(windows.SelectedIndex<0)
      for(int i=0;i<windows.Items.Count;i++)
        if(windows.Items[i].ToString().IndexOf("ChatGPT",StringComparison.OrdinalIgnoreCase)>=0)
          { windows.SelectedIndex=i; break; }
    if(windows.SelectedIndex<0 && windows.Items.Count>0) windows.SelectedIndex=0;
  }
  void AttachClick(object sender,EventArgs e) {
    if(overlay!=null) { overlay.Close(); return; }
    var choice=windows.SelectedItem as Choice;
    if(choice==null||!Win.IsWindow(choice.Handle)) { status.Text="Refresh and choose an open window."; return; }
    overlay=new PetForm(choice.Handle,pets.SelectedIndex,(int)rope.Value);
    overlay.FormClosed+=(s,a)=>{overlay=null;attach.Text="Attach";status.Text="Detached.";};
    overlay.Show(); attach.Text="Detach"; status.Text="Attached. Click the pet to poke it.";
  }
  protected override void OnFormClosed(FormClosedEventArgs e) { if(overlay!=null) overlay.Close(); base.OnFormClosed(e); }
}
class PetForm : Form {
  const int AX=220,AY=46;
  IntPtr target; int pet,length;
  Timer timer=new Timer();
  double angle,velocity,lastX=double.NaN,poke;
  DateTime lastTick=DateTime.UtcNow;
  PointF center;
  Color key=Color.FromArgb(1,2,3);
  bool first=true;
  public PetForm(IntPtr h,int preset,int ropeLength) {
    target=h;pet=preset;length=ropeLength;
    FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
    StartPosition=FormStartPosition.Manual;ClientSize=new Size(440,390);
    BackColor=key;TransparencyKey=key;DoubleBuffered=true;
    timer.Interval=16;timer.Tick+=Tick;
    MouseDown+=(s,e)=>{
      if(e.Button==MouseButtons.Left&&HitPet(e.Location))
        {velocity+=angle<=0?2.4:-2.4;poke=1;timer.Interval=16;}
    };
    Shown+=(s,e)=>{UpdateTarget();timer.Start();};
    FormClosed+=(s,e)=>timer.Dispose();
  }
  void Tick(object sender,EventArgs e) {
    if(!Win.IsWindow(target)){Close();return;}
    if(Win.IsIconic(target)||!Win.IsWindowVisible(target)){if(Visible)Hide();lastX=double.NaN;return;}
    var now=DateTime.UtcNow;
    double dt=Math.Min(.05,Math.Max(.001,(now-lastTick).TotalSeconds));lastTick=now;
    double old=angle,oldPoke=poke;
    bool moved=UpdateTarget();
    velocity+=(-14*Math.Sin(angle)-3.3*velocity)*dt;
    angle=Math.Max(-.82,Math.Min(.82,angle+velocity*dt));
    poke=Math.Max(0,poke-3.8*dt);
    center=new PointF((float)(AX+length*Math.Sin(angle)),(float)(AY+length*Math.Cos(angle)+29));
    if(moved||Math.Abs(old-angle)>.001||Math.Abs(oldPoke-poke)>.001)Invalidate();
    timer.Interval=Math.Abs(angle)<.003&&Math.Abs(velocity)<.015&&poke==0?66:16;
    if(!Visible)Show();
  }
  bool UpdateTarget() {
    Win.Rect r;if(!Win.GetWindowRect(target,out r))return false;
    double x=r.Left+36,y=r.Top+4;
    bool moved=first||x!=lastX;
    if(!double.IsNaN(lastX))velocity-=Math.Max(-120,Math.Min(120,x-lastX))*.025;
    lastX=x;
    int left=(int)x-AX,top=(int)y-AY;
    if(Left!=left||Top!=top||first){SetDesktopLocation(left,top);first=false;moved=true;}
    return moved;
  }
  bool HitPet(Point p) {
    double dx=p.X-center.X,dy=p.Y-center.Y;
    return dx*dx/(47*47.0)+dy*dy/(50*50.0)<1;
  }
  protected override void OnPaint(PaintEventArgs e) {
    base.OnPaint(e);
    var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
    Color fill=pet==1?Color.FromArgb(225,177,106):pet==2?Color.FromArgb(145,222,199):Color.FromArgb(213,169,229);
    Color edge=pet==1?Color.FromArgb(110,72,46):pet==2?Color.FromArgb(52,106,98):Color.FromArgb(103,70,119);
    float endX=(float)(AX+length*Math.Sin(angle)),endY=(float)(AY+length*Math.Cos(angle));
    using(var pen=new Pen(Color.FromArgb(92,100,117),2))g.DrawLine(pen,AX,AY,endX,endY);
    using(var b=new SolidBrush(Color.FromArgb(92,100,117)))g.FillEllipse(b,AX-6,AY-6,12,12);
    var save=g.Save();g.TranslateTransform(center.X,center.Y);
    g.RotateTransform((float)(angle*180/Math.PI*.65));
    float squash=(float)(1-.12*poke);g.ScaleTransform(1/squash,squash);
    using(var b=new SolidBrush(fill)) {
      if(pet==0) {
        g.FillPolygon(b,new[]{new PointF(-38,-26),new PointF(-28,-63),new PointF(-10,-35)});
        g.FillPolygon(b,new[]{new PointF(10,-35),new PointF(28,-63),new PointF(38,-26)});
      } else if(pet==1) {g.FillEllipse(b,-43,-49,26,26);g.FillEllipse(b,17,-49,26,26);}
      g.FillEllipse(b,-43,-39,86,81);
    }
    using(var pen=new Pen(edge,2))g.DrawEllipse(pen,-43,-39,86,81);
    using(var b=new SolidBrush(edge)){g.FillEllipse(b,-23,-6,7,11);g.FillEllipse(b,16,-6,7,11);}
    using(var b=new SolidBrush(Color.FromArgb(232,115,145)))g.FillEllipse(b,-7,13,14,8);
    g.Restore(save);
  }
}
