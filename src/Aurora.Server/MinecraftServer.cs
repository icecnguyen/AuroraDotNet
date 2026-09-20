using System;
using System.Net;
using System.Threading;
using Aurora.Threading;
using Aurora.Network;
using Aurora.World;

namespace Aurora.Server;

public sealed class MinecraftServer : IDisposable
{
    private readonly WorkerPool _workerPool;
    private readonly TcpServer _tcpServer;
    
    public ConnectionManager ConnectionManager { get; }
    public TickManager TickManager { get; }
    public PlayerManager PlayerManager { get; }
    public WorldManager WorldManager { get; }
    public CommandManager CommandManager { get; }

    public MinecraftServer()
    {
        // Setup subsystems
        TickManager = new TickManager();
        PlayerManager = new PlayerManager();
        WorldManager = new WorldManager();
        CommandManager = new CommandManager();
        ConnectionManager = new ConnectionManager();
        
        // Setup threading and networking
        _workerPool = new WorkerPool(4);
        _tcpServer = new TcpServer(new IPEndPoint(IPAddress.Any, 25565), ConnectionManager, WorldManager);
    }

    public void Start()
    {
        _workerPool.Start();
        _tcpServer.Start();
        
        // Register server loop on the worker pool
        _workerPool.EnqueueTask(TickLoop);
    }

    private void TickLoop()
    {
        // 1. Advance the global server tick
        TickManager.AdvanceTick();
        WorldManager.Tick();
        ConnectionManager.Tick();
        
        // 2. Schedule next tick immediately with delay simulation
        Thread.Sleep(TickManager.MillisecondsPerTick);
        _workerPool.EnqueueTask(TickLoop);
    }
    
    public void Stop()
    {
        _tcpServer.Dispose();
        ConnectionManager.Dispose();
        _workerPool.Dispose();
    }

    public void Dispose()
    {
        Stop();
    }
}
