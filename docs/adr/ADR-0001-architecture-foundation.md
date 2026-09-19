# ADR-0001: Foundational Architecture & Concurrency

## Status
Accepted

## Context
Traditional Minecraft servers (e.g., vanilla, Spigot, early Bukkit) operate predominantly on a single-threaded game loop. This creates severe performance bottlenecks as player counts, entity counts, and redstone activity scale. Attempting to bolt multi-threading onto a single-threaded codebase typically leads to race conditions, excessive locking, and maintenance nightmares.

## Problem
We need an architectural foundation that supports scaling across modern multi-core processors without sacrificing deterministic world simulation or introducing heavy lock contention.

## Constraints
- A Minecraft "tick" must remain the logical boundary of time.
- The engine must be deterministic and testable where practical.
- The state of the world must remain coherent (no tearing or race conditions).

## Proposed Design
We will adopt a **Region-Based Ownership Model** coupled with a **Task-Based Scheduler**.
1. **Tick != Thread:** The game simulation tick is divided into discrete phases (Input, Simulation, Synchronization, Output).
2. **Region Ownership:** The world is split into regions. During the parallel simulation phase, a worker thread claims exclusive ownership over a region.
3. **No Locks:** Since a worker has exclusive ownership of a region, it can mutate that region's state without acquiring locks.
4. **Message Passing:** Interactions crossing region boundaries (e.g., an entity moving from Region A to Region B) generate messages or deferred commands that are resolved during the synchronization phase.

## Alternatives
- **Actor Model (e.g., Akka.NET):** Adds significant overhead and complexity for fine-grained spatial simulation like Minecraft.
- **ECS (Entity Component System):** Excellent for entities, but doesn't intrinsically solve chunk/block concurrency without spatial partitioning. (We may still use ECS *within* regions).
- **Fine-grained Locking:** Mutexes on individual chunks. High likelihood of deadlocks and massive context-switching overhead.

## Decision
We select the Region-Based Ownership Model with a phase-based tick. The internal structure will avoid global locks and rely on a custom task scheduler.

## Consequences
- **Positive:** Massive horizontal scalability across CPU cores. Lower latency spikes.
- **Negative:** Increased complexity in handling cross-region interactions. Developer overhead in ensuring data doesn't "leak" across region boundaries during parallel phases.
