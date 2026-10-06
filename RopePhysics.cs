using System;
using System.Drawing;

// Twelve constrained segments with light numerical inertia, but no rope
// gravity. The hanging pet is the only node accelerated by gravity.
class RopePhysics {
  public const int Segments=12;
  const float FixedStep=1f/120;
  float accumulator;
  readonly PointF[] point=new PointF[Segments+1];
  readonly PointF[] previous=new PointF[Segments+1];
  readonly PointF[] velocity=new PointF[Segments+1];
  readonly float segmentLength;
  readonly float petGravity;
  float pendingX,pendingY;
  public PointF End { get { return point[Segments]; } }
  public PointF[] Points { get { return point; } }
  public RopePhysics(int length,float anchorX,float anchorY) : this(length,anchorX,anchorY,980) { }
  internal RopePhysics(int length,float anchorX,float anchorY,float endpointGravity) {
    petGravity=endpointGravity;
    segmentLength=length/(float)Segments;
    for(int i=0;i<=Segments;i++) point[i]=previous[i]=new PointF(anchorX,anchorY+i*segmentLength);
  }
  public void ShiftAnchor(float dx,float dy) {
    // Rebase the moving anchor frame over fixed steps, retaining screen-space
    // inertia. There is no per-segment movement multiplier or added impulse.
    pendingX+=dx;pendingY+=dy;
  }
  public void Poke(float horizontal) {
    previous[Segments].X-=horizontal*.12f;
    previous[Segments].Y-=.12f;
  }
  public void Step(float dt,float anchorX,float anchorY) {
    accumulator+=Math.Max(.001f,Math.Min(.05f,dt));
    float shiftX=pendingX/accumulator*FixedStep,shiftY=pendingY/accumulator*FixedStep;
    while(accumulator>=FixedStep) {
      Integrate(anchorX,anchorY,shiftX,shiftY);
      accumulator-=FixedStep;
      pendingX-=shiftX;pendingY-=shiftY;
    }
    point[0]=previous[0]=new PointF(anchorX,anchorY);
  }
  void Integrate(float anchorX,float anchorY,float shiftX,float shiftY) {
    for(int i=1;i<=Segments;i++) {
      point[i].X-=shiftX;point[i].Y-=shiftY;
      previous[i].X-=shiftX;previous[i].Y-=shiftY;
      // Dissipation is relative to the anchor frame. Uniform window velocity
      // should not keep driving a wave or leave the cord tilted indefinitely.
      float damping=i==Segments?.97f:.93f;
      velocity[i]=new PointF((point[i].X-previous[i].X-shiftX)*damping,
        (point[i].Y-previous[i].Y-shiftY)*damping);
    }
    for(int i=1;i<=Segments;i++) {
      PointF v=velocity[i];
      if(i<Segments) {
        PointF before=i==1?PointF.Empty:velocity[i-1],after=velocity[i+1];
        // Damp short spatial ripples without forcing the node positions onto
        // a straight line. Low-frequency bending remains free to propagate.
        v=new PointF(v.X*.8f+(before.X+after.X)*.1f,v.Y*.8f+(before.Y+after.Y)*.1f);
      }
      float vx=Math.Max(-2.6f,Math.Min(2.6f,v.X));
      float vy=Math.Max(-1.2f,Math.Min(3,v.Y));
      previous[i]=point[i];
      point[i].X+=vx+shiftX;
      point[i].Y+=vy+shiftY;
      if(i==Segments)point[i].Y+=petGravity*FixedStep*FixedStep;
    }
    float cordLength=segmentLength*Segments;
    float chordX=point[Segments].X-anchorX,chordY=point[Segments].Y-anchorY;
    float chord=(float)Math.Sqrt(chordX*chordX+chordY*chordY);
    if(chord>1&&chord<cordLength-.2f) {
      float bow=0;
      for(int i=1;i<Segments;i++)bow=Math.Max(bow,Math.Abs((point[i].X-anchorX)*chordY-(point[i].Y-anchorY)*chordX)/chord);
      // Perfectly collinear compression has no preferred bend direction.
      // Break that numerical symmetry without injecting velocity or gravity.
      if(bow<.02f) {
        int middle=Segments/2;
        point[middle].X-=.1f;previous[middle].X-=.1f;
      }
    }
    for(int iteration=0;iteration<96;iteration++) {
      point[0]=new PointF(anchorX,anchorY);
      // A long-range tension constraint catches abrupt upward anchor moves
      // without waiting for stretch to travel through all twelve segments.
      float ex=point[Segments].X-anchorX,ey=point[Segments].Y-anchorY;
      float reach=(float)Math.Sqrt(ex*ex+ey*ey),length=segmentLength*Segments;
      if(reach>length)point[Segments]=new PointF(anchorX+ex*length/reach,anchorY+ey*length/reach);
      for(int n=0;n<Segments;n++) {
        int i=iteration%2==0?n:Segments-1-n;
        float dx=point[i+1].X-point[i].X,dy=point[i+1].Y-point[i].Y;
        float distance=(float)Math.Sqrt(dx*dx+dy*dy);
        if(distance<.0001f)continue;
        float error=(distance-segmentLength)/distance;
        if(i==0) { point[1].X-=dx*error;point[1].Y-=dy*error; }
        else {
          // The pet is fifty times heavier than one rope node in the solver.
          // Rope nodes carry only enough inertia for stable curved motion.
          float left=i==Segments-1?50f/51:.5f;
          point[i].X+=dx*error*left;point[i].Y+=dy*error*left;
          point[i+1].X-=dx*error*(1-left);point[i+1].Y-=dy*error*(1-left);
        }
      }
    }
    point[0]=new PointF(anchorX,anchorY);
    float endX=point[Segments].X-anchorX,endY=point[Segments].Y-anchorY;
    float endDistance=(float)Math.Sqrt(endX*endX+endY*endY),maxLength=segmentLength*Segments;
    if(endDistance>maxLength)point[Segments]=new PointF(anchorX+endX*maxLength/endDistance,anchorY+endY*maxLength/endDistance);
  }
  public bool IsSettled(float anchorX,float anchorY) {
    for(int i=1;i<=Segments;i++) {
      if(Math.Abs(point[i].X-anchorX)>2f ||
         Math.Abs(point[i].Y-(anchorY+i*segmentLength))>2f ||
         Math.Abs(point[i].X-previous[i].X)>.06f ||
         Math.Abs(point[i].Y-previous[i].Y)>.06f) return false;
    }
    return Math.Abs(pendingX)<.001f&&Math.Abs(pendingY)<.001f;
  }
}
