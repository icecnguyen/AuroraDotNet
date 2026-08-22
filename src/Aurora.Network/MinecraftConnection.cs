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
    public int CurrentState { get; set; } // Handshake
    public int ClientProtocolVersion { get; set; }
    public string Username { get; private set; } = "Unknown";
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;
    public long Ping { get; set; }
    
    // Position
    public double X { get; set; } = 0.5;
    public double Y { get; set; } = 100.0;
    public double Z { get; set; } = 0.5;
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    
    // Entity
    private static int _entityIdCounter = 1;
    public int EntityId { get; } = System.Threading.Interlocked.Increment(ref _entityIdCounter);

    public event EventHandler<ConnectionEventArgs>? OnDisconnected;

    private readonly ConnectionManager _connectionManager;
    private readonly Aurora.World.WorldManager _worldManager;

    public MinecraftConnection(Socket socket, ConnectionManager connectionManager, Aurora.World.WorldManager worldManager)
    {
        ArgumentNullException.ThrowIfNull(socket);
        _socket = socket;
        _connectionManager = connectionManager;
        _worldManager = worldManager;
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
                Username = string.IsNullOrEmpty(ls.Name) ? "Unknown" : ls.Name;
#pragma warning disable CA1303
                Console.WriteLine($"[Network] Player '{Username}' logging in...");
#pragma warning restore CA1303
                
                var success = new Aurora.Protocol.Login.LoginSuccessPacket
                {
                    Uuid = ls.Uuid == Guid.Empty ? Guid.NewGuid() : ls.Uuid,
                    Username = Username
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

                // 3. Send Registry Data
                try
                {
                    // For safety, load them relative to AppDomain.CurrentDomain.BaseDirectory
                    var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                    var rawRegistriesPath = System.IO.Path.Combine(baseDir, "Resources", "RawRegistries");
                    if (System.IO.Directory.Exists(rawRegistriesPath))
                    {
                        var orderedRegistries = new string[]
                        {
                            "raw_minecraft_worldgen_biome.bin",
                            "raw_minecraft_chat_type.bin",
                            "raw_minecraft_trim_pattern.bin",
                            "raw_minecraft_trim_material.bin",
                            "raw_minecraft_wolf_variant.bin",
                            "raw_minecraft_painting_variant.bin",
                            "raw_minecraft_dimension_type.bin",
                            "raw_minecraft_damage_type.bin",
                            "raw_minecraft_banner_pattern.bin",
                            "raw_minecraft_enchantment.bin",
                            "raw_minecraft_jukebox_song.bin",
                            "raw_minecraft_instrument.bin"
                        };

                        foreach (var binFileName in orderedRegistries)
                        {
                            var binFile = System.IO.Path.Combine(rawRegistriesPath, binFileName);
                            if (!System.IO.File.Exists(binFile)) continue;
                            
                            var fullBuffer = System.IO.File.ReadAllBytes(binFile);
                            // The first byte of fullBuffer is the PacketId (0x07 for Registry Data).
                            // The rest is the payload.
                            var payload = new byte[fullBuffer.Length - 1];
                            System.Array.Copy(fullBuffer, 1, payload, 0, payload.Length);
                            
                            var registryPacket = new Aurora.Protocol.Configuration.RegistryDataPacket
                            {
                                Payload = payload
                            };
                            SendPacket(registryPacket);
                        }
                    }
                    else
                    {
#pragma warning disable CA1303
                        Console.WriteLine("[Network] WARNING: Missing RawRegistries folder! Client will likely disconnect.");
#pragma warning restore CA1303
                    }
                }
#pragma warning disable CA1031
                catch (System.Exception ex)
                {
#pragma warning disable CA1303
                    Console.WriteLine($"[Network] ERROR reading NBT: {ex.Message}");
#pragma warning restore CA1303
                }
#pragma warning restore CA1031

                // 4. Send Update Tags (0x0D)
                var tagsBaseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                var tagsFile = System.IO.Path.Combine(tagsBaseDir, "Resources", "raw_tags.bin");
                if (System.IO.File.Exists(tagsFile))
                {
                    var fullBuffer = System.IO.File.ReadAllBytes(tagsFile);
                    var payload = new byte[fullBuffer.Length - 1];
                    System.Array.Copy(fullBuffer, 1, payload, 0, payload.Length);
                    
                    var tagsPacket = new Aurora.Protocol.Configuration.UpdateTagsPacket
                    {
                        Payload = payload
                    };
                    SendPacket(tagsPacket);
                }
                else
                {
#pragma warning disable CA1303
                    Console.WriteLine("[Network] WARNING: Missing raw_tags.bin! Client will likely disconnect.");
#pragma warning restore CA1303
                }

                // 5. Send Finish Configuration (0x03 in 1.21.4)
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
                
                // 1. Send Join Game
                var joinGame = new Aurora.Protocol.Play.JoinGamePacket { EntityId = this.EntityId };
                SendPacket(joinGame);
                
                // 2. Send Center View Position
                // In 1.21.4, Update View Position (0x58) uses VarInt for X and Z.
                // VarInt(0) is 1 byte (0x00). So two VarInts(0, 0) is just 2 bytes.
                var centerPos = new Aurora.Protocol.Play.RawPacket(0x58, new byte[] { 0, 0 }); // X=0, Z=0
                // Send 49 initial chunks (7x7) based on player's spawn position (0, 0)
                // TODO: Make this dynamic based on view distance
                for (int cx = -3; cx <= 3; cx++)
                {
                    for (int cz = -3; cz <= 3; cz++)
                    {
                        var chunk = _worldManager.GetOrGenerateChunk(cx, cz);
                        var chunkDataBytes = Aurora.World.ChunkSerializer.Serialize(chunk);
                        var lightDataBytes = Aurora.World.ChunkSerializer.SerializeLight(chunk);
                        var chunkPacket = new Aurora.Protocol.Play.ChunkDataPacket
                        {
                            X = cx,
                            Z = cz,
                            ChunkData = chunkDataBytes,
                            LightData = lightDataBytes
                        };
                        SendPacket(chunkPacket);
                    }
                }
                
                // 4. Send Player Position to dismiss "Joining world..." screen
                var playerPos = new Aurora.Protocol.Play.PlayerPositionPacket();
                SendPacket(playerPos);
                
                // 5. Spawn existing players for this player, and spawn this player for others
                {
                    // Create info for myself
                    var myInfo = new Aurora.Protocol.Play.PlayerInfoUpdatePacket
                    {
                        // 0: add_player(1), 1: init_chat(2), 2: gamemode(4), 3: listed(8), 4: latency(16), 5: display_name(32). 
                        // 1 + 4 + 8 + 16 = 29 = 0x1D
                        Actions = 0x1D, 
                        Entries = new[]
                        {
                            new Aurora.Protocol.Play.PlayerInfoEntry
                            {
                                UUID = this.Id,
                                Name = this.Username,
                                GameMode = 1,
                                Listed = true,
                                Ping = 0,
                                HasDisplayName = false
                            }
                        }
                    };
                    
                    var mySpawn = new Aurora.Protocol.Play.SpawnEntityPacket
                    {
                        EntityId = this.EntityId,
                        EntityUUID = this.Id,
                        Type = 147, // Player
                        X = this.X, Y = this.Y, Z = this.Z,
                        Pitch = this.Pitch, Yaw = this.Yaw, HeadYaw = this.Yaw
                    };
                    
                    // Tell everyone about me
                    _connectionManager.BroadcastPacket(myInfo, except: this.Id);
                    _connectionManager.BroadcastPacket(mySpawn, except: this.Id);
                    
                    // Tell me about everyone
                    foreach (var other in _connectionManager.Players)
                    {
                        if (other.Id == this.Id || other.CurrentState != 4) continue;
                        
                        var otherInfo = new Aurora.Protocol.Play.PlayerInfoUpdatePacket
                        {
                            Actions = 0x1D,
                            Entries = new[]
                            {
                                new Aurora.Protocol.Play.PlayerInfoEntry
                                {
                                    UUID = other.Id,
                                    Name = other.Username,
                                    GameMode = 1,
                                    Listed = true,
                                    Ping = (int)other.Ping,
                                    HasDisplayName = false
                                }
                            }
                        };
                        SendPacket(otherInfo);
                        
                        var otherSpawn = new Aurora.Protocol.Play.SpawnEntityPacket
                        {
                            EntityId = other.EntityId,
                            EntityUUID = other.Id,
                            Type = 147,
                            X = other.X, Y = other.Y, Z = other.Z,
                            Pitch = other.Pitch, Yaw = other.Yaw, HeadYaw = other.Yaw
                        };
                        SendPacket(otherSpawn);
                    }
                }

                // 6. Start Keep Alive Task
                _keepAliveTask = System.Threading.Tasks.Task.Run(KeepAliveLoop);
            }
            else
            {
                // Ignore other config packets (e.g. client settings)
            }
        }
        else if (CurrentState == 4) // Play
        {
            if (packetId == 0x1A) // Serverbound Keep Alive
            {
                var keepAlive = new Aurora.Protocol.Play.KeepAliveServerboundPacket();
                keepAlive.Read(ref reader);
                if (keepAlive.KeepAliveId == _lastKeepAliveId)
                {
                    Ping = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastKeepAliveId;
                }
            }
            else if (packetId == 0x1C) // Position
            {
                var pos = new Aurora.Protocol.Play.SetPlayerPositionPacket();
                pos.Read(ref reader);
                X = pos.X; Y = pos.Y; Z = pos.Z;
                
                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.EntityTeleportPacket
                {
                    EntityId = this.EntityId,
                    X = this.X, Y = this.Y, Z = this.Z,
                    Yaw = this.Yaw, Pitch = this.Pitch, OnGround = pos.OnGround
                }, except: this.Id);
            }
            else if (packetId == 0x1D) // Position and Rotation
            {
                var posRot = new Aurora.Protocol.Play.SetPlayerPositionAndRotationPacket();
                posRot.Read(ref reader);
                X = posRot.X; Y = posRot.Y; Z = posRot.Z;
                Yaw = posRot.Yaw; Pitch = posRot.Pitch;
                
                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.EntityTeleportPacket
                {
                    EntityId = this.EntityId,
                    X = this.X, Y = this.Y, Z = this.Z,
                    Yaw = this.Yaw, Pitch = this.Pitch, OnGround = posRot.OnGround
                }, except: this.Id);
                
                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.EntityHeadRotationPacket
                {
                    EntityId = this.EntityId,
                    HeadYaw = this.Yaw
                }, except: this.Id);
            }
            else if (packetId == 0x1E) // Rotation
            {
                var rot = new Aurora.Protocol.Play.SetPlayerRotationPacket();
                rot.Read(ref reader);
                Yaw = rot.Yaw; Pitch = rot.Pitch;
                
                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.EntityTeleportPacket
                {
                    EntityId = this.EntityId,
                    X = this.X, Y = this.Y, Z = this.Z,
                    Yaw = this.Yaw, Pitch = this.Pitch, OnGround = rot.OnGround
                }, except: this.Id);
                
                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.EntityHeadRotationPacket
                {
                    EntityId = this.EntityId,
                    HeadYaw = this.Yaw
                }, except: this.Id);
            }
            else if (packetId == 0x24) // Serverbound Player Action (Block Dig)
            {
                int status = reader.ReadVarInt();
                var position = reader.ReadPosition();
                byte face = reader.ReadByte();
                int sequence = reader.ReadVarInt();

                if (status == 2) // Block broken (simplified)
                {
                    // Update RAM
                    _worldManager.SetBlock(position.X, position.Y, position.Z, Aurora.World.Generation.SurfaceBuilder.Air);

                    var blockUpdate = new Aurora.Protocol.Play.BlockUpdatePacket
                    {
                        Location = Aurora.Protocol.Play.BlockUpdatePacket.EncodePosition(position.X, position.Y, position.Z),
                        BlockStateId = Aurora.World.Generation.SurfaceBuilder.Air // Air
                    };
                    _connectionManager.BroadcastPacket(blockUpdate);
                }
            }
            else if (packetId == 0x3C) // Use Item On Block (Place)
            {
                var place = new Aurora.Protocol.Play.UseItemOnBlockPacket();
                place.Read(ref reader);
                
                var (x, y, z) = place.BlockPosition;
                
                // Calculate the position of the placed block based on the face clicked
                // Face: 0=Bottom, 1=Top, 2=North, 3=South, 4=West, 5=East
                if (place.Face == 0) y--;
                else if (place.Face == 1) y++;
                else if (place.Face == 2) z--;
                else if (place.Face == 3) z++;
                else if (place.Face == 4) x--;
                else if (place.Face == 5) x++;
                
                // Update RAM
                _worldManager.SetBlock(x, y, z, Aurora.World.Generation.SurfaceBuilder.Stone);

                long newLocation = Aurora.Protocol.Play.BlockUpdatePacket.EncodePosition(x, y, z);
                
                var update = new Aurora.Protocol.Play.BlockUpdatePacket
                {
                    Location = newLocation,
                    BlockStateId = Aurora.World.Generation.SurfaceBuilder.Stone // Stone
                };
                
                _connectionManager.BroadcastPacket(update);
            }
            else if (packetId == 0x05) // Chat Command
            {
                var cmd = new Aurora.Protocol.Play.ChatCommandServerboundPacket();
                cmd.Read(ref reader);
                
                string response = $"Unknown command: /{cmd.Command}";
                if (cmd.Command.StartsWith("ping", System.StringComparison.OrdinalIgnoreCase)) response = $"Pong! Your ping is {Ping}ms.";
                else if (cmd.Command.StartsWith("pos", System.StringComparison.OrdinalIgnoreCase)) response = $"You are at X={X:F1}, Y={Y:F1}, Z={Z:F1}";
                
                SendPacket(new Aurora.Protocol.Play.SystemChatMessagePacket { Content = response });
            }
            else if (packetId == 0x07) // Chat Message
            {
                var chat = new Aurora.Protocol.Play.ChatMessageServerboundPacket();
                chat.Read(ref reader);
                
                // Echo back for now
                var responsePacket = new Aurora.Protocol.Play.SystemChatMessagePacket { Content = $"<{Username}> {chat.Message}" };
                
                if (_connectionManager != null)
                {
                    _connectionManager.BroadcastPacket(responsePacket);
                }
                else
                {
                    SendPacket(responsePacket);
                }
            }
        }
    }
    
    private System.Threading.Tasks.Task? _keepAliveTask;
    private long _lastKeepAliveId;

    private async System.Threading.Tasks.Task KeepAliveLoop()
    {
#pragma warning disable CA1031
        try
        {
            while (CurrentState == 4) // Play
            {
                _lastKeepAliveId = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var keepAlive = new Aurora.Protocol.Play.KeepAliveClientboundPacket { KeepAliveId = _lastKeepAliveId };
                SendPacket(keepAlive);
                await System.Threading.Tasks.Task.Delay(15000).ConfigureAwait(false); // 15 seconds
            }
        }
        catch
        {
            // Ignore disconnected
        }
#pragma warning restore CA1031
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
