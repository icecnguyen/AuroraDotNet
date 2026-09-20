using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aurora.Core.Math;
using Aurora.World;
using Aurora.World.Entities;
using Aurora.World.Storage;

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
    
    // Using Pipe for receiving and Channel for thread-safe concurrent sending
    private readonly Pipe _receivePipe;
    private readonly Channel<byte[]> _sendChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Player? Player { get; private set; }
    public EndPoint? RemoteEndPoint { get; }
    
    public PipeReader Reader => _receivePipe.Reader;

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

    public int ViewDistance { get; set; } = 8;
    private readonly HashSet<ChunkPosition> _loadedChunks = new();
    private short _selectedSlot;
    private readonly int[] _hotbarItemIds = new int[9];
    private int _voidDamageTimer;

    private readonly ConnectionManager _connectionManager;
    private readonly Aurora.World.WorldManager _worldManager;
    private readonly Aurora.Core.Configuration.ServerConfiguration _serverConfig;

    private static void WriteVarIntToStream(System.IO.MemoryStream ms, int value)
    {
        uint uval = (uint)value;
        while ((uval & ~0x7Fu) != 0)
        {
            ms.WriteByte((byte)((uval & 0x7F) | 0x80));
            uval >>= 7;
        }
        ms.WriteByte((byte)uval);
    }

    private void SendCenterViewPosition(int chunkX, int chunkZ)
    {
        using var ms = new System.IO.MemoryStream();
        WriteVarIntToStream(ms, chunkX);
        WriteVarIntToStream(ms, chunkZ);
        SendPacket(new Aurora.Protocol.Play.RawPacket(0x58, ms.ToArray()));
    }

    private void SendViewDistance(int radius)
    {
        using var ms = new System.IO.MemoryStream();
        WriteVarIntToStream(ms, radius);
        SendPacket(new Aurora.Protocol.Play.RawPacket(0x59, ms.ToArray()));
    }

    private void SendChunk(int cx, int cz)
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

    private void SendImmediateSpawnChunks(int centerX, int centerZ)
    {
        SendPacket(new Aurora.Protocol.Play.ChunkBatchStartPacket());

        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                int cx = centerX + dx;
                int cz = centerZ + dz;
                _loadedChunks.Add(new ChunkPosition(cx, cz));
                SendChunk(cx, cz);
                count++;
            }
        }

        SendPacket(new Aurora.Protocol.Play.ChunkBatchFinishedPacket { BatchSize = count });
    }

    private void UpdatePlayerChunks(int centerChunkX, int centerChunkZ)
    {
        SendCenterViewPosition(centerChunkX, centerChunkZ);

        var neededChunks = new List<(int X, int Z, int DistSq)>();
        for (int cx = centerChunkX - ViewDistance; cx <= centerChunkX + ViewDistance; cx++)
        {
            for (int cz = centerChunkZ - ViewDistance; cz <= centerChunkZ + ViewDistance; cz++)
            {
                var pos = new ChunkPosition(cx, cz);
                if (!_loadedChunks.Contains(pos))
                {
                    int dx = cx - centerChunkX;
                    int dz = cz - centerChunkZ;
                    neededChunks.Add((cx, cz, dx * dx + dz * dz));
                }
            }
        }

        if (neededChunks.Count == 0) return;

        neededChunks.Sort((a, b) => a.DistSq.CompareTo(b.DistSq));

        // High-speed parallel pre-generation across CPU cores
        Parallel.ForEach(neededChunks, item =>
        {
            if (!_cts.IsCancellationRequested && CurrentState == 4)
            {
                _worldManager.GetOrGenerateChunk(item.X, item.Z);
            }
        });

        const int batchCapacity = 16;
        for (int i = 0; i < neededChunks.Count; i += batchCapacity)
        {
            if (_cts.IsCancellationRequested || CurrentState != 4) break;

            int batchCount = Math.Min(batchCapacity, neededChunks.Count - i);
            SendPacket(new Aurora.Protocol.Play.ChunkBatchStartPacket());
            for (int j = 0; j < batchCount; j++)
            {
                var item = neededChunks[i + j];
                _loadedChunks.Add(new ChunkPosition(item.X, item.Z));
                SendChunk(item.X, item.Z);
            }
            SendPacket(new Aurora.Protocol.Play.ChunkBatchFinishedPacket { BatchSize = batchCount });
        }
    }

    private void SpawnItemInWorld(System.Numerics.Vector3 position, ItemStack stack, System.Numerics.Vector3 velocity, int pickupDelay = 10)
    {
        var itemEntity = new Aurora.World.Entities.ItemEntity(position, stack, velocity, pickupDelay);
        _worldManager.AddItemEntity(itemEntity);

        var spawn = new Aurora.Protocol.Play.SpawnEntityPacket
        {
            EntityId = itemEntity.EntityId,
            EntityUUID = itemEntity.Uuid,
            Type = 68, // minecraft:item in 1.21.4 (Protocol 768)
            X = itemEntity.Position.X,
            Y = itemEntity.Position.Y,
            Z = itemEntity.Position.Z,
            Pitch = 0,
            Yaw = 0,
            HeadYaw = 0,
            Data = 0,
            VelocityX = (short)(velocity.X * 8000),
            VelocityY = (short)(velocity.Y * 8000),
            VelocityZ = (short)(velocity.Z * 8000)
        };
        var meta = new Aurora.Protocol.Play.SetItemEntityDataPacket
        {
            EntityId = itemEntity.EntityId,
            ItemId = stack.ItemId,
            ItemCount = stack.Count
        };

        _connectionManager.BroadcastPacket(spawn);
        _connectionManager.BroadcastPacket(meta);
    }

    private void UpdatePlayerMovement(double newX, double newY, double newZ, bool onGround)
    {
        double oldY = Y;
        X = newX;
        Y = newY;
        Z = newZ;

        if (Player != null)
        {
            Player.Position = new System.Numerics.Vector3((float)X, (float)Y, (float)Z);
            Player.OnGround = onGround;

            if (Player.GameMode == GameMode.Survival)
            {
                if (newY < oldY)
                {
                    Player.FallDistance += (float)(oldY - newY);
                }
                else if (newY > oldY)
                {
                    Player.FallDistance = 0.0f;
                }

                if (onGround)
                {
                    if (Player.FallDistance > 3.0f)
                    {
                        float damage = Player.FallDistance - 3.0f;
                        ApplyDamage(damage);
                    }
                    Player.FallDistance = 0.0f;
                }
            }
        }
    }

    public void ApplyDamage(float amount, bool isVoid = false)
    {
        if (Player == null || !Player.IsAlive || Player.GameMode != GameMode.Survival) return;
        if (!isVoid && Player.InvulnerabilityTicks > 0) return;

        Player.Damage(amount);
        if (!isVoid) Player.InvulnerabilityTicks = 10;

        _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.EntityEventPacket
        {
            EntityId = this.EntityId,
            EventId = 2 // Hurt animation (red flash)
        });

        SendHealthUpdate();

        if (Player.Health <= 0)
        {
            OnPlayerDied();
        }
    }

    private void OnPlayerDied()
    {
        if (Player == null) return;

        // Broadcast death animation
        _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.EntityEventPacket
        {
            EntityId = this.EntityId,
            EventId = 3 // Death animation
        });

        // Drop all inventory items into world
        for (int i = 0; i < Player.Inventory.Capacity; i++)
        {
            var stack = Player.Inventory.GetItem(i);
            if (!stack.IsEmpty)
            {
                Player.Inventory.SetItem(i, ItemStack.Empty);
                var dropPos = new System.Numerics.Vector3((float)X, (float)Y + 0.5f, (float)Z);
#pragma warning disable CA5394 // Random is used for game physics jitter
                var dropVel = new System.Numerics.Vector3(
                    (float)(Random.Shared.NextDouble() * 0.4 - 0.2),
                    0.25f,
                    (float)(Random.Shared.NextDouble() * 0.4 - 0.2));
#pragma warning restore CA5394
                SpawnItemInWorld(dropPos, stack, dropVel, pickupDelay: 40);
            }
        }

        // Resync empty slots to player
        for (short s = 0; s < 46; s++)
        {
            SendPacket(new Aurora.Protocol.Play.SetContainerSlotPacket
            {
                WindowId = 0,
                StateId = 0,
                Slot = s,
                ItemId = 0,
                ItemCount = 0
            });
        }
    }

    private void SendHealthUpdate()
    {
        if (Player == null) return;
        SendPacket(new Aurora.Protocol.Play.SetHealthPacket
        {
            Health = Player.Health,
            Food = Player.FoodLevel,
            FoodSaturation = Player.FoodSaturation
        });
    }

    public void Tick()
    {
        if (Player == null || !Player.IsAlive) return;

        if (Player.GameMode == GameMode.Survival)
        {
            if (Player.InvulnerabilityTicks > 0)
            {
                Player.InvulnerabilityTicks--;
            }

            // 1. Void Damage
            if (Y < -64.0)
            {
                if (Y < -128.0)
                {
                    ApplyDamage(Player.Health, isVoid: true);
                }
                else
                {
                    _voidDamageTimer++;
                    if (_voidDamageTimer >= 10) // Every 0.5s
                    {
                        _voidDamageTimer = 0;
                        ApplyDamage(4.0f, isVoid: true);
                    }
                }
            }
            else
            {
                _voidDamageTimer = 0;
            }

            // 2. Underwater / Drowning
            int headX = (int)Math.Floor(X);
            int headY = (int)Math.Floor(Y + 1.6);
            int headZ = (int)Math.Floor(Z);
            ushort headBlock = _worldManager.GetBlock(headX, headY, headZ);
            string blockName = BlockRegistry.GetBlockName(headBlock);
            bool isWater = headBlock == Block.Water || blockName.Contains("water", StringComparison.Ordinal);

            Player.BreathManager.Tick(Player, isWater, out bool tookDrowningDamage);
            if (tookDrowningDamage)
            {
                ApplyDamage(Aurora.World.Gameplay.BreathManager.DrowningDamage);
            }

            // 3. Hunger simulation & natural regen & starvation
            Player.HungerManager.Tick(Player, out bool hungerChanged);
            if (hungerChanged)
            {
                SendHealthUpdate();
                if (Player.Health <= 0)
                {
                    OnPlayerDied();
                }
            }
        }

        // 4. Check item pickups periodically every tick
        CheckItemPickups();
    }

    private void CheckItemPickups()
    {
        if (Player == null || Player.GameMode == GameMode.Spectator || !Player.IsAlive) return;

        var playerPos = new System.Numerics.Vector3((float)X, (float)Y, (float)Z);
        foreach (var item in _worldManager.ItemEntities)
        {
            if (item.IsDead || item.PickupDelay > 0) continue;

            float distSq = System.Numerics.Vector3.DistanceSquared(playerPos, item.Position);
            if (distSq <= 2.25f) // 1.5 blocks pickup radius
            {
                var stack = item.Item;
                int beforeCount = stack.Count;
                if (Player.TryPickupItem(ref stack, out int changedSlot))
                {
                    int pickedUpCount = beforeCount - stack.Count;
                    item.Item = stack;

                    // 1. Play pickup animation to all players
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.TakeItemEntityPacket
                    {
                        CollectedEntityId = item.EntityId,
                        CollectorEntityId = this.EntityId,
                        PickupCount = pickedUpCount
                    });

                    // 2. If entity is completely picked up, destroy entity
                    if (stack.IsEmpty)
                    {
                        item.IsDead = true;
                        _worldManager.RemoveItemEntity(item.EntityId);
                        _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.RemoveEntitiesPacket(item.EntityId));
                    }
                    else
                    {
                        _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetItemEntityDataPacket
                        {
                            EntityId = item.EntityId,
                            ItemId = stack.ItemId,
                            ItemCount = stack.Count
                        });
                    }

                    // 3. Update hotbar cache and sync equipment
                    for (int i = 0; i < 9; i++)
                    {
                        _hotbarItemIds[i] = Player.GetHotbarItem(i).ItemId;
                    }
                    var currentHeld = Player.GetHeldItem();
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                    {
                        EntityId = this.EntityId,
                        Slot = 0,
                        ItemId = currentHeld.ItemId,
                        ItemCount = currentHeld.Count
                    });

                    // 4. Synchronize changed container slot to player so item appears in UI!
                    if (changedSlot >= 0)
                    {
                        var updated = Player.Inventory.GetItem(changedSlot);
                        SendPacket(new Aurora.Protocol.Play.SetContainerSlotPacket
                        {
                            WindowId = 0,
                            StateId = 0,
                            Slot = (short)changedSlot,
                            ItemId = updated.ItemId,
                            ItemCount = updated.Count
                        });
                    }
                }
            }
        }
    }

    public MinecraftConnection(Socket socket, ConnectionManager connectionManager, Aurora.World.WorldManager worldManager, Aurora.Core.Configuration.ServerConfiguration? serverConfig = null)
    {
        ArgumentNullException.ThrowIfNull(socket);
        _socket = socket;
        _connectionManager = connectionManager;
        _worldManager = worldManager;
        _serverConfig = serverConfig ?? new Aurora.Core.Configuration.ServerConfiguration();
        RemoteEndPoint = _socket.RemoteEndPoint;

        _receivePipe = new Pipe();
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
                if (await _sendChannel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    while (_sendChannel.Reader.TryRead(out var packetBytes))
                    {
                        int offset = 0;
                        while (offset < packetBytes.Length)
                        {
                            int sent = await _socket.SendAsync(packetBytes.AsMemory(offset), SocketFlags.None, _cts.Token).ConfigureAwait(false);
                            if (sent == 0) break;
                            offset += sent;
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
        finally
        {
            Disconnect();
        }
    }

    private async Task ProcessPacketsAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var result = await _receivePipe.Reader.ReadAsync(_cts.Token).ConfigureAwait(false);
                var buffer = result.Buffer;

                while (true)
                {
                    if (buffer.IsEmpty)
                        break;

                    // Try to read packet length
                    var sequenceReader = new SequenceReader<byte>(buffer);
                    if (!Aurora.Protocol.VarInt.TryRead(ref sequenceReader, out int length, out int lengthBytes))
                    {
                        break; // Need more data from pipe
                    }

                    if (buffer.Length < lengthBytes + length)
                    {
                        break; // Need more data for full packet payload
                    }

                    // We have a full packet
                    var packetSlice = buffer.Slice(lengthBytes, length);
                    buffer = buffer.Slice(lengthBytes + length);

                    try
                    {
                        HandlePacket(packetSlice);
                    }
#pragma warning disable CA1031
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Network] Packet processing error: {ex.Message}");
                        Disconnect();
                        return;
                    }
#pragma warning restore CA1031
                }

                _receivePipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Console.WriteLine($"[Network] ProcessPackets exception: {ex.Message}");
        }
#pragma warning restore CA1031
        finally
        {
            Disconnect();
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
                
                int onlineCount = System.Linq.Enumerable.Count(_connectionManager.Players);
                var protocolStr = ClientProtocolVersion > 0 ? ClientProtocolVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) : "768";
                var res = new Aurora.Protocol.Status.StatusResponsePacket
                {
                    JsonResponse = "{\"version\":{\"name\":\"Aurora 1.21.4\",\"protocol\":" + protocolStr + "},\"players\":{\"max\":" + _serverConfig.MaxPlayers.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"online\":" + onlineCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + "},\"description\":{\"text\":\"" + _serverConfig.Motd + "\"}}"
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
                Id = ls.Uuid != Guid.Empty ? ls.Uuid : CreateOfflineUuid(Username);
#pragma warning disable CA1303
                Console.WriteLine($"[Network] Player '{Username}' ({Id}) logging in...");
#pragma warning restore CA1303
                
                var success = new Aurora.Protocol.Login.LoginSuccessPacket
                {
                    Uuid = this.Id,
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
                
                // 1. Load player persistence or create fresh player data
                Player = PlayerDataStorage.Load(this.Id, _serverConfig.LevelName, new Aurora.Core.Ids.EntityId(this.EntityId), this.Username);
                if (Player != null)
                {
                    this.X = Player.Position.X;
                    this.Y = Player.Position.Y;
                    this.Z = Player.Position.Z;
                    this.Yaw = Player.Yaw;
                    this.Pitch = Player.Pitch;
                    this._selectedSlot = (short)Player.SelectedSlot;
                    for (int i = 0; i < 9; i++)
                    {
                        _hotbarItemIds[i] = Player.GetHotbarItem(i).ItemId;
                    }
                }
                else
                {
                    var spawn = _worldManager.FindSpawnPosition();
                    this.X = spawn.X;
                    this.Y = spawn.Y;
                    this.Z = spawn.Z;
                    var defaultMode = _serverConfig.GameMode.Equals("creative", StringComparison.OrdinalIgnoreCase)
                        ? GameMode.Creative
                        : (_serverConfig.GameMode.Equals("adventure", StringComparison.OrdinalIgnoreCase)
                            ? GameMode.Adventure
                            : (_serverConfig.GameMode.Equals("spectator", StringComparison.OrdinalIgnoreCase)
                                ? GameMode.Spectator
                                : GameMode.Survival));
                    Player = new Player(new Aurora.Core.Ids.EntityId(this.EntityId), this.Id, this.Username, defaultMode);
                    Player.Position = new System.Numerics.Vector3((float)this.X, (float)this.Y, (float)this.Z);
                    PlayerDataStorage.Save(Player, _serverConfig.LevelName);
                }

                // 2. Send Join Game with configured View Distance & Player GameMode
                var joinGame = new Aurora.Protocol.Play.JoinGamePacket 
                { 
                    EntityId = this.EntityId,
                    ViewDistance = _serverConfig.ViewDistance,
                    SimulationDistance = _serverConfig.SimulationDistance,
                    MaxPlayers = _serverConfig.MaxPlayers,
                    HashedSeed = _worldManager.HashedSeed,
                    GameMode = (byte)Player.GameMode
                };
                SendPacket(joinGame);
                
                // 3. Send Player Info Update (0x40) for local player
                var myInfo = new Aurora.Protocol.Play.PlayerInfoUpdatePacket
                {
                    Actions = 0x1D, // add_player(1) | gamemode(4) | listed(8) | latency(16)
                    Entries = new[]
                    {
                        new Aurora.Protocol.Play.PlayerInfoEntry
                        {
                            UUID = this.Id,
                            Name = this.Username,
                            GameMode = (int)Player.GameMode,
                            Listed = true,
                            Ping = 0,
                            HasDisplayName = false
                        }
                    }
                };
                SendPacket(myInfo);
                _connectionManager.BroadcastPacket(myInfo, except: this.Id);

                // 4. Send Player Abilities (0x3A) matching GameMode
                SendPlayerAbilities();

                // 4b. Send Declare Commands (0x11) for Brigadier syntax & autocomplete
                SendPacket(new Aurora.Protocol.Play.DeclareCommandsPacket());

                // 5. Send Set Health (0x62)
                SendPacket(new Aurora.Protocol.Play.SetHealthPacket
                {
                    Health = Player.Health,
                    Food = Player.FoodLevel,
                    FoodSaturation = Player.FoodSaturation
                });

                // 6. Send View Distance (0x59)
                SendViewDistance(this.ViewDistance);

                // 7. Send Center View Position (0x58)
                int spawnChunkX = (int)Math.Floor(this.X) >> 4;
                int spawnChunkZ = (int)Math.Floor(this.Z) >> 4;
                SendCenterViewPosition(spawnChunkX, spawnChunkZ);

                // Send immediate 3x3 spawn chunks with chunk batching to dismiss loading screen instantly!
                SendImmediateSpawnChunks(spawnChunkX, spawnChunkZ);

                // 8. Send Player Position (0x42) IMMEDIATELY to dismiss "Joining world..." screen!
                var playerPos = new Aurora.Protocol.Play.PlayerPositionPacket
                {
                    TeleportId = 1,
                    X = this.X,
                    Y = this.Y,
                    Z = this.Z
                };
                SendPacket(playerPos);

                // Sync existing item entities to this player
                foreach (var item in _worldManager.ItemEntities)
                {
                    if (item.IsDead) continue;
                    SendPacket(new Aurora.Protocol.Play.SpawnEntityPacket
                    {
                        EntityId = item.EntityId,
                        EntityUUID = item.Uuid,
                        Type = 68, // minecraft:item in 1.21.4 (Protocol 768)
                        X = item.Position.X,
                        Y = item.Position.Y,
                        Z = item.Position.Z,
                        Data = 0
                    });
                    SendPacket(new Aurora.Protocol.Play.SetItemEntityDataPacket
                    {
                        EntityId = item.EntityId,
                        ItemId = item.Item.ItemId,
                        ItemCount = item.Item.Count
                    });
                }
                
                // 9. Tell this player about other online players
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
                                GameMode = other.Player != null ? (int)other.Player.GameMode : 1,
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

                    // Send other player's held equipment to this player
                    if (other.Player != null)
                    {
                        var otherHeld = other.Player.GetHeldItem();
                        if (!otherHeld.IsEmpty)
                        {
                            SendPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                            {
                                EntityId = other.EntityId,
                                Slot = 0,
                                ItemId = otherHeld.ItemId,
                                ItemCount = otherHeld.Count
                            });
                        }
                    }
                }

                // Announce this player entity to others
                var mySpawn = new Aurora.Protocol.Play.SpawnEntityPacket
                {
                    EntityId = this.EntityId,
                    EntityUUID = this.Id,
                    Type = 147,
                    X = this.X, Y = this.Y, Z = this.Z,
                    Pitch = this.Pitch, Yaw = this.Yaw, HeadYaw = this.Yaw
                };
                _connectionManager.BroadcastPacket(mySpawn, except: this.Id);

                // Broadcast this player's held item to other players
                var myHeld = Player.GetHeldItem();
                if (!myHeld.IsEmpty)
                {
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                    {
                        EntityId = this.EntityId,
                        Slot = 0,
                        ItemId = myHeld.ItemId,
                        ItemCount = myHeld.Count
                    }, except: this.Id);
                }

                // 10. Stream chunks around spawn asynchronously in background
                _ = System.Threading.Tasks.Task.Run(() => UpdatePlayerChunks(spawnChunkX, spawnChunkZ));

                // 11. Start Keep Alive Task
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
                
                int oldChunkX = (int)Math.Floor(X) >> 4;
                int oldChunkZ = (int)Math.Floor(Z) >> 4;

                UpdatePlayerMovement(pos.X, pos.Y, pos.Z, pos.OnGround);

                int newChunkX = (int)Math.Floor(X) >> 4;
                int newChunkZ = (int)Math.Floor(Z) >> 4;

                if (newChunkX != oldChunkX || newChunkZ != oldChunkZ)
                {
                    UpdatePlayerChunks(newChunkX, newChunkZ);
                }

                CheckItemPickups();
                
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
                
                int oldChunkX = (int)Math.Floor(X) >> 4;
                int oldChunkZ = (int)Math.Floor(Z) >> 4;

                UpdatePlayerMovement(posRot.X, posRot.Y, posRot.Z, posRot.OnGround);
                Yaw = posRot.Yaw; Pitch = posRot.Pitch;
                if (Player != null)
                {
                    Player.Yaw = Yaw;
                    Player.Pitch = Pitch;
                }

                int newChunkX = (int)Math.Floor(X) >> 4;
                int newChunkZ = (int)Math.Floor(Z) >> 4;

                if (newChunkX != oldChunkX || newChunkZ != oldChunkZ)
                {
                    UpdatePlayerChunks(newChunkX, newChunkZ);
                }

                CheckItemPickups();
                
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
                if (Player != null)
                {
                    Player.Yaw = Yaw;
                    Player.Pitch = Pitch;
                    Player.OnGround = rot.OnGround;
                }
                
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
            else if (packetId == 0x27 || packetId == 0x24) // Serverbound Player Action (Block Dig)
            {
                int status = reader.ReadVarInt();
                var position = reader.ReadPosition();
                byte face = reader.ReadByte();
                int sequence = reader.ReadVarInt();

                long encodedPos = Aurora.Protocol.Play.BlockUpdatePacket.EncodePosition(position.X, position.Y, position.Z);

                if (status == 0) // Started digging
                {
                    if (Player?.GameMode == GameMode.Creative)
                    {
                        // Instant break in Creative
                        _worldManager.SetBlock(position.X, position.Y, position.Z, Block.Air);
                        _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.BlockUpdatePacket
                        {
                            Location = encodedPos,
                            BlockStateId = Block.Air
                        });
                        _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.BlockDestroyStagePacket
                        {
                            EntityId = this.EntityId,
                            Location = encodedPos,
                            DestroyStage = -1
                        });
                    }
                    else
                    {
                        // Show stage 0 crack in Survival
                        _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.BlockDestroyStagePacket
                        {
                            EntityId = this.EntityId,
                            Location = encodedPos,
                            DestroyStage = 0
                        });
                    }
                }
                else if (status == 1) // Cancelled digging
                {
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.BlockDestroyStagePacket
                    {
                        EntityId = this.EntityId,
                        Location = encodedPos,
                        DestroyStage = -1
                    });
                }
                else if (status == 2) // Finished digging
                {
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.BlockDestroyStagePacket
                    {
                        EntityId = this.EntityId,
                        Location = encodedPos,
                        DestroyStage = -1
                    });

                    ushort oldBlock = _worldManager.GetBlock(position.X, position.Y, position.Z);
                    _worldManager.SetBlock(position.X, position.Y, position.Z, Block.Air);

                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.BlockUpdatePacket
                    {
                        Location = encodedPos,
                        BlockStateId = Block.Air
                    });

                    if (Player?.GameMode == GameMode.Survival)
                    {
                        int heldId = Player.GetHeldItem().ItemId;
                        if (Aurora.World.Gameplay.BlockHardnessRegistry.CanHarvest(oldBlock, heldId))
                        {
                            var drop = Aurora.World.BlockDropRegistry.GetDrop(oldBlock);
                            if (!drop.IsEmpty)
                            {
                                var dropPos = new System.Numerics.Vector3(position.X + 0.5f, position.Y + 0.25f, position.Z + 0.5f);
#pragma warning disable CA5394 // Random is used for game physics jitter
                                var dropVel = new System.Numerics.Vector3((float)(Random.Shared.NextDouble() * 0.2 - 0.1), 0.2f, (float)(Random.Shared.NextDouble() * 0.2 - 0.1));
#pragma warning restore CA5394
                                SpawnItemInWorld(dropPos, drop, dropVel, pickupDelay: 10);
                            }
                        }
                        Aurora.World.Gameplay.HungerManager.AddExhaustion(Player, 0.005f);
                    }
                }
                else if (status == 3 || status == 4) // 3 = Drop entire stack (Ctrl+Q), 4 = Drop 1 item (Q)
                {
                    if (Player != null)
                    {
                        var dropped = Player.DropHeldItem(dropEntireStack: status == 3);
                        if (!dropped.IsEmpty)
                        {
                            float pitchRad = Pitch * (MathF.PI / 180f);
                            float yawRad = -Yaw * (MathF.PI / 180f);
                            float vx = -MathF.Sin(yawRad) * MathF.Cos(pitchRad) * 0.35f;
                            float vy = -MathF.Sin(pitchRad) * 0.35f + 0.1f;
                            float vz = MathF.Cos(yawRad) * MathF.Cos(pitchRad) * 0.35f;
                            var dropPos = new System.Numerics.Vector3((float)X, (float)Y + 1.32f, (float)Z);
                            var dropVel = new System.Numerics.Vector3(vx, vy, vz);
                            SpawnItemInWorld(dropPos, dropped, dropVel, pickupDelay: 40);

                            _hotbarItemIds[_selectedSlot] = Player.GetHotbarItem(_selectedSlot).ItemId;
                            var currentHeld = Player.GetHeldItem();
                            _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                            {
                                EntityId = this.EntityId,
                                Slot = 0,
                                ItemId = currentHeld.ItemId,
                                ItemCount = currentHeld.Count
                            });
                        }
                    }
                }

                if (sequence > 0)
                {
                    SendPacket(new Aurora.Protocol.Play.AcknowledgeBlockChangePacket { SequenceId = sequence });
                }
            }
            else if (packetId == 0x33) // Held Item Slot (0-8)
            {
                var held = new Aurora.Protocol.Play.HeldItemSlotPacket();
                held.Read(ref reader);
                if (held.SlotId >= 0 && held.SlotId < 9)
                {
                    _selectedSlot = held.SlotId;
                    if (Player != null)
                    {
                        Player.SelectedSlot = held.SlotId;
                        var currentHeld = Player.GetHeldItem();
                        _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                        {
                            EntityId = this.EntityId,
                            Slot = 0,
                            ItemId = currentHeld.ItemId,
                            ItemCount = currentHeld.Count
                        }, except: this.Id);
                    }
                }
            }
            else if (packetId == 0x36) // Set Creative Mode Slot
            {
                var creative = new Aurora.Protocol.Play.SetCreativeModeSlotPacket();
                creative.Read(ref reader);
                if (creative.Slot >= 36 && creative.Slot <= 44)
                {
                    int hotbarSlot = creative.Slot - 36;
                    _hotbarItemIds[hotbarSlot] = creative.ItemId;
                    if (Player != null)
                    {
                        Player.SetHotbarItem(hotbarSlot, new ItemStack(creative.ItemId, (byte)Math.Clamp(creative.ItemCount, 1, 64)));
                        if (hotbarSlot == _selectedSlot)
                        {
                            var currentHeld = Player.GetHeldItem();
                            _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                            {
                                EntityId = this.EntityId,
                                Slot = 0,
                                ItemId = currentHeld.ItemId,
                                ItemCount = currentHeld.Count
                            }, except: this.Id);
                        }
                    }
                }
                else if (creative.Slot >= 0 && creative.Slot < 9)
                {
                    int hotbarSlot = creative.Slot;
                    _hotbarItemIds[hotbarSlot] = creative.ItemId;
                    if (Player != null)
                    {
                        Player.SetHotbarItem(hotbarSlot, new ItemStack(creative.ItemId, (byte)Math.Clamp(creative.ItemCount, 1, 64)));
                        if (hotbarSlot == _selectedSlot)
                        {
                            var currentHeld = Player.GetHeldItem();
                            _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                            {
                                EntityId = this.EntityId,
                                Slot = 0,
                                ItemId = currentHeld.ItemId,
                                ItemCount = currentHeld.Count
                            }, except: this.Id);
                        }
                    }
                }
                else if (Player != null && creative.Slot >= 0 && creative.Slot < Player.Inventory.Capacity)
                {
                    Player.Inventory.SetItem(creative.Slot, new ItemStack(creative.ItemId, (byte)Math.Clamp(creative.ItemCount, 1, 64)));
                }
            }
            else if (packetId == 0x3A) // Swing Arm
            {
                var swing = new Aurora.Protocol.Play.SwingArmServerboundPacket();
                swing.Read(ref reader);
                byte anim = (byte)(swing.Hand == 1 ? 3 : 0); // 0 = main hand, 3 = offhand
                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.AnimatePacket
                {
                    EntityId = this.EntityId,
                    Animation = anim
                }, except: this.Id);
            }
            else if (packetId == 0x28) // Player Command (Sneak / Sprint)
            {
                var pCmd = new Aurora.Protocol.Play.PlayerCommandServerboundPacket();
                pCmd.Read(ref reader);
                if (pCmd.ActionId == 0) // Start sneaking
                {
                    if (Player != null) Player.IsSneaking = true;
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEntityDataPacket
                    {
                        EntityId = this.EntityId,
                        IsSneaking = true,
                        IsSprinting = Player?.IsSprinting ?? false
                    }, except: this.Id);
                }
                else if (pCmd.ActionId == 1) // Stop sneaking
                {
                    if (Player != null) Player.IsSneaking = false;
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEntityDataPacket
                    {
                        EntityId = this.EntityId,
                        IsSneaking = false,
                        IsSprinting = Player?.IsSprinting ?? false
                    }, except: this.Id);
                }
                else if (pCmd.ActionId == 3) // Start sprinting
                {
                    if (Player != null) Player.IsSprinting = true;
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEntityDataPacket
                    {
                        EntityId = this.EntityId,
                        IsSneaking = Player?.IsSneaking ?? false,
                        IsSprinting = true
                    }, except: this.Id);
                }
                else if (pCmd.ActionId == 4) // Stop sprinting
                {
                    if (Player != null) Player.IsSprinting = false;
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEntityDataPacket
                    {
                        EntityId = this.EntityId,
                        IsSneaking = Player?.IsSneaking ?? false,
                        IsSprinting = false
                    }, except: this.Id);
                }
            }
            else if (packetId == 0x26) // Player Abilities
            {
                var abilities = new Aurora.Protocol.Play.PlayerAbilitiesServerboundPacket();
                abilities.Read(ref reader);
                if (Player != null)
                {
                    Player.Abilities.Flying = (abilities.Flags & 0x02) != 0;
                }
            }
            else if (packetId == 0x08) // Chunk Batch Received (0x08 in 1.21.4)
            {
                var batch = new Aurora.Protocol.Play.ChunkBatchReceivedPacket();
                batch.Read(ref reader);
            }
            else if (packetId == 0x0A) // Client Command (Respawn)
            {
                var cCmd = new Aurora.Protocol.Play.ClientCommandServerboundPacket();
                cCmd.Read(ref reader);
                if (cCmd.ActionId == 0) // Respawn
                {
                    RespawnPlayer();
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

                // Resolve held block from active hotbar slot
                int heldItemId = (_selectedSlot >= 0 && _selectedSlot < 9) ? _hotbarItemIds[_selectedSlot] : 0;
                ushort blockToPlace = BlockRegistry.GetBlockStateFromItem(heldItemId);
                if (blockToPlace == Block.Air && Player?.GameMode == GameMode.Creative)
                {
                    blockToPlace = Block.Cobblestone;
                }

                if (blockToPlace != Block.Air)
                {
                    // Update RAM
                    _worldManager.SetBlock(x, y, z, blockToPlace);

                    long newLocation = Aurora.Protocol.Play.BlockUpdatePacket.EncodePosition(x, y, z);
                    
                    var update = new Aurora.Protocol.Play.BlockUpdatePacket
                    {
                        Location = newLocation,
                        BlockStateId = blockToPlace
                    };
                    
                    _connectionManager.BroadcastPacket(update);

                    if (Player != null && Player.GameMode == GameMode.Survival)
                    {
                        var heldStack = Player.GetHeldItem();
                        if (!heldStack.IsEmpty && heldStack.Count > 0)
                        {
                            var newStack = heldStack.Count > 1 
                                ? new ItemStack(heldStack.ItemId, (byte)(heldStack.Count - 1)) 
                                : ItemStack.Empty;
                            Player.Inventory.SetItem(36 + _selectedSlot, newStack);
                            _hotbarItemIds[_selectedSlot] = newStack.ItemId;
                            SendPacket(new Aurora.Protocol.Play.SetContainerSlotPacket
                            {
                                WindowId = 0,
                                StateId = 0,
                                Slot = (short)(36 + _selectedSlot),
                                ItemId = newStack.ItemId,
                                ItemCount = newStack.Count
                            });

                            _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                            {
                                EntityId = this.EntityId,
                                Slot = 0,
                                ItemId = newStack.ItemId,
                                ItemCount = newStack.Count
                            });
                        }
                    }
                }

                if (place.Sequence > 0)
                {
                    SendPacket(new Aurora.Protocol.Play.AcknowledgeBlockChangePacket { SequenceId = place.Sequence });
                }
            }
            else if (packetId == 0x3D) // Use Item (Eat food / interact)
            {
                var use = new Aurora.Protocol.Play.UseItemPacket();
                use.Read(ref reader);

                if (Player != null)
                {
                    var held = Player.GetHeldItem();
                    if (!held.IsEmpty && Aurora.World.Gameplay.FoodRegistry.TryGetFood(held.ItemId, out var food))
                    {
                        if (Player.FoodLevel < 20 || food.CanAlwaysEat)
                        {
                            Aurora.World.Gameplay.HungerManager.Eat(Player, food.Nutrition, food.Saturation);

                            // Decrement held item in Survival
                            if (Player.GameMode == GameMode.Survival)
                            {
                                int newCount = held.Count - 1;
                                var newHeld = newCount > 0 ? new ItemStack(held.ItemId, (byte)newCount) : ItemStack.Empty;
                                Player.SetHeldItem(newHeld);
                                _hotbarItemIds[_selectedSlot] = newHeld.ItemId;

                                SendPacket(new Aurora.Protocol.Play.SetContainerSlotPacket
                                {
                                    WindowId = 0,
                                    StateId = 0,
                                    Slot = (short)(36 + _selectedSlot),
                                    ItemId = newHeld.ItemId,
                                    ItemCount = newHeld.Count
                                });

                                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                                {
                                    EntityId = this.EntityId,
                                    Slot = 0,
                                    ItemId = newHeld.ItemId,
                                    ItemCount = newHeld.Count
                                });
                            }

                            // Broadcast bite / use stop animation
                            _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.EntityEventPacket
                            {
                                EntityId = this.EntityId,
                                EventId = 9 // Item use stop / finish
                            });

                            SendHealthUpdate();
                        }
                    }
                }

                if (use.Sequence > 0)
                {
                    SendPacket(new Aurora.Protocol.Play.AcknowledgeBlockChangePacket { SequenceId = use.Sequence });
                }
            }
            else if (packetId == 0x10) // Click Container
            {
                var click = new Aurora.Protocol.Play.ClickContainerPacket();
                click.Read(ref reader);

                if (Player != null && click.WindowId == 0)
                {
                    // If output slot 0 was clicked and had an item, consume crafting inputs
                    if (click.Slot == 0)
                    {
                        var output = Player.Inventory.GetItem(0);
                        if (!output.IsEmpty)
                        {
                            Aurora.World.Gameplay.CraftingManager.ConsumeCraftingInputs(Player.Inventory);
                        }
                    }

                    // Apply changed slots
                    foreach (var (slotId, itemId, count) in click.ChangedSlots)
                    {
                        if (slotId >= 0 && slotId < Player.Inventory.Capacity)
                        {
                            Player.Inventory.SetItem(slotId, count > 0 ? new ItemStack(itemId, (byte)count) : ItemStack.Empty);
                        }
                    }

                    // Update 2x2 crafting result
                    Aurora.World.Gameplay.CraftingManager.UpdateCraftingResult(Player.Inventory);

                    // Resync crafting result (slot 0)
                    var result = Player.Inventory.GetItem(0);
                    SendPacket(new Aurora.Protocol.Play.SetContainerSlotPacket
                    {
                        WindowId = 0,
                        StateId = click.StateId,
                        Slot = 0,
                        ItemId = result.ItemId,
                        ItemCount = result.Count
                    });

                    // Update hotbar cache and sync equipment
                    for (int i = 0; i < 9; i++)
                    {
                        _hotbarItemIds[i] = Player.GetHotbarItem(i).ItemId;
                    }

                    var currentHeld = Player.GetHeldItem();
                    _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                    {
                        EntityId = this.EntityId,
                        Slot = 0,
                        ItemId = currentHeld.ItemId,
                        ItemCount = currentHeld.Count
                    });
                }
            }
            else if (packetId == 0x05 || packetId == 0x06) // Chat Command (0x05) or Chat Command Signed (0x06)
            {
                string rawCmd;
                if (packetId == 0x05)
                {
                    var cmd = new Aurora.Protocol.Play.ChatCommandServerboundPacket();
                    cmd.Read(ref reader);
                    rawCmd = cmd.Command;
                }
                else
                {
                    var cmdSigned = new Aurora.Protocol.Play.ChatCommandSignedServerboundPacket();
                    cmdSigned.Read(ref reader);
                    rawCmd = cmdSigned.Command;
                }

                ExecuteCommand(rawCmd);
            }
            else if (packetId == 0x0D) // Command Suggestions Request (0x0D)
            {
                var req = new Aurora.Protocol.Play.CommandSuggestionRequestPacket();
                req.Read(ref reader);
                HandleCommandSuggestions(req.TransactionId, req.Text);
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
        
        using var ms = new System.IO.MemoryStream();
        WriteVarIntToStream(ms, packetData.Length);
        ms.Write(packetData);
        
        byte[] fullBytes = ms.ToArray();
        _sendChannel.Writer.TryWrite(fullBytes);
    }

    public static Guid CreateOfflineUuid(string username)
    {
#pragma warning disable CA5351 // Minecraft protocol specifies MD5 for offline player UUIDs (RFC 4122 v3)
        byte[] hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
#pragma warning restore CA5351
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30); // UUID version 3
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // IETF variant
        return new Guid(hash);
    }

    public void SendPlayerAbilities()
    {
        if (Player == null) return;
        SendPacket(new Aurora.Protocol.Play.PlayerAbilitiesPacket
        {
            Flags = Player.Abilities.Flags,
            FlyingSpeed = Player.Abilities.FlySpeed,
            WalkingSpeed = Player.Abilities.WalkSpeed
        });
    }

    private void ExecuteCommand(string rawCmd)
    {
        if (string.IsNullOrWhiteSpace(rawCmd)) return;

        string cmd = rawCmd.TrimStart('/');
        var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string commandName = parts[0];
        string response;

        if (commandName.Equals("ping", StringComparison.OrdinalIgnoreCase))
        {
            response = $"Pong! Your ping is {Ping}ms.";
        }
        else if (commandName.Equals("pos", StringComparison.OrdinalIgnoreCase))
        {
            response = $"You are at X={X:F1}, Y={Y:F1}, Z={Z:F1}";
        }
        else if (commandName.Equals("gamemode", StringComparison.OrdinalIgnoreCase) || commandName.Equals("gm", StringComparison.OrdinalIgnoreCase))
        {
            HandleGameModeCommand(cmd);
            return;
        }
        else if (commandName.Equals("heal", StringComparison.OrdinalIgnoreCase))
        {
            if (Player != null)
            {
                Player.Heal(20.0f);
                Player.FoodLevel = 20;
                Player.FoodSaturation = 5.0f;
                SendPacket(new Aurora.Protocol.Play.SetHealthPacket
                {
                    Health = Player.Health,
                    Food = Player.FoodLevel,
                    FoodSaturation = Player.FoodSaturation
                });
                response = "§aYou have been fully healed!";
            }
            else
            {
                response = "§cPlayer not found.";
            }
        }
        else if (commandName.Equals("kill", StringComparison.OrdinalIgnoreCase))
        {
            if (Player != null)
            {
                Player.Damage(Player.Health);
                SendPacket(new Aurora.Protocol.Play.SetHealthPacket
                {
                    Health = 0.0f,
                    Food = Player.FoodLevel,
                    FoodSaturation = Player.FoodSaturation
                });
                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.AnimatePacket
                {
                    EntityId = this.EntityId,
                    Animation = 1 // Hurt
                });
                response = "§cYou were killed.";
            }
            else
            {
                response = "§cPlayer not found.";
            }
        }
        else if (commandName.Equals("time", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length >= 3 && parts[1].Equals("set", StringComparison.OrdinalIgnoreCase))
            {
                string timeArg = parts[2].ToUpperInvariant();
                long targetTime = timeArg switch
                {
                    "DAY" => 1000,
                    "NOON" => 6000,
                    "NIGHT" => 13000,
                    "MIDNIGHT" => 18000,
                    _ => long.TryParse(timeArg, System.Globalization.CultureInfo.InvariantCulture, out long parsed) ? parsed : -1
                };

                if (targetTime >= 0)
                {
                    var timePacket = new Aurora.Protocol.Play.UpdateTimePacket
                    {
                        WorldAge = 0,
                        TimeOfDay = targetTime,
                        IsIncreasing = true
                    };
                    _connectionManager.BroadcastPacket(timePacket);
                    response = $"§aSet the time to {targetTime}";
                }
                else
                {
                    response = $"§cUnknown time '{parts[2]}'";
                }
            }
            else
            {
                response = "§cUsage: /time set <day|night|noon|midnight|number>";
            }
        }
        else if (commandName.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            if (Player != null)
            {
                Player.Inventory.Clear();
                for (int i = 0; i < 9; i++)
                {
                    _hotbarItemIds[i] = 0;
                }
                _connectionManager.BroadcastPacket(new Aurora.Protocol.Play.SetEquipmentPacket
                {
                    EntityId = this.EntityId,
                    Slot = 0,
                    ItemId = 0,
                    ItemCount = 0
                }, except: this.Id);
                response = $"§aCleared the inventory of {Username}";
            }
            else
            {
                response = "§cPlayer not found.";
            }
        }
        else if (commandName.Equals("seed", StringComparison.OrdinalIgnoreCase))
        {
            response = $"§eSeed: [§a{_worldManager.Seed}§e]";
        }
        else if (commandName.Equals("help", StringComparison.OrdinalIgnoreCase) || commandName.Equals("?", StringComparison.OrdinalIgnoreCase))
        {
            response = "§6Available commands: §f/gamemode, /gm, /heal, /kill, /ping, /pos, /time, /clear, /seed, /help";
        }
        else
        {
            response = $"§cUnknown command: /{cmd}";
        }

        SendPacket(new Aurora.Protocol.Play.SystemChatMessagePacket { Content = response });
    }

    private void HandleCommandSuggestions(int transactionId, string text)
    {
        var matches = new List<string>();
        string normalized = text.TrimStart('/');

        if (normalized.StartsWith("gamemode ", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("gm ", StringComparison.OrdinalIgnoreCase))
        {
            string[] modes = ["survival", "creative", "adventure", "spectator"];
            string[] split = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string prefix = split.Length > 1 ? split[1] : string.Empty;

            foreach (var m in modes)
            {
                if (m.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(m);
                }
            }
        }
        else if (normalized.StartsWith("time set ", StringComparison.OrdinalIgnoreCase))
        {
            string[] times = ["day", "night", "noon", "midnight"];
            string[] split = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string prefix = split.Length > 2 ? split[2] : string.Empty;

            foreach (var t in times)
            {
                if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(t);
                }
            }
        }
        else if (normalized.StartsWith("time ", StringComparison.OrdinalIgnoreCase))
        {
            matches.Add("set");
        }
        else
        {
            string[] rootCommands = ["gamemode", "gm", "heal", "kill", "ping", "pos", "time", "clear", "seed", "help"];
            string prefix = normalized.Trim();
            foreach (var cmd in rootCommands)
            {
                if (cmd.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add("/" + cmd);
                }
            }
        }

        int lastSpace = text.LastIndexOf(' ');
        int start = lastSpace >= 0 ? lastSpace + 1 : 0;
        int length = text.Length - start;

        SendPacket(new Aurora.Protocol.Play.CommandSuggestionsResponsePacket
        {
            TransactionId = transactionId,
            Start = start,
            Length = length,
            Matches = matches
        });
    }

    private void HandleGameModeCommand(string command)
    {
        if (Player == null) return;

        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            SendPacket(new Aurora.Protocol.Play.SystemChatMessagePacket { Content = "§cUsage: /gamemode <survival|creative|adventure|spectator>" });
            return;
        }

        string modeStr = parts[1].ToUpperInvariant();
        GameMode newMode;
        if (modeStr is "CREATIVE" or "C" or "1") newMode = GameMode.Creative;
        else if (modeStr is "SURVIVAL" or "S" or "0") newMode = GameMode.Survival;
        else if (modeStr is "ADVENTURE" or "A" or "2") newMode = GameMode.Adventure;
        else if (modeStr is "SPECTATOR" or "SP" or "3") newMode = GameMode.Spectator;
        else
        {
            SendPacket(new Aurora.Protocol.Play.SystemChatMessagePacket { Content = $"§cUnknown game mode '{parts[1]}'" });
            return;
        }

        Player.SetGameMode(newMode);

        // 1. Send Game State Change (0x23)
        SendPacket(new Aurora.Protocol.Play.GameStateChangePacket
        {
            Reason = 3,
            Value = (float)newMode
        });

        // 2. Send Player Abilities (0x3A)
        SendPlayerAbilities();

        // 3. Update Tab List GameMode (0x40 with action 4)
        var updateGamemode = new Aurora.Protocol.Play.PlayerInfoUpdatePacket
        {
            Actions = 0x04, // update_game_mode
            Entries = new[]
            {
                new Aurora.Protocol.Play.PlayerInfoEntry
                {
                    UUID = this.Id,
                    Name = this.Username,
                    GameMode = (int)newMode,
                    Listed = true,
                    Ping = (int)this.Ping,
                    HasDisplayName = false
                }
            }
        };
        SendPacket(updateGamemode);
        _connectionManager.BroadcastPacket(updateGamemode, except: this.Id);

        // 4. Feedback
        SendPacket(new Aurora.Protocol.Play.SystemChatMessagePacket { Content = $"§aSet game mode to {newMode} Mode" });
    }

    private void RespawnPlayer()
    {
        if (Player == null) return;

        Player.ResetForRespawn();

        // 1. Send Respawn Packet (0x4C)
        var respawn = new Aurora.Protocol.Play.RespawnPacket
        {
            Dimension = 0,
            DimensionName = "minecraft:overworld",
            HashedSeed = _worldManager.HashedSeed,
            GameMode = (byte)Player.GameMode,
            PreviousGameMode = 255,
            CopyMetadata = 1
        };
        SendPacket(respawn);

        // Client unloads all chunks upon respawn; clear tracker so spawn chunks are resent
        _loadedChunks.Clear();

        // 2. Send Abilities (0x3A)
        SendPlayerAbilities();

        // 3. Send Health (0x62)
        SendPacket(new Aurora.Protocol.Play.SetHealthPacket
        {
            Health = Player.Health,
            Food = Player.FoodLevel,
            FoodSaturation = Player.FoodSaturation
        });

        // 4. Find spawn position & send Center View Position (0x58)
        var spawn = _worldManager.FindSpawnPosition();
        this.X = spawn.X;
        this.Y = spawn.Y;
        this.Z = spawn.Z;
        Player.Position = new System.Numerics.Vector3((float)this.X, (float)this.Y, (float)this.Z);

        int spawnChunkX = (int)Math.Floor(this.X) >> 4;
        int spawnChunkZ = (int)Math.Floor(this.Z) >> 4;
        SendCenterViewPosition(spawnChunkX, spawnChunkZ);

        // 5. Send immediate 3x3 spawn chunks with chunk batching to dismiss loading screen instantly!
        SendImmediateSpawnChunks(spawnChunkX, spawnChunkZ);

        // 6. Send Player Position (0x42)
        var playerPos = new Aurora.Protocol.Play.PlayerPositionPacket
        {
            TeleportId = 2,
            X = this.X,
            Y = this.Y,
            Z = this.Z
        };
        SendPacket(playerPos);

        // 7. Stream outer chunks asynchronously in background
        _ = System.Threading.Tasks.Task.Run(() => UpdatePlayerChunks(spawnChunkX, spawnChunkZ));
    }

    public void SavePlayerData()
    {
        if (Player != null)
        {
            Player.Position = new System.Numerics.Vector3((float)X, (float)Y, (float)Z);
            Player.Yaw = Yaw;
            Player.Pitch = Pitch;
            Player.SelectedSlot = _selectedSlot;
            try
            {
                PlayerDataStorage.Save(Player, _serverConfig.LevelName);
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                Console.WriteLine($"[Network] Error saving player data for '{Username}': {ex.Message}");
            }
#pragma warning restore CA1031
        }
    }

    public void Disconnect()
    {
        if (!_cts.IsCancellationRequested)
        {
            SavePlayerData();
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
