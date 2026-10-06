using System;
using System.Drawing;

// A small position-based rope. The node and distance-constraint approach was
// evaluated against jongallant/Unity-Verlet-Rope (MIT), then adapted to avoid Unity.
class RopePhysics {
  public const int Segments=12;
  const float FixedStep=1f/120;
  float accumulator;
  readonly PointF[] point=new PointF[Segments+1];
  readonly PointF[] previous=new PointF[Segments+1];
  readonly float segmentLength;
  public PointF End { get { return point[Segments]; } }
  public PointF[] Points { get { return point; } }
  public RopePhysics(int length,float anchorX,float anchorY) {
    segmentLength=length/(float)Segments;
    for(int i=0;i<=Segments;i++) point[i]=previous[i]=new PointF(anchorX,anchorY+i*segmentLength);
  }
  public void ShiftAnchor(float dx,float dy) {
    // The overlay moves with the target. Rebase free particles into its new coordinates.
    for(int i=1;i<=Segments;i++) {
      float factor=.82f;
      float verticalFactor=dy<0?.22f:.4f;
      point[i].X-=dx*factor;point[i].Y-=dy*verticalFactor;
      previous[i].X-=dx*factor;previous[i].Y-=dy*verticalFactor;
    }
  }
  public void Poke(float horizontal) {
    previous[Segments].X-=horizontal*.28f;
    previous[Segments].Y-=.7f;
  }
  public void Step(float dt,float anchorX,float anchorY) {
    accumulator+=Math.Max(0,Math.Min(.05f,dt));
    while(accumulator>=FixedStep) {
      Integrate(anchorX,anchorY);
      accumulator-=FixedStep;
    }
    point[0]=previous[0]=new PointF(anchorX,anchorY);
  }
  void Integrate(float anchorX,float anchorY) {
    for(int i=1;i<=Segments;i++) {
      float vx=Math.Max(-4,Math.Min(4,(point[i].X-previous[i].X)*.973f));
      float vy=Math.Max(-1.2f,Math.Min(3,(point[i].Y-previous[i].Y)*.91f));
      previous[i]=point[i];
      point[i].X+=vx;
      point[i].Y+=vy+980*FixedStep*FixedStep;
    }
    for(int iteration=0;iteration<48;iteration++) {
      point[0]=new PointF(anchorX,anchorY);
      for(int n=0;n<Segments;n++) {
        int i=iteration%2==0?n:Segments-1-n;
        float dx=point[i+1].X-point[i].X,dy=point[i+1].Y-point[i].Y;
        float distance=(float)Math.Sqrt(dx*dx+dy*dy);
        if(distance<.0001f)continue;
        float error=(distance-segmentLength)/distance;
        if(i==0) { point[1].X-=dx*error;point[1].Y-=dy*error; }
        else {
          float left=i==Segments-1?.8f:.5f;
          point[i].X+=dx*error*left;point[i].Y+=dy*error*left;
          point[i+1].X-=dx*error*(1-left);point[i+1].Y-=dy*error*(1-left);
        }
      }
    }
    point[0]=new PointF(anchorX,anchorY);
  }
  public bool IsSettled(float anchorX,float anchorY) {
    for(int i=1;i<=Segments;i++) {
      if(Math.Abs(point[i].X-anchorX)>2f ||
         Math.Abs(point[i].Y-(anchorY+i*segmentLength))>2f ||
         Math.Abs(point[i].X-previous[i].X)>.15f ||
         Math.Abs(point[i].Y-previous[i].Y)>.15f) return false;
    }
    return true;
  }
}
