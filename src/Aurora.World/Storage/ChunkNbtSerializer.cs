using System;
using System.Collections.Generic;
using Aurora.Core.Math;
using Aurora.Core.Nbt;

namespace Aurora.World.Storage;

/// <summary>
/// Serializes and deserializes Minecraft 1.21.4 Anvil Chunks to and from standard NBT compound trees.
/// Fully compatible with vanilla Minecraft, MCA Selector, and Pumpkin-MC.
/// </summary>
public static class ChunkNbtSerializer
{
    public const int MinecraftDataVersion = 4082; // Minecraft 1.21.4

    public static NbtCompound Serialize(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        var root = new NbtCompound();
        root.Add("DataVersion", new NbtInt(MinecraftDataVersion));
        root.Add("xPos", new NbtInt(chunk.Position.X));
        root.Add("zPos", new NbtInt(chunk.Position.Z));
        root.Add("yPos", new NbtInt(-4)); // Lowest section is Y = -4 (Y = -64)
        root.Add("Status", new NbtString("minecraft:full"));

        var sectionsList = new NbtList(NbtTagType.Compound);

        var rawSkyLight = chunk.RawSkyLight;
        var rawBlockLight = chunk.RawBlockLight;

        // 24 Sections from Y = -4 to 19 (heights -64 to 319)
        for (int i = 0; i < 24; i++)
        {
            sbyte sectionY = (sbyte)(i - 4);
            int baseY = -64 + (i * 16);

            var sectionComp = new NbtCompound();
            sectionComp.Add("Y", new NbtByte((byte)sectionY));

            // 1. Gather distinct blocks in this 16x16x16 section
            var uniqueBlocks = new List<ushort>();
            var blockToPalette = new Dictionary<ushort, int>();

            for (int y = 0; y < 16; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        ushort state = chunk.GetBlockState(x, baseY + y, z);
                        if (!blockToPalette.ContainsKey(state))
                        {
                            blockToPalette[state] = uniqueBlocks.Count;
                            uniqueBlocks.Add(state);
                        }
                    }
                }
            }

            // 2. Build block_states compound
            var blockStatesComp = new NbtCompound();
            var paletteList = new NbtList(NbtTagType.Compound);

            foreach (ushort state in uniqueBlocks)
            {
                var entry = new NbtCompound();
                entry.Add("Name", new NbtString(Block.GetName(state)));
                paletteList.Add(entry);
            }
            blockStatesComp.Add("palette", paletteList);

            if (uniqueBlocks.Count > 1)
            {
                int bitsPerEntry = Math.Max(4, (int)Math.Ceiling(Math.Log2(uniqueBlocks.Count)));
                int entriesPerLong = 64 / bitsPerEntry;
                int longCount = (4096 + entriesPerLong - 1) / entriesPerLong;
                long[] dataArray = new long[longCount];

                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            int blockIndex = (y * 16 + z) * 16 + x;
                            int longIndex = blockIndex / entriesPerLong;
                            int bitOffset = (blockIndex % entriesPerLong) * bitsPerEntry;

                            ushort state = chunk.GetBlockState(x, baseY + y, z);
                            int paletteIdx = blockToPalette[state];

                            dataArray[longIndex] |= ((long)paletteIdx & ((1L << bitsPerEntry) - 1)) << bitOffset;
                        }
                    }
                }

                blockStatesComp.Add("data", new NbtLongArray(dataArray));
            }
            // If uniqueBlocks.Count == 1, no 'data' tag needed (Vanilla single-valued optimization!)

            sectionComp.Add("block_states", blockStatesComp);

            // 3. Biomes (Single-Valued plains container)
            var biomesComp = new NbtCompound();
            var biomePalette = new NbtList(NbtTagType.String);
            biomePalette.Add(new NbtString("minecraft:plains"));
            biomesComp.Add("palette", biomePalette);
            sectionComp.Add("biomes", biomesComp);

            // 4. Lighting nibbles
            int sectionOffset = i * 4096;
            byte[] skyBuffer = new byte[2048];
            byte[] blockBuffer = new byte[2048];
            bool hasBlockLight = false;

            for (int k = 0; k < 2048; k++)
            {
                byte sky1 = rawSkyLight[sectionOffset + k * 2];
                byte sky2 = rawSkyLight[sectionOffset + k * 2 + 1];
                skyBuffer[k] = (byte)((sky1 & 0x0F) | ((sky2 & 0x0F) << 4));

                byte bl1 = rawBlockLight[sectionOffset + k * 2];
                byte bl2 = rawBlockLight[sectionOffset + k * 2 + 1];
                blockBuffer[k] = (byte)((bl1 & 0x0F) | ((bl2 & 0x0F) << 4));
                if (bl1 > 0 || bl2 > 0) hasBlockLight = true;
            }

            sectionComp.Add("SkyLight", new NbtByteArray(skyBuffer));
            if (hasBlockLight)
            {
                sectionComp.Add("BlockLight", new NbtByteArray(blockBuffer));
            }

            sectionsList.Add(sectionComp);
        }

        root.Add("sections", sectionsList);
        root.Add("block_entities", new NbtList(NbtTagType.Compound));
        root.Add("entities", new NbtList(NbtTagType.Compound));

        return root;
    }

    public static Chunk Deserialize(NbtCompound root)
    {
        ArgumentNullException.ThrowIfNull(root);

        int chunkX = root.GetInt("xPos");
        int chunkZ = root.GetInt("zPos");
        var chunk = new Chunk(new ChunkPosition(chunkX, chunkZ));

        var sectionsList = root.GetList("sections");
        if (sectionsList == null) return chunk;

        foreach (var secTag in sectionsList)
        {
            if (secTag is not NbtCompound secComp) continue;

            sbyte sectionY = (sbyte)secComp.GetByte("Y");
            if (sectionY < -4 || sectionY > 19) continue;

            int baseY = sectionY * 16;

            // 1. Blocks
            var blockStatesComp = secComp.GetCompound("block_states");
            if (blockStatesComp != null)
            {
                var paletteList = blockStatesComp.GetList("palette");
                if (paletteList != null && paletteList.Count > 0)
                {
                    ushort[] paletteIds = new ushort[paletteList.Count];
                    for (int p = 0; p < paletteList.Count; p++)
                    {
                        if (paletteList[p] is NbtCompound entry)
                        {
                            string name = entry.GetString("Name");
                            paletteIds[p] = Block.GetId(name);
                        }
                    }

                    long[]? dataArray = blockStatesComp.GetLongArray("data");
                    if (dataArray == null || paletteList.Count == 1)
                    {
                        // Single-valued section
                        ushort singleId = paletteIds[0];
                        for (int y = 0; y < 16; y++)
                        {
                            for (int z = 0; z < 16; z++)
                            {
                                for (int x = 0; x < 16; x++)
                                {
                                    chunk.SetBlockState(x, baseY + y, z, singleId);
                                }
                            }
                        }
                    }
                    else
                    {
                        int bitsPerEntry = Math.Max(4, (int)Math.Ceiling(Math.Log2(paletteList.Count)));
                        int entriesPerLong = 64 / bitsPerEntry;
                        long mask = (1L << bitsPerEntry) - 1;

                        for (int y = 0; y < 16; y++)
                        {
                            for (int z = 0; z < 16; z++)
                            {
                                for (int x = 0; x < 16; x++)
                                {
                                    int blockIndex = (y * 16 + z) * 16 + x;
                                    int longIndex = blockIndex / entriesPerLong;
                                    int bitOffset = (blockIndex % entriesPerLong) * bitsPerEntry;

                                    if (longIndex < dataArray.Length)
                                    {
                                        int palIdx = (int)((dataArray[longIndex] >> bitOffset) & mask);
                                        if (palIdx < paletteIds.Length)
                                        {
                                            chunk.SetBlockState(x, baseY + y, z, paletteIds[palIdx]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 2. Light
            byte[]? skyBuffer = secComp.GetByteArray("SkyLight");
            if (skyBuffer != null && skyBuffer.Length == 2048)
            {
                for (int k = 0; k < 2048; k++)
                {
                    byte sky1 = (byte)(skyBuffer[k] & 0x0F);
                    byte sky2 = (byte)((skyBuffer[k] >> 4) & 0x0F);

                    int idx1 = k * 2;
                    int y1 = idx1 / 256;
                    int z1 = (idx1 % 256) / 16;
                    int x1 = idx1 % 16;
                    chunk.SetSkyLight(x1, baseY + y1, z1, sky1);

                    int idx2 = k * 2 + 1;
                    int y2 = idx2 / 256;
                    int z2 = (idx2 % 256) / 16;
                    int x2 = idx2 % 16;
                    chunk.SetSkyLight(x2, baseY + y2, z2, sky2);
                }
            }

            byte[]? blockBuffer = secComp.GetByteArray("BlockLight");
            if (blockBuffer != null && blockBuffer.Length == 2048)
            {
                for (int k = 0; k < 2048; k++)
                {
                    byte bl1 = (byte)(blockBuffer[k] & 0x0F);
                    byte bl2 = (byte)((blockBuffer[k] >> 4) & 0x0F);

                    int idx1 = k * 2;
                    int y1 = idx1 / 256;
                    int z1 = (idx1 % 256) / 16;
                    int x1 = idx1 % 16;
                    chunk.SetBlockLight(x1, baseY + y1, z1, bl1);

                    int idx2 = k * 2 + 1;
                    int y2 = idx2 / 256;
                    int z2 = (idx2 % 256) / 16;
                    int x2 = idx2 % 16;
                    chunk.SetBlockLight(x2, baseY + y2, z2, bl2);
                }
            }
        }

        return chunk;
    }
}
