using System;
using Aurora.World.Entities;

namespace Aurora.World.Gameplay;

/// <summary>
/// Ported from Pumpkin's hunger.rs: manages player hunger, saturation, exhaustion,
/// natural health regeneration, and starvation damage.
/// </summary>
public sealed class HungerManager
{
    public const int MaxFood = 20;
    public const float ExhaustionCost = 4.0f;
    public const float MaxExhaustion = 40.0f;

    public int TickTimer { get; set; }

    /// <summary>
    /// Advances hunger simulation by 1 tick (called 20 times per second).
    /// </summary>
    public void Tick(Player player, out bool healthChanged)
    {
        ArgumentNullException.ThrowIfNull(player);
        healthChanged = false;

        // 1. Consume accumulated exhaustion
        if (player.FoodExhaustion > ExhaustionCost)
        {
            player.FoodExhaustion -= ExhaustionCost;
            if (player.FoodSaturation > 0.0f)
            {
                player.FoodSaturation = Math.Max(0.0f, player.FoodSaturation - 1.0f);
            }
            else
            {
                player.FoodLevel = Math.Max(0, player.FoodLevel - 1);
            }
            healthChanged = true;
        }

        // 2. Natural health regeneration & Starvation
        if (player.FoodSaturation > 0.0f && player.FoodLevel >= 20 && player.Health < 20.0f)
        {
            // Rapid heal (consumes saturation)
            TickTimer++;
            if (TickTimer >= 10)
            {
                float cost = Math.Min(player.FoodSaturation, 6.0f);
                float heal = cost / 6.0f;
                player.Heal(heal);
                player.FoodExhaustion = Math.Min(MaxExhaustion, player.FoodExhaustion + cost);
                TickTimer = 0;
                healthChanged = true;
            }
        }
        else if (player.FoodLevel >= 18 && player.Health < 20.0f)
        {
            // Normal heal (consumes 6.0 exhaustion)
            TickTimer++;
            if (TickTimer >= 80)
            {
                player.Heal(1.0f);
                player.FoodExhaustion = Math.Min(MaxExhaustion, player.FoodExhaustion + 6.0f);
                TickTimer = 0;
                healthChanged = true;
            }
        }
        else if (player.FoodLevel == 0)
        {
            // Starvation damage
            TickTimer++;
            if (TickTimer >= 80)
            {
                player.Damage(1.0f);
                TickTimer = 0;
                healthChanged = true;
            }
        }
        else
        {
            TickTimer = 0;
        }
    }

    /// <summary>
    /// Consumes food, restoring hunger and saturation up to limits.
    /// </summary>
    public static void Eat(Player player, int food, float saturation)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.FoodLevel = Math.Min(MaxFood, player.FoodLevel + food);
        player.FoodSaturation = Math.Clamp(player.FoodSaturation + saturation, 0.0f, player.FoodLevel);
    }

    /// <summary>
    /// Adds food exhaustion (e.g. from sprinting, jumping, breaking blocks).
    /// </summary>
    public static void AddExhaustion(Player player, float exhaustion)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.FoodExhaustion = Math.Min(MaxExhaustion, player.FoodExhaustion + exhaustion);
    }

    /// <summary>
    /// Resets hunger state to default full state upon respawn.
    /// </summary>
    public void Restart(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.FoodLevel = MaxFood;
        player.FoodSaturation = 5.0f;
        player.FoodExhaustion = 0.0f;
        TickTimer = 0;
    }
}
