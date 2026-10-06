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
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
  [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h,uint command);
  [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point point);
  [DllImport("user32.dll",EntryPoint="GetWindowLongW")] public static extern int GetWindowLong(IntPtr h,int index);
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
    Win.SetForegroundWindow(choice.Handle);
    overlay.Show(); attach.Text="Detach"; status.Text="Attached. Click the pet to poke it.";
  }
  protected override void OnFormClosed(FormClosedEventArgs e) { if(overlay!=null) overlay.Close(); base.OnFormClosed(e); }
}
class PetForm : Form {
  const int AX=260,AY=52,SECOND_X=304;
  IntPtr target; int pet,length;
  Timer timer=new Timer();
  RopePhysics rope;
  HitForm hit,anchorHit;
  bool stowed;
  PointF[] fromPoints,drawPoints=new PointF[9];
  double transition=1;
  double lastX=double.NaN,lastY=double.NaN,poke;
  DateTime lastTick=DateTime.UtcNow;
  PointF center;
  Color key=Color.FromArgb(1,2,3);
  bool first=true;
  public PetForm(IntPtr h,int preset,int ropeLength) {
    target=h;pet=preset;length=ropeLength;
    rope=new RopePhysics(length,AX,AY);
    FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;
    StartPosition=FormStartPosition.Manual;ClientSize=new Size(520,420);
    BackColor=key;TransparencyKey=key;DoubleBuffered=true;
    timer.Interval=16;timer.Tick+=Tick;
    hit=new HitForm(86,81,()=>{rope.Poke(center.X<=AX?11:-11);poke=.55;timer.Interval=16;});
    anchorHit=new HitForm(20,20,ToggleStowed);
    // Position and initialize the pet before the first native window is shown.
    UpdateTarget();
    center=new PointF(AX,AY+length+29);
    Array.Copy(rope.Points,drawPoints,drawPoints.Length);
    Shown+=(s,e)=>{SyncHit();hit.Show();anchorHit.Show();KeepAboveTarget();timer.Start();};
    FormClosed+=(s,e)=>{timer.Dispose();hit.Close();hit.Dispose();anchorHit.Close();anchorHit.Dispose();};
  }
  protected override bool ShowWithoutActivation { get { return true; } }
  protected override CreateParams CreateParams {
    get { var p=base.CreateParams;p.ExStyle|=0x08000020;return p; } // NOACTIVATE | TRANSPARENT
  }
  internal IntPtr HitHandle { get { return hit.Handle; } }
  internal Region HitRegion { get { return hit.Region; } }
  internal IntPtr AnchorHandle { get { return anchorHit.Handle; } }
  internal bool IsStowed { get { return stowed; } }
  void ToggleStowed() {
    fromPoints=(PointF[])drawPoints.Clone();
    stowed=!stowed;transition=0;
    rope=stowed?new RopePhysics(length,AX,AY,SECOND_X,AY):new RopePhysics(length,AX,AY);
    if(stowed)hit.Hide();
    timer.Interval=16;Invalidate();
  }
  void Tick(object sender,EventArgs e) {
    if(!Win.IsWindow(target)){Close();return;}
    if(Win.IsIconic(target)||!Win.IsWindowVisible(target)) {
      if(Visible)Hide();if(hit.Visible)hit.Hide();if(anchorHit.Visible)anchorHit.Hide();lastX=lastY=double.NaN;
      lastTick=DateTime.UtcNow;timer.Interval=66;return;
    }
    var now=DateTime.UtcNow;
    double dt=Math.Min(.05,Math.Max(.001,(now-lastTick).TotalSeconds));lastTick=now;
    float oldX=rope.End.X,oldY=rope.End.Y;double oldPoke=poke;
    bool moved=UpdateTarget();
    bool sleeping=!moved&&transition==1&&poke==0&&rope.IsSettled(AX,AY);
    if(!sleeping)rope.Step((float)dt,AX,AY);
    transition=Math.Min(1,transition+dt*3.5);
    float blend=(float)(transition*transition*(3-2*transition));
    for(int i=0;i<drawPoints.Length;i++) {
      PointF end=rope.Points[i],start=transition<1?fromPoints[i]:end;
      drawPoints[i]=new PointF(start.X+(end.X-start.X)*blend,start.Y+(end.Y-start.Y)*blend);
    }
    poke=Math.Max(0,poke-3.8*dt);
    center=new PointF(drawPoints[8].X,drawPoints[8].Y+29);
    if(moved||transition<1||Math.Abs(oldX-rope.End.X)>.01||Math.Abs(oldY-rope.End.Y)>.01||oldPoke!=poke)Invalidate();
    timer.Interval=transition==1&&rope.IsSettled(AX,AY)&&poke==0?66:16;
    if(!Visible) Show();
    SyncHit();
    if(!anchorHit.Visible)anchorHit.Show();
    if(!stowed && transition>.7 && !hit.Visible)hit.Show();
    KeepAboveTarget();
  }
  void SyncHit() {
    hit.SetDesktopLocation(Left+(int)Math.Round(center.X)-43,Top+(int)Math.Round(center.Y)-39);
    anchorHit.SetDesktopLocation(Left+AX-10,Top+AY-10);
  }
  void KeepAboveTarget() {
    if(!Visible)return;
    // Keep both layers directly above target and below unrelated windows.
    IntPtr above=VisibleAbove(target);
    while(above==Handle||above==hit.Handle||above==anchorHit.Handle)above=VisibleAbove(above);
    if(VisibleAbove(target)!=Handle||VisibleAbove(Handle)!=(hit.Visible?hit.Handle:anchorHit.Handle)||
       (hit.Visible&&VisibleAbove(hit.Handle)!=anchorHit.Handle)) {
      Win.SetWindowPos(Handle,above,0,0,0,0,0x0013);
      if(hit.Visible)Win.SetWindowPos(hit.Handle,above,0,0,0,0,0x0013);
      Win.SetWindowPos(anchorHit.Handle,above,0,0,0,0,0x0013);
    }
  }
  static IntPtr VisibleAbove(IntPtr h) {
    for(int i=0;i<128;i++) {
      h=Win.GetWindow(h,3);
      if(h==IntPtr.Zero||Win.IsWindowVisible(h))return h;
    }
    return IntPtr.Zero;
  }
  bool UpdateTarget() {
    Win.Rect r;if(!Win.GetWindowRect(target,out r))return false;
    double x=r.Left+36,y=r.Top+4;
    bool moved=first||x!=lastX||y!=lastY;
    if(!double.IsNaN(lastX)) {
      double dx=x-lastX,dy=y-lastY;
      if(Math.Abs(dx)>250||Math.Abs(dy)>250) {rope=stowed?new RopePhysics(length,AX,AY,SECOND_X,AY):new RopePhysics(length,AX,AY);transition=1;}
      else rope.ShiftAnchor((float)dx,(float)dy);
    }
    lastX=x;lastY=y;
    int left=(int)x-AX,top=(int)y-AY;
    if(Left!=left||Top!=top||first){SetDesktopLocation(left,top);first=false;moved=true;}
    return moved;
  }
  protected override void OnPaint(PaintEventArgs e) {
    base.OnPaint(e);
    var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
    Color fill=pet==1?Color.FromArgb(225,177,106):pet==2?Color.FromArgb(145,222,199):Color.FromArgb(213,169,229);
    Color edge=pet==1?Color.FromArgb(110,72,46):pet==2?Color.FromArgb(52,106,98):Color.FromArgb(103,70,119);
    var nodes=drawPoints;
    using(var pen=new Pen(Color.FromArgb(92,100,117),2))
      for(int i=0;i<nodes.Length-1;i++)g.DrawLine(pen,nodes[i],nodes[i+1]);
    using(var b=new SolidBrush(Color.FromArgb(92,100,117))) {
      g.FillEllipse(b,AX-6,AY-6,12,12);
      if(stowed)g.FillEllipse(b,SECOND_X-5,AY-5,10,10);
    }
    if(stowed)return;
    var save=g.Save();g.TranslateTransform(center.X,center.Y);
    var end=nodes[nodes.Length-1];var before=nodes[nodes.Length-2];
    g.RotateTransform((float)(Math.Atan2(end.X-before.X,end.Y-before.Y)*180/Math.PI*.65));
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
class HitForm : Form {
  public HitForm(int width,int height,Action poke) {
    FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;
    StartPosition=FormStartPosition.Manual;ClientSize=new Size(width,height);
    BackColor=Color.White;Opacity=.01;
    using(var path=new GraphicsPath()) {
      path.AddEllipse(new Rectangle(0,0,width,height));
      Region=new Region(path);
    }
    MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left)poke();};
  }
  protected override bool ShowWithoutActivation { get { return true; } }
  protected override CreateParams CreateParams {
    get { var p=base.CreateParams;p.ExStyle|=0x08000000;return p; }
  }
}
