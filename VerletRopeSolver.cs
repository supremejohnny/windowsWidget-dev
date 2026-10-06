using System;

// Adapted from code4fukui/physics-rope, revision 0cf6431 (James Wrightson, MIT).
// Copyright (c) 2018 James Wrightson.
// See third_party/physics-rope/LICENSE. The native app uses this C# solver;
// original JavaScript is retained only for reference-parity tests.
sealed class RopeNode {
  public double X,Y,OldX,OldY;
  public double GravityX,GravityY;
  public double Damping=.95,InverseMass=1;
  public bool Fixed;
  public RopeNode(double x,double y) { X=OldX=x;Y=OldY=y;GravityX=GravityY=0; }
}
sealed class VerletRopeSolver {
  public readonly RopeNode[] Nodes;
  readonly double segmentLength;
  readonly int iterations;
  public double PreviousStep;
  public double FrameVelocityX,FrameVelocityY;
  public double ErrorTolerance;
  public double MaxRelativeSpeed;
  public int IterationsUsed { get; private set; }
  public VerletRopeSolver(RopeNode[] nodes,double spacing,int solverIterations) {
    Nodes=nodes;segmentLength=spacing;iterations=solverIterations;
    FrameVelocityX=FrameVelocityY=ErrorTolerance=MaxRelativeSpeed=0;
  }
  public void Update(double dt) {
    double timeCorrection=PreviousStep>0?dt/PreviousStep:0;
    foreach(var p in Nodes) {
      if(p.Fixed)continue;
      double vx=p.X-p.OldX,vy=p.Y-p.OldY;
      p.OldX=p.X;p.OldY=p.Y;
      // Upstream damping is per 60-Hz frame. Scale it for our fixed 120 Hz
      // step while retaining exact upstream behavior at 60 Hz.
      double damping=Math.Pow(p.Damping,dt*60)*timeCorrection;
      double rx=vx-FrameVelocityX*PreviousStep,ry=vy-FrameVelocityY*PreviousStep;
      double limit=MaxRelativeSpeed*PreviousStep;
      if(MaxRelativeSpeed>0&&rx*rx+ry*ry>limit*limit) {
        double scale=limit/Math.Sqrt(rx*rx+ry*ry);rx*=scale;ry*=scale;
      }
      p.X+=rx*damping+FrameVelocityX*dt+p.GravityX*dt*dt;
      p.Y+=ry*damping+FrameVelocityY*dt+p.GravityY*dt*dt;
    }
    for(int iteration=0;iteration<iterations;iteration++) {
      double error=0;
      for(int i=0;i<Nodes.Length;i++) {
        if(Nodes[i].Fixed)continue;
        if(i+1<Nodes.Length)error=Math.Max(error,Constrain(i,i+1));
        if(i>0)error=Math.Max(error,Constrain(i,i-1));
      }
      IterationsUsed=iteration+1;
      if(ErrorTolerance>0&&error<ErrorTolerance)break;
    }
    PreviousStep=dt;
  }
  double Constrain(int i,int j) {
    RopeNode a=Nodes[i],b=Nodes[j];
    double dx=b.X-a.X,dy=b.Y-a.Y,length=Math.Sqrt(dx*dx+dy*dy);
    if(length<1e-9)return segmentLength;
    double correction=(length-segmentLength)/length*.25;
    double aShare=1,bShare=1;
    if(!a.Fixed&&!b.Fixed) {
      double total=a.InverseMass+b.InverseMass;
      aShare=2*a.InverseMass/total;bShare=2*b.InverseMass/total;
    }
    if(!a.Fixed){a.X+=dx*correction*aShare;a.Y+=dy*correction*aShare;}
    if(!b.Fixed){b.X-=dx*correction*bShare;b.Y-=dy*correction*bShare;}
    return Math.Abs(length-segmentLength);
  }
}
