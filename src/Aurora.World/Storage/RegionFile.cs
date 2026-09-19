using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace Aurora.World.Storage;

/// <summary>
/// Handles reading and writing Minecraft Anvil format region files (*.mca).
/// Conforms to standard 4096-byte sector header layout.
/// </summary>
public sealed class RegionFile : IDisposable
{
    private const int SectorSize = 4096;
    private const byte CompressionZlib = 2;

    private readonly FileStream _fileStream;
    private readonly int[] _offsets = new int[1024];
    private readonly int[] _timestamps = new int[1024];
    private readonly object _lock = new();
    private bool _disposed;

    public RegionFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _fileStream = new FileStream(
            filePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite,
            SectorSize,
            FileOptions.WriteThrough);

        lock (_lock)
        {
            if (_fileStream.Length < SectorSize * 2)
            {
                // Initialize empty 8KB header (Sector 0: Offsets, Sector 1: Timestamps)
                _fileStream.SetLength(SectorSize * 2);
                _fileStream.Position = 0;
                Span<byte> emptyHeader = stackalloc byte[SectorSize * 2];
                emptyHeader.Clear();
                _fileStream.Write(emptyHeader);
                _fileStream.Flush();
            }
            else
            {
                // Read header table
                Span<byte> header = stackalloc byte[SectorSize * 2];
                _fileStream.Position = 0;
                _fileStream.ReadExactly(header);

                for (int i = 0; i < 1024; i++)
                {
                    int offByte = i * 4;
                    int sectorOffset = (header[offByte] << 16) | (header[offByte + 1] << 8) | header[offByte + 2];
                    int sectorCount = header[offByte + 3];
                    _offsets[i] = (sectorOffset << 8) | sectorCount;

                    int timeByte = SectorSize + (i * 4);
                    _timestamps[i] = BinaryPrimitives.ReadInt32BigEndian(header.Slice(timeByte, 4));
                }
            }
        }
    }

    private static int GetChunkIndex(int chunkX, int chunkZ)
    {
        return (chunkX & 31) + (chunkZ & 31) * 32;
    }

    public byte[]? ReadChunkData(int chunkX, int chunkZ)
    {
        lock (_lock)
        {
            if (_disposed) return null;

            int index = GetChunkIndex(chunkX, chunkZ);
            int entry = _offsets[index];
            if (entry == 0) return null;

            int sectorOffset = entry >> 8;
            int sectorCount = entry & 0xFF;

            if (sectorOffset < 2) return null;

            long fileOffset = (long)sectorOffset * SectorSize;
            if (fileOffset + 5 > _fileStream.Length) return null;

            _fileStream.Position = fileOffset;
            Span<byte> meta = stackalloc byte[5];
            _fileStream.ReadExactly(meta);

            int length = BinaryPrimitives.ReadInt32BigEndian(meta.Slice(0, 4));
            byte compressionType = meta[4];

            if (length <= 1 || length > sectorCount * SectorSize) return null;

            byte[] compressed = new byte[length - 1];
            _fileStream.ReadExactly(compressed);

            using var memStream = new MemoryStream(compressed);
            using var decompressedStream = new MemoryStream();

            if (compressionType == CompressionZlib)
            {
                using var zlib = new ZLibStream(memStream, CompressionMode.Decompress);
                zlib.CopyTo(decompressedStream);
            }
            else
            {
                using var deflate = new DeflateStream(memStream, CompressionMode.Decompress);
                deflate.CopyTo(decompressedStream);
            }

            return decompressedStream.ToArray();
        }
    }

    public void WriteChunkData(int chunkX, int chunkZ, byte[] uncompressedData)
    {
        ArgumentNullException.ThrowIfNull(uncompressedData);

        lock (_lock)
        {
            if (_disposed) return;

            // Compress payload with ZLib
            byte[] compressedData;
            using (var mem = new MemoryStream())
            {
                using (var zlib = new ZLibStream(mem, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(uncompressedData, 0, uncompressedData.Length);
                }
                compressedData = mem.ToArray();
            }

            int payloadLength = compressedData.Length + 1; // +1 for compression type byte
            int totalBytesNeeded = payloadLength + 4; // +4 for length field
            int sectorsNeeded = (totalBytesNeeded + SectorSize - 1) / SectorSize;

            int index = GetChunkIndex(chunkX, chunkZ);
            int existingEntry = _offsets[index];
            int existingSectorOffset = existingEntry >> 8;
            int existingSectorCount = existingEntry & 0xFF;

            int targetSector;
            if (existingSectorOffset >= 2 && sectorsNeeded <= existingSectorCount)
            {
                targetSector = existingSectorOffset;
            }
            else
            {
                // Append at end of file, aligned to SectorSize
                targetSector = (int)((_fileStream.Length + SectorSize - 1) / SectorSize);
                _fileStream.SetLength((long)(targetSector + sectorsNeeded) * SectorSize);
            }

            // Write chunk data
            long writePos = (long)targetSector * SectorSize;
            _fileStream.Position = writePos;

            Span<byte> lengthSpan = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lengthSpan, payloadLength);
            _fileStream.Write(lengthSpan);
            _fileStream.WriteByte(CompressionZlib);
            _fileStream.Write(compressedData);

            // Pad remaining bytes in the last sector
            int padding = (sectorsNeeded * SectorSize) - totalBytesNeeded;
            if (padding > 0)
            {
                Span<byte> padSpan = stackalloc byte[padding];
                padSpan.Clear();
                _fileStream.Write(padSpan);
            }

            // Update header in memory and on disk
            int newEntry = (targetSector << 8) | (sectorsNeeded & 0xFF);
            _offsets[index] = newEntry;

            Span<byte> headerEntry = stackalloc byte[4];
            headerEntry[0] = (byte)((targetSector >> 16) & 0xFF);
            headerEntry[1] = (byte)((targetSector >> 8) & 0xFF);
            headerEntry[2] = (byte)(targetSector & 0xFF);
            headerEntry[3] = (byte)(sectorsNeeded & 0xFF);

            _fileStream.Position = index * 4;
            _fileStream.Write(headerEntry);

            // Update timestamp
            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _timestamps[index] = now;
            BinaryPrimitives.WriteInt32BigEndian(headerEntry, now);
            _fileStream.Position = SectorSize + (index * 4);
            _fileStream.Write(headerEntry);

            _fileStream.Flush();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _disposed = true;
                _fileStream.Flush();
                _fileStream.Dispose();
            }
        }
    }
}
