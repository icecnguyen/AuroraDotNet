# AuroraDotNet

AuroraDotNet is a high-performance, experimental Minecraft server implementation written entirely in C# (.NET 9.0). It aims to support the Minecraft 1.21.4 (Protocol 768) specification, leveraging modern .NET features like `System.IO.Pipelines`, `ReadOnlySequence`, and zero-allocation parsing for network efficiency.

## Features
- **Protocol Support:** Currently targets Minecraft 1.21.4.
- **High Performance I/O:** Built on top of `System.IO.Pipelines` for high throughput and low memory footprint.
- **Strict Configuration State Flow:** Fully handles the modern Minecraft `Configuration` network state (Feature Flags, Known Packs, Registry Data).
- **Extensible Architecture:** Clear separation of concerns between `Network`, `Protocol`, `Core`, and `World` modules.

## Getting Started

1. Ensure you have the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed.
2. Build the project:
   ```bash
   dotnet build -c Release
   ```
3. Run the server:
   ```bash
   dotnet run --project src/Aurora.Bootstrap -c Release
   ```

## Next Steps / Roadmap
- **Vanilla Registry NBT Sync:** The server requires a proper `minecraft:dimension_type` and `minecraft:worldgen/biome` NBT dump from a vanilla 1.21.4 server to fully transition the client to the Play state.
- **World & Chunk Generation:** Implement standard Anvil chunk loading and terrain generation.
- **Entity & Physics Engine:** Server-side bounding boxes and movement validation.

## License
MIT License
