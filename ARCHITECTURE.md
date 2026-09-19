# AuroraDotNet Architecture

## Core Architectural Principles

These rules guide the development of the AuroraDotNet server engine.

### Rule 1 — Tick != Thread
A Minecraft tick is a logical simulation boundary, not a primary application loop running on a single thread. The architecture relies on an asynchronous scheduling model where a tick encompasses:
- Input processing
- Parallel simulation
- Synchronization
- Commit
- Network Output

### Rule 2 — Region Ownership
The world is partitioned into logical regions. At any given simulation phase, a region has a strictly defined owner (a specific worker thread). This prevents arbitrary cross-thread mutation. Cross-region mutations use controlled scheduling or message-passing command buffers.

### Rule 3 — Minimize Locks
We prefer message passing, queues, and immutable data over lock-based synchronization. We employ the simplest correct synchronization mechanism and benchmark it to avoid both locking contention and overly complex lock-free data structures.

### Rule 4 — No Blocking I/O in Simulation
World simulation never waits for disk, network, compression, databases, or external processes. All I/O operations are offloaded to asynchronous background pipelines (e.g., Save Queue -> I/O Worker -> Storage).

### Rule 5 — Protocol is Not the Game Engine
Network code strictly translates between Minecraft packets and Aurora's internal representations. It contains no world simulation logic.

### Rule 6 — Public API != Internal Implementation
The plugin API exposes interfaces and abstractions (`IWorld`, `IPlayer`, `IEntity`), insulating plugins from internal engine restructuring.

---

## Subsystem Layout

```
                  ┌─────────────────────────────────┐
                  │       Aurora.Bootstrap          │
                  │   Host, Web Dashboard & Config  │
                  └───────────────┬─────────────────┘
                                  │
                  ┌───────────────▼─────────────────┐
                  │        Aurora.Server            │
                  │  Player Lifecycle & Game Loop   │
                  └───────┬─────────────────┬───────┘
                          │                 │
       ┌──────────────────▼──────┐   ┌──────▼──────────────────┐
       │     Aurora.Network      │   │      Aurora.World       │
       │ TcpServer, Pipelines,   │   │ Chunks, Noise Gen,      │
       │ Connection Management   │   │ Anvil Storage, Registry │
       └──────────┬──────────────┘   └──────────────┬──────────┘
                  │                                 │
                  └───────────────┬─────────────────┘
                                  │
                  ┌───────────────▼─────────────────┐
                  │       Aurora.Protocol           │
                  │ 1.21.4 (768) Packets & NBT      │
                  └───────────────┬─────────────────┘
                                  │
                  ┌───────────────▼─────────────────┐
                  │   Aurora.Core & Threading       │
                  │ Primitives, Math, WorkerPool    │
                  └─────────────────────────────────┘
```

### 1. Networking & Chunk Batching Pipeline
- Built on `System.IO.Pipelines` with zero heap allocation per packet cycle.
- Compliant with **Minecraft 1.21.4 (Protocol 768)** chunk batching:
  - Immediate spawn area (3x3 chunks) transmitted synchronously wrapped in `ChunkBatchStart (0x0D)` and `ChunkBatchFinished (0x0C)` to release client loading screens in < 50ms.
  - Outer chunks pre-generated in parallel via multi-core SIMD noise pipelines and streamed asynchronously in 16-chunk batches.
  - Responsive tab-completion via Brigadier packet graph `0x11` and real-time suggestion responses `0x10`.

### 2. Terrain & Surface Rules Engine
- Multi-noise sampling (Temperature, Humidity, Continentalness, Erosion, Weirdness, Depth) inspired by vanilla and ported in part from [Pumpkin-MC](https://github.com/Pumpkin-MC/Pumpkin).
- 3D cellular caves, aquifers, and carver passes with smooth interpolation across 4x4x4 noise cells.
- Surface builders applying bedrock floors, deepslate transitions, stone layers, ocean sediments, beaches, and biome-specific grass/sand.

### 3. Persistence & Anvil Storage Engine
- Standard Minecraft Region format (`.mca`) parser with Deflate compression and 4KB sector alignment.
- Binary NBT reader/writer for player metadata, position, health, inventory, and game mode.

