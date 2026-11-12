using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class BlueSappTests
{
 static int checks;
 static void Assert(bool ok,string name){checks++;if(!ok)throw new Exception("FAIL: "+name);}
 public static void Run(){
  checks=0;
  using(var font=new MisakiFont(System.IO.Path.Combine(Application.streamingAssetsPath,"Misaki"))){
   foreach(char c in "日本語戦闘防御回避階層やられてしまった・・・↑→↓←FJ; SPACE")Assert(font.Lookup(c.ToString()).character==c.ToString(),"font contains "+c);
   Assert(font.Width("FJ")==16&&font.Width("日本")==32,"half and full glyph widths");
   Assert(font.Width("F\n日本")==32&&font.Width("F\r\n日本")==32,"multiline width and CRLF");
   Assert(font.Width("")==0&&font.Width("日本",4)==64,"empty and title scale");
   Assert(font.Lookup("not-a-glyph").character=="?","missing glyph fallback");
   Assert(MisakiFont.IsInk(new Color32(0,0,0,255))&&!MisakiFont.IsInk(new Color32(255,255,255,255))&&!MisakiFont.IsInk(new Color32(0,0,0,0)),"font mask ink white background transparent");
  }
  var gate=new EncounterGate();
  Assert(!gate.Ready(10000)&&gate.Count(10000)==0,"encounter waits indefinitely for Return");
  gate.Confirm(100);
  Assert(gate.Count(100)==3&&gate.Count(101)==2&&gate.Count(102)==1,"countdown 3 2 1");
  Assert(!gate.Ready(102.99f)&&gate.Ready(103),"battle starts after full countdown");
  gate.Confirm(102);
  Assert(gate.StartsAt==103,"repeat Return does not reset countdown");
  var first=new RhythmBattle(0,104);
  Assert(first.Due==106&&!first.Press(1,103,new RpgState()),"first note has three seconds lead after battle start");
  Assert(RhythmLayout.ActionAt(0)==0&&RhythmLayout.ActionAt(1)==2&&RhythmLayout.ActionAt(2)==1,"keyboard layout F Space J");
  for(int lane=0;lane<3;lane++){
   Rect r=RhythmLayout.Lane(lane);
   Assert(r.xMin>=800&&r.xMax<=1200&&r.yMin>=0&&r.yMax<690,"lane within right panel "+lane);
   if(lane>0)Assert(RhythmLayout.Lane(lane-1).xMax<r.xMin,"lanes do not overlap "+lane);
  }
  Assert(RhythmLayout.NoteY(10,8,false)<RhythmLayout.NoteY(10,9,false),"notes fall downwards");
  Assert(RhythmLayout.NoteY(10,10,false)==RhythmLayout.Target&&RhythmLayout.NoteY(10,10,true)==RhythmLayout.Target,"armor preserves judgment time");
  Assert(Math.Abs(RhythmLayout.NoteY(10,8.1f,true)-RhythmLayout.NoteY(10,8,true))<Math.Abs(RhythmLayout.NoteY(10,8.1f,false)-RhythmLayout.NoteY(10,8,false)),"armor slows visual travel");
  var w=new RpgWorld();Assert(w.Encounter()==0,"entry tutorial");w.Turn(-1);Assert(!w.Move(1),"wall blocks");w.Turn(1);Assert(w.Move(1)&&w.state.x==2,"forward move");Assert(w.Move(-1)&&w.state.x==1,"backward move");
  w.state.x=5;w.state.y=5;w.state.direction=1;Assert(!w.Move(1),"closed door blocks");Assert(w.Open()&&w.Move(1),"open door");
  Assert(RhythmBattle.InWindow(.19f)&&!RhythmBattle.InWindow(.21f),"timing boundary");
  var s=new RpgState();var b=new RhythmBattle(1,0);Assert(!b.Press(0,2,s)&&b.index==0,"wrong key ignored");Assert(!b.Press(1,1,s),"early input ignored");Assert(b.Press(1,2,s)&&b.hp==23,"machine gun damage");int hp=s.hp;b.Press(0,2.75f,s);Assert(s.hp==hp-1,"shield reduction");b.Press(2,3.5f,s);Assert(s.hp==hp-1,"dodge avoids damage");b.Press(1,4.25f,s);b.Miss(5.3f,s);Assert(s.hp==hp-6,"shield miss damage");
  var armored=new RpgState{armor=true};var a=new RhythmBattle(1,0);a.Press(1,2,armored);a.Press(0,2.75f,armored);Assert(armored.hp==40,"armor protects shield");a.Miss(4,armored);Assert(armored.hp==38,"armor reduces miss");
  s.hp=1;var death=new RhythmBattle(3,0);death.Miss(3,s);death.Miss(4,s);Assert(s.hp==0&&death.done,"defeat");
  w.state.armor=true;w.state.hp=0;w.Town();Assert(w.state.hp==w.state.MaxHp&&w.state.armor,"town restores hp preserves armor");w.state.Gain(10);Assert(w.state.level==2&&w.state.MaxHp==48,"level up");
  var copy=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(w.state));Assert(copy.armor&&copy.level==2&&copy.x==1,"save serialization roundtrip");
  foreach(var target in new[]{new Vector2Int(3,5),new Vector2Int(11,9)}){var q=new Queue<Vector2Int>();var seen=new HashSet<Vector2Int>();q.Enqueue(new Vector2Int(1,1));while(q.Count>0){var p=q.Dequeue();if(!seen.Add(p))continue;for(int i=0;i<4;i++){var n=p+new Vector2Int(RpgWorld.Dx[i],RpgWorld.Dy[i]);if(!w.Wall(n.x,n.y)&&!seen.Contains(n))q.Enqueue(n);}}Assert(seen.Contains(target),"map target reachable "+target);}
  for(int floor=1;floor<=4;floor++){
   var maze=new RpgWorld();maze.SetFloor(floor);
   Assert(maze.Width==new[]{13,16,19,23}[floor-1]&&maze.Height==11,"floor dimensions "+floor);
   var q=new Queue<Vector2Int>();var visited=new HashSet<Vector2Int>();q.Enqueue(new Vector2Int(1,1));
   while(q.Count>0){var p=q.Dequeue();if(!visited.Add(p))continue;for(int d=0;d<4;d++){var n=p+new Vector2Int(RpgWorld.Dx[d],RpgWorld.Dy[d]);if(!maze.Wall(n.x,n.y)&&!visited.Contains(n))q.Enqueue(n);}}
   Assert(visited.Contains(maze.BossPosition)&&visited.Contains(new Vector2Int(5,3))&&visited.Contains(new Vector2Int(9,7)),"boss and enemies reachable "+floor);
   var actions=FloorKeys.Actions(floor);Assert(actions.Length==floor*2+1,"key count "+floor);
   string names="";foreach(int action in actions)names+=FloorKeys.Name(action);
   Assert(names==new[]{"FSPJ","DFSPJK","SDFSPJKL","ASDFSPJKL;"}[floor-1],"physical keyboard order "+floor);
   var fight=new RhythmBattle(3,0,floor);
   foreach(int action in actions)Assert(Array.IndexOf(fight.pattern,action)>=0,"chart includes action "+action+" floor "+floor);
   for(int lane=0;lane<actions.Length;lane++){
    Rect rect=RhythmLayout.Lane(lane,floor);Assert(rect.xMin>=800&&rect.xMax<=1200&&rect.width>30,"lane fits "+floor+"/"+lane);
    if(lane>0)Assert(RhythmLayout.Lane(lane-1,floor).xMax<rect.xMin,"no overlap "+floor+"/"+lane);
   }
   var player=new RpgState();int before=player.hp;
   for(int i=0;i<fight.pattern.Length;i++)Assert(fight.Press(fight.Key,fight.Due,player),"new chart key accepted "+floor+"/"+i);
   Assert(player.hp>0&&fight.hp<fight.maxHp,"full chart deals damage and survives "+floor);
   maze.state.x=maze.BossPosition.x;maze.state.y=maze.BossPosition.y;
   Assert(maze.Encounter()==3&&!maze.Advance(),"uncleared boss gates descent "+floor);
   maze.Win(3);Assert(maze.Encounter()==-1&&maze.Cleared,"boss flag per floor "+floor);
   var saved=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(maze.state));Assert(saved.floor==floor&&(saved.clearedFloors&(1<<(floor-1)))!=0,"floor save roundtrip "+floor);
   Assert(maze.Advance()==(floor<4),"descent and final floor boundary "+floor);
   if(floor<4)Assert(!maze.Cleared&&maze.state.x==1&&maze.seen.GetLength(0)==maze.Width,"new floor resets exploration without boss flag leak");
  }
  Assert(FloorKeys.Key(8)==KeyCode.Semicolon&&FloorKeys.Kind(8)==1,"semicolon is attack");
  var legacy=JsonUtility.FromJson<RpgState>("{\"version\":1,\"boss\":true,\"level\":1}");
  var oldWorld=new RpgWorld{state=legacy};oldWorld.SetFloor(1);Assert(oldWorld.Advance()&&oldWorld.state.floor==2,"old cleared save unlocks second floor");
  BlueSappBuild.Prepare(); Debug.Log("BLUESAPP TESTS PASSED: "+checks);
 }
}
