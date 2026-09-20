using System;
using Aurora.World.Entities;

namespace Aurora.World.Gameplay;

/// <summary>
/// Basic 2x2 inventory crafting recipe processor for Survival mode.
/// </summary>
public static class CraftingManager
{
    /// <summary>
    /// Evaluates the 2x2 crafting grid (slots 1..4 in player inventory) and updates slot 0 with the result.
    /// </summary>
    public static void UpdateCraftingResult(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var slot1 = inventory.GetItem(1);
        var slot2 = inventory.GetItem(2);
        var slot3 = inventory.GetItem(3);
        var slot4 = inventory.GetItem(4);

        int filledCount = (slot1.IsEmpty ? 0 : 1) + (slot2.IsEmpty ? 0 : 1) + (slot3.IsEmpty ? 0 : 1) + (slot4.IsEmpty ? 0 : 1);

        if (filledCount == 0)
        {
            inventory.SetItem(0, ItemStack.Empty);
            return;
        }

        // 1. Single Log -> 4 Planks
        if (filledCount == 1)
        {
            var single = !slot1.IsEmpty ? slot1 : !slot2.IsEmpty ? slot2 : !slot3.IsEmpty ? slot3 : slot4;
            string itemName = ItemRegistry.GetItemName(single.ItemId);
            if (itemName.StartsWith("minecraft:", StringComparison.Ordinal))
            {
                itemName = itemName.Substring(10);
            }

            if (itemName.EndsWith("_log", StringComparison.Ordinal) || itemName.EndsWith("_wood", StringComparison.Ordinal) || itemName.EndsWith("_stem", StringComparison.Ordinal) || itemName.EndsWith("_hyphae", StringComparison.Ordinal))
            {
                string woodType = itemName.Replace("_log", "", StringComparison.Ordinal)
                                          .Replace("_wood", "", StringComparison.Ordinal)
                                          .Replace("_stem", "", StringComparison.Ordinal)
                                          .Replace("_hyphae", "", StringComparison.Ordinal);
                string planksName = woodType + "_planks";
                int planksId = ItemRegistry.GetItemId(planksName);
                if (planksId > 0)
                {
                    inventory.SetItem(0, new ItemStack(planksId, 4));
                    return;
                }
            }
        }

        // 2. Four Planks -> Crafting Table
        if (filledCount == 4)
        {
            bool allPlanks = IsPlanks(slot1.ItemId) && IsPlanks(slot2.ItemId) && IsPlanks(slot3.ItemId) && IsPlanks(slot4.ItemId);
            if (allPlanks)
            {
                int tableId = ItemRegistry.GetItemId("crafting_table");
                if (tableId > 0)
                {
                    inventory.SetItem(0, new ItemStack(tableId, 1));
                    return;
                }
            }
        }

        // 3. Two Planks (vertical column: 1 & 3 or 2 & 4) -> 4 Sticks
        if (filledCount == 2)
        {
            bool col1 = IsPlanks(slot1.ItemId) && IsPlanks(slot3.ItemId) && slot2.IsEmpty && slot4.IsEmpty;
            bool col2 = IsPlanks(slot2.ItemId) && IsPlanks(slot4.ItemId) && slot1.IsEmpty && slot3.IsEmpty;
            if (col1 || col2)
            {
                int stickId = ItemRegistry.GetItemId("stick");
                if (stickId > 0)
                {
                    inventory.SetItem(0, new ItemStack(stickId, 4));
                    return;
                }
            }
        }

        // No recipe match
        inventory.SetItem(0, ItemStack.Empty);
    }

    private static bool IsPlanks(int itemId)
    {
        if (itemId <= 0) return false;
        string name = ItemRegistry.GetItemName(itemId);
        return name.EndsWith("_planks", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Consumes 1 item from each filled crafting slot (1..4) when the output item is taken.
    /// </summary>
    public static void ConsumeCraftingInputs(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        for (int i = 1; i <= 4; i++)
        {
            var item = inventory.GetItem(i);
            if (!item.IsEmpty)
            {
                if (item.Count <= 1)
                {
                    inventory.SetItem(i, ItemStack.Empty);
                }
                else
                {
                    inventory.SetItem(i, new ItemStack(item.ItemId, (byte)(item.Count - 1)));
                }
            }
        }

        UpdateCraftingResult(inventory);
    }
}
