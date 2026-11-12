using System;
using UnityEngine;

// Shared geometry keeps visual lanes in physical keyboard order.
public static class RhythmLayout
{
    public const float Top = 90, Target = 650;
    public static int ActionAt(int lane,int floor=1)
    {
        var actions=FloorKeys.Actions(floor);
        if(lane<0||lane>=actions.Length)throw new ArgumentOutOfRangeException(nameof(lane));return actions[lane];
    }
    public static Rect Lane(int lane,int floor=1)
    {
        ActionAt(lane,floor);float stride=378f/FloorKeys.Actions(floor).Length;
        return new Rect(818 + lane * stride, Top, stride-6, Target - Top);
    }
    public static float NoteY(float due, float now, bool armor)
        => Target - (due - now) * (Target - Top) / (armor ? 3 : 2);
}
