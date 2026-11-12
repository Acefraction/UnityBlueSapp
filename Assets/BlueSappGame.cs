using System;
using System.IO;
using UnityEngine;

public sealed class BlueSappGame : MonoBehaviour
{
    enum Mode {Title,Intro,Town,Explore,Encounter,Battle,Dead,Clear}
    EncounterGate gate;
    int pendingEnemy;
    Mode mode; RpgWorld world=new RpgWorld(); RhythmBattle battle;
    bool map; int choice; string message=""; MisakiFont bitmapFont;
    Texture2D pixel; AudioSource music,sfx; float nextBeat; int beat;
    string SavePath=>Path.Combine(Application.persistentDataPath,"bluesapp.json");
    static readonly Color Cyan=new Color(0.2f,0.9f,1), Blue=new Color(0.05f,0.1f,0.65f);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot() {if(FindFirstObjectByType<BlueSappGame>()==null)new GameObject("BlueSapp").AddComponent<BlueSappGame>();}
    System.Collections.IEnumerator Start() { if(Array.IndexOf(Environment.GetCommandLineArgs(),"-bluesapp-smoke")<0)yield break; yield return new WaitForSeconds(1); Switch(Mode.Explore);world.state.tutorial=true;world.Reveal();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../explore.png"));yield return new WaitForSeconds(1);world.state.armor=true;StartBattle(3);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../battle.png"));yield return new WaitForSeconds(1);Debug.Log("BLUESAPP SMOKE PASSED");Application.Quit(); }
    void Awake() {
        Application.runInBackground=true; Application.targetFrameRate=60; pixel=Texture2D.whiteTexture;
        bitmapFont=new MisakiFont(Path.Combine(Application.streamingAssetsPath,"Misaki"));
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
    void StartBattle(int id){pendingEnemy=id;gate=new EncounterGate();Switch(Mode.Encounter);music.Stop();message="";}
    void BeginBattle(){battle=new RhythmBattle(pendingEnemy,Time.time+1,world.state.floor);nextBeat=Time.time;beat=0;Switch(Mode.Battle);}
    void Save(){try{File.WriteAllText(SavePath,JsonUtility.ToJson(world.state));message="セーブした。";}catch(Exception e){message="保存できなかった: "+e.Message;}}
    void Load(){try{var s=JsonUtility.FromJson<RpgState>(File.ReadAllText(SavePath));if(s==null||s.version!=1||s.level<1||s.level>100||s.experience<0)throw new Exception("保存内容が不正");world=new RpgWorld{state=s};world.SetFloor(Mathf.Clamp(s.floor,1,4));EnterTown();}catch(Exception){message="読み込めるセーブがありません。";}}
    void Update(){
        bool enter=Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter);
        if(mode==Mode.Title||mode==Mode.Town){if(Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.DownArrow))choice=1-choice;
            if(enter){Sound(660);if(mode==Mode.Title){if(choice==1)Load();else{world=new RpgWorld();Switch(Mode.Intro);}}else if(choice==1)Save();else{Switch(Mode.Explore);world.Reveal();Check();}}return;}
        if(mode==Mode.Intro){if(enter)EnterTown();return;}
        if(mode==Mode.Encounter){
            if(!gate.Confirmed&&enter){gate.Confirm(Time.time);Sound(660);}
            if(gate.Ready(Time.time))BeginBattle();
            return;
        }
        if(mode==Mode.Clear){if(enter){if(world.Advance()){Switch(Mode.Explore);Check();}else EnterTown();}return;}
        if(mode==Mode.Dead){if(enter)EnterTown();return;}
        if(mode==Mode.Battle){
            if(Time.time>=nextBeat){Sound(beat%2==0?880:440);nextBeat+=.375f;beat++;}
            foreach(int action in FloorKeys.Actions(world.state.floor))
                if(Input.GetKeyDown(FloorKeys.Key(action)))battle.Press(action,Time.time,world.state);
            battle.Miss(Time.time,world.state);
            if(battle.done){if(world.state.hp<=0)Switch(Mode.Dead);else{int id=battle.id;world.state.Gain(id==3?20:5);world.Win(id);Switch(id==3?Mode.Clear:Mode.Explore);message="敵を倒した。";}}return;
        }
        if(Input.GetKeyDown(KeyCode.M))map=!map;
        bool moved=false;if(Input.GetKeyDown(KeyCode.LeftArrow))world.Turn(-1);if(Input.GetKeyDown(KeyCode.RightArrow))world.Turn(1);
        if(Input.GetKeyDown(KeyCode.UpArrow))moved=world.Move(1);if(Input.GetKeyDown(KeyCode.DownArrow))moved=world.Move(-1);
        if(moved){Sound(120);Check();}
        if(mode!=Mode.Explore)return;
        if(enter){if(world.state.x==1&&world.state.y==1)EnterTown();else if(world.state.floor==1&&world.state.x==3&&world.state.y==5&&!world.state.armor){world.state.armor=true;message="外装を装着した！ 防御と予測機能が向上した。";Sound(990);}else if(world.state.x==world.BossPosition.x&&world.state.y==world.BossPosition.y&&world.Advance()){Switch(Mode.Explore);Check();}else message=world.Open()?"黄色い扉を開いた。":"ここには何もない。";}
    }
    void Check(){int id=world.Encounter();if(id>=0)StartBattle(id);else message=world.state.floor==1&&world.state.x==3&&world.state.y==5&&!world.state.armor?"外装パーツがある。Enterで装着。":world.state.x==1&&world.state.y==1?"Enterで街に戻る。":"矢印:移動・旋回  Enter:調べる  M:地図";}
    void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),pixel);GUI.color=Color.white;}
    void Line(float x,float y,float x2,float y2,Color c){var save=GUI.matrix;Vector2 d=new Vector2(x2-x,y2-y);GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,new Vector2(x,y));Box(x,y,d.magnitude,2,c);GUI.matrix=save;}
    void Label(float x,float y,string s,bool large=false){bitmapFont.Draw(x,y,s,Cyan,large?4:2);}
    void OnGUI(){GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1200f,Screen.height/800f,1));
        Box(0,0,1200,800,Color.black);if(mode!=Mode.Battle)DrawAndroid();Line(800,0,800,800,Cyan);
        if(mode==Mode.Explore)Dungeon();else if(mode==Mode.Battle)BattleView();else if(mode==Mode.Encounter)EncounterView();else if(mode==Mode.Town)TownView();
        else if(mode==Mode.Title){Label(90,160,"BLUE SAPP",true);Label(90,230,"FRAME / FIRST EXPEDITION");Label(90,340,(choice==0?"> ":"  ")+"はじめから\n"+(choice==1?"> ":"  ")+"つづきから");Label(90,460,message);}
        else if(mode==Mode.Intro){Label(40,90,"この町に来たばかりの俺は、ジャンクヤードで\n女性型のロボットを見つけた。\nなけなしの金をはたいて買った。\n\n――こいつが話せるとは思わなかった。\n\n「マスター・・・ワタシノ・・・\n ブヒンヲ・・・アツメテ・・・\n キットヤクニタツ・・・」\n\nEnterで街へ");}
        else {Label(150,290,mode==Mode.Dead?"やられてしまった・・・":$"第{world.state.floor}層クリア！",true);Label(170,380,mode==Mode.Clear&&world.state.floor<4?"Returnで次の層へ":"リターンキーで街に戻る");}
        Box(0,690,800,110,new Color(.015f,.025f,.08f));Line(0,690,800,690,Cyan);Label(16,700,$"HP {world.state.hp}/{world.state.MaxHp}   LV {world.state.level}   EXP {world.state.experience}   外装: {(world.state.armor?"装着":"なし")}");Label(16,738,mode==Mode.Battle?FloorKeys.Hint(world.state.floor):message);
        if(map&&mode==Mode.Explore)MapView();
    }
    void EncounterView(){
        Label(40,35,pendingEnemy==3?"正常な多脚ロボが現れた！":"壊れた多脚ロボが現れた！",true);
        if(gate.Confirmed){Label(350,300,gate.Count(Time.time).ToString(),true);return;}
        if(pendingEnemy==0){
            Label(45,140,"リズムにあわせてキーをたたけ！",true);
            Label(45,220,"ノーツが下の判定ラインに重なるときに入力しよう。\n\nF：ファイバーシールドで防御\nSPACE：回避\nJ：マシンガンで攻撃");
        }else{
            Box(310,210,180,110,Blue);Box(345,235,110,18,Color.red);
            for(int i=0;i<6;i++){if(pendingEnemy!=3&&i==4)continue;Line(330+i*25,300,100+i*100,410,Cyan);}
        }
        Label(100,540,"Returnで戦闘開始",true);
    }
    void TownView(){Box(0,0,800,690,new Color(.015f,.025f,.12f));for(int i=0;i<9;i++){int h=100+(i*47)%180;Box(i*96,360-h,85,h,Blue);for(int j=0;j<4;j++)Box(i*96+15,370-h+j*30,12,12,Color.yellow);}Label(45,50,"ジャンクヤードの街",true);Label(80,430,(choice==0?"> ":"  ")+"ダンジョンにはいる\n\n"+(choice==1?"> ":"  ")+"セーブする");}
    void Dungeon(){
        Box(0,0,800,690,Color.black);int fx=RpgWorld.Dx[world.state.direction],fy=RpgWorld.Dy[world.state.direction];int rx=-fy,ry=fx;
        for(int depth=4;depth>=0;depth--){float near=340f/(depth+1),far=340f/(depth+2);float top=330-near*.8f,bottom=330+near*.8f,ft=330-far*.8f,fb=330+far*.8f;int x=world.state.x+fx*depth,y=world.state.y+fy*depth;
            if(world.Wall(x,y)||world.Closed(x,y)){Box(400-near,top,near*2,bottom-top,world.Closed(x,y)?Color.yellow:Blue);Line(400-near,top,400+near,top,Cyan);Line(400-near,bottom,400+near,bottom,Cyan);continue;}
            for(int side=-1;side<=1;side+=2){if(world.Wall(x+rx*side,y+ry*side)||world.Closed(x+rx*side,y+ry*side)){float a=400+side*near,b=400+side*far;for(float xx=Mathf.Min(a,b);xx<=Mathf.Max(a,b);xx+=2){float t=Mathf.InverseLerp(a,b,xx);Box(xx,Mathf.Lerp(top,ft,t),2,Mathf.Lerp(bottom,fb,t)-Mathf.Lerp(top,ft,t),world.Closed(x+rx*side,y+ry*side)?Color.yellow:Blue);}Line(a,top,b,ft,Cyan);Line(a,bottom,b,fb,Cyan);Line(b,ft,b,fb,Cyan);}}
            Line(400-near,bottom,400-far,fb,Cyan);Line(400+near,bottom,400+far,fb,Cyan);
        }
        Label(20,15,$"第{world.state.floor}層 / ロボ区画   {new[]{"北","東","南","西"}[world.state.direction]}");
    }
    // A code-native 40 x 72 pixel illustration: fixed palette and hard edges.
    // These drawing routines can later be replaced with the user's final CG.
    void DrawAndroid(){
        bool armor=world.state.armor;
        Color shadow=new Color(.12f,.12f,.32f),steel=new Color(.42f,.48f,.65f),light=new Color(.75f,.83f,.9f),accent=new Color(.2f,.85f,.9f);
        Box(810,0,390,800,new Color(.025f,.025f,.09f));
        Label(835,24,"SAPP / ANDROID");
        // All coordinates below are snapped to an eight-pixel grid.
        Action<int,int,int,int,Color> p=(x,y,w,h,c)=>Box(840+x*8,90+y*8,w*8,h*8,c);
        p(15,1,10,1,shadow);p(13,2,14,3,steel);p(12,5,16,6,shadow);
        p(14,5,12,8,steel);p(16,6,8,6,light);p(15,7,2,2,accent);p(23,7,2,2,accent);
        p(19,10,3,1,shadow);p(17,13,6,2,steel);p(18,15,4,2,accent);
        p(10,17,20,3,steel);p(9,18,3,5,light);p(28,18,3,5,light);
        p(13,20,14,8,shadow);p(19,19,2,22,steel);
        for(int y=21;y<28;y+=2){p(14,y,5,1,steel);p(21,y,5,1,steel);}
        p(16,29,8,2,accent);p(17,32,6,2,steel);p(16,35,8,2,steel);
        p(13,39,14,4,steel);p(15,43,10,3,shadow);
        for(int side=0;side<2;side++){
            int arm=side==0?9:29,leg=side==0?14:23;
            p(arm,23,2,10,steel);p(arm-1,33,4,3,accent);p(arm,36,2,10,light);p(arm-1,46,4,4,steel);
            p(leg,46,3,12,steel);p(leg-1,58,5,3,accent);p(leg,61,3,10,light);p(leg-1,71,6,2,steel);
            if(armor){p(arm-1,22,4,9,light);p(leg-1,46,5,10,light);p(leg-1,63,5,7,steel);}
        }
        if(armor){p(12,20,16,7,steel);p(14,21,5,4,light);p(21,21,5,4,light);p(17,28,6,8,shadow);p(12,39,16,5,light);}
        Label(835,755,armor?"外装装着 / SHIELD +":"機械フレーム / NO ARMOR");
    }
    void BattleView(){
        Label(30,20,battle.id==3?"BOSS / 正常な多脚ロボ":"壊れた多脚ロボ",true);
        Box(310,210,180,110,Blue);Box(345,235,110,18,Color.red);
        for(int i=0;i<6;i++){if(battle.id!=3&&i==4)continue;float x=330+i*25;Line(x,300,100+i*100,430,Cyan);Line(100+i*100,430,70+i*110,530,Cyan);}
        Label(40,553,$"敵 HP {battle.hp}/{battle.maxHp}");
        Box(40,580,720,20,new Color(.2f,.1f,.1f));Box(40,580,720*battle.hp/(float)battle.maxHp,20,Color.red);
        Label(40,615,$"自分 HP {world.state.hp}/{world.state.MaxHp}");
        Box(40,642,720,20,new Color(.05f,.08f,.2f));Box(40,642,720*Mathf.Clamp01(world.state.hp/(float)world.state.MaxHp),20,new Color(.15f,.4f,1));
        Box(802,0,398,800,new Color(.025f,.025f,.09f));Label(830,20,"RHYTHM SUPPORT");
        // Physical keyboard order: left hand F, thumbs Space, right hand J.
        int floor=world.state.floor;int count=FloorKeys.Actions(floor).Length;
        const float target=RhythmLayout.Target,top=RhythmLayout.Top;
        for(int lane=0;lane<count;lane++){
            Rect rect=RhythmLayout.Lane(lane,floor);float x=rect.x,width=rect.width;int action=RhythmLayout.ActionAt(lane,floor);Color color=FloorKeys.Kind(action)==1?Color.yellow:Cyan;
            Box(x,top,width,target-top,new Color(.04f,.06f,.15f));Line(x+width/2,top,x+width/2,target,Blue);
            for(int i=battle.index;i<battle.index+8;i++){
                if(battle.pattern[i%battle.pattern.Length]!=action)continue;
                float due=battle.start+2+i*.75f,y=RhythmLayout.NoteY(due,Time.time,world.state.armor);
                if(y>=top&&y<target+24)Box(x+4,y-7,width-8,14,color);
            }
            Box(x,target-2,width,4,color);bitmapFont.Draw(x+(width-bitmapFont.Width(FloorKeys.Name(action)))/2,685,FloorKeys.Name(action),Cyan);
        }
    }
    void OnDestroy(){bitmapFont?.Dispose();}
    void MapView(){
        Box(0,0,1200,800,new Color(0,0,.05f,.78f));Label(60,40,$"第{world.state.floor}層 AUTOMAP / Mで閉じる");
        float size=Mathf.Min(46,Mathf.Min(1000f/world.Width,580f/world.Height)),left=(1200-world.Width*size)/2;
        for(int y=0;y<world.Height;y++)for(int x=0;x<world.Width;x++){
            if(!world.seen[x,y])continue;
            Color c=world.Wall(x,y)?Blue:world.Closed(x,y)?Color.yellow:new Color(.12f,.17f,.23f);
            Box(left+x*size,110+y*size,size-3,size-3,c);
            if(x==world.BossPosition.x&&y==world.BossPosition.y)bitmapFont.Draw(left+x*size,110+y*size,world.Cleared?">":"B",Cyan);
        }
        bitmapFont.Draw(left+world.state.x*size,110+world.state.y*size,new[]{"↑","→","↓","←"}[world.state.direction],Cyan);
    }
}
