using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Windows.Forms;

static class Win {
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
  public delegate bool EnumProc(IntPtr h,IntPtr p);
  public delegate void EventProc(IntPtr hook,uint evt,IntPtr h,int obj,int child,uint thread,uint time);
  [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,EventProc callback,uint process,uint thread,uint flags);
  [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
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
  ComboBox windows=new ComboBox(), pets=new ComboBox(), corners=new ComboBox(), lengths=new ComboBox();
  static readonly int[] LengthPixels={55,85,130,190};
  Button attach=new Button();
  Label status=new Label();
  PetForm overlay;
  public SettingsForm() {
    Text="Window Pet"; ClientSize=new Size(350,270); FormBorderStyle=FormBorderStyle.FixedDialog;
    MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
    Controls.Add(new Label{Text="Window",Location=new Point(16,18),AutoSize=true});
    windows.Location=new Point(16,40); windows.Size=new Size(258,25);
    windows.DropDownStyle=ComboBoxStyle.DropDownList; Controls.Add(windows);
    var refresh=new Button{Text="Refresh",Location=new Point(280,39),Size=new Size(58,27)};
    refresh.Click+=(s,e)=>RefreshWindows(); Controls.Add(refresh);
    Controls.Add(new Label{Text="Pet",Location=new Point(16,80),AutoSize=true});
    pets.Location=new Point(16,101); pets.Size=new Size(155,25);
    pets.DropDownStyle=ComboBoxStyle.DropDownList;
    pets.Items.AddRange(new object[]{"Lilac cat","Honey bear","Mint ghost","amiya_test1"}); pets.SelectedIndex=0;
    Controls.Add(pets);
    Controls.Add(new Label{Text="Position",Location=new Point(190,80),AutoSize=true});
    corners.Location=new Point(190,101);corners.Size=new Size(148,25);
    corners.DropDownStyle=ComboBoxStyle.DropDownList;
    corners.Items.AddRange(new object[]{"左上","右上","左下","右下"});corners.SelectedIndex=0;Controls.Add(corners);
    Controls.Add(new Label{Text="Rope length",Location=new Point(16,139),AutoSize=true});
    lengths.Location=new Point(16,160);lengths.Size=new Size(322,25);
    lengths.DropDownStyle=ComboBoxStyle.DropDownList;
    lengths.Items.AddRange(new object[]{"短 (55 px)","中 (85 px)","长 (130 px)","超长 (190 px)"});
    lengths.SelectedIndex=1;Controls.Add(lengths);
    attach.Text="Attach"; attach.Location=new Point(16,200); attach.Size=new Size(322,32);
    attach.Click+=AttachClick; Controls.Add(attach);
    status.Location=new Point(16,240); status.Size=new Size(322,26);
    status.Text="Choose a window, corner, pet, and length."; Controls.Add(status);
    var about=new LinkLabel{Text="0.0.1-pre / About",AutoSize=true,Location=new Point(224,7)};
    about.LinkClicked+=(s,e)=>ShowAbout();Controls.Add(about);
    RefreshWindows();
  }
  void ShowAbout() {
    using(var dialog=new Form()) {
      dialog.Text="Window Pet 0.0.1-pre";dialog.Size=new Size(560,420);
      dialog.StartPosition=FormStartPosition.CenterParent;
      var notice=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Text=PetAssets.Notices()};
      dialog.Controls.Add(notice);dialog.ShowDialog(this);
    }
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
    overlay=new PetForm(choice.Handle,pets.SelectedIndex,LengthPixels[lengths.SelectedIndex],corners.SelectedIndex);
    overlay.FormClosed+=(s,a)=>{overlay=null;attach.Text="Attach";status.Text="Detached.";};
    Win.SetForegroundWindow(choice.Handle);
    overlay.Show(); attach.Text="Detach"; status.Text="Attached. Click the pet to poke it.";
  }
  protected override void OnFormClosed(FormClosedEventArgs e) { if(overlay!=null) overlay.Close(); base.OnFormClosed(e); }
}
class PetForm : Form {
  internal const int AX=300,AY=52;
  internal const int CanvasWidth=600,CanvasHeight=440;
  IntPtr target; int pet,length,corner;
  Timer timer=new Timer();
  RopePhysics rope;
  PetBodyPhysics body;
  HitForm hit,anchorHit;
  bool stowed;
  PointF[] fromPoints=new PointF[RopePhysics.Segments+1],drawPoints=new PointF[RopePhysics.Segments+1],stowedPoints=new PointF[RopePhysics.Segments+1];
  LayeredSurface surface;
  Image petImage;
  Win.EventProc locationCallback;
  IntPtr locationHook;
  bool syncingLocation;
  bool physicsWake;
  internal int LocationEvents { get; private set; }
  readonly Stopwatch clock=Stopwatch.StartNew();
  double transition=1;
  int physicsSteps;
  double lastX=double.NaN,lastY=double.NaN,poke;
  double lastTick;
  PointF center;
  bool first=true;
  public PetForm(IntPtr h,int preset,int ropeLength,int targetCorner) {
    target=h;pet=preset;length=ropeLength;corner=targetCorner;
    for(int i=0;i<stowedPoints.Length;i++) {
      float t=i/(float)(stowedPoints.Length-1);
      stowedPoints[i]=new PointF(AX,AY+18*t);
    }
    rope=new RopePhysics(length,AX,AY);
    FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;
    StartPosition=FormStartPosition.Manual;ClientSize=new Size(CanvasWidth,CanvasHeight);
    surface=new LayeredSurface(CanvasWidth,CanvasHeight);
    if(pet==3)petImage=PetAssets.LoadAmiya();
    body=petImage!=null?PetBodyPhysics.FromImage((Bitmap)petImage,160,148):new PetBodyPhysics(0,63,6500);
    timer.Interval=16;timer.Tick+=Tick;
    hit=new HitForm(pet==3?160:86,pet==3?148:81,
      ()=>{rope.Poke(center.X<=AX?7:-7);poke=.42;timer.Interval=16;});
    anchorHit=new HitForm(20,20,ToggleStowed);
    // Position and initialize the pet before the first native window is shown.
    UpdateTarget();
    center=new PointF(AX,AY+length);
    Array.Copy(rope.Points,drawPoints,drawPoints.Length);
    Shown+=(s,e)=>{
      RenderFrame();SyncHit();hit.Show();anchorHit.Show();KeepAboveTarget();
      uint process;Win.GetWindowThreadProcessId(target,out process);
      locationCallback=OnTargetLocation;
      locationHook=Win.SetWinEventHook(0x800B,0x800B,IntPtr.Zero,locationCallback,process,0,0);
      timer.Start();
    };
  }
  void OnTargetLocation(IntPtr hook,uint evt,IntPtr h,int obj,int child,uint thread,uint time) {
    if(h!=target||obj!=0||child!=0||IsDisposed||Disposing||syncingLocation)return;
    syncingLocation=true;
    try {
      LocationEvents++;
      if(Win.IsIconic(target)||!Win.IsWindowVisible(target))return;
      if(UpdateTarget()) {
        SyncHit();
        if(!stowed){physicsWake=true;timer.Interval=16;}
      }
    } finally {syncingLocation=false;}
  }
  protected override void Dispose(bool disposing) {
    if(disposing) {
      if(locationHook!=IntPtr.Zero){Win.UnhookWinEvent(locationHook);locationHook=IntPtr.Zero;}
      timer.Dispose();
      if(surface!=null){surface.Dispose();surface=null;}
      if(petImage!=null){petImage.Dispose();petImage=null;}
      if(hit!=null)hit.Dispose();
      if(anchorHit!=null)anchorHit.Dispose();
    }
    base.Dispose(disposing);
  }
  protected override bool ShowWithoutActivation { get { return true; } }
  protected override CreateParams CreateParams {
    get { var p=base.CreateParams;p.ExStyle|=0x08080020;return p; } // NOACTIVATE | LAYERED | TRANSPARENT
  }
  internal IntPtr HitHandle { get { return hit.Handle; } }
  internal Region HitRegion { get { return hit.Region; } }
  internal IntPtr AnchorHandle { get { return anchorHit.Handle; } }
  internal bool IsStowed { get { return stowed; } }
  internal int PhysicsSteps { get { return physicsSteps; } }
  internal PointF[] StowedPoints { get { return stowedPoints; } }
  internal PointF[] RenderedRopePoints { get { return drawPoints; } }
  internal double BodyAngle { get { return body.Angle; } }
  internal double BodyRestAngle { get { return body.RestAngle; } }
  internal PointF BodyCenterOfMass { get { return body.RotatedCenter; } }
  void ToggleStowed() {
    Array.Copy(drawPoints,fromPoints,drawPoints.Length);
    stowed=!stowed;transition=0;
    if(!stowed)rope=new RopePhysics(length,AX,AY);
    body.ResetMotion();
    if(stowed)hit.Hide();
    timer.Interval=16;RenderFrame();
  }
  void Tick(object sender,EventArgs e) {
    if(!Win.IsWindow(target)){Close();return;}
    if(Win.IsIconic(target)||!Win.IsWindowVisible(target)) {
      if(Visible)Hide();if(hit.Visible)hit.Hide();if(anchorHit.Visible)anchorHit.Hide();lastX=lastY=double.NaN;
      body.ResetMotion();
      lastTick=clock.Elapsed.TotalSeconds;timer.Interval=66;return;
    }
    double now=clock.Elapsed.TotalSeconds;
    double dt=Math.Min(.05,Math.Max(.001,now-lastTick));lastTick=now;
    double oldPoke=poke;bool wasTransitioning=transition<1;
    bool moved=UpdateTarget()||physicsWake;physicsWake=false;
    bool sleeping=stowed||(!moved&&transition==1&&poke==0&&rope.IsSettled(AX,AY)&&body.IsSettled);
    if(!sleeping){rope.Step((float)dt,AX,AY);physicsSteps++;}
    transition=Math.Min(1,transition+dt*3.5);
    float blend=(float)(transition*transition*(3-2*transition));
    for(int i=0;i<drawPoints.Length;i++) {
      PointF end=stowed?stowedPoints[i]:rope.Points[i],start=transition<1?fromPoints[i]:end;
      drawPoints[i]=new PointF(start.X+(end.X-start.X)*blend,start.Y+(end.Y-start.Y)*blend);
    }
    poke=Math.Max(0,poke-3.8*dt);
    drawPoints[0]=new PointF(AX,AY); // Anchor is never part of an interpolated or simulated transform.
    center=drawPoints[drawPoints.Length-1];
    if(!stowed)body.Update(dt,Left+center.X,Top+center.Y);
    if(moved||wasTransitioning||!sleeping||oldPoke!=poke)RenderFrame();
    timer.Interval=transition==1&&(stowed||(rope.IsSettled(AX,AY)&&body.IsSettled&&poke==0))?66:16;
    if(!Visible) Show();
    SyncHit();
    if(!anchorHit.Visible)anchorHit.Show();
    if(!stowed && transition>.7 && !hit.Visible)hit.Show();
    KeepAboveTarget();
  }
  void SyncHit() {
    PointF mass=body.RotatedCenter;
    hit.SetDesktopLocation(Left+(int)Math.Round(center.X+mass.X)-(pet==3?80:43),
      Top+(int)Math.Round(center.Y+mass.Y)-(pet==3?74:40));
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
    double x=(corner==1||corner==3)?r.Right-36:r.Left+36;
    double y=(corner==2||corner==3)?r.Bottom-4:r.Top+4;
    bool moved=first||x!=lastX||y!=lastY;
    if(!double.IsNaN(lastX)) {
      double dx=x-lastX,dy=y-lastY;
      if(!stowed) {
        if(Math.Abs(dx)>250||Math.Abs(dy)>250) {rope=new RopePhysics(length,AX,AY);transition=1;}
        else rope.ShiftAnchor((float)dx,(float)dy);
      }
    }
    lastX=x;lastY=y;
    int left=(int)x-AX,top=(int)y-AY;
    if(Left!=left||Top!=top||first){SetDesktopLocation(left,top);first=false;moved=true;}
    return moved;
  }
  internal void RenderFrame() { surface.Present(Handle,Left,Top,DrawScene); }
  internal void DrawScene(Graphics g) {
    g.SmoothingMode=SmoothingMode.AntiAlias;
    g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    Color fill=pet==1?Color.FromArgb(225,177,106):pet==2?Color.FromArgb(145,222,199):Color.FromArgb(213,169,229);
    Color edge=pet==1?Color.FromArgb(110,72,46):pet==2?Color.FromArgb(52,106,98):Color.FromArgb(103,70,119);
    var nodes=drawPoints;
    using(var path=new GraphicsPath()) {
      for(int i=0;i<nodes.Length-1;i++) {
        PointF p0=nodes[Math.Max(0,i-1)],p1=nodes[i],p2=nodes[i+1],p3=nodes[Math.Min(nodes.Length-1,i+2)];
        path.AddBezier(p1,new PointF(p1.X+(p2.X-p0.X)/6,p1.Y+(p2.Y-p0.Y)/6),
          new PointF(p2.X-(p3.X-p1.X)/6,p2.Y-(p3.Y-p1.Y)/6),p2);
      }
      using(var outline=new Pen(Color.FromArgb(225,64,74,89),2.8f))
      using(var core=new Pen(Color.FromArgb(245,180,189,202),1.2f)) {
        outline.StartCap=outline.EndCap=core.StartCap=core.EndCap=LineCap.Round;
        outline.LineJoin=core.LineJoin=LineJoin.Round;
        g.DrawPath(outline,path);g.DrawPath(core,path);
      }
    }
    using(var shadow=new SolidBrush(Color.FromArgb(45,22,28,38)))g.FillEllipse(shadow,AX-6,AY-5,12,12);
    using(var ring=new SolidBrush(Color.FromArgb(255,75,86,102)))g.FillEllipse(ring,AX-5.5f,AY-5.5f,11,11);
    using(var inset=new SolidBrush(Color.FromArgb(255,206,216,227)))g.FillEllipse(inset,AX-3.5f,AY-3.5f,7,7);
    using(var pin=new SolidBrush(Color.FromArgb(255,89,104,122)))g.FillEllipse(pin,AX-1.5f,AY-1.5f,3,3);
    if(stowed)return;
    var save=g.Save();g.TranslateTransform(center.X,center.Y);
    g.RotateTransform((float)(body.Angle*180/Math.PI));
    float squash=(float)(1-.12*poke);g.ScaleTransform(1/squash,squash);
    if(petImage!=null) {
      g.InterpolationMode=InterpolationMode.HighQualityBicubic;
      g.DrawImage(petImage,new RectangleF(-80,0,160,148));
      g.Restore(save);return;
    }
    g.TranslateTransform(0,63);
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
