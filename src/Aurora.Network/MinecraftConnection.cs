using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora.Network;

public class ConnectionEventArgs : EventArgs
{
    public MinecraftConnection Connection { get; }
    public ConnectionEventArgs(MinecraftConnection connection) => Connection = connection;
}

/// <summary>
/// Represents an active network connection utilizing System.IO.Pipelines for high-performance I/O.
/// </summary>
public sealed class MinecraftConnection : IDisposable
{
    private readonly Socket _socket;
    private readonly CancellationTokenSource _cts = new();
    
    // Using IDuplexPipe to abstract the connection's stream
    private readonly Pipe _receivePipe;
    private readonly Pipe _sendPipe;

    public Guid Id { get; } = Guid.NewGuid();
    public EndPoint? RemoteEndPoint { get; }
    
    public PipeReader Reader => _receivePipe.Reader;
    public PipeWriter Writer => _sendPipe.Writer;

    // Use a numeric state (0 = Handshake, 1 = Status, 2 = Login, 3 = Config, 4 = Play)
    public int CurrentState { get; set; }
    public int ClientProtocolVersion { get; set; }

    public event EventHandler<ConnectionEventArgs>? OnDisconnected;

    public MinecraftConnection(Socket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);
        _socket = socket;
        RemoteEndPoint = _socket.RemoteEndPoint;

        _receivePipe = new Pipe();
        _sendPipe = new Pipe();
    }

    public void StartProcessing()
    {
        _ = ReceiveLoopAsync();
        _ = ProcessPacketsAsync();
        _ = SendLoopAsync();
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var memory = _receivePipe.Writer.GetMemory(512);
                int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, _cts.Token).ConfigureAwait(false);
                
                if (bytesRead == 0)
                {
                    break; // Client gracefully closed
                }

                _receivePipe.Writer.Advance(bytesRead);
                var flushResult = await _receivePipe.Writer.FlushAsync(_cts.Token).ConfigureAwait(false);

                if (flushResult.IsCompleted || flushResult.IsCanceled)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
        finally
        {
            await _receivePipe.Writer.CompleteAsync().ConfigureAwait(false);
            Disconnect();
        }
    }

    private async Task SendLoopAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var readResult = await _sendPipe.Reader.ReadAsync(_cts.Token).ConfigureAwait(false);
                var buffer = readResult.Buffer;

                if (!buffer.IsEmpty)
                {
                    var arraySegment = GetArray(buffer);
                    await _socket.SendAsync(arraySegment, SocketFlags.None, _cts.Token).ConfigureAwait(false);
                    _sendPipe.Reader.AdvanceTo(buffer.End);
                }

                if (readResult.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
        finally
        {
            await _sendPipe.Reader.CompleteAsync().ConfigureAwait(false);
            Disconnect();
        }
    }

    private static ArraySegment<byte> GetArray(ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment && MemoryMarshal.TryGetArray(buffer.First, out var segment))
        {
            return segment;
        }
        return new ArraySegment<byte>(buffer.ToArray());
    }

    private async Task ProcessPacketsAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            var result = await _receivePipe.Reader.ReadAsync(_cts.Token).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (buffer.IsEmpty && result.IsCompleted)
            {
                break;
            }

            // Try to read packet length
            var sequenceReader = new SequenceReader<byte>(buffer);
            if (!Aurora.Protocol.VarInt.TryRead(ref sequenceReader, out int length, out int lengthBytes))
            {
                _receivePipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                return; // Need more data
            }

            if (buffer.Length < lengthBytes + length)
            {
                _receivePipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                return; // Need more data
            }

            // We have a full packet
            var packetSlice = buffer.Slice(lengthBytes, length);
            var nextPacketStart = buffer.GetPosition(lengthBytes + length);

            try
            {
                HandlePacket(packetSlice);
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
#pragma warning disable CA1303
                Console.WriteLine($"[Network] Packet processing error: {ex.Message}");
#pragma warning restore CA1303
                Disconnect();
                return;
            }
#pragma warning restore CA1031

            _receivePipe.Reader.AdvanceTo(nextPacketStart);
        }
    }

    private void HandlePacket(ReadOnlySequence<byte> packetData)
    {
        var reader = new Aurora.Protocol.PacketReader(packetData);
        int packetId = reader.ReadVarInt();
        
        // Very basic routing based on CurrentState
        // 0 = Handshake, 1 = Status, 2 = Login, 3 = Config, 4 = Play
        if (CurrentState == 0)
        {
            if (packetId == 0x00) // Handshake
            {
                var hs = new Aurora.Protocol.Handshake.HandshakePacket();
                hs.Read(ref reader);
                ClientProtocolVersion = hs.ProtocolVersion;
                CurrentState = (int)hs.NextState;
#pragma warning disable CA1303
                Console.WriteLine($"[Network] Handshake completed. Next state: {CurrentState}");
#pragma warning restore CA1303
            }
        }
        else if (CurrentState == 1) // Status
        {
            if (packetId == 0x00) // Status Request
            {
                var req = new Aurora.Protocol.Status.StatusRequestPacket();
                req.Read(ref reader);
                
                var protocolStr = ClientProtocolVersion > 0 ? ClientProtocolVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) : "767";
                var res = new Aurora.Protocol.Status.StatusResponsePacket
                {
                    JsonResponse = "{\"version\":{\"name\":\"Aurora 26.2\",\"protocol\":" + protocolStr + "},\"players\":{\"max\":100,\"online\":0},\"description\":{\"text\":\"AuroraDotNet Server\"}}"
                };
                SendPacket(res);
            }
            else if (packetId == 0x01) // Ping Request
            {
                var ping = new Aurora.Protocol.Status.PingRequestPacket();
                ping.Read(ref reader);
                
                var pong = new Aurora.Protocol.Status.PongResponsePacket { Payload = ping.Payload };
                SendPacket(pong);
            }
        }
        else if (CurrentState == 2) // Login
        {
            if (packetId == 0x00) // Login Start
            {
                var ls = new Aurora.Protocol.Login.LoginStartPacket();
                ls.Read(ref reader);
#pragma warning disable CA1303
                Console.WriteLine($"[Network] Player '{ls.Name}' logging in...");
#pragma warning restore CA1303
                
                var success = new Aurora.Protocol.Login.LoginSuccessPacket
                {
                    Uuid = ls.Uuid == Guid.Empty ? Guid.NewGuid() : ls.Uuid,
                    Username = ls.Name
                };
                SendPacket(success);
            }
            else if (packetId == 0x03) // Login Acknowledged
            {
#pragma warning disable CA1303
                Console.WriteLine($"[Network] Login Acknowledged. Moving to Config State.");
#pragma warning restore CA1303
                CurrentState = 3; // Config
                
                // 1. Send Feature Flags
                var featureFlags = new Aurora.Protocol.Configuration.FeatureFlagsPacket();
                SendPacket(featureFlags);

                // 2. Send Known Packs
                var knownPacks = new Aurora.Protocol.Configuration.KnownPacksPacket();
                SendPacket(knownPacks);

                // 3. Send Registry Data (Note: A real Vanilla 1.21.4 NBT dump is required here to bypass client disconnect)
                var registryData = new Aurora.Protocol.Configuration.RegistryDataPacket
                {
                    RegistryId = "minecraft:dimension_type",
                    // NbtData = ... (needs to be loaded from a valid NBT dump)
                };
                SendPacket(registryData);

                // 4. Send Finish Configuration (0x03 in 1.21.4)
                var finishConfig = new Aurora.Protocol.Configuration.FinishConfigurationPacket();
                SendPacket(finishConfig);
            }
        }
        else if (CurrentState == 3) // Config
        {
            if (packetId == 0x03) // Acknowledge Finish Configuration
            {
#pragma warning disable CA1303
                Console.WriteLine($"[Network] Config Finished. Moving to Play State.");
#pragma warning restore CA1303
                CurrentState = 4; // Play
                
                // We should send Join Game, but since it's highly complex, let's just log it for now
                // Actually, if we don't send Join Game, it will get stuck at "Joining world..." again.
                // Send Join Game
                var joinGame = new Aurora.Protocol.Play.JoinGamePacket();
                SendPacket(joinGame);
                
                // Send Player Position to dismiss "Joining world..." screen
                var playerPos = new Aurora.Protocol.Play.PlayerPositionPacket();
                SendPacket(playerPos);
            }
            else
            {
                // Ignore other config packets (e.g. client settings)
            }
        }
        else if (CurrentState == 4) // Play
        {
            // Ignore play packets for now
        }
    }
    
    public void SendPacket(Aurora.Protocol.IPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        
        // Serialize to temporary buffer
        var arrayBuffer = new System.Buffers.ArrayBufferWriter<byte>();
        var packetWriter = new Aurora.Protocol.PacketWriter(arrayBuffer);
        
        packetWriter.WriteVarInt(packet.PacketId);
        packet.Write(ref packetWriter);
        
        var packetData = arrayBuffer.WrittenSpan;
        
        // Write length prefix + packet data to actual pipe
        Aurora.Protocol.VarInt.Write(_sendPipe.Writer, packetData.Length);
        var span = _sendPipe.Writer.GetSpan(packetData.Length);
        packetData.CopyTo(span);
        _sendPipe.Writer.Advance(packetData.Length);
        
        // Fire and forget flush
        var _ = _sendPipe.Writer.FlushAsync().AsTask();
    }

    public void Disconnect()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            try { _socket.Shutdown(SocketShutdown.Both); } catch (SocketException) { } catch (ObjectDisposedException) { }
            try { _socket.Close(); } catch (SocketException) { } catch (ObjectDisposedException) { }
            
            OnDisconnected?.Invoke(this, new ConnectionEventArgs(this));
        }
    }

    public void Dispose()
    {
        Disconnect();
        _cts.Dispose();
        _socket.Dispose();
    }
}
