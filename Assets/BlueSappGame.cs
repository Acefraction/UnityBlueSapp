using System;
using System.IO;
using UnityEngine;

public sealed class BlueSappGame : MonoBehaviour
{
    enum Mode {Title,Intro,Town,Explore,Battle,Dead,Clear}
    Mode mode; RpgWorld world=new RpgWorld(); RhythmBattle battle;
    bool map; int choice; string message=""; GUIStyle text,big; Font font;
    Texture2D pixel; AudioSource music,sfx; float nextBeat; int beat;
    string SavePath=>Path.Combine(Application.persistentDataPath,"bluesapp.json");
    static readonly Color Cyan=new Color(0.2f,0.9f,1), Blue=new Color(0.05f,0.1f,0.65f);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot() {if(FindFirstObjectByType<BlueSappGame>()==null)new GameObject("BlueSapp").AddComponent<BlueSappGame>();}
    System.Collections.IEnumerator Start() { if(Array.IndexOf(Environment.GetCommandLineArgs(),"-bluesapp-smoke")<0)yield break; yield return new WaitForSeconds(1); Switch(Mode.Explore);world.state.tutorial=true;world.Reveal();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../explore.png"));yield return new WaitForSeconds(1);world.state.armor=true;StartBattle(3);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../battle.png"));yield return new WaitForSeconds(1);Debug.Log("BLUESAPP SMOKE PASSED");Application.Quit(); }
    void Awake() {
        Application.runInBackground=true; Application.targetFrameRate=60; pixel=Texture2D.whiteTexture;
        font=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Arial"},20);
        music=gameObject.AddComponent<AudioSource>();sfx=gameObject.AddComponent<AudioSource>(); ChangeMusic();
    }
    AudioClip Tone(float freq,float duration,float volume) {int rate=22050;var data=new float[(int)(rate*duration)];for(int i=0;i<data.Length;i++)data[i]=(Mathf.Sin(2*Mathf.PI*freq*i/rate)>0?1:-1)*volume*Mathf.Min(1,(data.Length-i)/(rate*0.04f));var clip=AudioClip.Create("generated tone",data.Length,1,rate,false);clip.SetData(data,0);return clip;}
    void ChangeMusic() {
        music.Stop(); float[] notes=mode==Mode.Dead?new[]{220f,196,174.6f,164.8f,146.8f,130.8f,0,0}:mode==Mode.Battle?new[]{164.8f,164.8f,196,220,164.8f,246.9f,220,196}:new[]{220f,261.6f,329.6f,293.7f,220,196,246.9f,196};
        int rate=22050;float step=0.375f;var data=new float[(int)(rate*step*notes.Length)];for(int i=0;i<data.Length;i++){int n=(int)(i/(rate*step));float t=i/(float)rate%step;data[i]=notes[n]==0?0:(Mathf.Sin(2*Mathf.PI*notes[n]*t)>0?1:-1)*0.055f*(1-t/step);}
        var old=music.clip;music.clip=AudioClip.Create("BlueSapp original loop",data.Length,1,rate,false);music.clip.SetData(data,0);music.loop=true;music.Play();if(old!=null)Destroy(old);
    }
    void Sound(float f) {var clip=Tone(f,.09f,.08f);sfx.PlayOneShot(clip);Destroy(clip,1);}
    void Switch(Mode m) {mode=m;choice=0;ChangeMusic();}
    void EnterTown(){world.Town();message="街に戻り、HPが全回復した。";Switch(Mode.Town);}
    void StartBattle(int id){battle=new RhythmBattle(id,Time.time);nextBeat=Time.time+2;beat=0;Switch(Mode.Battle);message=id==0?"リズムにあわせてキーをたたけ！ J:攻撃 F:防御 SPACE:回避":"";}
    void Save(){try{File.WriteAllText(SavePath,JsonUtility.ToJson(world.state));message="セーブした。";}catch(Exception e){message="保存できなかった: "+e.Message;}}
    void Load(){try{var s=JsonUtility.FromJson<RpgState>(File.ReadAllText(SavePath));if(s==null||s.version!=1||s.level<1||s.level>100||s.experience<0)throw new Exception("保存内容が不正");world=new RpgWorld{state=s};EnterTown();}catch(Exception){message="読み込めるセーブがありません。";}}
    void Update(){
        bool enter=Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter);
        if(mode==Mode.Title||mode==Mode.Town){if(Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.DownArrow))choice=1-choice;
            if(enter){Sound(660);if(mode==Mode.Title){if(choice==1)Load();else{world=new RpgWorld();Switch(Mode.Intro);}}else if(choice==1)Save();else{Switch(Mode.Explore);world.Reveal();Check();}}return;}
        if(mode==Mode.Intro){if(enter)EnterTown();return;}
        if(mode==Mode.Dead||mode==Mode.Clear){if(enter)EnterTown();return;}
        if(mode==Mode.Battle){
            if(Time.time>=nextBeat){Sound(beat%2==0?880:440);nextBeat+=.375f;beat++;}
            if(Input.GetKeyDown(KeyCode.F))battle.Press(0,Time.time,world.state);
            if(Input.GetKeyDown(KeyCode.J))battle.Press(1,Time.time,world.state);
            if(Input.GetKeyDown(KeyCode.Space))battle.Press(2,Time.time,world.state);
            battle.Miss(Time.time,world.state);
            if(battle.done){if(world.state.hp<=0)Switch(Mode.Dead);else{int id=battle.id;world.state.Gain(id==3?20:5);if(id==0)world.state.tutorial=true;else if(id==3)world.state.boss=true;else world.state.defeated|=1<<(id-1);Switch(id==3?Mode.Clear:Mode.Explore);message="敵を倒した。";}}return;
        }
        if(Input.GetKeyDown(KeyCode.M))map=!map;
        bool moved=false;if(Input.GetKeyDown(KeyCode.LeftArrow))world.Turn(-1);if(Input.GetKeyDown(KeyCode.RightArrow))world.Turn(1);
        if(Input.GetKeyDown(KeyCode.UpArrow))moved=world.Move(1);if(Input.GetKeyDown(KeyCode.DownArrow))moved=world.Move(-1);
        if(moved){Sound(120);Check();}
        if(enter){if(world.state.x==1&&world.state.y==1)EnterTown();else if(world.state.x==3&&world.state.y==5&&!world.state.armor){world.state.armor=true;message="外装を装着した！ 防御と予測機能が向上した。";Sound(990);}else message=world.Open()?"黄色い扉を開いた。":"ここには何もない。";}
    }
    void Check(){int id=world.Encounter();if(id>=0)StartBattle(id);else message=world.state.x==3&&world.state.y==5&&!world.state.armor?"外装パーツがある。Enterで装着。":world.state.x==1&&world.state.y==1?"Enterで街に戻る。":"矢印:移動・旋回  Enter:調べる  M:地図";}
    void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),pixel);GUI.color=Color.white;}
    void Line(float x,float y,float x2,float y2,Color c){var save=GUI.matrix;Vector2 d=new Vector2(x2-x,y2-y);GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,new Vector2(x,y));Box(x,y,d.magnitude,2,c);GUI.matrix=save;}
    void Label(float x,float y,string s,bool large=false){GUI.Label(new Rect(x,y,780,200),s,large?big:text);}
    void OnGUI(){GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1200f,Screen.height/800f,1));if(text==null){text=new GUIStyle(GUI.skin.label){font=font,fontSize=18};text.normal.textColor=Cyan;big=new GUIStyle(text){fontSize=30};}
        Box(0,0,1200,800,Color.black);DrawAndroid();Line(800,0,800,800,Cyan);
        if(mode==Mode.Explore)Dungeon();else if(mode==Mode.Battle)BattleView();else if(mode==Mode.Town)TownView();
        else if(mode==Mode.Title){Label(90,160,"BLUE SAPP",true);Label(90,230,"FRAME / FIRST EXPEDITION");Label(90,340,(choice==0?"> ":"  ")+"はじめから\n"+(choice==1?"> ":"  ")+"つづきから");Label(90,460,message);}
        else if(mode==Mode.Intro){Label(40,90,"この町に来たばかりの俺は、ジャンクヤードで\n女性型のロボットを見つけた。\nなけなしの金をはたいて買った。\n\n――こいつが話せるとは思わなかった。\n\n「マスター・・・ワタシノ・・・\n ブヒンヲ・・・アツメテ・・・\n キットヤクニタツ・・・」\n\nEnterで街へ");}
        else {Label(150,290,mode==Mode.Dead?"やられてしまった・・・":"第一層クリア！",true);Label(170,380,"リターンキーで街に戻る");}
        Box(0,690,800,110,new Color(.015f,.025f,.08f));Line(0,690,800,690,Cyan);Label(16,700,$"HP {world.state.hp}/{world.state.MaxHp}   LV {world.state.level}   EXP {world.state.experience}   外装: {(world.state.armor?"装着":"なし")}");Label(16,738,mode==Mode.Battle?"F:盾   J:マシンガン   SPACE:回避":message);
        if(map&&mode==Mode.Explore)MapView();
    }
    void TownView(){Box(0,0,800,690,new Color(.015f,.025f,.12f));for(int i=0;i<9;i++){int h=100+(i*47)%180;Box(i*96,360-h,85,h,Blue);for(int j=0;j<4;j++)Box(i*96+15,370-h+j*30,12,12,Color.yellow);}Label(45,50,"ジャンクヤードの街",true);Label(80,430,(choice==0?"> ":"  ")+"ダンジョンにはいる\n\n"+(choice==1?"> ":"  ")+"セーブする");}
    void Dungeon(){
        Box(0,0,800,690,Color.black);int fx=RpgWorld.Dx[world.state.direction],fy=RpgWorld.Dy[world.state.direction];int rx=-fy,ry=fx;
        for(int depth=4;depth>=0;depth--){float near=340f/(depth+1),far=340f/(depth+2);float top=330-near*.8f,bottom=330+near*.8f,ft=330-far*.8f,fb=330+far*.8f;int x=world.state.x+fx*depth,y=world.state.y+fy*depth;
            if(world.Wall(x,y)||world.Closed(x,y)){Box(400-near,top,near*2,bottom-top,world.Closed(x,y)?Color.yellow:Blue);Line(400-near,top,400+near,top,Cyan);Line(400-near,bottom,400+near,bottom,Cyan);continue;}
            for(int side=-1;side<=1;side+=2){if(world.Wall(x+rx*side,y+ry*side)||world.Closed(x+rx*side,y+ry*side)){float a=400+side*near,b=400+side*far;for(float xx=Mathf.Min(a,b);xx<=Mathf.Max(a,b);xx+=2){float t=Mathf.InverseLerp(a,b,xx);Box(xx,Mathf.Lerp(top,ft,t),2,Mathf.Lerp(bottom,fb,t)-Mathf.Lerp(top,ft,t),world.Closed(x+rx*side,y+ry*side)?Color.yellow:Blue);}Line(a,top,b,ft,Cyan);Line(a,bottom,b,fb,Cyan);Line(b,ft,b,fb,Cyan);}}
            Line(400-near,bottom,400-far,fb,Cyan);Line(400+near,bottom,400+far,fb,Cyan);
        }
        Label(20,15,$"第一層 / 壊れかけロボ   {new[]{"北","東","南","西"}[world.state.direction]}");
    }
    void DrawAndroid(){bool armor=world.state.armor;Color metal=armor?new Color(.35f,.65f,.95f):new Color(.45f,.5f,.55f);Label(840,25,"ANDROID / SAPP");Box(954,95,88,105,metal);Box(962,120,26,8,Cyan);Box(1008,120,26,8,Cyan);Box(988,200,18,25,metal);Box(950,225,96,100,metal);Box(970,325,56,80,metal);Box(948,405,100,55,metal);for(int s=-1;s<=1;s+=2){float x=997+s*75;Line(997+s*50,240,x,355,metal);Line(x,355,x+s*12,455,metal);Box(x-9,350,18,18,Cyan);float leg=997+s*28;Box(leg-13,460,26,115,metal);Box(leg-10,585,20,125,metal);Box(leg-20,710,45,15,metal);Box(leg-15,572,30,15,Cyan);}if(!armor){for(int y=240;y<320;y+=16)Line(950,y,1046,y,Cyan);Line(997,225,997,460,Cyan);}else{Box(937,230,120,55,metal);Box(940,405,115,55,metal);}Label(835,755,armor?"外装装着 / SHIELD +":"機械フレーム / NO ARMOR");}
    void BattleView(){Label(30,20,battle.id==3?"BOSS / 正常な多脚ロボ":"壊れた多脚ロボ",true);Box(310,150,180,110,Blue);Box(345,175,110,18,Color.red);for(int i=0;i<6;i++){if(battle.id!=3&&i==4)continue;float x=330+i*25;Line(x,240,100+i*100,330,Cyan);Line(100+i*100,330,70+i*110,400,Cyan);}Box(40,440,720,18,new Color(.2f,.1f,.1f));Box(40,440,720*battle.hp/(float)battle.maxHp,18,Color.red);Label(40,465,$"敵 HP {battle.hp}/{battle.maxHp}");
        string[] keys={"F / 防御","J / 攻撃","SPACE / 回避"};float travel=world.state.armor?3:2;for(int lane=0;lane<3;lane++){float y=530+lane*48;Label(15,y,keys[lane]);Line(175,y+15,770,y+15,Blue);Box(260,y,3,32,Cyan);for(int i=battle.index;i<battle.index+8;i++){if(battle.pattern[i%8]!=lane)continue;float due=battle.start+2+i*.75f;float x=260+(due-Time.time)*480/travel;if(x>=175&&x<770)Box(x,y,22,28,lane==1?Color.yellow:Cyan);}}}
    void MapView(){Box(0,0,1200,800,new Color(0,0,.05f,.78f));Label(60,40,"AUTOMAP / Mで閉じる");float size=46;for(int y=0;y<11;y++)for(int x=0;x<13;x++){if(!world.seen[x,y])continue;Box(270+x*size,110+y*size,43,43,world.Wall(x,y)?Blue:world.Closed(x,y)?Color.yellow:new Color(.12f,.17f,.23f));}Label(270+world.state.x*size,110+world.state.y*size,new[]{"↑","→","↓","←"}[world.state.direction]);}
}
