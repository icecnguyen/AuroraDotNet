using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora.Network;

public sealed class TcpServer : IDisposable
{
    private readonly Socket _listener;
    private readonly CancellationTokenSource _cts;
    private readonly ConnectionManager _connectionManager;
    private readonly Aurora.World.WorldManager _worldManager;
    private readonly Aurora.Core.Configuration.ServerConfiguration _serverConfig;

    public TcpServer(IPEndPoint endPoint, ConnectionManager connectionManager, Aurora.World.WorldManager worldManager, Aurora.Core.Configuration.ServerConfiguration? serverConfig = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(worldManager);
        
        _connectionManager = connectionManager;
        _worldManager = worldManager;
        _serverConfig = serverConfig ?? new Aurora.Core.Configuration.ServerConfiguration();
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(endPoint);
        _cts = new CancellationTokenSource();
    }

    public void Start()
    {
        _listener.Listen(100);
        _ = AcceptLoopAsync();
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var clientSocket = await _listener.AcceptAsync(_cts.Token).ConfigureAwait(false);
                clientSocket.NoDelay = true;
                
                // Construct the connection wrapping the pipeline
                var connection = new MinecraftConnection(clientSocket, _connectionManager, _worldManager, _serverConfig);
                
                // Add to manager
                _connectionManager.AddConnection(connection);
                
                // Start the receive and send loops
                connection.StartProcessing();
                
                Console.WriteLine($"[Network] Accepted connection {connection.Id} from {connection.RemoteEndPoint}");
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[Network] Socket error: {ex.Message}");
        }
#pragma warning disable CA1031 // Suppress for global catch on Accept loop
        catch (Exception ex)
        {
            Console.WriteLine($"[Network] Unexpected error: {ex.Message}");
        }
#pragma warning restore CA1031
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Dispose();
        _cts.Dispose();
    }
}
