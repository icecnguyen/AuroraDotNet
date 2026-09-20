using System;

namespace Aurora.World.Gameplay;

/// <summary>
/// Provides Minecraft block hardness and mining calculation according to vanilla rules.
/// </summary>
public static class BlockHardnessRegistry
{
    public static float GetHardness(ushort stateId)
    {
        string name = BlockRegistry.GetBlockName(stateId);
        if (name.StartsWith("minecraft:", StringComparison.Ordinal))
        {
            name = name.Substring(10);
        }

        return name switch
        {
            "air" or "cave_air" or "void_air" => 0.0f,
            "bedrock" or "barrier" or "command_block" or "end_portal_frame" => -1.0f, // Unbreakable
            "obsidian" or "crying_obsidian" or "respawn_anchor" => 50.0f,
            "anvil" or "chipped_anvil" or "damaged_anvil" => 5.0f,
            "coal_ore" or "iron_ore" or "copper_ore" or "gold_ore" or "redstone_ore" or "emerald_ore" or "lapis_ore" or "diamond_ore" or "nether_quartz_ore" or "nether_gold_ore" => 3.0f,
            "deepslate_coal_ore" or "deepslate_iron_ore" or "deepslate_copper_ore" or "deepslate_gold_ore" or "deepslate_redstone_ore" or "deepslate_emerald_ore" or "deepslate_lapis_ore" or "deepslate_diamond_ore" or "deepslate" or "reinforced_deepslate" => 4.5f,
            "stone" or "andesite" or "diorite" or "granite" or "tuff" => 1.5f,
            "cobblestone" or "mossy_cobblestone" or "stone_bricks" or "mossy_stone_bricks" or "cracked_stone_bricks" or "chiseled_stone_bricks" or "blackstone" or "basalt" => 2.0f,
            "oak_log" or "spruce_log" or "birch_log" or "jungle_log" or "acacia_log" or "dark_oak_log" or "mangrove_log" or "cherry_log" or "pale_oak_log" or "crimson_stem" or "warped_stem" => 2.0f,
            "oak_wood" or "spruce_wood" or "birch_wood" or "jungle_wood" or "acacia_wood" or "dark_oak_wood" or "mangrove_wood" or "cherry_wood" or "pale_oak_wood" or "crimson_hyphae" or "warped_hyphae" => 2.0f,
            "oak_planks" or "spruce_planks" or "birch_planks" or "jungle_planks" or "acacia_planks" or "dark_oak_planks" or "mangrove_planks" or "cherry_planks" or "bamboo_planks" or "crimson_planks" or "warped_planks" => 2.0f,
            "dirt" or "coarse_dirt" or "podzol" or "mycelium" or "mud" => 0.5f,
            "grass_block" => 0.6f,
            "sand" or "red_sand" or "gravel" or "soul_sand" or "soul_soil" or "clay" => 0.5f,
            "snow_block" => 0.2f,
            "ice" or "packed_ice" or "blue_ice" => 0.5f,
            "glass" or "tinted_glass" or "glass_pane" => 0.3f,
            "oak_leaves" or "spruce_leaves" or "birch_leaves" or "jungle_leaves" or "acacia_leaves" or "dark_oak_leaves" or "mangrove_leaves" or "cherry_leaves" or "pale_oak_leaves" or "azalea_leaves" => 0.2f,
            "dandelion" or "poppy" or "blue_orchid" or "allium" or "azure_bluet" or "red_tulip" or "orange_tulip" or "white_tulip" or "pink_tulip" or "oxeye_daisy" or "cornflower" or "lily_of_the_valley" or "wither_rose" or "sunflower" or "lilac" or "rose_bush" or "peony" or "torch" or "short_grass" or "tall_grass" or "fern" or "large_fern" => 0.0f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Checks if the block can be harvested for drops with the given held item.
    /// </summary>
    public static bool CanHarvest(ushort stateId, int heldItemId)
    {
        string blockName = BlockRegistry.GetBlockName(stateId);
        if (blockName.StartsWith("minecraft:", StringComparison.Ordinal))
        {
            blockName = blockName.Substring(10);
        }

        string heldItem = ItemRegistry.GetItemName(heldItemId);
        if (heldItem.StartsWith("minecraft:", StringComparison.Ordinal))
        {
            heldItem = heldItem.Substring(10);
        }

        bool isPickaxe = heldItem.EndsWith("_pickaxe", StringComparison.Ordinal);

        // Blocks requiring pickaxe to drop anything
        if (blockName.Contains("stone", StringComparison.Ordinal) ||
            blockName.Contains("ore", StringComparison.Ordinal) ||
            blockName.Contains("cobble", StringComparison.Ordinal) ||
            blockName.Contains("deepslate", StringComparison.Ordinal) ||
            blockName == "obsidian")
        {
            if (!isPickaxe) return false;

            if (blockName == "obsidian")
            {
                return heldItem.StartsWith("diamond", StringComparison.Ordinal) || heldItem.StartsWith("netherite", StringComparison.Ordinal);
            }

            if (blockName.Contains("diamond_ore", StringComparison.Ordinal) || blockName.Contains("emerald_ore", StringComparison.Ordinal) || blockName.Contains("gold_ore", StringComparison.Ordinal) || blockName.Contains("redstone_ore", StringComparison.Ordinal))
            {
                return heldItem.StartsWith("iron", StringComparison.Ordinal) || heldItem.StartsWith("diamond", StringComparison.Ordinal) || heldItem.StartsWith("netherite", StringComparison.Ordinal);
            }

            if (blockName.Contains("iron_ore", StringComparison.Ordinal) || blockName.Contains("copper_ore", StringComparison.Ordinal) || blockName.Contains("lapis_ore", StringComparison.Ordinal))
            {
                return !heldItem.StartsWith("wooden", StringComparison.Ordinal) && !heldItem.StartsWith("golden", StringComparison.Ordinal);
            }

            return true;
        }

        return true;
    }
}
