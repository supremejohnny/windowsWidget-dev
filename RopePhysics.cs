using System;
using System.Drawing;

// Native pendant adapter for the physics-rope Verlet core. Only the anchor
// moves with the target window; free nodes remain in simulation world space.
class RopePhysics {
  public const int Segments=12;
  const float FixedStep=1f/120;
  readonly PointF[] point=new PointF[Segments+1];
  readonly VerletRopeSolver solver;
  readonly float segmentLength;
  double offsetX,offsetY,simOffsetX,simOffsetY,pendingX,pendingY;
  float accumulator;
  public PointF End { get { return point[Segments]; } }
  public PointF[] Points { get { return point; } }
  public RopePhysics(int length,float anchorX,float anchorY) : this(length,anchorX,anchorY,980) { }
  internal RopePhysics(int length,float anchorX,float anchorY,float endpointGravity) {
    segmentLength=length/(float)Segments;
    var nodes=new RopeNode[Segments+1];
    for(int i=0;i<=Segments;i++) {
      point[i]=new PointF(anchorX,anchorY+i*segmentLength);
      nodes[i]=new RopeNode(point[i].X,point[i].Y);
      nodes[i].Damping=.90;
    }
    nodes[0].Fixed=true;
    nodes[Segments].InverseMass=.1;
    nodes[Segments].Damping=.93;
    nodes[Segments].GravityY=endpointGravity;
    solver=new VerletRopeSolver(nodes,segmentLength,2000);
    solver.ErrorTolerance=.004;
    solver.MaxRelativeSpeed=240;
    solver.PreviousStep=FixedStep;
  }
  public void ShiftAnchor(float dx,float dy) {
    offsetX+=dx;offsetY+=dy;pendingX+=dx;pendingY+=dy;
  }
  public void Poke(float horizontal) {
    solver.Nodes[Segments].OldX-=horizontal*.12;
    solver.Nodes[Segments].OldY-=.12;
  }
  public void Step(float dt,float anchorX,float anchorY) {
    accumulator+=Math.Max(.001f,Math.Min(.05f,dt));
    double shiftX=pendingX/accumulator*FixedStep,shiftY=pendingY/accumulator*FixedStep;
    while(accumulator>=FixedStep) {
      simOffsetX+=shiftX;simOffsetY+=shiftY;
      solver.Nodes[0].X=anchorX+simOffsetX;solver.Nodes[0].Y=anchorY+simOffsetY;
      solver.FrameVelocityX=shiftX/FixedStep;solver.FrameVelocityY=shiftY/FixedStep;
      PrepareSlack();
      solver.Update(FixedStep);
      accumulator-=FixedStep;pendingX-=shiftX;pendingY-=shiftY;
    }
    for(int i=1;i<=Segments;i++) {
      RopeNode n=solver.Nodes[i];
      point[i]=new PointF((float)(n.X-offsetX),(float)(n.Y-offsetY));
    }
    point[0]=new PointF(anchorX,anchorY);
  }
  void PrepareSlack() {
    RopeNode root=solver.Nodes[0],end=solver.Nodes[Segments];
    double dx=end.X-root.X,dy=end.Y-root.Y,chord=Math.Sqrt(dx*dx+dy*dy);
    if(chord<1e-6)return;
    // Bound inward pendant rebound after the target stops, leaving tangential
    // swing and every rope node untouched. Abrupt upward drags otherwise send
    // the free pendant into a large compression bounce.
    if(Math.Abs(solver.FrameVelocityX)+Math.Abs(solver.FrameVelocityY)<1) {
      double vx=(end.X-end.OldX)/FixedStep,vy=(end.Y-end.OldY)/FixedStep;
      double inward=(vx*dx+vy*dy)/chord;
      if(inward< -120) {
        vx-=(inward+120)*dx/chord;vy-=(inward+120)*dy/chord;
        end.OldX=end.X-vx*FixedStep;end.OldY=end.Y-vy*FixedStep;
      }
    }
    if(chord>=segmentLength*Segments-.02)return;
    double bow=0;
    for(int i=1;i<Segments;i++) {
      RopeNode n=solver.Nodes[i];
      bow=Math.Max(bow,Math.Abs((n.X-root.X)*dy-(n.Y-root.Y)*dx)/chord);
    }
    if(bow<.001) {
      // Zero gravity and perfect vertical alignment leave no bend direction
      // when slack forms. A subpixel seed breaks symmetry with no velocity.
      RopeNode middle=solver.Nodes[Segments/2];
      double seedX=-dy/chord*.01,seedY=dx/chord*.01;
      middle.X+=seedX;middle.OldX+=seedX;
      middle.Y+=seedY;middle.OldY+=seedY;
    }
  }
  public bool IsSettled(float anchorX,float anchorY) {
    for(int i=1;i<=Segments;i++) {
      RopeNode n=solver.Nodes[i];
      if(Math.Abs(point[i].X-anchorX)>2||Math.Abs(point[i].Y-(anchorY+i*segmentLength))>2||
        Math.Abs(n.X-n.OldX)>.06||Math.Abs(n.Y-n.OldY)>.06)return false;
    }
    return Math.Abs(pendingX)<.001&&Math.Abs(pendingY)<.001;
  }
}
