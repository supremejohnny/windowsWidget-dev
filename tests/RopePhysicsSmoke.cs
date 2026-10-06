using System;
using System.Drawing;

class RopePhysicsSmoke {
  static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
  static void Main() {
    try { Run(); }
    catch(Exception e) { Console.Error.WriteLine(e.Message);Environment.Exit(1); }
  }
  static float ChordLength(RopePhysics rope) {
    float dx=rope.End.X-rope.Points[0].X,dy=rope.End.Y-rope.Points[0].Y;
    return (float)Math.Sqrt(dx*dx+dy*dy);
  }
  static float Bow(RopePhysics rope) {
    PointF root=rope.Points[0];float dx=rope.End.X-root.X,dy=rope.End.Y-root.Y;
    float chord=ChordLength(rope),max=0;
    foreach(var p in rope.Points)max=Math.Max(max,Math.Abs((p.X-root.X)*dy-(p.Y-root.Y)*dx)/chord);
    return max;
  }
  static void Run() {
    var rope=new RopePhysics(120,260,52);
    Check(rope.End.X==260 && rope.End.Y==172,"initial rope position");
    var vertical=new RopePhysics(120,260,52);
    vertical.ShiftAnchor(0,-35);
    vertical.Step(1f/60,260,52);
    Check(vertical.End.Y<=172.01f,"upward anchor movement pulls without stretching the rope");
    for(int i=0;i<45;i++)vertical.Step(1f/60,260,52);
    Check(Math.Abs(vertical.End.Y-172)<2,"vertical movement settles");
    rope.ShiftAnchor(-55,-35);
    for(int i=0;i<180;i++) rope.Step(1f/60,260,52);
    Check(rope.IsSettled(260,52),"hanging rope can sleep at rest");
    Check(Math.Abs(rope.Points[0].X-260)<.001 && Math.Abs(rope.Points[0].Y-52)<.001,"fixed anchor");
    for(int i=0;i<RopePhysics.Segments;i++) {
      double dx=rope.Points[i+1].X-rope.Points[i].X;
      double dy=rope.Points[i+1].Y-rope.Points[i].Y;
      Check(Math.Abs(Math.Sqrt(dx*dx+dy*dy)-120.0/RopePhysics.Segments)<.5,"segment length");
    }
    float oldX=rope.End.X;
    rope.Poke(7);rope.Step(1f/60,260,52);
    Check(Math.Abs(rope.End.X-oldX)>.2,"gentle poke response");
    foreach(int length in new[]{55,85,130,190}) {
      var dragged=new RopePhysics(length,260,52);
      float peak=0;
      for(int frame=0;frame<240;frame++) {
        if(frame<10)dragged.ShiftAnchor(6,0);
        dragged.Step(1f/60,260,52);
        peak=Math.Max(peak,Math.Abs(dragged.End.X-260));
      }
      Console.WriteLine("{0}px rope peak swing: {1:F1}px",length,peak);
      Check(peak>5,"window acceleration gives visible swing for "+length+"px rope");
      Check(peak<45,"controlled swing amplitude for "+length+"px rope");
      var lifted=new RopePhysics(length,260,52);
      float highest=52+length;
      for(int frame=0;frame<180;frame++) {
        if(frame<10)lifted.ShiftAnchor(0,-6);
        lifted.Step(1f/60,260,52);
        highest=Math.Min(highest,lifted.End.Y);
      }
      Check(highest>52+length-10,"gentle upward window motion must not fold the rope");
      var sixty=new RopePhysics(length,260,52);
      var oneTwenty=new RopePhysics(length,260,52);
      var twoForty=new RopePhysics(length,260,52);
      sixty.Poke(7);oneTwenty.Poke(7);twoForty.Poke(7);
      for(int frame=0;frame<240;frame++) {
        if(frame<10)sixty.ShiftAnchor(6,-3);
        sixty.Step(1f/60,260,52);
        for(int sub=0;sub<2;sub++) {
          if(frame<10)oneTwenty.ShiftAnchor(3,-1.5f);
          oneTwenty.Step(1f/120,260,52);
        }
        for(int sub=0;sub<4;sub++) {
          if(frame<10)twoForty.ShiftAnchor(1.5f,-.75f);
          twoForty.Step(1f/240,260,52);
        }
        Check(sixty.Points[0]==new PointF(260,52),"anchor stays pinned throughout motion");
        Check(!float.IsNaN(sixty.End.X)&&!float.IsNaN(sixty.End.Y),"finite simulation");
      }
      Check(Math.Abs(sixty.End.X-oneTwenty.End.X)<.05 && Math.Abs(sixty.End.Y-oneTwenty.End.Y)<.05,
        "fixed timestep is independent of 60/120 Hz updates");
      Check(Math.Abs(sixty.End.X-twoForty.End.X)<.05 && Math.Abs(sixty.End.Y-twoForty.End.Y)<.05,
        "anchor input is retained at 240 Hz updates");
      Check(sixty.IsSettled(260,52),"all rope lengths settle");

      var slow=new RopePhysics(length,260,52);float prior=0,maxBow=0,maxSlack=0;
      for(int frame=1;frame<=240;frame++) {
        float position=frame<=180?(float)(25*(1-Math.Cos(Math.PI*frame/180))):50;
        slow.ShiftAnchor(position-prior,0);prior=position;
        slow.Step(1f/60,260,52);
        maxBow=Math.Max(maxBow,Bow(slow));
        maxSlack=Math.Max(maxSlack,length-ChordLength(slow));
      }
      Console.WriteLine("{0}px eased drag: bow {1:F3}px, slack {2:F3}px",length,maxBow,maxSlack);
      Check(maxBow<.05f&&maxSlack<.1f,"slow eased drag keeps a tensioned cord without waves");

      var steady=new RopePhysics(length,260,52);
      for(int frame=0;frame<300;frame++) {
        steady.ShiftAnchor(1f/3,0);steady.Step(1f/60,260,52);
      }
      Check(Math.Abs(steady.End.X-260)<1.5f,"uniform window speed must not continuously drive the rope");

      var slack=new RopePhysics(length,260,52);float mostSlack=0;
      for(int frame=0;frame<240;frame++) {
        if(frame<12)slack.ShiftAnchor(0,10);
        slack.Step(1f/60,260,52);
        mostSlack=Math.Max(mostSlack,length-ChordLength(slack));
        Check(ChordLength(slack)<=length+.01f,"cord transmits tension without stretching");
        float drawnLength=0;
        for(int i=1;i<=RopePhysics.Segments;i++) {
          float sx=slack.Points[i].X-slack.Points[i-1].X,sy=slack.Points[i].Y-slack.Points[i-1].Y;
          drawnLength+=(float)Math.Sqrt(sx*sx+sy*sy);
        }
        Check(drawnLength>length-1&&drawnLength<length+.1f,"slack shape preserves rope length: "+drawnLength+" / "+length+", frame "+frame);
        float lastCross=0;int reversals=0;
        for(int i=1;i<RopePhysics.Segments;i++) {
          PointF a=slack.Points[i-1],b=slack.Points[i],c=slack.Points[i+1];
          float cross=(b.X-a.X)*(c.Y-b.Y)-(b.Y-a.Y)*(c.X-b.X);
          if(Math.Abs(cross)<.01f)continue;
          if(lastCross*cross<0)reversals++;
          lastCross=cross;
        }
        Check(reversals==0,"slack cord forms one bend rather than a wave");
      }
      Console.WriteLine("{0}px downward acceleration: max slack {1:F2}px",length,mostSlack);
      Check(mostSlack>.1f,"fast downward acceleration can briefly release tension");
      Check(slack.IsSettled(260,52),"slack cord returns to tension and settles");
    }
    Console.WriteLine("Rope physics smoke test passed");
  }
}
