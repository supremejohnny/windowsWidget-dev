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
      float factor=.82f;
      point[i].X-=dx*factor;point[i].Y-=dy*factor;
      previous[i].X-=dx*factor;previous[i].Y-=dy*factor;
    }
  }
  public void Poke(float horizontal) {
    point[Segments].X+=horizontal;
    point[Segments].Y+=3;
  }
  public void Step(float dt,float anchorX,float anchorY) {
    dt=Math.Max(.008f,Math.Min(.033f,dt));
    for(int i=1;i<=Segments;i++) {
      float vx=Math.Max(-10,Math.Min(10,(point[i].X-previous[i].X)*.91f));
      float vy=Math.Max(-10,Math.Min(10,(point[i].Y-previous[i].Y)*.91f));
      previous[i]=point[i];
      point[i].X+=vx;
      point[i].Y+=vy+980*dt*dt*(i==Segments?1.7f:1f);
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
          float left=i==Segments-1?.78f:.5f;
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
