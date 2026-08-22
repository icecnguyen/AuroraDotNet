namespace Aurora.World.Generation.Density;

/// <summary>
/// A router that holds the main density functions for climate and terrain shaping.
/// Corresponds to Minecraft 1.18+ NoiseRouter.
/// </summary>
public sealed class NoiseRouter
{
    public IDensityFunction Temperature { get; }
    public IDensityFunction Humidity { get; }
    public IDensityFunction Continentalness { get; }
    public IDensityFunction Erosion { get; }
    public IDensityFunction Depth { get; }
    public IDensityFunction Weirdness { get; }
    
    public IDensityFunction FinalDensity { get; }
    
    // For biomes and surface builders
    public IDensityFunction InitialDensityWithoutJaggedness { get; }
    public IDensityFunction FluidLevelFloodedness { get; }

    public NoiseRouter(
        IDensityFunction temperature,
        IDensityFunction humidity,
        IDensityFunction continentalness,
        IDensityFunction erosion,
        IDensityFunction depth,
        IDensityFunction weirdness,
        IDensityFunction finalDensity,
        IDensityFunction initialDensityWithoutJaggedness,
        IDensityFunction fluidLevelFloodedness)
    {
        Temperature = temperature;
        Humidity = humidity;
        Continentalness = continentalness;
        Erosion = erosion;
        Depth = depth;
        Weirdness = weirdness;
        FinalDensity = finalDensity;
        InitialDensityWithoutJaggedness = initialDensityWithoutJaggedness;
        FluidLevelFloodedness = fluidLevelFloodedness;
    }
}
