using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class BlueSappTests
{
 static int checks;
 static void Assert(bool ok,string name){checks++;if(!ok)throw new Exception("FAIL: "+name);}
 public static void Run(){
  var w=new RpgWorld();Assert(w.Encounter()==0,"entry tutorial");w.Turn(-1);Assert(!w.Move(1),"wall blocks");w.Turn(1);Assert(w.Move(1)&&w.state.x==2,"forward move");Assert(w.Move(-1)&&w.state.x==1,"backward move");
  w.state.x=5;w.state.y=5;w.state.direction=1;Assert(!w.Move(1),"closed door blocks");Assert(w.Open()&&w.Move(1),"open door");
  Assert(RhythmBattle.InWindow(.19f)&&!RhythmBattle.InWindow(.21f),"timing boundary");
  var s=new RpgState();var b=new RhythmBattle(1,0);Assert(!b.Press(0,2,s)&&b.index==0,"wrong key ignored");Assert(!b.Press(1,1,s),"early input ignored");Assert(b.Press(1,2,s)&&b.hp==23,"machine gun damage");int hp=s.hp;b.Press(0,2.75f,s);Assert(s.hp==hp-1,"shield reduction");b.Press(2,3.5f,s);Assert(s.hp==hp-1,"dodge avoids damage");b.Press(1,4.25f,s);b.Miss(5.3f,s);Assert(s.hp==hp-6,"shield miss damage");
  var armored=new RpgState{armor=true};var a=new RhythmBattle(1,0);a.Press(1,2,armored);a.Press(0,2.75f,armored);Assert(armored.hp==40,"armor protects shield");a.Miss(4,armored);Assert(armored.hp==38,"armor reduces miss");
  s.hp=1;var death=new RhythmBattle(3,0);death.Miss(3,s);death.Miss(4,s);Assert(s.hp==0&&death.done,"defeat");
  w.state.armor=true;w.state.hp=0;w.Town();Assert(w.state.hp==w.state.MaxHp&&w.state.armor,"town restores hp preserves armor");w.state.Gain(10);Assert(w.state.level==2&&w.state.MaxHp==48,"level up");
  var copy=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(w.state));Assert(copy.armor&&copy.level==2&&copy.x==1,"save serialization roundtrip");
  foreach(var target in new[]{new Vector2Int(3,5),new Vector2Int(11,9)}){var q=new Queue<Vector2Int>();var seen=new HashSet<Vector2Int>();q.Enqueue(new Vector2Int(1,1));while(q.Count>0){var p=q.Dequeue();if(!seen.Add(p))continue;for(int i=0;i<4;i++){var n=p+new Vector2Int(RpgWorld.Dx[i],RpgWorld.Dy[i]);if(!w.Wall(n.x,n.y)&&!seen.Contains(n))q.Enqueue(n);}}Assert(seen.Contains(target),"map target reachable "+target);}
  BlueSappBuild.Prepare(); Debug.Log("BLUESAPP TESTS PASSED: "+checks);
 }
}
