using System;
using UnityEngine;

// Shared geometry keeps visual lanes in physical keyboard order.
public static class RhythmLayout
{
    public const float Top = 90, Target = 650;
    public static int ActionAt(int lane)
    {
        switch (lane) { case 0: return 0; case 1: return 2; case 2: return 1;
            default: throw new ArgumentOutOfRangeException(nameof(lane)); }
    }
    public static Rect Lane(int lane)
    {
        ActionAt(lane);
        return new Rect(818 + lane * 126, Top, 116, Target - Top);
    }
    public static float NoteY(float due, float now, bool armor)
        => Target - (due - now) * (Target - Top) / (armor ? 3 : 2);
}
