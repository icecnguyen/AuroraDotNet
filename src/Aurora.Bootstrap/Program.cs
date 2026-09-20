using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Network;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1303 // Do not pass literals as localized parameters
namespace Aurora.Bootstrap;

sealed class Program
{
    public static EventConsoleWriter? Logger;

    static async Task Main(string[] args)
    {
        // Hook Console
        Logger = new EventConsoleWriter(Console.Out);
        Console.SetOut(Logger);

        Console.WriteLine("Starting AuroraDotNet (1.21.4 Protocol Support)...");

        var config = Aurora.Core.Configuration.ServerConfiguration.Load("server.properties");
        Console.WriteLine($"[Config] Level: '{config.LevelName}' | Seed: {config.ResolvedSeed} (raw: '{config.LevelSeed}') | Port: {config.ServerPort} | Mode: {config.GameMode}");

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddCors();
        var app = builder.Build();

        app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        app.UseDefaultFiles(); // Serve index.html automatically
        app.UseStaticFiles(); // will serve wwwroot/index.html

        using var connectionManager = new ConnectionManager();
        using var worldManager = new Aurora.World.WorldManager(config.ResolvedSeed, config.LevelName);
        IPAddress bindIp = string.IsNullOrWhiteSpace(config.ServerIp)
            ? IPAddress.Any
            : (IPAddress.TryParse(config.ServerIp, out var ip) ? ip : IPAddress.Any);
        var endpoint = new IPEndPoint(bindIp, config.ServerPort);
        using var tcpServer = new TcpServer(endpoint, connectionManager, worldManager, config);

#pragma warning disable CA1031
#pragma warning disable CA2007

        var process = System.Diagnostics.Process.GetCurrentProcess();
        var startTime = process.StartTime;

        app.MapGet("/api/status", () => {
            var uptime = DateTime.Now - startTime;
            return new { 
                Players = System.Linq.Enumerable.Count(connectionManager.Players),
                Status = "Online",
                Version = "1.21.4",
                Protocol = 768,
                MemoryMB = process.WorkingSet64 / 1024 / 1024,
                Uptime = $"{(int)uptime.TotalHours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}"
            };
        });

        app.MapGet("/api/players", () => {
            var players = System.Linq.Enumerable.Select(connectionManager.Players, p => new {
                Id = p.Id,
                Username = p.Username,
                Ping = p.Ping,
                Uptime = $"{(int)(DateTimeOffset.UtcNow - p.ConnectedAt).TotalMinutes}m {(DateTimeOffset.UtcNow - p.ConnectedAt).Seconds}s"
            });
            return Results.Ok(players);
        });

        app.MapPost("/api/players/{id}/kick", (Guid id) => {
            connectionManager.KickPlayer(id);
            return Results.Ok();
        });

        app.MapGet("/api/logs", async (HttpContext ctx) => {
            ctx.Response.Headers.Append("Content-Type", "text/event-stream");
            
            Action<string> logHandler = async (msg) => {
                try {
                    await ctx.Response.WriteAsync($"data: {msg}\n\n");
                    await ctx.Response.Body.FlushAsync();
                } catch { }
            };

            Logger.OnLog += logHandler;
            try {
                await Task.Delay(Timeout.Infinite, ctx.RequestAborted);
            } catch (TaskCanceledException) {
            } finally {
                Logger.OnLog -= logHandler;
            }
        });

        app.MapPost("/api/command", async (HttpContext ctx) => {
            var reader = new System.IO.StreamReader(ctx.Request.Body);
            var cmd = await reader.ReadToEndAsync();
            Console.WriteLine($"[Web Console] Command received: {cmd}");
            return Results.Ok();
        });

        _ = Task.Run(() => {
            tcpServer.Start();
            Console.WriteLine($"Minecraft Server listening on {endpoint}");
        });

        Console.WriteLine("Web Dashboard listening on http://0.0.0.0:5000");
        await app.RunAsync("http://0.0.0.0:5000");
    }
}
#pragma warning restore CA2007
#pragma warning restore CA1031
#pragma warning restore CA1303
