using System;
using UnityEngine;

[Serializable]
public sealed class RpgState
{
    public int version=1, x=1, y=1, direction=1, hp=40, level=1, experience;
    public bool armor, tutorial, boss;
    public int defeated;
    public int MaxHp => 32+level*8;
    public int Attack => 5+level*2;
    public void Rest() { hp=MaxHp; }
    public void Gain(int xp) { experience+=xp; while(experience>=level*10) { experience-=level*10; level++; hp=MaxHp; } }
}

public sealed class RpgWorld
{
    public static readonly string[] Map={
        "#############",
        "#...#.......#",
        "#.#.#.###.#.#",
        "#.#...#...#.#",
        "#.#####.###.#",
        "#.....D.....#",
        "###.#.###.#.#",
        "#...#...#.#.#",
        "#.#####.#.#.#",
        "#.......#...#",
        "#############"};
    public static readonly int[] Dx={0,1,0,-1}, Dy={-1,0,1,0};
    public RpgState state=new RpgState();
    public bool door;
    public bool[,] seen=new bool[13,11];
    public bool Wall(int x,int y) => x<0||y<0||y>=Map.Length||x>=Map[0].Length||Map[y][x]=='#';
    public bool Closed(int x,int y) => !door&&!Wall(x,y)&&Map[y][x]=='D';
    public void Reveal() { for(int y=state.y-1;y<=state.y+1;y++) for(int x=state.x-1;x<=state.x+1;x++) if(x>=0&&y>=0&&x<13&&y<11) seen[x,y]=true; }
    public bool Move(int step) { int x=state.x+Dx[state.direction]*step,y=state.y+Dy[state.direction]*step; if(Wall(x,y)||Closed(x,y)) return false; state.x=x;state.y=y;Reveal();return true; }
    public bool Open() { int x=state.x+Dx[state.direction], y=state.y+Dy[state.direction]; if(!Closed(x,y)) return false; door=true;return true; }
    public void Turn(int n) { state.direction=(state.direction+n+4)%4; }
    public void Town() {state.Rest();state.x=1;state.y=1;state.direction=1;}
    public int Encounter() { if(state.x==1&&state.y==1&&!state.tutorial)return 0; if(state.x==11&&state.y==9&&!state.boss)return 3; if(state.x==5&&state.y==3&&(state.defeated&1)==0)return 1; if(state.x==9&&state.y==7&&(state.defeated&2)==0)return 2; return -1; }
}

public sealed class RhythmBattle
{
    public readonly int id;
    public int hp, maxHp, index, hits;
    public float start;
    public bool done;
    public readonly int[] pattern={1,0,2,1,0,1,2,1}; // J, F, Space
    public RhythmBattle(int enemy,float now) { id=enemy;maxHp=hp=enemy==3?100:enemy==0?14:30;start=now; }
    public int Key => pattern[index%pattern.Length];
    public float Due => start+2+index*0.75f;
    public static bool InWindow(float offset) => Math.Abs(offset)<=0.2f;
    public bool Press(int key,float now,RpgState s) { if(done||key!=Key||!InWindow(now-Due))return false; Resolve(true,s);return true; }
    public void Miss(float now,RpgState s) {if(!done&&now>Due+0.2f)Resolve(false,s);}
    private void Resolve(bool hit,RpgState s) {
        int key=Key; if(hit) {hits++;if(key==1)hp=Math.Max(0,hp-s.Attack);else if(key==0)s.hp=Math.Max(0,s.hp-(s.armor?0:1));}
        else if(key!=1)s.hp=Math.Max(0,s.hp-Math.Max(1,(id==3?9:5)-(s.armor?3:0)));
        index++;done=hp==0||s.hp==0;
    }
}
