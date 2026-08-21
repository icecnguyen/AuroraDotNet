using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Network;

#pragma warning disable CA1303 // Do not pass literals as localized parameters
namespace Aurora.Bootstrap;

sealed class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting AuroraDotNet (26.2 Protocol Support)...");

        using var connectionManager = new ConnectionManager();
        var endpoint = new IPEndPoint(IPAddress.Any, 25565);
        using var tcpServer = new TcpServer(endpoint, connectionManager);

        tcpServer.Start();
        Console.WriteLine($"Server listening on {endpoint}");

        // Keep running until canceled
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Shutting down...");
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
        }
        
        Console.WriteLine("Server stopped.");
    }
}
#pragma warning restore CA1303
