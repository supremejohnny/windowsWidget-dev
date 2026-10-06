using System;
using System.Drawing;
using System.Drawing.Imaging;

class PetBodyPhysicsSmoke {
  static void Check(bool ok,string why) { if(!ok)throw new Exception(why); }
  static void Near(double value,double expected,double tolerance,string why) {
    Check(Math.Abs(value-expected)<=tolerance,why+": "+value);
  }
  static void Main() {
    using(var bitmap=new Bitmap(100,100,PixelFormat.Format32bppPArgb)) {
      using(var g=Graphics.FromImage(bitmap)) {
        g.Clear(Color.Transparent);
        using(var brush=new SolidBrush(Color.White))g.FillRectangle(brush,60,20,20,40);
      }
      var measured=PetBodyPhysics.FromImage(bitmap,100,100);
      Near(measured.CenterX,20,.1,"alpha-weighted horizontal center");
      Near(measured.CenterY,40,.1,"alpha-weighted vertical center");
      Check(measured.MomentPerMass>measured.CenterX*measured.CenterX+measured.CenterY*measured.CenterY,
        "moment includes the shape around its center");
      Near(measured.RestAngle,Math.Atan2(20,40),.001,"mass hangs below pivot");
    }
    var body=new PetBodyPhysics(0,85,9500);
    const double dt=1.0/60;
    body.Update(dt,0,0);
    for(int i=1;i<=8;i++)body.Update(dt,i*5,0);
    Check(body.Angle>.01,"pet lags a rightward accelerating pivot");
    for(int i=0;i<600;i++)body.Update(dt,40,0);
    Near(body.Angle,body.RestAngle,.008,"gravity returns center of mass below pivot");
    Check(body.IsSettled,"body settles after motion");
    body.ResetMotion();
    Near(body.Angle,body.RestAngle,.00001,"reset returns to equilibrium");
    using(var asset=new Bitmap("assets/amiya_test1/character.png")) {
      using(var rendered=new Bitmap(320,296,PixelFormat.Format32bppPArgb)) {
        using(var g=Graphics.FromImage(rendered))g.DrawImage(asset,new Rectangle(0,0,320,296));
        var actual=PetBodyPhysics.FromImage(rendered,160,148);
        Check(actual.CenterY>55&&actual.CenterY<115,"real pet has a valid center of mass below top attachment");
        Check(Math.Abs(actual.CenterX)<20,"real pet mass stays near image center");
        Console.WriteLine("Amiya center of mass ({0:F2}, {1:F2}) px, moment/mass {2:F1} px²",actual.CenterX,actual.CenterY,actual.MomentPerMass);
      }
    }
    Console.WriteLine("Pet body physics smoke test passed");
  }
}
