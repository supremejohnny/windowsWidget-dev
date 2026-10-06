using System;
using System.Drawing;

// A small position-based rope. The node and distance-constraint approach was
// evaluated against jongallant/Unity-Verlet-Rope (MIT), then adapted to avoid Unity.
class RopePhysics {
  const int Segments=8;
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
      point[i].X-=dx;point[i].Y-=dy;
      previous[i].X-=dx;previous[i].Y-=dy;
    }
  }
  public void Poke(float horizontal) {
    point[Segments].X+=horizontal;
    point[Segments].Y+=7;
  }
  public void Step(float dt,float anchorX,float anchorY) {
    dt=Math.Max(.008f,Math.Min(.033f,dt));
    for(int i=1;i<=Segments;i++) {
      float vx=Math.Max(-12,Math.Min(12,(point[i].X-previous[i].X)*.94f));
      float vy=Math.Max(-12,Math.Min(12,(point[i].Y-previous[i].Y)*.94f));
      previous[i]=point[i];
      point[i].X+=vx;
      point[i].Y+=vy+1050*dt*dt*(i==Segments?1.4f:1f);
    }
    for(int iteration=0;iteration<12;iteration++) {
      point[0]=new PointF(anchorX,anchorY);
      for(int i=0;i<Segments;i++) {
        float dx=point[i+1].X-point[i].X,dy=point[i+1].Y-point[i].Y;
        float distance=(float)Math.Sqrt(dx*dx+dy*dy);
        if(distance<.0001f)continue;
        float error=(distance-segmentLength)/distance;
        if(i==0) { point[1].X-=dx*error;point[1].Y-=dy*error; }
        else {
          point[i].X+=dx*error*.5f;point[i].Y+=dy*error*.5f;
          point[i+1].X-=dx*error*.5f;point[i+1].Y-=dy*error*.5f;
        }
      }
    }
    point[0]=new PointF(anchorX,anchorY);
  }
  public bool IsSettled(float anchorX,float anchorY) {
    for(int i=1;i<=Segments;i++) {
      if(Math.Abs(point[i].X-anchorX)>.4f ||
         Math.Abs(point[i].Y-(anchorY+i*segmentLength))>.4f ||
         Math.Abs(point[i].X-previous[i].X)>.15f ||
         Math.Abs(point[i].Y-previous[i].Y)>.15f) return false;
    }
    return true;
  }
}
