using System;

namespace Aurora.World.Entities;

/// <summary>
/// Player abilities determining flight, invulnerability, and speeds.
/// </summary>
public sealed class PlayerAbilities
{
    public bool Invulnerable { get; set; }
    public bool Flying { get; set; }
    public bool AllowFlying { get; set; }
    public bool CreativeMode { get; set; }
    public float FlySpeed { get; set; } = 0.05f;
    public float WalkSpeed { get; set; } = 0.10f;

    public byte Flags
    {
        get
        {
            byte f = 0;
            if (Invulnerable) f |= 0x01;
            if (Flying) f |= 0x02;
            if (AllowFlying) f |= 0x04;
            if (CreativeMode) f |= 0x08;
            return f;
        }
        set
        {
            Invulnerable = (value & 0x01) != 0;
            Flying = (value & 0x02) != 0;
            AllowFlying = (value & 0x04) != 0;
            CreativeMode = (value & 0x08) != 0;
        }
    }

    public static PlayerAbilities CreateDefault(GameMode mode)
    {
        return mode switch
        {
            GameMode.Creative => new PlayerAbilities
            {
                Invulnerable = true,
                Flying = false,
                AllowFlying = true,
                CreativeMode = true,
                FlySpeed = 0.05f,
                WalkSpeed = 0.10f
            },
            GameMode.Spectator => new PlayerAbilities
            {
                Invulnerable = true,
                Flying = true,
                AllowFlying = true,
                CreativeMode = true,
                FlySpeed = 0.05f,
                WalkSpeed = 0.10f
            },
            _ => new PlayerAbilities
            {
                Invulnerable = false,
                Flying = false,
                AllowFlying = false,
                CreativeMode = false,
                FlySpeed = 0.05f,
                WalkSpeed = 0.10f
            }
        };
    }
}
