using System;
using UnityEngine;

[Serializable]
public sealed class RpgState
{
    public int version=1, x=1, y=1, direction=1, hp=40, level=1, experience;
    public bool armor, tutorial, boss;
    public int defeated;
    public int floor=1, clearedFloors, defeatedFloors;
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
    public string[] Tiles { get; private set; }=Map;
    public int Width=>Tiles[0].Length;
    public int Height=>Tiles.Length;
    public Vector2Int BossPosition=>new Vector2Int(Width-2,Height-2);
    public bool Cleared => (state.clearedFloors & (1<<(state.floor-1)))!=0 || (state.floor==1&&state.boss);
    public void SetFloor(int floor) {
        if(floor<1||floor>4)throw new ArgumentOutOfRangeException(nameof(floor));
        state.floor=floor; Tiles=floor==1?Map:Generate(floor);seen=new bool[Width,Height];door=false;
        state.x=1;state.y=1;state.direction=1;Reveal();
    }
    private static string[] Generate(int floor) {
        int width=new[]{13,16,19,23}[floor-1],height=11;
        var cells=new char[width,height];for(int y=0;y<height;y++)for(int x=0;x<width;x++)cells[x,y]='#';
        // Fixed seed gives each floor a stable, connected maze across visits and saves.
        var random=new System.Random(743+floor);var stack=new System.Collections.Generic.Stack<Vector2Int>();
        cells[1,1]='.';stack.Push(new Vector2Int(1,1));
        while(stack.Count>0){var p=stack.Peek();var options=new System.Collections.Generic.List<int>();
            for(int i=0;i<4;i++){int x=p.x+Dx[i]*2,y=p.y+Dy[i]*2;if(x>0&&y>0&&x<width-1&&y<height-1&&cells[x,y]=='#')options.Add(i);}
            if(options.Count==0){stack.Pop();continue;}
            int d=options[random.Next(options.Count)];cells[p.x+Dx[d],p.y+Dy[d]]='.';
            var n=new Vector2Int(p.x+Dx[d]*2,p.y+Dy[d]*2);cells[n.x,n.y]='.';stack.Push(n);
        }
        cells[width-2,height-2]='.';cells[width-3,height-2]='.';
        var result=new string[height];for(int y=0;y<height;y++){var row=new char[width];for(int x=0;x<width;x++)row[x]=cells[x,y];result[y]=new string(row);}return result;
    }
    public bool Advance(){if(!Cleared||state.floor==4)return false;SetFloor(state.floor+1);return true;}
    public void Win(int id){if(id==0)state.tutorial=true;else if(id==3){state.clearedFloors|=1<<(state.floor-1);if(state.floor==1)state.boss=true;}else if(state.floor==1)state.defeated|=1<<(id-1);else state.defeatedFloors|=1<<((state.floor-2)*2+id-1);}
    public bool Wall(int x,int y) => x<0||y<0||y>=Height||x>=Width||Tiles[y][x]=='#';
    public bool Closed(int x,int y) => !door&&!Wall(x,y)&&Tiles[y][x]=='D';
    public void Reveal() { for(int y=state.y-1;y<=state.y+1;y++) for(int x=state.x-1;x<=state.x+1;x++) if(x>=0&&y>=0&&x<Width&&y<Height) seen[x,y]=true; }
    public bool Move(int step) { int x=state.x+Dx[state.direction]*step,y=state.y+Dy[state.direction]*step; if(Wall(x,y)||Closed(x,y)) return false; state.x=x;state.y=y;Reveal();return true; }
    public bool Open() { int x=state.x+Dx[state.direction], y=state.y+Dy[state.direction]; if(!Closed(x,y)) return false; door=true;return true; }
    public void Turn(int n) { state.direction=(state.direction+n+4)%4; }
    public void Town() {state.Rest();state.x=1;state.y=1;state.direction=1;}
    public int Encounter() {
        if(state.floor==1&&state.x==1&&state.y==1&&!state.tutorial)return 0;
        if(state.x==BossPosition.x&&state.y==BossPosition.y&&!Cleared)return 3;
        int flags=state.floor==1?state.defeated:(state.defeatedFloors>>((state.floor-2)*2));
        if(state.x==5&&state.y==3&&(flags&1)==0)return 1;
        if(state.x==9&&state.y==7&&(flags&2)==0)return 2;return -1;
    }
}

public sealed class RhythmBattle
{
    public readonly int id;
    public int hp, maxHp, index, hits;
    public float start;
    public bool done;
    public readonly int[] pattern={1,0,2,1,0,1,2,1}; // J, F, Space
    public RhythmBattle(int enemy,float now,int floor=1) {
        id=enemy;maxHp=hp=(enemy==3?100:enemy==0?14:30)+(floor-1)*(enemy==3?25:8);start=now;
        if(floor>1){int[] actions=FloorKeys.Actions(floor);var expanded=new System.Collections.Generic.List<int>();
            foreach(int action in actions){expanded.Add(action);expanded.Add(1);}pattern=expanded.ToArray();}
    }
    public int Key => pattern[index%pattern.Length];
    public float Due => start+2+index*0.75f;
    public static bool InWindow(float offset) => Math.Abs(offset)<=0.2f;
    public bool Press(int key,float now,RpgState s) { if(done||key!=Key||!InWindow(now-Due))return false; Resolve(true,s);return true; }
    public void Miss(float now,RpgState s) {if(!done&&now>Due+0.2f)Resolve(false,s);}
    private void Resolve(bool hit,RpgState s) {
        int key=Key; if(hit) {hits++;if(FloorKeys.Kind(key)==1)hp=Math.Max(0,hp-s.Attack);else if(FloorKeys.Kind(key)==0)s.hp=Math.Max(0,s.hp-(s.armor?0:1));}
        else if(FloorKeys.Kind(key)!=1)s.hp=Math.Max(0,s.hp-Math.Max(1,(id==3?9:5)-(s.armor?3:0)));
        index++;done=hp==0||s.hp==0;
    }
}
