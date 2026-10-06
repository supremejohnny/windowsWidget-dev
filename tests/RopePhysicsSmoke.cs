using System;
using System.Drawing;

class RopePhysicsSmoke {
  static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
  static void Main() {
    var rope=new RopePhysics(120,260,52);
    Check(rope.End.X==260 && rope.End.Y==172,"initial rope position");
    var vertical=new RopePhysics(120,260,52);
    vertical.ShiftAnchor(0,-35);
    vertical.Step(1f/60,260,52);
    Check(vertical.End.Y>172,"vertical movement response");
    for(int i=0;i<45;i++)vertical.Step(1f/60,260,52);
    Check(Math.Abs(vertical.End.Y-172)<2,"vertical movement settles");
    rope.ShiftAnchor(-55,-35); // target moved left and up; particles retain screen inertia
    for(int i=0;i<180;i++) rope.Step(1f/60,260,52);
    Check(rope.IsSettled(260,52),"hanging rope can sleep at rest");
    Check(Math.Abs(rope.Points[0].X-260)<.001 && Math.Abs(rope.Points[0].Y-52)<.001,"fixed anchor");
    for(int i=0;i<RopePhysics.Segments;i++) {
      double dx=rope.Points[i+1].X-rope.Points[i].X;
      double dy=rope.Points[i+1].Y-rope.Points[i].Y;
      Check(Math.Abs(Math.Sqrt(dx*dx+dy*dy)-120.0/RopePhysics.Segments)<.5,"segment length");
    }
    float oldX=rope.End.X;
    rope.Poke(11);rope.Step(1f/60,260,52);
    Check(Math.Abs(rope.End.X-oldX)>1,"poke response");
    foreach(int length in new[]{55,85,130,190}) {
      var sixty=new RopePhysics(length,260,52);
      var oneTwenty=new RopePhysics(length,260,52);
      sixty.ShiftAnchor(42,-28);oneTwenty.ShiftAnchor(42,-28);
      for(int frame=0;frame<240;frame++) {
        sixty.Step(1f/60,260,52);
        oneTwenty.Step(1f/120,260,52);oneTwenty.Step(1f/120,260,52);
        Check(sixty.Points[0]==new PointF(260,52),"anchor stays pinned throughout motion");
        Check(!float.IsNaN(sixty.End.X)&&!float.IsNaN(sixty.End.Y),"finite simulation");
      }
      Check(Math.Abs(sixty.End.X-oneTwenty.End.X)<.05 && Math.Abs(sixty.End.Y-oneTwenty.End.Y)<.05,
        "fixed timestep is independent of 60/120 Hz updates");
      Check(sixty.IsSettled(260,52),"all rope lengths settle");
    }
    Console.WriteLine("Rope physics smoke test passed");
  }
}
