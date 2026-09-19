using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using Aurora.Core.Math;
using Aurora.Core.Nbt;

namespace Aurora.World.Storage;

/// <summary>
/// World persistence manager handling standard Minecraft Anvil region files (*.mca).
/// Stores chunks as official Minecraft 1.21.4 NBT compound trees, fully compatible with vanilla Minecraft.
/// </summary>
public sealed class AnvilWorldStorage : IDisposable
{
    private const uint LegacyAuroMagic = 0x4155524F; // "AURO" for backward compatibility
    private const ushort LegacyStorageVersion = 1;

    private readonly string _regionDir;
    private readonly ConcurrentDictionary<long, RegionFile> _regionFiles = new();
    private readonly object _lock = new();
    private bool _disposed;

    public AnvilWorldStorage(string worldDirectory)
    {
        ArgumentNullException.ThrowIfNull(worldDirectory);
        _regionDir = Path.Combine(worldDirectory, "region");
        if (!Directory.Exists(_regionDir))
        {
            Directory.CreateDirectory(_regionDir);
        }
    }

    private static long GetRegionKey(int rx, int rz)
    {
        return ((long)rx << 32) | (uint)rz;
    }

    private RegionFile GetOrCreateRegionFile(int chunkX, int chunkZ)
    {
        int rx = chunkX >> 5;
        int rz = chunkZ >> 5;
        long key = GetRegionKey(rx, rz);

        return _regionFiles.GetOrAdd(key, _ =>
        {
            string filePath = Path.Combine(_regionDir, $"r.{rx}.{rz}.mca");
            return new RegionFile(filePath);
        });
    }

    public bool TryLoadChunk(ChunkPosition pos, out Chunk? chunk)
    {
        chunk = null;
        if (_disposed) return false;

        var region = GetOrCreateRegionFile(pos.X, pos.Z);
        byte[]? rawData = region.ReadChunkData(pos.X, pos.Z);
        if (rawData == null || rawData.Length < 4) return false;

        // 1. Check for legacy AURO format
        var span = rawData.AsSpan();
        uint magic = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(0, 4));
        if (magic == LegacyAuroMagic)
        {
            return TryLoadLegacyChunk(pos, rawData, out chunk);
        }

        // 2. Standard Minecraft Vanilla NBT Chunk
        try
        {
            using var ms = new MemoryStream(rawData);
            var root = NbtReader.ReadRoot(ms);
            chunk = ChunkNbtSerializer.Deserialize(root);
            return true;
        }
#pragma warning disable CA1031 // Corrupted chunks on disk should safely fall back to world generation
        catch (Exception)
        {
            chunk = null;
            return false;
        }
#pragma warning restore CA1031
    }

    private static bool TryLoadLegacyChunk(ChunkPosition pos, byte[] rawData, out Chunk? chunk)
    {
        chunk = null;
        if (rawData.Length < 14) return false;

        var span = rawData.AsSpan();
        ushort version = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2));
        if (version != LegacyStorageVersion) return false;

        int cx = BinaryPrimitives.ReadInt32BigEndian(span.Slice(6, 4));
        int cz = BinaryPrimitives.ReadInt32BigEndian(span.Slice(10, 4));
        if (cx != pos.X || cz != pos.Z) return false;

        var loadedChunk = new Chunk(pos);
        int expectedBlocks = 16 * 384 * 16;
        int offset = 14;

        if (span.Length < offset + expectedBlocks * 2 + expectedBlocks * 2) return false;

        var blockSpan = MemoryMarshal.Cast<byte, ushort>(span.Slice(offset, expectedBlocks * 2));
        offset += expectedBlocks * 2;

        var skySpan = span.Slice(offset, expectedBlocks);
        offset += expectedBlocks;

        var blockLightSpan = span.Slice(offset, expectedBlocks);

        for (int y = -64; y < 320; y++)
        {
            for (int z = 0; z < 16; z++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int localY = y + 64;
                    int idx = (localY * 256) + (z * 16) + x;

                    loadedChunk.SetBlockState(x, y, z, blockSpan[idx]);
                    loadedChunk.SetSkyLight(x, y, z, skySpan[idx]);
                    loadedChunk.SetBlockLight(x, y, z, blockLightSpan[idx]);
                }
            }
        }

        chunk = loadedChunk;
        return true;
    }

    public void SaveChunk(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (_disposed) return;

        // Serialize to official Minecraft 1.21.4 NBT structure
        var rootNbt = ChunkNbtSerializer.Serialize(chunk);

        using var ms = new MemoryStream();
        NbtWriter.WriteRoot(ms, rootNbt, "");
        byte[] payload = ms.ToArray();

        var pos = chunk.Position;
        var region = GetOrCreateRegionFile(pos.X, pos.Z);
        region.WriteChunkData(pos.X, pos.Z, payload);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _disposed = true;
                foreach (var region in _regionFiles.Values)
                {
                    region.Dispose();
                }
                _regionFiles.Clear();
            }
        }
    }
}
