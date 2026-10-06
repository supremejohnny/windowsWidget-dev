using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// A rigid pet hanging from the rope endpoint. All mass properties are measured
// relative to the top-center attachment point in the rendered image.
class PetBodyPhysics {
  public readonly double CenterX,CenterY,MomentPerMass;
  public double Angle { get; private set; } // radians, clockwise in screen coordinates
  public double AngularVelocity { get; private set; }
  public double RestAngle { get { return Math.Atan2(CenterX,CenterY); } }
  public bool IsSettled { get { return Math.Abs(Angle-RestAngle)<.007 && Math.Abs(AngularVelocity)<.025; } }
  bool sampled;
  double priorX,priorY,velocityX,velocityY,accelerationX,accelerationY;

  public PetBodyPhysics(double centerX,double centerY,double momentPerMass) {
    if(centerY<=0||momentPerMass<=0)throw new ArgumentOutOfRangeException("Invalid pet mass distribution");
    CenterX=centerX;CenterY=centerY;MomentPerMass=momentPerMass;
    Angle=RestAngle;
  }
  public static PetBodyPhysics FromImage(Bitmap bitmap,double renderWidth,double renderHeight) {
    if(bitmap==null)throw new ArgumentNullException("bitmap");
    BitmapData data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
    double mass=0,sx=0,sy=0,moment=0;
    try {
      int stride=Math.Abs(data.Stride);
      byte[] pixels=new byte[stride*bitmap.Height];
      Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
      for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++) {
        double weight=pixels[(data.Stride>=0?y:bitmap.Height-1-y)*stride+x*4+3]/255.0;
        if(weight==0)continue;
        double px=((x+.5)/bitmap.Width-.5)*renderWidth;
        double py=(y+.5)/bitmap.Height*renderHeight;
        mass+=weight;sx+=weight*px;sy+=weight*py;
        moment+=weight*(px*px+py*py);
      }
    } finally { bitmap.UnlockBits(data); }
    if(mass<1)throw new ArgumentException("Pet image is transparent", "bitmap");
    return new PetBodyPhysics(sx/mass,sy/mass,moment/mass);
  }
  public void ResetMotion() {
    Angle=RestAngle;AngularVelocity=0;
    sampled=false;velocityX=velocityY=accelerationX=accelerationY=0;
  }
  public void Update(double dt,double pivotX,double pivotY) {
    dt=Math.Max(.001,Math.Min(.05,dt));
    if(!sampled) {
      priorX=pivotX;priorY=pivotY;sampled=true;
    } else {
      double dx=pivotX-priorX,dy=pivotY-priorY;
      priorX=pivotX;priorY=pivotY;
      if(Math.Abs(dx)>250||Math.Abs(dy)>250) {
        velocityX=velocityY=accelerationX=accelerationY=0;
      } else {
        // Smooth target velocity before differentiating it. Native window move
        // events arrive in uneven pixel steps; raw acceleration is too noisy.
        double alpha=1-Math.Exp(-dt*14);
        double nextX=velocityX+alpha*(dx/dt-velocityX);
        double nextY=velocityY+alpha*(dy/dt-velocityY);
        double rawX=Math.Max(-2200,Math.Min(2200,(nextX-velocityX)/dt));
        double rawY=Math.Max(-2200,Math.Min(2200,(nextY-velocityY)/dt));
        velocityX=nextX;velocityY=nextY;
        double accelAlpha=1-Math.Exp(-dt*12);
        accelerationX+=accelAlpha*(rawX-accelerationX);
        accelerationY+=accelAlpha*(rawY-accelerationY);
      }
    }
    double sine=Math.Sin(Angle),cosine=Math.Cos(Angle);
    double rx=CenterX*cosine-CenterY*sine;
    double ry=CenterX*sine+CenterY*cosine;
    double torque=rx*(980-accelerationY)+ry*accelerationX;
    AngularVelocity+=((torque/MomentPerMass)-3.3*AngularVelocity)*dt;
    AngularVelocity=Math.Max(-3,Math.Min(3,AngularVelocity));
    Angle+=AngularVelocity*dt;
    if(Angle<-.65){Angle=-.65;AngularVelocity=Math.Max(0,AngularVelocity);}
    if(Angle>.65){Angle=.65;AngularVelocity=Math.Min(0,AngularVelocity);}
  }
  public PointF RotatedCenter {
    get {
      double s=Math.Sin(Angle),c=Math.Cos(Angle);
      return new PointF((float)(CenterX*c-CenterY*s),(float)(CenterX*s+CenterY*c));
    }
  }
}
