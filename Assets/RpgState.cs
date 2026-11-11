using System;

[Serializable]
public sealed class RpgState
{
    public int version = 1;
    public int x = 3, y = 3;
    public int hp = 30, maxHp = 30, mp = 12, maxMp = 12;
    public int level = 1, experience, gold, potions = 3;
    public bool hasKey, victory;

    public void Rest() { hp = maxHp; mp = maxMp; }

    public bool UsePotion()
    {
        if (potions <= 0 || hp >= maxHp) return false;
        potions--;
        hp = Math.Min(maxHp, hp + 20);
        return true;
    }

    public bool GainExperience(int amount)
    {
        experience += amount;
        if (experience < level * 12) return false;
        experience -= level * 12;
        level++;
        maxHp += 8;
        maxMp += 3;
        Rest();
        return true;
    }
}
