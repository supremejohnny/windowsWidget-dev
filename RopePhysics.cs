using System;
using System.Drawing;

// A light, tension-only tether. Only the pet endpoint has inertia; the twelve
// drawing segments take their shape from its distance to the pinned anchor.
class RopePhysics {
  public const int Segments=12;
  const float FixedStep=1f/120;
  const int ShapeSamples=64;
  static readonly float MotionBlend=1-(float)Math.Exp(-14*FixedStep);
  static readonly float SwingDamping=1-(float)Math.Exp(-3.2*FixedStep);
  readonly PointF[] point=new PointF[Segments+1];
  readonly PointF[] shape=new PointF[ShapeSamples+1];
  readonly float[] arc=new float[ShapeSamples+1];
  readonly float length;
  float accumulator,x,y,vx,vy,pendingX,pendingY;
  float anchorVelocityX,anchorVelocityY,accelerationX,accelerationY;
  public PointF End { get { return point[Segments]; } }
  public PointF[] Points { get { return point; } }
  public RopePhysics(int ropeLength,float anchorX,float anchorY) {
    length=ropeLength;y=length;
    BuildShape(anchorX,anchorY);
  }
  public void ShiftAnchor(float dx,float dy) {
    // Input belongs to the anchor, rather than giving each rope segment its
    // own displacement and velocity. Constant-speed motion adds no new force.
    pendingX+=dx;pendingY+=dy;
  }
  public void Poke(float horizontal) { vx+=horizontal*10;vy-=14; }
  public void Step(float dt,float anchorX,float anchorY) {
    dt=Math.Max(.001f,Math.Min(.05f,dt));
    accumulator+=dt;
    float inputX=pendingX/accumulator,inputY=pendingY/accumulator,consumed=0;
    while(accumulator>=FixedStep) {
      Integrate(inputX,inputY);
      accumulator-=FixedStep;consumed+=FixedStep;
    }
    // Keep input belonging to an unfinished fixed step, even at >120 Hz.
    pendingX-=inputX*consumed;pendingY-=inputY*consumed;
    BuildShape(anchorX,anchorY);
  }
  void Integrate(float inputX,float inputY) {
    // Smooth irregular native move events before deriving anchor acceleration.
    float nextX=anchorVelocityX+Math.Max(-1200*FixedStep,Math.Min(1200*FixedStep,(inputX-anchorVelocityX)*MotionBlend));
    float nextY=anchorVelocityY+Math.Max(-1600*FixedStep,Math.Min(1600*FixedStep,(inputY-anchorVelocityY)*MotionBlend));
    float ax=(nextX-anchorVelocityX)/FixedStep;
    float ay=(nextY-anchorVelocityY)/FixedStep;
    anchorVelocityX=nextX;anchorVelocityY=nextY;
    accelerationX+=(ax-accelerationX)*MotionBlend;
    accelerationY+=(ay-accelerationY)*MotionBlend;
    vx-=accelerationX*FixedStep;
    vy+=(980-accelerationY)*FixedStep;

    // Damp the swing relative to the anchor, rather than dragging the rope
    // through screen space. The cord can pull the pet, but cannot push it.
    float radius=(float)Math.Sqrt(x*x+y*y);
    if(radius>.001f) {
      float nx=x/radius,ny=y/radius,radial=vx*nx+vy*ny;
      vx-=(vx-radial*nx)*SwingDamping;vy-=(vy-radial*ny)*SwingDamping;
    }
    vx=Math.Max(-240,Math.Min(240,vx));
    vy=Math.Max(-120,Math.Min(300,vy));
    x+=vx*FixedStep;y+=vy*FixedStep;
    radius=(float)Math.Sqrt(x*x+y*y);
    if(radius>length) {
      float nx=x/radius,ny=y/radius;
      x=nx*length;y=ny*length;
      float outward=vx*nx+vy*ny;
      if(outward>0){vx-=outward*nx;vy-=outward*ny;}
    }
  }
  float SampleCurve(float depth,float bendX,float bendY) {
    float total=0;shape[0]=new PointF(0,0);arc[0]=0;
    for(int i=1;i<=ShapeSamples;i++) {
      float t=i/(float)ShapeSamples,bend=4*t*(1-t)*depth;
      // One smooth bow across the chord. Keeping its side consistent avoids
      // a sudden shape flip when the pet crosses beneath the anchor.
      shape[i]=new PointF(x*t+bendX*bend,y*t+bendY*bend);
      float dx=shape[i].X-shape[i-1].X,dy=shape[i].Y-shape[i-1].Y;
      total+=(float)Math.Sqrt(dx*dx+dy*dy);arc[i]=total;
    }
    return total;
  }
  void BuildShape(float anchorX,float anchorY) {
    float distance=(float)Math.Sqrt(x*x+y*y);
    if(length-distance<.001f) {
      for(int i=0;i<=Segments;i++) {
        float t=i/(float)Segments;
        point[i]=new PointF(anchorX+x*t,anchorY+y*t);
      }
    } else {
      float bendX=distance>.001f?-y/distance:-1,bendY=distance>.001f?x/distance:0;
      float low=0,high=length;
      for(int i=0;i<12;i++) {
        float mid=(low+high)/2;
        if(SampleCurve(mid,bendX,bendY)<length)low=mid;else high=mid;
      }
      float total=SampleCurve((low+high)/2,bendX,bendY);int sample=1;
      for(int i=0;i<=Segments;i++) {
        float along=total*i/Segments;
        while(sample<ShapeSamples&&arc[sample]<along)sample++;
        float span=arc[sample]-arc[sample-1];
        float t=span>.00001f?(along-arc[sample-1])/span:0;
        point[i]=new PointF(anchorX+shape[sample-1].X+(shape[sample].X-shape[sample-1].X)*t,
          anchorY+shape[sample-1].Y+(shape[sample].Y-shape[sample-1].Y)*t);
      }
    }
    point[0]=new PointF(anchorX,anchorY);
    point[Segments]=new PointF(anchorX+x,anchorY+y);
  }
  public bool IsSettled(float anchorX,float anchorY) {
    return Math.Abs(x)<1.5f&&Math.Abs(y-length)<.5f&&Math.Abs(vx)<3&&Math.Abs(vy)<3&&
      Math.Abs(accelerationX)<4&&Math.Abs(accelerationY)<4&&
      Math.Abs(anchorVelocityX)<2&&Math.Abs(anchorVelocityY)<2&&
      Math.Abs(pendingX)<.001f&&Math.Abs(pendingY)<.001f;
  }
}
