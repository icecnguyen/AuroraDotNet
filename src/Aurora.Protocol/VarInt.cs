using System;
using System.Buffers;
using System.IO;

namespace Aurora.Protocol;

public static class VarInt
{
    private const int SegmentBits = 0x7F;
    private const int ContinueBit = 0x80;

    public static int Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        int value = 0;
        int position = 0;
        byte currentByte;

        while (true)
        {
            int byteRead = stream.ReadByte();
            if (byteRead == -1) throw new EndOfStreamException();
            currentByte = (byte)byteRead;
            value |= (currentByte & SegmentBits) << position;
            if ((currentByte & ContinueBit) == 0) break;
            position += 7;
            if (position >= 32) throw new InvalidDataException("VarInt is too big");
        }

        return value;
    }
    
    public static bool TryRead(ref SequenceReader<byte> reader, out int value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;
        int position = 0;
        
        while (true)
        {
            if (!reader.TryRead(out byte currentByte))
            {
                // Not enough data
                return false;
            }
            
            bytesRead++;
            value |= (currentByte & SegmentBits) << position;
            
            if ((currentByte & ContinueBit) == 0) break;
            
            position += 7;
            if (position >= 32) throw new InvalidDataException("VarInt is too big");
        }

        return true;
    }
    
    public static void Write(Stream stream, int value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        uint uval = (uint)value;
        while (true)
        {
            if ((uval & ~SegmentBits) == 0)
            {
                stream.WriteByte((byte)uval);
                return;
            }
            stream.WriteByte((byte)((uval & SegmentBits) | ContinueBit));
            uval >>= 7;
        }
    }
    
    public static void Write(IBufferWriter<byte> writer, int value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        
        uint uval = (uint)value;
        while (true)
        {
            if ((uval & ~SegmentBits) == 0)
            {
                var span = writer.GetSpan(1);
                span[0] = (byte)uval;
                writer.Advance(1);
                return;
            }
            
            var chunk = writer.GetSpan(1);
            chunk[0] = (byte)((uval & SegmentBits) | ContinueBit);
            writer.Advance(1);
            uval >>= 7;
        }
    }
}
