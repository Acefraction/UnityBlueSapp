using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Same PNG glyphs and ink mask as UnityKaeShoot. No system font dependency.
public sealed class MisakiFont : IDisposable
{
    [Serializable] public sealed class Glyph { public string character, sheet; public int x,y,w,h; }
    [Serializable] public sealed class Catalog { public Glyph[] glyphs; }
    readonly Dictionary<string,Glyph> glyphs=new Dictionary<string,Glyph>();
    readonly Texture2D full,half;
    public MisakiFont(string directory) {
        var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(directory,"glyphs.json")));
        foreach(var g in catalog.glyphs)glyphs[g.character]=g;
        full=LoadMask(Path.Combine(directory,"misaki_gothic_2nd.png"));
        half=LoadMask(Path.Combine(directory,"misaki_gothic_2nd_4x8.png"));
    }
    public Glyph Lookup(string character) => glyphs.TryGetValue(character,out var g)?g:glyphs["?"];
    public static bool IsInk(Color32 c) => c.a>128&&(c.r<=240||c.g<=240||c.b<=240);
    static Texture2D LoadMask(string path) {
        var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
        if(!source.LoadImage(File.ReadAllBytes(path))){Release(source);throw new IOException("Invalid font PNG: "+path);}
        int w=source.width,h=source.height;var pixels=source.GetPixels32();var padded=new Color32[(w+2)*(h+2)];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)padded[(y+1)*(w+2)+x+1]=IsInk(pixels[y*w+x])?new Color32(255,255,255,255):new Color32(0,0,0,0);
        var mask=new Texture2D(w+2,h+2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        mask.SetPixels32(padded);mask.Apply(false,true);Release(source);return mask;
    }
    public float Width(string value,int scale=2) {
        int width=0,max=0;foreach(char c in value){if(c=='\r')continue;if(c=='\n'){max=Math.Max(max,width);width=0;}else width+=Lookup(c.ToString()).w;}
        return Math.Max(max,width)*scale;
    }
    public void Draw(float x,float y,string value,Color color,int scale=2) {
        float left=Mathf.Round(x),cursor=left,top=Mathf.Round(y);Color previous=GUI.color;GUI.color=color;
        foreach(char c in value){if(c=='\r')continue;if(c=='\n'){cursor=left;top+=10*scale;continue;}
            var g=Lookup(c.ToString());var texture=g.sheet=="full"?full:half;
            var uv=new Rect((g.x+1f)/texture.width,(texture.height-1f-g.y-g.h)/texture.height,g.w/(float)texture.width,g.h/(float)texture.height);
            GUI.DrawTextureWithTexCoords(new Rect(cursor,top,g.w*scale,g.h*scale),texture,uv);cursor+=g.w*scale;
        }
        GUI.color=previous;
    }
    static void Release(Texture2D texture){if(Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture);}
    public void Dispose(){Release(full);Release(half);}
}
