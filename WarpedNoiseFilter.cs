
using Godot;

public class WarpedNoiseFilter : INoiseFilter {

    NoiseSettings settings;
    FastNoiseLite noise;

    public WarpedNoiseFilter(NoiseSettings settings)
    {
        this.settings = settings;
        noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = settings.frequency;
        noise.FractalOctaves = settings.octaves;
        noise.FractalGain = settings.roughness;
        noise.FractalLacunarity = settings.persistence;
        noise.DomainWarpEnabled = true;
        noise.DomainWarpFractalOctaves = settings.warpOctaves;
        noise.DomainWarpFrequency = settings.warpFrequency;
        noise.DomainWarpFractalGain = settings.warpRoughness;
        noise.DomainWarpFractalLacunarity = settings.warpPersistence;
        noise.Seed = Universe.Seed.GetHashCode();
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = (noise.GetNoise3Dv(point * settings.frequency + settings.center) + 1) * 0.5f;
        noiseValue = noiseValue - settings.minValue;
        return noiseValue * settings.strength;
    }
}