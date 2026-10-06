import { Rope } from "../third_party/physics-rope/Rope.js";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const points = Rope.generate({ x:100, y:240 }, { x:700, y:240 }, 8, .88, .95);
const rope = new Rope(points, 500);
const snapshots = new Set([0, 1, 5, 20, 60, 120]);
const lines = ["frame,node,x,y"];
for (let frame=0; frame<=120; frame++) {
  points[0].pos.x = 100 + 30*Math.sin(frame*.07);
  points[0].pos.y = 240 + 15*Math.sin(frame*.05);
  rope.update({x:0,y:3000}, 1/60);
  if (snapshots.has(frame))points.forEach((p,i)=>lines.push(`${frame},${i},${p.pos.x.toFixed(12)},${p.pos.y.toFixed(12)}`));
}
const csv=lines.join("\n")+"\n";
const fixture=fileURLToPath(new URL("./fixtures/physics-rope-reference.csv",import.meta.url));
if (process.argv.includes("--check")) {
  if(readFileSync(fixture,"utf8").replaceAll("\r\n","\n")!==csv)throw new Error("Upstream reference fixture changed");
  console.log("Original physics-rope JS: 450 node samples match reference fixture");
} else {
  mkdirSync(fileURLToPath(new URL("./fixtures/",import.meta.url)),{recursive:true});
  writeFileSync(fixture,csv);
  console.log("Generated 450 samples from unchanged upstream Rope.js");
}
