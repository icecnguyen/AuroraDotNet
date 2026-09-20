using System;
using Aurora.World.Entities;

namespace Aurora.World.Gameplay;

/// <summary>
/// Ported from Pumpkin's breath.rs: handles player underwater oxygen depletion,
/// surface recovery, and drowning damage.
/// </summary>
public sealed class BreathManager
{
    public const short MaxAir = 300;
    public const short AirRecoveryRate = 4;
    public const short AirDepletionRate = 1;
    public const int DrowningInterval = 20;
    public const float DrowningDamage = 2.0f;

    public int DrowningTick { get; set; }

    /// <summary>
    /// Advances air / drowning simulation by 1 tick.
    /// </summary>
    public void Tick(Player player, bool isSubmergedInWater, out bool tookDamage)
    {
        ArgumentNullException.ThrowIfNull(player);
        tookDamage = false;

        if (isSubmergedInWater)
        {
            player.Air = (short)Math.Max(0, player.Air - AirDepletionRate);
            if (player.Air <= 0)
            {
                DrowningTick++;
                if (DrowningTick >= DrowningInterval)
                {
                    DrowningTick = 0;
                    player.Damage(DrowningDamage);
                    tookDamage = true;
                }
            }
        }
        else
        {
            player.Air = (short)Math.Min(MaxAir, player.Air + AirRecoveryRate);
            DrowningTick = 0;
        }
    }

    /// <summary>
    /// Resets air supply to full capacity.
    /// </summary>
    public void Reset(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.Air = MaxAir;
        DrowningTick = 0;
    }
}
