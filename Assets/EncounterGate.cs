using System;

public sealed class EncounterGate
{
    public bool Confirmed { get; private set; }
    public float StartsAt { get; private set; }
    public void Confirm(float now)
    {
        if (Confirmed) return;
        Confirmed = true;
        StartsAt = now + 3;
    }
    public bool Ready(float now) => Confirmed && now >= StartsAt;
    public int Count(float now) => Confirmed ? Math.Max(1, (int)Math.Ceiling(StartsAt - now)) : 0;
}
