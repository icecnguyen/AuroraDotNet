namespace Aurora.World;

/// <summary>
/// Official Minecraft 1.21.4 Protocol Block State IDs extracted from vanilla data reports.
/// </summary>
public static class Block
{
    // Air & Basic Terrains
    public const ushort Air = 0;
    public const ushort Stone = 1;
    public const ushort Granite = 2;
    public const ushort Diorite = 4;
    public const ushort Andesite = 6;
    public const ushort Deepslate = 25918;
    public const ushort Tuff = 22094;
    public const ushort Bedrock = 85;

    // Surface & Soil
    public const ushort GrassBlock = 9;
    public const ushort Dirt = 10;
    public const ushort CoarseDirt = 11;
    public const ushort Podzol = 13;
    public const ushort Sand = 118;
    public const ushort RedSand = 123;
    public const ushort Gravel = 124;

    // Liquids & Frozen
    public const ushort Water = 86;
    public const ushort Lava = 102;
    public const ushort Ice = 5949;
    public const ushort SnowBlock = 5950;

    // Wood & Leaves
    public const ushort OakLog = 137;
    public const ushort OakLeaves = 279;
    public const ushort BirchLog = 143;
    public const ushort BirchLeaves = 335;
    public const ushort SpruceLog = 140;
    public const ushort SpruceLeaves = 307;

    // Vegetation
    public const ushort ShortGrass = 2048;
    public const ushort Dandelion = 2118;
    public const ushort Poppy = 2120;

    // Standard Ores
    public const ushort CoalOre = 133;
    public const ushort DeepslateCoalOre = 134;
    public const ushort IronOre = 131;
    public const ushort DeepslateIronOre = 132;
    public const ushort CopperOre = 23955;
    public const ushort DeepslateCopperOre = 23956;
    public const ushort GoldOre = 129;
    public const ushort DeepslateGoldOre = 130;
    public const ushort RedstoneOre = 5904;
    public const ushort DeepslateRedstoneOre = 5906;
    public const ushort DiamondOre = 4329;
    public const ushort DeepslateDiamondOre = 4330;
    public const ushort LapisOre = 563;
    public const ushort DeepslateLapisOre = 564;

    // Building Materials & Structures
    public const ushort Cobblestone = 14;
    public const ushort OakPlanks = 15;
    public const ushort Glass = 562;
    public const ushort CraftingTable = 4332;
    public const ushort OakStairs = 2940;
    public const ushort CobblestoneStairs = 4780;
    public const ushort OakFence = 6017;
    public const ushort OakDoor = 4688;
    public const ushort MossyCobblestone = 2396;
    public const ushort StoneBricks = 6770;
    public const ushort MossyStoneBricks = 6771;
    public const ushort CrackedStoneBricks = 6772;
    public const ushort Sandstone = 578;
    public const ushort SmoothSandstone = 12192;
    public const ushort CutSandstone = 580;
    public const ushort Obsidian = 2397;
    public const ushort CryingObsidian = 20462;
    public const ushort Netherrack = 6018;
    public const ushort GoldBlock = 2134;
    public const ushort Chest = 3010;
    public const ushort HayBlock = 11605;
    public const ushort PackedIce = 11625;
    public const ushort Snow = 5941;

    // Ocean & Marine Life
    public const ushort Clay = 5967;
    public const ushort Seagrass = 2051;
    public const ushort Kelp = 13773;
    public const ushort KelpPlant = 13799;
    public const ushort MagmaBlock = 13556;

    // Flora & Desert
    public const ushort Fern = 2049;
    public const ushort Cactus = 5951;
    public const ushort DeadBush = 2050;
    public const ushort SugarCane = 5968;
    public const ushort LilyPad = 7632;
    public const ushort Cornflower = 2129;
    public const ushort Allium = 2122;

    // Light Sources & Active
    public const ushort Torch = 2398;
    public const ushort WallTorch = 2399;
    public const ushort Lantern = 19519;
    public const ushort Campfire = 19527;

    // Expanded Wood & Leaves
    public const ushort AcaciaLog = 149;
    public const ushort AcaciaLeaves = 391;
    public const ushort AcaciaPlanks = 19;
    public const ushort AcaciaStairs = 10694;
    public const ushort JungleLog = 146;
    public const ushort JungleLeaves = 363;
    public const ushort JunglePlanks = 18;
    public const ushort JungleStairs = 8611;
    public const ushort DarkOakLog = 155;
    public const ushort DarkOakLeaves = 447;
    public const ushort DarkOakPlanks = 21;
    public const ushort DarkOakStairs = 10854;
    public const ushort Vine = 7101;

    // Expanded Soils & Terracotta (Badlands & Swamps)
    public const ushort Terracotta = 11623;
    public const ushort WhiteTerracotta = 10155;
    public const ushort OrangeTerracotta = 10156;
    public const ushort YellowTerracotta = 10159;
    public const ushort BrownTerracotta = 10167;
    public const ushort RedTerracotta = 10169;
    public const ushort LightGrayTerracotta = 10163;
    public const ushort Mud = 25916;
    public const ushort MudBricks = 6775;
    public const ushort BlueOrchid = 2121;

    // Biome Structures (Pyramids, Temples, Huts, Ships)
    public const ushort ChiseledSandstone = 579;
    public const ushort Tnt = 2138;
    public const ushort StonePressurePlate = 5818;
    public const ushort Dispenser = 567;
    public const ushort TripwireHook = 8304;
    public const ushort Tripwire = 8438;
    public const ushort Cauldron = 8172;
    public const ushort FlowerPot = 9341;
    public const ushort RedMushroomBlock = 6846;
    public const ushort BrownMushroomBlock = 6782;
    public const ushort MushroomStem = 6910;

    private static readonly System.Collections.Generic.Dictionary<ushort, string> IdToName = new();
    private static readonly System.Collections.Generic.Dictionary<string, ushort> NameToId = new(System.StringComparer.OrdinalIgnoreCase);

    static Block()
    {
        void Register(ushort id, string name)
        {
            IdToName[id] = name;
            NameToId[name] = id;
            if (name.StartsWith("minecraft:", System.StringComparison.Ordinal))
            {
                NameToId[name.Substring(10)] = id;
            }
        }

        Register(Air, "minecraft:air");
        Register(Stone, "minecraft:stone");
        Register(Granite, "minecraft:granite");
        Register(Diorite, "minecraft:diorite");
        Register(Andesite, "minecraft:andesite");
        Register(Deepslate, "minecraft:deepslate");
        Register(Tuff, "minecraft:tuff");
        Register(Bedrock, "minecraft:bedrock");

        Register(GrassBlock, "minecraft:grass_block");
        Register(Dirt, "minecraft:dirt");
        Register(CoarseDirt, "minecraft:coarse_dirt");
        Register(Podzol, "minecraft:podzol");
        Register(Sand, "minecraft:sand");
        Register(RedSand, "minecraft:red_sand");
        Register(Gravel, "minecraft:gravel");

        Register(Water, "minecraft:water");
        Register(Lava, "minecraft:lava");
        Register(Ice, "minecraft:ice");
        Register(SnowBlock, "minecraft:snow_block");

        Register(OakLog, "minecraft:oak_log");
        Register(OakLeaves, "minecraft:oak_leaves");
        Register(BirchLog, "minecraft:birch_log");
        Register(BirchLeaves, "minecraft:birch_leaves");
        Register(SpruceLog, "minecraft:spruce_log");
        Register(SpruceLeaves, "minecraft:spruce_leaves");

        Register(ShortGrass, "minecraft:short_grass");
        Register(Dandelion, "minecraft:dandelion");
        Register(Poppy, "minecraft:poppy");

        Register(CoalOre, "minecraft:coal_ore");
        Register(DeepslateCoalOre, "minecraft:deepslate_coal_ore");
        Register(IronOre, "minecraft:iron_ore");
        Register(DeepslateIronOre, "minecraft:deepslate_iron_ore");
        Register(CopperOre, "minecraft:copper_ore");
        Register(DeepslateCopperOre, "minecraft:deepslate_copper_ore");
        Register(GoldOre, "minecraft:gold_ore");
        Register(DeepslateGoldOre, "minecraft:deepslate_gold_ore");
        Register(RedstoneOre, "minecraft:redstone_ore");
        Register(DeepslateRedstoneOre, "minecraft:deepslate_redstone_ore");
        Register(DiamondOre, "minecraft:diamond_ore");
        Register(DeepslateDiamondOre, "minecraft:deepslate_diamond_ore");
        Register(LapisOre, "minecraft:lapis_ore");
        Register(DeepslateLapisOre, "minecraft:deepslate_lapis_ore");

        Register(Cobblestone, "minecraft:cobblestone");
        Register(OakPlanks, "minecraft:oak_planks");
        Register(Glass, "minecraft:glass");
        Register(CraftingTable, "minecraft:crafting_table");
        Register(OakStairs, "minecraft:oak_stairs");
        Register(CobblestoneStairs, "minecraft:cobblestone_stairs");
        Register(OakFence, "minecraft:oak_fence");
        Register(OakDoor, "minecraft:oak_door");
        Register(MossyCobblestone, "minecraft:mossy_cobblestone");
        Register(StoneBricks, "minecraft:stone_bricks");
        Register(MossyStoneBricks, "minecraft:mossy_stone_bricks");
        Register(CrackedStoneBricks, "minecraft:cracked_stone_bricks");
        Register(Sandstone, "minecraft:sandstone");
        Register(SmoothSandstone, "minecraft:smooth_sandstone");
        Register(CutSandstone, "minecraft:cut_sandstone");
        Register(Obsidian, "minecraft:obsidian");
        Register(CryingObsidian, "minecraft:crying_obsidian");
        Register(Netherrack, "minecraft:netherrack");
        Register(GoldBlock, "minecraft:gold_block");
        Register(Chest, "minecraft:chest");
        Register(HayBlock, "minecraft:hay_block");
        Register(PackedIce, "minecraft:packed_ice");
        Register(Snow, "minecraft:snow");

        Register(Clay, "minecraft:clay");
        Register(Seagrass, "minecraft:seagrass");
        Register(Kelp, "minecraft:kelp");
        Register(KelpPlant, "minecraft:kelp_plant");
        Register(MagmaBlock, "minecraft:magma_block");

        Register(Fern, "minecraft:fern");
        Register(Cactus, "minecraft:cactus");
        Register(DeadBush, "minecraft:dead_bush");
        Register(SugarCane, "minecraft:sugar_cane");
        Register(LilyPad, "minecraft:lily_pad");
        Register(Cornflower, "minecraft:cornflower");
        Register(Allium, "minecraft:allium");

        Register(Torch, "minecraft:torch");
        Register(WallTorch, "minecraft:wall_torch");
        Register(Lantern, "minecraft:lantern");
        Register(Campfire, "minecraft:campfire");

        Register(AcaciaLog, "minecraft:acacia_log");
        Register(AcaciaLeaves, "minecraft:acacia_leaves");
        Register(AcaciaPlanks, "minecraft:acacia_planks");
        Register(AcaciaStairs, "minecraft:acacia_stairs");
        Register(JungleLog, "minecraft:jungle_log");
        Register(JungleLeaves, "minecraft:jungle_leaves");
        Register(JunglePlanks, "minecraft:jungle_planks");
        Register(JungleStairs, "minecraft:jungle_stairs");
        Register(DarkOakLog, "minecraft:dark_oak_log");
        Register(DarkOakLeaves, "minecraft:dark_oak_leaves");
        Register(DarkOakPlanks, "minecraft:dark_oak_planks");
        Register(DarkOakStairs, "minecraft:dark_oak_stairs");
        Register(Vine, "minecraft:vine");

        Register(Terracotta, "minecraft:terracotta");
        Register(WhiteTerracotta, "minecraft:white_terracotta");
        Register(OrangeTerracotta, "minecraft:orange_terracotta");
        Register(YellowTerracotta, "minecraft:yellow_terracotta");
        Register(BrownTerracotta, "minecraft:brown_terracotta");
        Register(RedTerracotta, "minecraft:red_terracotta");
        Register(LightGrayTerracotta, "minecraft:light_gray_terracotta");
        Register(Mud, "minecraft:mud");
        Register(MudBricks, "minecraft:mud_bricks");
        Register(BlueOrchid, "minecraft:blue_orchid");

        Register(ChiseledSandstone, "minecraft:chiseled_sandstone");
        Register(Tnt, "minecraft:tnt");
        Register(StonePressurePlate, "minecraft:stone_pressure_plate");
        Register(Dispenser, "minecraft:dispenser");
        Register(TripwireHook, "minecraft:tripwire_hook");
        Register(Tripwire, "minecraft:tripwire");
        Register(Cauldron, "minecraft:cauldron");
        Register(FlowerPot, "minecraft:flower_pot");
        Register(RedMushroomBlock, "minecraft:red_mushroom_block");
        Register(BrownMushroomBlock, "minecraft:brown_mushroom_block");
        Register(MushroomStem, "minecraft:mushroom_stem");
    }

    public static string GetName(ushort id)
    {
        if (IdToName.TryGetValue(id, out var name))
        {
            return name;
        }
        return BlockRegistry.GetBlockName(id);
    }

    public static ushort GetId(string name)
    {
        if (string.IsNullOrEmpty(name)) return Air;
        if (NameToId.TryGetValue(name, out var id))
        {
            return id;
        }
        return BlockRegistry.GetDefaultStateId(name);
    }
}
