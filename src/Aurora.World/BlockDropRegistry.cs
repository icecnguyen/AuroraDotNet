using System;

namespace Aurora.World;

/// <summary>
/// Determines which item drops when a block is broken in Survival mode.
/// </summary>
public static class BlockDropRegistry
{
    public static Entities.ItemStack GetDrop(ushort blockStateId)
    {
        string blockName = Block.GetName(blockStateId);
        if (string.IsNullOrEmpty(blockName)) return Entities.ItemStack.Empty;

        if (blockName.StartsWith("minecraft:", StringComparison.Ordinal))
        {
            blockName = blockName.Substring(10);
        }

        string dropItemName = blockName switch
        {
            "air" or "water" or "lava" or "bedrock" or "glass" => string.Empty,
            "stone" => "cobblestone",
            "grass_block" => "dirt",
            "coal_ore" or "deepslate_coal_ore" => "coal",
            "iron_ore" or "deepslate_iron_ore" => "raw_iron",
            "copper_ore" or "deepslate_copper_ore" => "raw_copper",
            "gold_ore" or "deepslate_gold_ore" => "raw_gold",
            "diamond_ore" or "deepslate_diamond_ore" => "diamond",
            "lapis_ore" or "deepslate_lapis_ore" => "lapis_lazuli",
            "redstone_ore" or "deepslate_redstone_ore" => "redstone",
            "deepslate" => "cobbled_deepslate",
            _ => blockName
        };

        if (string.IsNullOrEmpty(dropItemName)) return Entities.ItemStack.Empty;

        int itemId = ItemRegistry.GetItemId("minecraft:" + dropItemName);
        if (itemId <= 0)
        {
            itemId = ItemRegistry.GetItemId(dropItemName);
        }

        if (itemId <= 0) return Entities.ItemStack.Empty;
        return new Entities.ItemStack(itemId, 1);
    }
}
