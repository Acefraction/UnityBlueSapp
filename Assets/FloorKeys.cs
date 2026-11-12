using System;
using UnityEngine;

public static class FloorKeys
{
    public static int[] Actions(int floor) {
        switch(floor){case 1:return new[]{0,2,1};case 2:return new[]{3,0,2,1,4};
            case 3:return new[]{5,3,0,2,1,4,6};case 4:return new[]{7,5,3,0,2,1,4,6,8};
            default:throw new ArgumentOutOfRangeException(nameof(floor));}
    }
    public static KeyCode Key(int action) => new[]{KeyCode.F,KeyCode.J,KeyCode.Space,KeyCode.D,KeyCode.K,KeyCode.S,KeyCode.L,KeyCode.A,KeyCode.Semicolon}[action];
    public static string Name(int action) => new[]{"F","J","SP","D","K","S","L","A",";"}[action];
    public static int Kind(int action) => action==2?2:(action==1||action==4||action==6||action==8?1:0);
    public static string Hint(int floor)=>floor==1?"F:盾  SPACE:回避  J:銃":string.Join(" / ",Array.ConvertAll(Actions(floor),a=>Name(a)))+"  左:防御 中央:回避 右:攻撃";
}
