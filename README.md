# AuroraDotNet

A completely new, independent Minecraft Java Edition server engine built from scratch in C# / .NET.

## Overview
AuroraDotNet is a multi-threaded, cross-platform Minecraft server implementation focusing on:
1. Complete independence from Bukkit, Paper, Spigot, Fabric, Forge, etc.
2. Concurrent world simulation using a region-based ownership model.
3. Decoupling of the Minecraft network protocol from the game engine.
4. Minimal locks and lock-free execution where practical.

## Current State
This project is in its absolute infancy (Phase 1: Foundation).
Currently, the foundational types are being established.

## Repository Structure
- `src/`: Core implementation modules (Core, Networking, World, Server, etc.)
- `tests/`: Automated unit and integration tests.
- `benchmarks/`: BenchmarkDotNet performance test suites.
- `docs/`: Architecture documentation and ADRs.
- `tools/`: Utility scripts and development tooling.

## Building
```bash
dotnet build
dotnet test
```
