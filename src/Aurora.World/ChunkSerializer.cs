using System;
using System.Buffers.Binary;
using System.IO;

namespace Aurora.World;

public static class ChunkSerializer
{
    private static void WriteVarInt(MemoryStream ms, int value)
    {
        uint uval = (uint)value;
        while ((uval & ~0x7Fu) != 0)
        {
            ms.WriteByte((byte)((uval & 0x7F) | 0x80));
            uval >>= 7;
        }
        ms.WriteByte((byte)uval);
    }

    public static byte[] Serialize(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        
        using var ms = new MemoryStream();
        Span<byte> longBytes = stackalloc byte[8];
        
        // 24 Sections (Y from -64 to 319)
        for (int i = 0; i < 24; i++)
        {
            int sectionY = -64 + (i * 16);
            
            // Check if section is homogeneous
            ushort firstBlock = chunk.GetBlockState(0, sectionY, 0);
            bool isHomogeneous = true;
            short blockCount = 0;

            for (int y = 0; y < 16; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        ushort block = chunk.GetBlockState(x, sectionY + y, z);
                        if (block != 0) blockCount++;
                        if (isHomogeneous && block != firstBlock)
                        {
                            isHomogeneous = false;
                        }
                    }
                }
            }
            
            // Write BlockCount (Big Endian)
            ms.WriteByte((byte)(blockCount >> 8));
            ms.WriteByte((byte)(blockCount & 0xFF));
            
            if (isHomogeneous)
            {
                // Write BlockStates Paletted Container (Single Valued)
                ms.WriteByte(0); // Bits per entry = 0
                WriteVarInt(ms, firstBlock); // Block state ID
                WriteVarInt(ms, 0); // Data array length = 0
            }
            else
            {
                // Write BlockStates Paletted Container (Global Palette - 15 bits)
                ms.WriteByte(15); // Bits per entry = 15
                
                // Data array length is exactly 1024 longs for 15 bits (4096 blocks / 4 blocks per long)
                WriteVarInt(ms, 1024);
                
                long currentLong = 0;
                int blocksInLong = 0;
                
                // Iterate Y, Z, X order for blocks
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            ushort block = chunk.GetBlockState(x, sectionY + y, z);
                            
                            // Pack 15 bits into currentLong
                            currentLong |= ((long)block & 0x7FFF) << (blocksInLong * 15);
                            blocksInLong++;
                            
                            if (blocksInLong == 4)
                            {
                                // Write out the full long (Big Endian)
                                BinaryPrimitives.WriteInt64BigEndian(longBytes, currentLong);
                                ms.Write(longBytes);
                                
                                currentLong = 0;
                                blocksInLong = 0;
                            }
                        }
                    }
                }
                
                // Note: blocksInLong will end exactly at 0 after 4096 iterations since 4096 % 4 == 0
            }
            
            // Write Biomes Paletted Container (Single Valued - Plains)
            // TODO: In the future, this should also support Global Palette for 3D biomes
            ms.WriteByte(0); // Bits per entry = 0
            WriteVarInt(ms, 1); // Biome ID (1 = Plains for example)
            WriteVarInt(ms, 0); // Data array length = 0
        }
        
        return ms.ToArray();
    }

    public static byte[] SerializeLight(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        
        using var ms = new MemoryStream();
        Span<byte> longBytes = stackalloc byte[8];
        
        long skyLightMask = 0;
        long emptySkyLightMask = 0;
        int skyLightArrayCount = 0;
        
        for (int i = 0; i < 26; i++)
        {
            if (i > 0 && i < 25)
            {
                skyLightMask |= (1L << i);
                skyLightArrayCount++;
            }
            else
            {
                emptySkyLightMask |= (1L << i);
            }
        }
        
        long blockLightMask = 0;
        long emptyBlockLightMask = (1L << 26) - 1;
        int blockLightArrayCount = 0;

        WriteVarInt(ms, 1); 
        BinaryPrimitives.WriteInt64BigEndian(longBytes, skyLightMask); ms.Write(longBytes);
        
        WriteVarInt(ms, 1);
        BinaryPrimitives.WriteInt64BigEndian(longBytes, blockLightMask); ms.Write(longBytes);
        
        WriteVarInt(ms, 1);
        BinaryPrimitives.WriteInt64BigEndian(longBytes, emptySkyLightMask); ms.Write(longBytes);
        
        WriteVarInt(ms, 1);
        BinaryPrimitives.WriteInt64BigEndian(longBytes, emptyBlockLightMask); ms.Write(longBytes);
        
        WriteVarInt(ms, skyLightArrayCount);
        
        var rawSkyLight = chunk.RawSkyLight;
        Span<byte> sectionBuffer = stackalloc byte[2048];
        for (int i = 0; i < 24; i++)
        {
            WriteVarInt(ms, 2048);
            
            int sectionOffset = i * 4096;
            
            for (int blockIndex = 0; blockIndex < 2048; blockIndex++)
            {
                byte light1 = rawSkyLight[sectionOffset + blockIndex * 2];
                byte light2 = rawSkyLight[sectionOffset + blockIndex * 2 + 1];
                sectionBuffer[blockIndex] = (byte)((light1 & 0x0F) | ((light2 & 0x0F) << 4));
            }
            ms.Write(sectionBuffer);
        }
        
        WriteVarInt(ms, blockLightArrayCount);

        return ms.ToArray();
    }
}
