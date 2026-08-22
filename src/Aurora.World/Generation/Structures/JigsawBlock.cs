using System;

namespace Aurora.World.Generation.Structures;

public readonly record struct JigsawBlock(
    int X,
    int Y,
    int Z,
    string Name,        // e.g. "minecraft:street"
    string Target,      // e.g. "minecraft:street"
    string Pool,        // The pool to draw from when connecting
    string JointType    // "rollable" or "aligned"
);
