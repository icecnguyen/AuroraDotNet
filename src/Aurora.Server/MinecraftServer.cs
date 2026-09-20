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
    public Aurora.Core.Configuration.ServerConfiguration Configuration { get; }

    public MinecraftServer(Aurora.Core.Configuration.ServerConfiguration? configuration = null)
    {
        Configuration = configuration ?? Aurora.Core.Configuration.ServerConfiguration.Load();

        // Setup subsystems
        TickManager = new TickManager();
        PlayerManager = new PlayerManager();
        WorldManager = new WorldManager(Configuration.ResolvedSeed, Configuration.LevelName);
        CommandManager = new CommandManager();
        ConnectionManager = new ConnectionManager();
        
        // Setup threading and networking
        _workerPool = new WorkerPool(Configuration.WorkerCount);
        IPAddress bindIp = string.IsNullOrWhiteSpace(Configuration.ServerIp)
            ? IPAddress.Any
            : (IPAddress.TryParse(Configuration.ServerIp, out var ip) ? ip : IPAddress.Any);
        _tcpServer = new TcpServer(new IPEndPoint(bindIp, Configuration.ServerPort), ConnectionManager, WorldManager, Configuration);
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
