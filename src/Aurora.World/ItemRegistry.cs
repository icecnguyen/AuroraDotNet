using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aurora.World;

/// <summary>
/// Complete item registry for Minecraft 1.21.4 (Protocol 768).
/// Provides high-speed O(1) lookups for all 1,385 official item types.
/// </summary>
public static class ItemRegistry
{
    private sealed record ItemData(
        string[] IdToName,
        Dictionary<string, int> NameToId);

    private static readonly ItemData Data = LoadData();

    public static int ItemCount => Data.IdToName.Length;

    private static ItemData LoadData()
    {
        var assembly = typeof(ItemRegistry).Assembly;
        using var stream = assembly.GetManifestResourceStream("Aurora.World.Data.items.bin");
        if (stream == null)
        {
            return new ItemData(
                Array.Empty<string>(),
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
        }

        Span<byte> header = stackalloc byte[2];
        ReadExact(stream, header);
        ushort totalCount = BinaryPrimitives.ReadUInt16BigEndian(header);

        var idToName = new string[totalCount];
        var nameToId = new Dictionary<string, int>(totalCount * 2, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < totalCount; i++)
        {
            int len = stream.ReadByte();
            if (len < 0) break;

            byte[] nameBytes = new byte[len];
            ReadExact(stream, nameBytes);
            string fullName = Encoding.UTF8.GetString(nameBytes);

            idToName[i] = fullName;
            nameToId[fullName] = i;

            if (fullName.StartsWith("minecraft:", StringComparison.OrdinalIgnoreCase))
            {
                string shortName = fullName.Substring(10);
                nameToId.TryAdd(shortName, i);
            }
        }

        return new ItemData(idToName, nameToId);
    }

    private static void ReadExact(Stream stream, Span<byte> destination)
    {
        int totalRead = 0;
        while (totalRead < destination.Length)
        {
            int read = stream.Read(destination.Slice(totalRead));
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected EOF while reading item registry.");
            }
            totalRead += read;
        }
    }

    /// <summary>
    /// Gets the full Minecraft resource location name for an item ID.
    /// </summary>
    public static string GetItemName(int itemId)
    {
        if (itemId >= 0 && itemId < Data.IdToName.Length)
        {
            return Data.IdToName[itemId];
        }
        return "minecraft:air";
    }

    /// <summary>
    /// Gets the protocol item ID for a name (supports with or without 'minecraft:' prefix).
    /// </summary>
    public static int GetItemId(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return 0;
        if (Data.NameToId.TryGetValue(itemName, out int id))
        {
            return id;
        }
        return 0;
    }
}
