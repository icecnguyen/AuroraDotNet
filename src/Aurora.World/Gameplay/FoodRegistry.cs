using System;
using System.Collections.Generic;

namespace Aurora.World.Gameplay;

/// <summary>
/// Food item details matching official Minecraft 1.21.4 values.
/// </summary>
public readonly record struct FoodInfo(int Nutrition, float Saturation, bool CanAlwaysEat = false);

/// <summary>
/// Registry of all edible vanilla food items.
/// </summary>
public static class FoodRegistry
{
    private static readonly Dictionary<string, FoodInfo> FoodsByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apple"] = new(4, 2.4f),
        ["baked_potato"] = new(5, 6.0f),
        ["beetroot"] = new(1, 1.2f),
        ["beetroot_soup"] = new(6, 7.2f),
        ["bread"] = new(5, 6.0f),
        ["carrot"] = new(3, 3.6f),
        ["chorus_fruit"] = new(4, 2.4f, CanAlwaysEat: true),
        ["cooked_beef"] = new(8, 12.8f),
        ["steak"] = new(8, 12.8f),
        ["cooked_chicken"] = new(6, 7.2f),
        ["cooked_cod"] = new(5, 6.0f),
        ["cooked_mutton"] = new(6, 9.6f),
        ["cooked_porkchop"] = new(8, 12.8f),
        ["cooked_rabbit"] = new(5, 6.0f),
        ["cooked_salmon"] = new(6, 9.6f),
        ["cookie"] = new(2, 0.4f),
        ["dried_kelp"] = new(1, 0.6f),
        ["enchanted_golden_apple"] = new(4, 9.6f, CanAlwaysEat: true),
        ["golden_apple"] = new(4, 9.6f, CanAlwaysEat: true),
        ["golden_carrot"] = new(6, 14.4f, CanAlwaysEat: true),
        ["honey_bottle"] = new(6, 1.2f, CanAlwaysEat: true),
        ["melon_slice"] = new(2, 1.2f),
        ["mushroom_stew"] = new(6, 7.2f),
        ["poisonous_potato"] = new(2, 1.2f),
        ["potato"] = new(1, 0.6f),
        ["pufferfish"] = new(1, 0.2f),
        ["pumpkin_pie"] = new(8, 4.8f),
        ["rabbit_stew"] = new(10, 12.0f),
        ["beef"] = new(3, 1.8f),
        ["chicken"] = new(2, 1.2f),
        ["cod"] = new(2, 0.4f),
        ["mutton"] = new(2, 1.2f),
        ["porkchop"] = new(3, 1.8f),
        ["rabbit"] = new(3, 1.8f),
        ["salmon"] = new(2, 0.4f),
        ["rotten_flesh"] = new(4, 0.8f),
        ["spider_eye"] = new(2, 3.2f),
        ["suspicious_stew"] = new(6, 7.2f),
        ["sweet_berries"] = new(2, 0.4f),
        ["glow_berries"] = new(2, 0.4f)
    };

    private static readonly Dictionary<int, FoodInfo> FoodsById = InitializeFoodsById();

    private static Dictionary<int, FoodInfo> InitializeFoodsById()
    {
        var dict = new Dictionary<int, FoodInfo>(FoodsByName.Count);
        foreach (var (name, info) in FoodsByName)
        {
            int id = ItemRegistry.GetItemId(name);
            if (id > 0)
            {
                dict.TryAdd(id, info);
            }
        }
        return dict;
    }

    /// <summary>
    /// Checks if an item ID corresponds to an edible food item.
    /// </summary>
    public static bool TryGetFood(int itemId, out FoodInfo foodInfo)
    {
        return FoodsById.TryGetValue(itemId, out foodInfo);
    }

    /// <summary>
    /// Checks if an item name corresponds to an edible food item.
    /// </summary>
    public static bool TryGetFood(string itemName, out FoodInfo foodInfo)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            foodInfo = default;
            return false;
        }

        string cleanName = itemName.StartsWith("minecraft:", StringComparison.OrdinalIgnoreCase)
            ? itemName.Substring(10)
            : itemName;

        return FoodsByName.TryGetValue(cleanName, out foodInfo);
    }
}
