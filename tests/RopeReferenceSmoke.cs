using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

class RopeReferenceSmoke {
  static void Main() {
    var expected=new Dictionary<int,double[]>();
    foreach(string line in File.ReadAllLines("tests/fixtures/physics-rope-reference.csv")) {
      if(line.StartsWith("frame"))continue;
      string[] field=line.Split(',');int frame=int.Parse(field[0]),node=int.Parse(field[1]);
      if(!expected.ContainsKey(frame))expected[frame]=new double[150];
      expected[frame][node*2]=double.Parse(field[2],CultureInfo.InvariantCulture);
      expected[frame][node*2+1]=double.Parse(field[3],CultureInfo.InvariantCulture);
    }
    var nodes=new RopeNode[75];
    for(int i=0;i<nodes.Length;i++) {
      nodes[i]=new RopeNode(100+600.0*i/74,240);
      nodes[i].GravityY=3000.88;
    }
    nodes[0].Fixed=nodes[74].Fixed=true;
    var solver=new VerletRopeSolver(nodes,8,500);
    double largestError=0;
    for(int frame=0;frame<=120;frame++) {
      nodes[0].X=100+30*Math.Sin(frame*.07);nodes[0].Y=240+15*Math.Sin(frame*.05);
      solver.Update(1.0/60);
      if(!expected.ContainsKey(frame))continue;
      for(int i=0;i<nodes.Length;i++) {
        largestError=Math.Max(largestError,Math.Abs(nodes[i].X-expected[frame][i*2]));
        largestError=Math.Max(largestError,Math.Abs(nodes[i].Y-expected[frame][i*2+1]));
      }
    }
    if(largestError>1e-6){Console.Error.WriteLine("Upstream parity error: "+largestError);Environment.Exit(1);}
    Console.WriteLine("Upstream JS / C# parity: 450 node samples; largest error {0:E2}px",largestError);
  }
}
