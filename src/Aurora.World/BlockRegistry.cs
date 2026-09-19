using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aurora.World;

/// <summary>
/// Complete block registry for Minecraft 1.21.4 (Protocol 768).
/// Provides high-speed O(1) lookups for all 1,095 official block types, 27,866 block state IDs,
/// and item-to-block placement mappings generated directly from vanilla data reports.
/// </summary>
public static class BlockRegistry
{
    private sealed record RegistryData(
        string[] BlockNames,
        ushort[] DefaultStates,
        ushort[] StateToBlockIndex,
        ushort[] ItemToBlockState,
        Dictionary<string, ushort> NameToIndex);

    private static readonly RegistryData Data = LoadData();

    private static RegistryData LoadData()
    {
        var assembly = typeof(BlockRegistry).Assembly;
        using var stream = assembly.GetManifestResourceStream("Aurora.World.Data.blocks.bin");
        if (stream == null)
        {
            return new RegistryData(
                Array.Empty<string>(),
                Array.Empty<ushort>(),
                Array.Empty<ushort>(),
                Array.Empty<ushort>(),
                new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase));
        }

        Span<byte> header = stackalloc byte[6];
        ReadExact(stream, header);

        ushort blockCount = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(0, 2));
        ushort stateCount = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(2, 2));
        ushort itemCount = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(4, 2));

        var blockNames = new string[blockCount];
        var defaultStates = new ushort[blockCount];
        var stateToBlockIndex = new ushort[stateCount];
        var itemToBlockState = new ushort[itemCount];
        var nameToIndex = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);

        Span<byte> buf2 = stackalloc byte[2];

        // 1. Read Block Names
        for (int i = 0; i < blockCount; i++)
        {
            ReadExact(stream, buf2);
            ushort strLen = BinaryPrimitives.ReadUInt16BigEndian(buf2);
            byte[] strBytes = new byte[strLen];
            ReadExact(stream, strBytes);
            string name = Encoding.UTF8.GetString(strBytes);

            blockNames[i] = name;
            nameToIndex[name] = (ushort)i;
            if (name.StartsWith("minecraft:", StringComparison.Ordinal))
            {
                nameToIndex[name.Substring(10)] = (ushort)i;
            }
        }

        // 2. Read Default States
        for (int i = 0; i < blockCount; i++)
        {
            ReadExact(stream, buf2);
            defaultStates[i] = BinaryPrimitives.ReadUInt16BigEndian(buf2);
        }

        // 3. Read State to Block Index
        for (int i = 0; i < stateCount; i++)
        {
            ReadExact(stream, buf2);
            stateToBlockIndex[i] = BinaryPrimitives.ReadUInt16BigEndian(buf2);
        }

        // 4. Read Item to Block State
        for (int i = 0; i < itemCount; i++)
        {
            ReadExact(stream, buf2);
            itemToBlockState[i] = BinaryPrimitives.ReadUInt16BigEndian(buf2);
        }

        return new RegistryData(blockNames, defaultStates, stateToBlockIndex, itemToBlockState, nameToIndex);
    }

    private static void ReadExact(Stream stream, Span<byte> span)
    {
        int total = 0;
        while (total < span.Length)
        {
            int r = stream.Read(span.Slice(total));
            if (r == 0) break;
            total += r;
        }
    }

    public static int TotalBlockTypes => Data.BlockNames.Length;
    public static int TotalBlockStates => Data.StateToBlockIndex.Length;

    public static string GetBlockName(ushort stateId)
    {
        if (stateId < Data.StateToBlockIndex.Length)
        {
            ushort bIdx = Data.StateToBlockIndex[stateId];
            if (bIdx < Data.BlockNames.Length)
            {
                return Data.BlockNames[bIdx];
            }
        }
        return "minecraft:air";
    }

    public static ushort GetDefaultStateId(string blockName)
    {
        if (string.IsNullOrEmpty(blockName)) return 0;

        if (Data.NameToIndex.TryGetValue(blockName, out var idx))
        {
            return Data.DefaultStates[idx];
        }
        return 0;
    }

    public static ushort GetBlockStateFromItem(int itemId)
    {
        if (itemId >= 0 && itemId < Data.ItemToBlockState.Length)
        {
            return Data.ItemToBlockState[itemId];
        }
        return 0;
    }

    public static bool IsAir(ushort stateId)
    {
        if (stateId == 0) return true;
        string name = GetBlockName(stateId);
        return name == "minecraft:air" || name == "minecraft:cave_air" || name == "minecraft:void_air";
    }
}
