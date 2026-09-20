using System;
using System.Globalization;
using System.IO;
using Aurora.Core.Configuration;
using Xunit;

namespace Aurora.Core.Tests;

public sealed class ServerPropertiesAndSeedTests : IDisposable
{
    private readonly string _tempFile;

    public ServerPropertiesAndSeedTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"server_test_{Guid.NewGuid():N}.properties");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public void JavaStringHashCalculatesCorrectValues()
    {
        // "minecraft" hash in Java:
        // 'm'=109, 'i'=105, 'n'=110, 'e'=101, 'c'=99, 'r'=114, 'a'=97, 'f'=102, 't'=116
        int hash = ServerConfiguration.JavaStringHash("minecraft");
        
        // Manual verification matching Java s[0]*31^(n-1) + ...
        int expected = 0;
        foreach (char c in "minecraft")
        {
            expected = unchecked(31 * expected + c);
        }

        Assert.Equal(expected, hash);
    }

    [Fact]
    public void ResolveSeedNumericSeedParsesCorrectly()
    {
        long seed = ServerConfiguration.ResolveSeed("123456789", out string resolvedStr);
        Assert.Equal(123456789L, seed);
        Assert.Equal("123456789", resolvedStr);

        long negativeSeed = ServerConfiguration.ResolveSeed("-987654321", out string negStr);
        Assert.Equal(-987654321L, negativeSeed);
        Assert.Equal("-987654321", negStr);
    }

    [Fact]
    public void ResolveSeedStringSeedUsesJavaStringHash()
    {
        long seed = ServerConfiguration.ResolveSeed("custom_seed_text", out string resolvedStr);
        long expected = ServerConfiguration.JavaStringHash("custom_seed_text");

        Assert.Equal(expected, seed);
        Assert.Equal("custom_seed_text", resolvedStr);
    }

    [Fact]
    public void ResolveSeedEmptyOrWhitespaceGeneratesRandom()
    {
        long seed1 = ServerConfiguration.ResolveSeed("", out string resolved1);
        long seed2 = ServerConfiguration.ResolveSeed("   ", out string resolved2);

        Assert.NotEqual(0L, seed1);
        Assert.NotEqual(0L, seed2);
        Assert.NotEmpty(resolved1);
        Assert.NotEmpty(resolved2);
    }

    [Fact]
    public void HashSeedReturnsDeterministicOutput()
    {
        long hash1 = ServerConfiguration.HashSeed(123456789L);
        long hash2 = ServerConfiguration.HashSeed(123456789L);
        long hashOther = ServerConfiguration.HashSeed(987654321L);

        Assert.Equal(hash1, hash2);
        Assert.NotEqual(hash1, hashOther);
    }

    [Fact]
    public void LoadNonExistentFileCreatesDefaultWithGeneratedSeed()
    {
        var config = ServerConfiguration.Load(_tempFile);

        Assert.True(File.Exists(_tempFile));
        Assert.Equal(25565, config.ServerPort);
        Assert.Equal("world", config.LevelName);
        Assert.Equal("survival", config.GameMode);
        Assert.NotEqual(0L, config.ResolvedSeed);
        Assert.NotEmpty(config.LevelSeed);
        Assert.NotEqual(0L, config.HashedSeed);

        // Reload and verify persistence
        var reloaded = ServerConfiguration.Load(_tempFile);
        Assert.Equal(config.ResolvedSeed, reloaded.ResolvedSeed);
        Assert.Equal(config.LevelSeed, reloaded.LevelSeed);
        Assert.Equal(config.HashedSeed, reloaded.HashedSeed);
    }

    [Fact]
    public void LoadCustomPropertiesFileParsesAllValuesCorrectly()
    {
        string content = """
            # Minecraft test properties
            server-port=25570
            level-name=custom_survival_world
            level-seed=42424242
            gamemode=creative
            difficulty=hard
            max-players=50
            view-distance=12
            simulation-distance=10
            motd=My Awesome Server
            pvp=false
            online-mode=true
            """;

        File.WriteAllText(_tempFile, content);

        var config = ServerConfiguration.Load(_tempFile);

        Assert.Equal(25570, config.ServerPort);
        Assert.Equal("custom_survival_world", config.LevelName);
        Assert.Equal("42424242", config.LevelSeed);
        Assert.Equal(42424242L, config.ResolvedSeed);
        Assert.Equal("creative", config.GameMode);
        Assert.Equal("hard", config.Difficulty);
        Assert.Equal(50, config.MaxPlayers);
        Assert.Equal(12, config.ViewDistance);
        Assert.Equal(10, config.SimulationDistance);
        Assert.Equal("My Awesome Server", config.Motd);
        Assert.False(config.Pvp);
        Assert.True(config.OnlineMode);
    }

    [Fact]
    public void SaveRoundTripPreservesValues()
    {
        var config = new ServerConfiguration
        {
            ServerPort = 19132,
            LevelName = "test_world",
            LevelSeed = "seed_roundtrip",
            ResolvedSeed = ServerConfiguration.JavaStringHash("seed_roundtrip"),
            GameMode = "adventure",
            MaxPlayers = 64,
            ViewDistance = 6,
            SimulationDistance = 6,
            Motd = "Aurora Test"
        };
        config.HashedSeed = ServerConfiguration.HashSeed(config.ResolvedSeed);

        config.Save(_tempFile);
        var loaded = ServerConfiguration.Load(_tempFile);

        Assert.Equal(19132, loaded.ServerPort);
        Assert.Equal("test_world", loaded.LevelName);
        Assert.Equal("seed_roundtrip", loaded.LevelSeed);
        Assert.Equal(config.ResolvedSeed, loaded.ResolvedSeed);
        Assert.Equal("adventure", loaded.GameMode);
        Assert.Equal(64, loaded.MaxPlayers);
        Assert.Equal(6, loaded.ViewDistance);
        Assert.Equal("Aurora Test", loaded.Motd);
    }
}
