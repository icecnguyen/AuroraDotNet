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
